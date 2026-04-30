#!/usr/bin/env bash
# =============================================================================
# test_verify.sh — Harness de regresión para verify.sh (T-02b)
# =============================================================================
#
# USO:
#   bash specs/002-scaffold-src-projects/_verification/test_verify.sh
#   (ejecutar siempre desde el repo-root)
#
# CASES:
#   A      — régimen C (count=0): fixture sin ningún .csproj
#   B      — régimen B (count=7): fixture con los 7 .csproj genéricos reales de T-04
#   A-full — régimen A (count=13): fixture con los 13 .csproj byte-exact del plan
#
# EXIT CODES:
#   0 — todos los cases pasaron
#   1 — al menos un case falló
#   2 — precondición de entorno fallida (abortado antes de los cases)
# =============================================================================

set -euo pipefail
IFS=$'\n\t'

# ---------------------------------------------------------------------------
# H1 — Guard de repo-root (idéntico al de verify.sh)
# ---------------------------------------------------------------------------
[[ -f .specify/memory/constitution.md ]] || { echo "[FAIL] run from repo root"; exit 2; }

# ---------------------------------------------------------------------------
# Trap EXIT defensivo (Sec-Bajo) — prefix-guard /tmp/* blinda contra TMP_ROOT corrupto
# ---------------------------------------------------------------------------
TMP_ROOT=""
trap 'if [[ -n "${TMP_ROOT:-}" && -d "$TMP_ROOT" && "$TMP_ROOT" == /tmp/* ]]; then rm -rf "$TMP_ROOT"; fi' EXIT

