#!/usr/bin/env bash
# =============================================================================
# verify.sh — Verificación estructural de la feature 002-scaffold-src-projects
# =============================================================================
#
# USO:
#   bash specs/002-scaffold-src-projects/_verification/verify.sh
#   (ejecutar siempre desde el repo-root)
#
# CHECKS:
#   C1  — Conteo de .csproj: deben existir exactamente 13.
#   C2  — Props comunes: <Nullable>enable</Nullable> e <ImplicitUsings>enable</ImplicitUsings>.
#   C3  — Ausencia de <PackageReference> y <ProjectReference>.
#   C4  — SDK correcto por proyecto (validación por archivo, no por contador).
#   C5  — TargetFramework(s) correcto por banda (anclado, no substring).
#   C6  — Cada carpeta src/TallerPro.*/ contiene exactamente CLAUDE.md + .csproj.
#   C7  — Hashes sha256 de los CLAUDE.md coinciden con claude-md-hashes.txt.
#   C8  — dotnet restore serial sobre los 12 proyectos no-Hybrid.
#   C9  — dotnet restore de Hybrid (tolerante: WARN si falta workload MAUI).
#   C10 — Sin cambios fuera de src/ (git status --porcelain filtrado).
#   C11 — Ausencia de props prohibidas (TreatWarningsAsErrors, LangVersion, AnalysisLevel).
#   C12 — EOL=LF en todos los .csproj (sin CRLF, sin BOM).
#
# EXIT CODES:
#   0 — Todos los checks pasaron (incluye SKIP y WARN sin fallos).
#   1 — Al menos un check falló (fallo funcional).
#   2 — Precondición de entorno fallida (abortado antes de los checks).
#
# NOTA B-01:
#   C8/C9 ejecutan 'dotnet restore' que contacta api.nuget.org para resolver
#   SDK/workloads; el endurecimiento del feed (NuGetAudit + lockfiles) vive en spec 001.
# =============================================================================

set -euo pipefail
IFS=$'\n\t'

# ---------------------------------------------------------------------------
# Precondiciones de entorno (P1–P4) — todas fallan con exit 2
# ---------------------------------------------------------------------------

# P1 — repo-root
[[ -f .specify/memory/constitution.md ]] || { echo "[FAIL] run from repo root"; exit 2; }

# P2 — git presente (N-01)
command -v git >/dev/null 2>&1 || { echo "[FAIL] git not in PATH"; exit 2; }

# P3 — hashing tool (H-03): sha256sum → openssl → fallo
HASH_BIN=""
if command -v sha256sum >/dev/null 2>&1; then
  HASH_BIN="sha256sum"
elif command -v openssl >/dev/null 2>&1; then
  HASH_BIN="openssl"
else
  echo "[FAIL] no sha256 tool available (install coreutils or openssl)"; exit 2
fi

# P4 — hashes.txt presente (H-09)
[[ -f specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt ]] || {
  echo "[FAIL] missing claude-md-hashes.txt — run T-01 first"; exit 2
}

# ---------------------------------------------------------------------------
# R-01: nullglob obligatorio antes de toda expansión de glob
# R-01: sin nullglob, un glob sin matches se expande al patrón literal (1 elemento erróneo);
# con nullglob, se expande a un array vacío. Requisito para régimen B y C.
shopt -s nullglob

# ---------------------------------------------------------------------------
# Contadores globales
# ---------------------------------------------------------------------------
passed=0
failed=0
skipped=0
warnings=0

# ---------------------------------------------------------------------------
# Helpers de emisión
# ---------------------------------------------------------------------------

emit_pass() {
  local id="$1"; shift
  echo "[PASS] ${id} — $*"
  (( passed++ )) || true
}

emit_fail() {
  local id="$1"; shift
  echo "[FAIL] ${id} — $*"
  (( failed++ )) || true
}

emit_skip() {
  local id="$1"; shift
  echo "[SKIP] ${id} — $*"
  (( skipped++ )) || true
}

emit_warn() {
  local id="$1"; shift
  echo "[WARN] ${id} — $*"
  (( warnings++ )) || true
}