# ---------------------------------------------------------------------------
# H2 — Verificar que los 13 CLAUDE.md existen (Q-03)
# ---------------------------------------------------------------------------
shopt -s nullglob
claude_md_files=( src/TallerPro.*/CLAUDE.md )
[[ ${#claude_md_files[@]} -eq 13 ]] || {
  echo "[FAIL] harness: expected 13 src/TallerPro.*/CLAUDE.md, found ${#claude_md_files[@]} — run T-01 first"
  exit 2
}

# ---------------------------------------------------------------------------
# H3 — Binarios requeridos
# ---------------------------------------------------------------------------
command -v git >/dev/null 2>&1 || { echo "[FAIL] harness: git not in PATH"; exit 2; }
{ command -v sha256sum >/dev/null 2>&1 || command -v openssl >/dev/null 2>&1; } || {
  echo "[FAIL] harness: no sha256 tool available (install coreutils or openssl)"; exit 2
}
command -v dotnet >/dev/null 2>&1 || { echo "[FAIL] harness: dotnet not in PATH"; exit 2; }

# ---------------------------------------------------------------------------
# H4 — Detectar workload MAUI una sola vez en el top-level (N-04, Q-04)
# S-02: readonly previene mutación accidental entre cases
# ---------------------------------------------------------------------------
if dotnet workload list 2>/dev/null | grep -Eq '^[[:space:]]*maui\b'; then
  HAS_MAUI=1
else
  HAS_MAUI=0
fi
readonly HAS_MAUI   # S-02: prevenir mutación accidental entre cases

# ---------------------------------------------------------------------------
# REAL_REPO — ruta absoluta al repo donde se ejecuta el harness
# ---------------------------------------------------------------------------
REAL_REPO="$(pwd)"
readonly REAL_REPO

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

# build_fixture <case_id>
# Crea TMP_ROOT, construye el layout mínimo del repo y hace git init limpio.
# Los .csproj se añaden luego vía seed_csprojs (antes del commit git).
build_fixture() {
  local case_id="$1"

  # Limpiar fixture anterior si existe
  if [[ -n "${TMP_ROOT:-}" && -d "$TMP_ROOT" && "$TMP_ROOT" == /tmp/* ]]; then
    rm -rf "$TMP_ROOT"
  fi

  TMP_ROOT=$(mktemp -d)
  [[ "$TMP_ROOT" == /tmp/* ]] || { echo "[FAIL] harness: mktemp returned unexpected path: $TMP_ROOT"; exit 2; }

  # Layout mínimo: constitution.md (satisface P1 de verify.sh)
  mkdir -p "$TMP_ROOT/.specify/memory/"
  touch "$TMP_ROOT/.specify/memory/constitution.md"

  # Copiar los 13 CLAUDE.md reales al fixture
  for claude_md in "${claude_md_files[@]}"; do
    local rel_dir
    rel_dir=$(dirname "$claude_md")
    mkdir -p "$TMP_ROOT/$rel_dir"
    cp "$REAL_REPO/$claude_md" "$TMP_ROOT/$claude_md"
  done

  # Crear directorio _verification y copiar verify.sh
  mkdir -p "$TMP_ROOT/specs/002-scaffold-src-projects/_verification/"
  cp "$REAL_REPO/specs/002-scaffold-src-projects/_verification/verify.sh" \
     "$TMP_ROOT/specs/002-scaffold-src-projects/_verification/verify.sh"

  # Regenerar claude-md-hashes.txt dentro del fixture usando los CLAUDE.md copiados
  (
    cd "$TMP_ROOT"
    if command -v sha256sum >/dev/null 2>&1; then
      sha256sum src/TallerPro.*/CLAUDE.md \
        | awk '{gsub(/\*/,"",$2); printf "%s  %s\n", $1, $2}' \
        > specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt
    else
      # openssl fallback: emular formato de sha256sum (hex  path)
      for f in src/TallerPro.*/CLAUDE.md; do
        printf '%s  %s\n' "$(openssl dgst -sha256 -r "$f" | awk '{print $1}')" "$f"
      done > specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt
    fi
  )

  # seed_csprojs debe llamarse ANTES de git init para que el commit incluya los .csproj
  seed_csprojs "$case_id"

  # git init limpio (Q-02)
  # S-01 — sin subshell: 'exit 2' termina el script padre directamente, sin
  # depender de la propagación del exit code del subshell vía 'set -e'.
  # pushd/popd contienen el cambio de CWD sin crear proceso hijo.
  pushd "$TMP_ROOT" > /dev/null
  git init -q
  git config core.autocrlf false
  git config core.eol lf
  git -c user.email=harness@local -c user.name=harness add -A
  git -c user.email=harness@local -c user.name=harness commit -q -m "fixture baseline"
  # Asserts post-commit (Q-02): sin esto C10 podría emitir [FAIL] espurio
  [[ "$(git log --oneline | wc -l | tr -d ' ')" -ge 1 ]] || { echo "[FAIL] harness: fixture has no commit"; exit 2; }
  [[ -z "$(git status --porcelain)" ]] || { echo "[FAIL] harness: fixture dirty after commit"; exit 2; }
  popd > /dev/null
}

# seed_csprojs <case_id>
# Escribe los .csproj según el case dentro de $TMP_ROOT.
# Llamado desde build_fixture ANTES del git init.
seed_csprojs() {
  local case_id="$1"

  case "$case_id" in
    A)
      # Régimen C: sin ningún .csproj
      ;;
    B)
      # Régimen B: 7 .csproj genéricos byte-exact (Domain, Application, Infrastructure,
      # Shared, LocalDb, Observability, Security) — los que T-04 crea realmente.
      # Banda genérica canónica (plan §Componentes, byte-exact):
      local generic_content
      generic_content='<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>'
      for proj in Domain Application Infrastructure Shared LocalDb Observability Security; do
        local dir="$TMP_ROOT/src/TallerPro.${proj}"
        mkdir -p "$dir"
        printf '%s\n' "$generic_content" > "$dir/TallerPro.${proj}.csproj"
      done
      ;;
    A-full)
      # Régimen A: 13 .csproj byte-exact del plan §Componentes

      # Banda genérica (Domain, Application, Infrastructure, Shared, LocalDb, Observability, Security)
      local generic_content
      generic_content='<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>'
      for proj in Domain Application Infrastructure Shared LocalDb Observability Security; do
        local dir="$TMP_ROOT/src/TallerPro.${proj}"
        mkdir -p "$dir"
        printf '%s\n' "$generic_content" > "$dir/TallerPro.${proj}.csproj"
      done

      # Components (Razor Class Library)
      mkdir -p "$TMP_ROOT/src/TallerPro.Components"
      printf '%s\n' '<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>' > "$TMP_ROOT/src/TallerPro.Components/TallerPro.Components.csproj"

      # Web band: Api, Web, Admin
      local web_content
      web_content='<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>'
      for proj in Api Web Admin; do
        local dir="$TMP_ROOT/src/TallerPro.${proj}"
        mkdir -p "$dir"
        printf '%s\n' "$web_content" > "$dir/TallerPro.${proj}.csproj"
      done

      # Hybrid (MAUI, multi-target)
      mkdir -p "$TMP_ROOT/src/TallerPro.Hybrid"
      printf '%s\n' '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net9.0-android;net9.0-windows10.0.19041.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>' > "$TMP_ROOT/src/TallerPro.Hybrid/TallerPro.Hybrid.csproj"

      # Analyzers (Roslyn)
      mkdir -p "$TMP_ROOT/src/TallerPro.Analyzers"
      printf '%s\n' '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IsRoslynComponent>true</IsRoslynComponent>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>' > "$TMP_ROOT/src/TallerPro.Analyzers/TallerPro.Analyzers.csproj"
      ;;
    *)
      echo "[FAIL] harness: unknown case_id: $case_id"; exit 2
      ;;
  esac
}

# run_verify_in_fixture
# Ejecuta verify.sh dentro del fixture, captura stdout y exit code.
# Rellena las variables globales: actual_output, actual_exit
run_verify_in_fixture() {
  actual_output=""
  actual_exit=0
  actual_output=$(cd "$TMP_ROOT" && bash specs/002-scaffold-src-projects/_verification/verify.sh 2>/dev/null) || actual_exit=$?
}

# build_expected <case_id>
# Retorna el string literal esperado según el case.
# Para A-full usa $HAS_MAUI (top-level, S-02).
build_expected() {
  local case_id="$1"

  case "$case_id" in
    A)
      # Case A: régimen C, count=0 — 13 líneas + resumen, exit 1
      printf '%s' '[FAIL] C1 — expected 13 .csproj, found 0
[PASS] C7 — all 13 CLAUDE.md hashes match
[PASS] C10 — no changes outside src/
[SKIP] C2 — no .csproj to inspect (count=0)
[SKIP] C3 — no .csproj to inspect (count=0)
[SKIP] C4 — no .csproj to inspect (count=0)
[SKIP] C5 — no .csproj to inspect (count=0)
[SKIP] C6 — no .csproj to inspect (count=0)
[SKIP] C8 — no .csproj to inspect (count=0)
[SKIP] C9 — no .csproj to inspect (count=0)
[SKIP] C11 — no .csproj to inspect (count=0)
[SKIP] C12 — no .csproj to inspect (count=0)
2 passed, 1 failed, 9 skipped, 0 warnings'
      ;;
    B)
      # Case B: régimen B, count=7 — 14 líneas + resumen, exit 1
      printf '%s' '[FAIL] C1 — expected 13 .csproj, found 7