# ---------------------------------------------------------------------------
# Wrapper sha256_of — contrato N-05: emite hex-64-lowercase exclusivamente
# ---------------------------------------------------------------------------
sha256_of() {
  if [[ "$HASH_BIN" == "sha256sum" ]]; then
    sha256sum "$1" | awk '{print $1}'
  else
    openssl dgst -sha256 -r "$1" | awk '{print $1}'
  fi
}

# ---------------------------------------------------------------------------
# C1 — Conteo de .csproj (siempre ejecuta)
# Deriva REGIME y existing_csprojs para uso de todos los checks dependientes.
# ---------------------------------------------------------------------------
check_C1() {
  # Expandir glob bajo nullglob para uso posterior y para el conteo
  existing_csprojs=( src/TallerPro.*/*.csproj )
  local count=${#existing_csprojs[@]}
  echo "bootstrap: ${count} .csproj detected" >&2

  if [[ "$count" == "13" ]]; then
    emit_pass C1 "13 .csproj found"
  else
    emit_fail C1 "expected 13 .csproj, found ${count}"
  fi

  # Derivar régimen
  case "$count" in
    0)  REGIME=c ;;
    13) REGIME=a ;;
    *)  REGIME=b ;;
  esac
}

# ---------------------------------------------------------------------------
# Helper should_run_check — contrato R-05
# Decide si un check dependiente del glob debe ejecutar en el régimen actual.
# ---------------------------------------------------------------------------
should_run_check() {
  local check_id="$1"   # C2..C12 (solo los dependientes del glob)
  case "$REGIME" in
    a|b) return 0 ;;    # ejecuta (lógica completa en A, subset presente en B)
    c)   return 1 ;;    # skip global
    *)   echo "[FAIL] internal: unknown regime $REGIME" >&2; exit 2 ;;
  esac
}

# ---------------------------------------------------------------------------
# Variables globales para régimen y glob — declaradas antes de C1
# ---------------------------------------------------------------------------
REGIME=""
existing_csprojs=()

# ---------------------------------------------------------------------------
# Mapa proyecto → SDK esperado (para C4)
# ---------------------------------------------------------------------------
expected_sdk_for() {
  local csproj="$1"
  local base
  base=$(basename "$csproj" .csproj)
  case "$base" in
    TallerPro.Components)
      echo "Microsoft.NET.Sdk.Razor" ;;
    TallerPro.Api|TallerPro.Web|TallerPro.Admin)
      echo "Microsoft.NET.Sdk.Web" ;;
    *)
      echo "Microsoft.NET.Sdk" ;;
  esac
}

# ---------------------------------------------------------------------------
# C2 — Props comunes: Nullable + ImplicitUsings
# ---------------------------------------------------------------------------
check_C2() {
  should_run_check C2 || { emit_skip C2 "no .csproj to inspect (count=0)"; return 0; }

  local ok=0
  local total=0
  for csproj in "${existing_csprojs[@]}"; do
    total=$(( total + 1 ))
    local nullable_count implicit_count
    nullable_count=$(grep -Ec '^[[:space:]]*<Nullable>enable</Nullable>[[:space:]]*$' "$csproj" || true)
    implicit_count=$(grep -Ec '^[[:space:]]*<ImplicitUsings>enable</ImplicitUsings>[[:space:]]*$' "$csproj" || true)
    if [[ "$nullable_count" == "1" && "$implicit_count" == "1" ]]; then
      ok=$(( ok + 1 ))
    else
      emit_fail C2 "missing Nullable/ImplicitUsings in ${csproj}"
    fi
  done

  if [[ "$ok" == "$total" ]]; then
    if [[ "$REGIME" == "a" ]]; then
      emit_pass C2 "props OK on 13"
    else
      emit_pass C2 "props OK on ${total} inspected"
    fi
  fi
}

# ---------------------------------------------------------------------------
# C3 — Ausencia de referencias prohibidas
# ---------------------------------------------------------------------------
check_C3() {
  should_run_check C3 || { emit_skip C3 "no .csproj to inspect (count=0)"; return 0; }

  local bad_files
  bad_files=$(grep -El '<(PackageReference|ProjectReference)\b' "${existing_csprojs[@]}" 2>/dev/null || true)

  if [[ -z "$bad_files" ]]; then
    if [[ "$REGIME" == "a" ]]; then
      emit_pass C3 "no forbidden references"
    else
      emit_pass C3 "no forbidden references on ${#existing_csprojs[@]} inspected"
    fi
  else
    while IFS= read -r archivo; do
      emit_fail C3 "forbidden reference in ${archivo}"
    done <<< "$bad_files"
  fi
}

# ---------------------------------------------------------------------------
# C4 — SDK correcto por proyecto (validación por archivo, O-01)
# ---------------------------------------------------------------------------
check_C4() {
  should_run_check C4 || { emit_skip C4 "no .csproj to inspect (count=0)"; return 0; }

  local ok=0
  local total=0
  local seen_sdks=()
  for csproj in "${existing_csprojs[@]}"; do
    total=$(( total + 1 ))
    local expected_sdk
    expected_sdk=$(expected_sdk_for "$csproj")

    # Escapar puntos para regex
    local escaped_sdk
    escaped_sdk=$(echo "$expected_sdk" | sed 's/\./\\./g')

    local sdk_count
    sdk_count=$(grep -Ec "^[[:space:]]*<Project[[:space:]]+Sdk=\"${escaped_sdk}\">[[:space:]]*\$" "$csproj" || true)

    if [[ "$sdk_count" == "1" ]]; then
      ok=$(( ok + 1 ))
      seen_sdks+=( "$expected_sdk" )
    else
      # Intentar extraer el SDK actual para el mensaje
      local actual_sdk
      actual_sdk=$(grep -Eo 'Sdk="[^"]*"' "$csproj" | head -1 | sed 's/Sdk="//;s/"//' || echo "unknown")
      emit_fail C4 "wrong SDK in ${csproj}: expected ${expected_sdk}, got ${actual_sdk}"
    fi
  done

  if [[ "$ok" == "$total" ]]; then
    if [[ "$REGIME" == "a" ]]; then
      emit_pass C4 "SDKs OK (9 Microsoft.NET.Sdk, 1 Razor, 3 Web)"
    else
      # Determinar si todos los SDKs son homogéneos
      local unique_sdks
      unique_sdks=$(printf '%s\n' "${seen_sdks[@]}" | sort -u)
      local unique_count
      unique_count=$(printf '%s\n' "${seen_sdks[@]}" | sort -u | wc -l | tr -d ' ')
      if [[ "$unique_count" == "1" ]]; then
        emit_pass C4 "SDKs OK on ${total} inspected (all ${unique_sdks})"
      else
        emit_pass C4 "SDKs OK on ${total} inspected"
      fi
    fi
  fi
}

# ---------------------------------------------------------------------------
# C5 — TargetFramework(s) por banda (anclado, no substring)
# ---------------------------------------------------------------------------
check_C5() {
  should_run_check C5 || { emit_skip C5 "no .csproj to inspect (count=0)"; return 0; }

  local ok=0
  local total=0
  local hybrid_skipped=false
  local analyzers_skipped=false

  for csproj in "${existing_csprojs[@]}"; do
    local base
    base=$(basename "$csproj" .csproj)

    case "$base" in
      TallerPro.Hybrid)
        total=$(( total + 1 ))
        local tf_count no_singular no_ios output_type use_maui single_project
        tf_count=$(grep -Ec '^[[:space:]]*<TargetFrameworks>net9\.0-android;net9\.0-windows10\.0\.19041\.0</TargetFrameworks>[[:space:]]*$' "$csproj" || true)
        no_singular=$(grep -Eq '<TargetFramework>' "$csproj" && echo "1" || echo "0")
        no_ios=$(grep -Eq 'net9\.0-(ios|maccatalyst|tizen)' "$csproj" && echo "1" || echo "0")
        output_type=$(grep -Ec '^[[:space:]]*<OutputType>Exe</OutputType>[[:space:]]*$' "$csproj" || true)
        use_maui=$(grep -Ec '^[[:space:]]*<UseMaui>true</UseMaui>[[:space:]]*$' "$csproj" || true)
        single_project=$(grep -Ec '^[[:space:]]*<SingleProject>true</SingleProject>[[:space:]]*$' "$csproj" || true)
        if [[ "$tf_count" == "1" && "$no_singular" == "0" && "$no_ios" == "0" && "$output_type" == "1" && "$use_maui" == "1" && "$single_project" == "1" ]]; then
          ok=$(( ok + 1 ))
        else
          emit_fail C5 "TargetFramework mismatch in ${csproj}"
        fi
        ;;
      TallerPro.Analyzers)
        total=$(( total + 1 ))
        local ns_count no_net9 roslyn_comp enforce_rules no_build_output
        ns_count=$(grep -Ec '^[[:space:]]*<TargetFramework>netstandard2\.0</TargetFramework>[[:space:]]*$' "$csproj" || true)
        no_net9=$(grep -Eq 'net9\.0' "$csproj" && echo "1" || echo "0")
        roslyn_comp=$(grep -Ec '^[[:space:]]*<IsRoslynComponent>true</IsRoslynComponent>[[:space:]]*$' "$csproj" || true)
        enforce_rules=$(grep -Ec '^[[:space:]]*<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>[[:space:]]*$' "$csproj" || true)
        no_build_output=$(grep -Ec '^[[:space:]]*<IncludeBuildOutput>false</IncludeBuildOutput>[[:space:]]*$' "$csproj" || true)
        if [[ "$ns_count" == "1" && "$no_net9" == "0" && "$roslyn_comp" == "1" && "$enforce_rules" == "1" && "$no_build_output" == "1" ]]; then
          ok=$(( ok + 1 ))
        else
          emit_fail C5 "TargetFramework mismatch in ${csproj}"
        fi
        ;;
      *)
        total=$(( total + 1 ))
        local tf_net9
        tf_net9=$(grep -Ec '^[[:space:]]*<TargetFramework>net9\.0</TargetFramework>[[:space:]]*$' "$csproj" || true)
        if [[ "$tf_net9" == "1" ]]; then
          ok=$(( ok + 1 ))
        else
          emit_fail C5 "TargetFramework mismatch in ${csproj}"
        fi
        ;;
    esac
  done

  # Detectar si Hybrid/Analyzers están ausentes en régimen B para el mensaje
  local hybrid_present=false analyzers_present=false
  for csproj in "${existing_csprojs[@]}"; do
    [[ "$csproj" == *"TallerPro.Hybrid"* ]] && hybrid_present=true
    [[ "$csproj" == *"TallerPro.Analyzers"* ]] && analyzers_present=true
  done

  if [[ "$ok" == "$total" ]]; then
    if [[ "$REGIME" == "a" ]]; then
      emit_pass C5 "TargetFramework OK on 13 (11 net9.0 + Hybrid multi-target + Analyzers netstandard2.0)"
    else
      local absent_note=""
      if [[ "$hybrid_present" == "false" && "$analyzers_present" == "false" ]]; then
        absent_note=" (net9.0; Hybrid/Analyzers absent, skipped in partial set)"
      elif [[ "$hybrid_present" == "false" ]]; then
        absent_note=" (Hybrid absent, skipped in partial set)"
      elif [[ "$analyzers_present" == "false" ]]; then
        absent_note=" (Analyzers absent, skipped in partial set)"
      fi
      emit_pass C5 "TargetFramework OK on ${total} inspected${absent_note}"
    fi
  fi
}

# ---------------------------------------------------------------------------
# C6 — Contenido de carpeta: exactamente CLAUDE.md + .csproj
# ---------------------------------------------------------------------------
check_C6() {
  should_run_check C6 || { emit_skip C6 "no .csproj to inspect (count=0)"; return 0; }

  local ok=0
  local total=0

  # R-04: iterar solo sobre dirs con .csproj presente (mismo subset que C2-C5)
  for csproj in "${existing_csprojs[@]}"; do
    local dir
    dir=$(dirname "$csproj")
    local proj_name
    proj_name=$(basename "$dir")
    total=$(( total + 1 ))

    # Obtener lista de archivos (sin . y ..), ignorar obj/ y bin/
    local actual_files
    actual_files=$(ls -A1 "$dir" | grep -v '^obj$' | grep -v '^bin$' | sort)
    local expected_files
    expected_files=$(printf '%s\n' "CLAUDE.md" "${proj_name}.csproj" | sort)

    if [[ "$actual_files" == "$expected_files" ]]; then
      ok=$(( ok + 1 ))
    else
      local extras
      extras=$(comm -23 <(echo "$actual_files") <(echo "$expected_files") | tr '\n' ' ')
      emit_fail C6 "unexpected entries in ${dir}: ${extras}"
    fi
  done

  if [[ "$ok" == "$total" ]]; then
    if [[ "$REGIME" == "a" ]]; then
      emit_pass C6 "contents OK on 13"
    else
      emit_pass C6 "contents OK on ${total} inspected"
    fi
  fi
}

# ---------------------------------------------------------------------------
# C7 — Hashes CLAUDE.md (siempre ejecuta; CA-08, N-05)
# ---------------------------------------------------------------------------
check_C7() {
  local hashes_file="specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt"
  local ok=0
  local total=0
  local any_fail=false

  while IFS= read -r line; do
    [[ -z "$line" ]] && continue
    local expected path
    expected=$(awk '{print $1}' <<< "$line")
    path=$(awk '{for(i=2;i<=NF;++i) printf "%s%s",$i,(i<NF?" ":"")}' <<< "$line")
    total=$(( total + 1 ))

    if [[ ! -f "$path" ]]; then
      emit_fail C7 "CLAUDE.md not found: ${path}"
      any_fail=true
      continue
    fi

    local actual
    actual=$(sha256_of "$path")

    if [[ "$expected" == "$actual" ]]; then
      ok=$(( ok + 1 ))
    else
      emit_fail C7 "hash mismatch for ${path}: expected ${expected}, got ${actual}"
      any_fail=true
    fi
  done < "$hashes_file"

  if [[ "$any_fail" == "false" && "$ok" == "$total" ]]; then
    emit_pass C7 "all ${total} CLAUDE.md hashes match"
  fi
}

# ---------------------------------------------------------------------------
# C8 — dotnet restore serial no-Hybrid (N-08)
# ---------------------------------------------------------------------------
check_C8() {
  should_run_check C8 || { emit_skip C8 "no .csproj to inspect (count=0)"; return 0; }

  # Ordenar lexicográficamente, excluir Hybrid
  local no_hybrid_csprojs=()
  local sorted_csprojs=()
  if [[ ${#existing_csprojs[@]} -gt 0 ]]; then
    # shellcheck disable=SC2207
    sorted_csprojs=( $(printf '%s\n' "${existing_csprojs[@]}" | sort) )
  fi
  for csproj in "${sorted_csprojs[@]+"${sorted_csprojs[@]}"}"; do
    if [[ "$csproj" != *"/TallerPro.Hybrid/"* ]]; then
      no_hybrid_csprojs+=( "$csproj" )
    fi
  done

  local ok=0
  local total=${#no_hybrid_csprojs[@]}
  for csproj in "${no_hybrid_csprojs[@]}"; do
    if dotnet restore "$csproj" --nologo --verbosity quiet >/dev/null 2>&1; then
      ok=$(( ok + 1 ))
    else
      emit_fail C8 "restore failed for ${csproj}"
    fi
  done

  if [[ "$ok" == "$total" && "$total" -gt 0 ]]; then
    if [[ "$REGIME" == "a" ]]; then
      emit_pass C8 "restore OK on 12 non-Hybrid"
    else
      emit_pass C8 "restore OK on ${total} inspected"
    fi
  elif [[ "$total" -eq 0 ]]; then
    emit_skip C8 "no non-Hybrid .csproj to restore"
  fi
}

# ---------------------------------------------------------------------------
# C9 — restore Hybrid tolerante (N-09 detección MAUI anclada)
# N-02: dos variantes de SKIP distintas según contexto
# ---------------------------------------------------------------------------
check_C9() {
  should_run_check C9 || { emit_skip C9 "no .csproj to inspect (count=0)"; return 0; }

  local hybrid_csproj="src/TallerPro.Hybrid/TallerPro.Hybrid.csproj"

  # Régimen B con Hybrid ausente
  if [[ ! -f "$hybrid_csproj" ]]; then
    emit_skip C9 "Hybrid not yet created"
    return 0
  fi

  # Régimen A o B con Hybrid presente: detectar workload MAUI (N-09, regex anclada)
  if dotnet workload list 2>/dev/null | grep -Eq '^[[:space:]]*maui\b'; then
    if dotnet restore "$hybrid_csproj" --nologo --verbosity quiet >/dev/null 2>&1; then
      emit_pass C9 "Hybrid restore ok"
    else
      emit_fail C9 "Hybrid restore failed"
    fi
  else
    emit_warn C9 "Hybrid restore skipped: MAUI workload missing"
  fi
}

# ---------------------------------------------------------------------------
# C10 — Sin cambios fuera de src/ (N-07, siempre ejecuta)
# ---------------------------------------------------------------------------
check_C10() {
  local out
  # Si no hay repositorio git, git status falla con exit!=0; capturar para no matar el script.
  # En ausencia de repo git no puede haber cambios no rastreados: PASS.
  out=$(git status --porcelain -- tests/ build/ '*.sln' .editorconfig Directory.Build.props Directory.Packages.props global.json .github/ 2>/dev/null) || out=""

  if [[ -z "$out" ]]; then
    emit_pass C10 "no changes outside src/"
  else
    emit_fail C10 "unexpected changes outside src/:"
    echo "$out"
  fi
}

# ---------------------------------------------------------------------------
# C11 — Props prohibidas (H-05, N-04)
# ---------------------------------------------------------------------------
check_C11() {
  should_run_check C11 || { emit_skip C11 "no .csproj to inspect (count=0)"; return 0; }

  local bad_files
  bad_files=$(grep -El "<(TreatWarningsAsErrors|LangVersion|AnalysisLevel)\b" "${existing_csprojs[@]}" 2>/dev/null || true)

  if [[ -z "$bad_files" ]]; then
    if [[ "$REGIME" == "a" ]]; then
      emit_pass C11 "no forbidden properties"
    else
      emit_pass C11 "no forbidden properties on ${#existing_csprojs[@]} inspected"
    fi
  else
    while IFS= read -r archivo; do
      emit_fail C11 "forbidden property in ${archivo}"
    done <<< "$bad_files"
  fi
}

# ---------------------------------------------------------------------------
# C12 — EOL=LF (H-07, N-04 flags explícitas)
# ---------------------------------------------------------------------------
check_C12() {
  should_run_check C12 || { emit_skip C12 "no .csproj to inspect (count=0)"; return 0; }

  # grep -UIl inside $() produces false positives on MSYS2/Windows when stdout
  # is a pipe. Use grep -UIq inside 'if' (no capture subshell) to avoid it.
  local crlf_files=()
  for f in "${existing_csprojs[@]}"; do
    if grep -UIq $'\r' "$f" 2>/dev/null; then
      crlf_files+=( "$f" )
    fi
  done

  if [[ ${#crlf_files[@]} -eq 0 ]]; then
    if [[ "$REGIME" == "a" ]]; then
      emit_pass C12 "all 13 use LF"
    else
      emit_pass C12 "all ${#existing_csprojs[@]} inspected use LF"
    fi
  else
    emit_fail C12 "CRLF detected in:"
    printf '%s\n' "${crlf_files[@]}"
  fi
}

# ===========================================================================
# MAIN — Orden determinista de emisión (R-03)
# ===========================================================================

# 1. C1 siempre primero (también deriva REGIME y existing_csprojs)
check_C1

# 2. Si régimen B: línea de contexto inmediatamente después de C1
if [[ "$REGIME" == "b" ]]; then
  echo "[INFO] partial inspection: count=${#existing_csprojs[@]}/13; inspecting present subset"
fi

# 3. C7 siempre ejecuta (después de C1/INFO, antes de C10)
check_C7

# 4. C10 siempre ejecuta
check_C10

# 5. C2 → C3 → C4 → C5 → C6 → C8 → C9 → C11 → C12 en orden numérico
check_C2
check_C3
check_C4
check_C5
check_C6
check_C8
check_C9
check_C11
check_C12

# 6. Resumen final
echo "${passed} passed, ${failed} failed, ${skipped} skipped, ${warnings} warnings"

# 7. Exit code
if (( failed > 0 )); then
  exit 1
else
  exit 0
fi