[INFO] partial inspection: count=7/13; inspecting present subset
[PASS] C7 — all 13 CLAUDE.md hashes match
[PASS] C10 — no changes outside src/
[PASS] C2 — props OK on 7 inspected
[PASS] C3 — no forbidden references on 7 inspected
[PASS] C4 — SDKs OK on 7 inspected (all Microsoft.NET.Sdk)
[PASS] C5 — TargetFramework OK on 7 inspected (net9.0; Hybrid/Analyzers absent, skipped in partial set)
[PASS] C6 — contents OK on 7 inspected
[PASS] C8 — restore OK on 7 inspected
[SKIP] C9 — Hybrid not yet created
[PASS] C11 — no forbidden properties on 7 inspected
[PASS] C12 — all 7 inspected use LF
10 passed, 1 failed, 1 skipped, 0 warnings'
      ;;
    A-full)
      # Case A-full: régimen A, count=13 — selección según HAS_MAUI (Q-04, Q-05)
      # Dos expected literales completos embebidos (C-03 simétrico con Cases A/B, sin interpolación)
      local EXPECTED_A_FULL
      if [[ "$HAS_MAUI" -eq 1 ]]; then
        # Variante "MAUI disponible" — 13 líneas de checks + 1 resumen
        EXPECTED_A_FULL=$'[PASS] C1 — 13 .csproj found\n[PASS] C7 — all 13 CLAUDE.md hashes match\n[PASS] C10 — no changes outside src/\n[PASS] C2 — props OK on 13\n[PASS] C3 — no forbidden references\n[PASS] C4 — SDKs OK (9 Microsoft.NET.Sdk, 1 Razor, 3 Web)\n[PASS] C5 — TargetFramework OK on 13 (11 net9.0 + Hybrid multi-target + Analyzers netstandard2.0)\n[PASS] C6 — contents OK on 13\n[PASS] C8 — restore OK on 12 non-Hybrid\n[PASS] C9 — Hybrid restore ok\n[PASS] C11 — no forbidden properties\n[PASS] C12 — all 13 use LF\n12 passed, 0 failed, 0 skipped, 0 warnings'
      else
        # Variante "MAUI ausente" — 13 líneas de checks + 1 resumen; C9 emite [WARN]
        EXPECTED_A_FULL=$'[PASS] C1 — 13 .csproj found\n[PASS] C7 — all 13 CLAUDE.md hashes match\n[PASS] C10 — no changes outside src/\n[PASS] C2 — props OK on 13\n[PASS] C3 — no forbidden references\n[PASS] C4 — SDKs OK (9 Microsoft.NET.Sdk, 1 Razor, 3 Web)\n[PASS] C5 — TargetFramework OK on 13 (11 net9.0 + Hybrid multi-target + Analyzers netstandard2.0)\n[PASS] C6 — contents OK on 13\n[PASS] C8 — restore OK on 12 non-Hybrid\n[WARN] C9 — Hybrid restore skipped: MAUI workload missing\n[PASS] C11 — no forbidden properties\n[PASS] C12 — all 13 use LF\n11 passed, 0 failed, 0 skipped, 1 warnings'
      fi
      # build_expected A-full retorna $EXPECTED_A_FULL directamente, sin interpolación
      printf '%s' "$EXPECTED_A_FULL"
      ;;
    *)
      echo "[FAIL] harness: unknown case_id: $case_id"; exit 2
      ;;
  esac
}

# assert_case <case_id>
# Orquesta build_fixture + run_verify_in_fixture + build_expected + diff + exit code check.
assert_case() {
  local case_id="$1"
  local label="CASE-${case_id}"

  build_fixture "$case_id"
  run_verify_in_fixture

  local expected
  expected=$(build_expected "$case_id")

  # Determinar exit code esperado
  local expected_exit
  case "$case_id" in
    A)     expected_exit=1 ;;
    B)     expected_exit=1 ;;
    A-full) expected_exit=0 ;;
    *)     echo "[FAIL] harness: unknown case_id for exit: $case_id"; exit 2 ;;
  esac

  local case_failed=0

  # Comparar exit code
  if [[ "$actual_exit" -ne "$expected_exit" ]]; then
    echo "[${label}] FAIL: expected exit=${expected_exit}, got exit=${actual_exit}"
    case_failed=1
  fi

  # Comparar salida línea-a-línea (C-05: printf '%s\n' en lugar de echo)
  local diff_out
  diff_out=$(diff -u <(printf '%s\n' "$actual_output") <(printf '%s\n' "$expected") 2>&1) || true
  if [[ -n "$diff_out" ]]; then
    if [[ "$case_failed" -eq 0 ]]; then
      echo "[${label}] FAIL:"
    fi
    echo "$diff_out"
    case_failed=1
  fi

  if [[ "$case_failed" -eq 0 ]]; then
    echo "[${label}] PASS"
    return 0
  else
    return 1
  fi
}

# ---------------------------------------------------------------------------
# Variables para conteo de resultados
# ---------------------------------------------------------------------------
actual_output=""
actual_exit=0
cases_passed=0
cases_failed=0

# ---------------------------------------------------------------------------
# Ejecución de los 3 cases
# ---------------------------------------------------------------------------
if assert_case A; then
  (( cases_passed++ )) || true
else
  (( cases_failed++ )) || true
fi

if assert_case B; then
  (( cases_passed++ )) || true
else
  (( cases_failed++ )) || true
fi

if assert_case "A-full"; then
  (( cases_passed++ )) || true
else
  (( cases_failed++ )) || true
fi

# ---------------------------------------------------------------------------
# Resumen final
# ---------------------------------------------------------------------------
echo "${cases_passed} cases passed, ${cases_failed} cases failed"

if (( cases_failed > 0 )); then
  exit 1
else
  exit 0
fi
