# Tasks: Scaffold de los 13 proyectos en `src/`

- **Spec**: `specs/002-scaffold-src-projects/spec.md`
- **Plan**: `specs/002-scaffold-src-projects/plan.md`

> Tareas atómicas (1-4h). Criterio de hecho verificable. Marcar `[x]` al cerrar.
>
> **Nota TDD**: esta feature es puramente estructural (archivos `.csproj` sin código). No hay lógica que testear con xUnit/bUnit. La "verificación" es estructural (SHA-256, diff XML, `dotnet restore`). Por eso no hay tarea "tests antes de implementación" — se sustituye por un script de verificación (T-02) que se ejecuta **antes** de crear los `.csproj` (debe fallar) y **después** (debe pasar), replicando el ciclo rojo→verde del TDD.

---

## T-01 — Snapshot SHA-256 de los 13 `CLAUDE.md` preexistentes

> **Iteración 2026-04-23 ronda 2** — Criterio alineado con **N-05** de `analyze.md`: formato literal `sha256sum` compatible (hex 64 minúsculas + dos espacios + ruta relativa), para que T-02 pueda verificar con fallback `sha256sum`/`openssl` usando solo el hash vía `awk '{print $1}'`.

- **Descripción**: capturar el hash sha256 de cada `src/TallerPro.*/CLAUDE.md` antes de tocar `src/`, para poder verificar al cierre (CA-08) que no fueron modificados durante la implementación. Guardar la lista en `specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt` con el formato canónico de `sha256sum` (interoperable con `sha256sum -c`).
- **Archivos**:
  - crea: `specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt`
  - lee: `src/TallerPro.*/CLAUDE.md` (13 archivos)
- **Formato canónico de `claude-md-hashes.txt`** (obligatorio, N-05):
  - 13 líneas, orden lexicográfico estable por ruta.
  - Cada línea: `<hex_sha256_64_lowercase><SP><SP><ruta_relativa_al_repo>` (dos espacios entre hash y ruta, sin asterisco de modo-binario, sin `*` prefijo, sin BOM).
  - Ejemplo: `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855  src/TallerPro.Domain/CLAUDE.md`.
  - EOL `LF`, sin BOM, sin línea en blanco final extra.
  - Rutas siempre relativas al repo-root con `/` como separador (aunque se genere en Windows).
- **Criterio**:
  - [ ] Archivo `_verification/claude-md-hashes.txt` existe.
  - [ ] Contiene exactamente 13 líneas, cada una cumpliendo el formato canónico.
  - [ ] `sha256sum -c specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt` (ejecutado desde repo-root) devuelve exit 0 y 13 líneas `OK`.
  - [ ] Reproducible: `cd <repo-root> && sha256sum src/TallerPro.*/CLAUDE.md` produce byte-a-byte el mismo contenido (salvo posible reordenación por locale; si `LC_ALL=C sha256sum ...` difiere del archivo, regenerar con `LC_ALL=C` y EOL `LF`).
  - [ ] Compatible con fallback openssl: `openssl dgst -sha256 -r src/TallerPro.Domain/CLAUDE.md | awk '{print $1}'` coincide con el hash en la línea correspondiente del archivo.
- **Depende de**: —
- **Agente**: `dotnet-dev` (tarea de bookkeeping, no requiere subagente especializado).
- **Estado**: [x] (2026-04-29 — 13 hashes, `sha256sum -c` exit 0)

## T-02 — Script de verificación estructural endurecido (debe fallar antes, pasar después)

> **Iteración 2026-04-23 ronda 2** — Hallazgos resueltos en esta revisión:
> - **Ronda 1 (2026-04-22)**: H-03 (alto), H-02 (medio), H-04 (medio), y opcionales H-05, H-07, H-09.
> - **Ronda 2 (2026-04-23)**: **N-04** (regex/flags de `grep` fijados para C5/C11/C12), **N-05** (contrato de salida de `sha256_of` fijado a hex-64-lowercase). Advertencias integradas: **N-01** (guard `command -v git`), **N-02** (semántica `[SKIP]` cuando C1 falla), **N-06** (formato `[PASS|FAIL|WARN|SKIP] Cxx — <detalle>` unificado con em-dash), **N-07** (C10 delega globbing a git), **N-08** (C8 serial), **N-09** (C9 detección MAUI anclada), **B-01** (nota informativa NuGet).
> - **Ronda 3 (2026-04-23 #2)**: **M-01** (inspección parcial cuando `0 < count < 13`: C2/C3/C4/C5/C6/C8/C9/C11/C12 iteran sobre el subset existente en lugar de `[SKIP]` global; solo SKIP global si `count == 0`), **M-02** (consolidación de la lista "siempre-ejecuta" vs "depende-de-count"), **S-01** (ejemplo del criterio de hecho alineado: C7 ejecuta lógica real con `count == 0` porque los `CLAUDE.md` preexisten a la feature).
> - **Ronda 4 (2026-04-23 #3)**: **R-01** (`shopt -s nullglob` obligatorio antes de toda expansión de glob), **R-02** (harness de regresión movido a nueva tarea **T-02b**), **R-03** (orden determinista de emisión fijado), **R-04** (C6 en régimen B alineado con subset de C2–C5), **R-05** (helper `should_run_check` canonizado con firma bash), **qa-residual** (valores literales exactos en ejemplo régimen B), **R-06** (narrativa redundante de regímenes eliminada de la Descripción; fuente de verdad única en §Régimen de ejecución).
> - **Ronda 5 (2026-04-23 #4)**: **C-01** (estructura canónica `check_Cxx()` + `return 0` explícita; prohibidos `continue` y `case $REGIME` inline), **C-02/Q-01** (fixture case B del régimen B realineada con los 7 genéricos reales de T-04: Domain, Application, Infrastructure, Shared, LocalDb, Observability, Security). Resto de fixes (Q-02 `git status` clean post-init, Q-03 guard pre-flight de CLAUDE.md, Q-04 detección MAUI previa, C-03 duplicar 14 líneas literales, Sec-Bajo trap EXIT con prefix-guard `/tmp/*`, C-05 `printf '%s\n'`) aplicados en la tarea **T-02b**.
> - **Ronda 6 (2026-04-23 #5)**: **O-01** (C4 reescrito como validación por archivo, no por contador; los umbrales 9/1/3 aplican en régimen A y describen el estado final; régimen B itera el subset presente). Resto de fixes (**Q-05/S-03** Case A-full con 2 expected literales completos embebidos, **S-01** `pushd/popd` en lugar de subshell para `git init`, **S-02** `readonly HAS_MAUI`) aplicados en **T-02b**.

- **Descripción**: crear `specs/002-scaffold-src-projects/_verification/verify.sh` (portable a bash y git-bash en Windows) que ejecuta las validaciones estructurales del `quickstart.md §Pasos 2-9` **más** el hardening exigido por las cuatro rondas de `analyze.md` (grep anclado con flags explícitas, check de intocabilidad fuera de `src/`, fallback sha256 con salida canónica, props prohibidas, EOL=LF, guards P1–P4, `shopt -s nullglob`, régimen de ejecución según `count`, orden determinista de emisión, helper `should_run_check` canónico). Comportamiento detallado por escenario en §Régimen de ejecución según `count` (fuente única de verdad). El script retorna exit 0 si todos los checks pasan (o pasan+saltan sin fallos), exit 1 si hay fallos funcionales, exit 2 si falla una precondición de entorno. El harness de regresión que valida los 3 regímenes vive en T-02b (tarea separada).
- **Archivos**:
  - crea: `specs/002-scaffold-src-projects/_verification/verify.sh` (permisos `0755`, EOL `LF`, sin BOM)
  - lee: `specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt` (formato canónico fijado por T-01, N-05)
  - lee: `src/TallerPro.*/*.csproj` (13 cuando existan)
  - lee (solo como guards de precondición): `.specify/memory/constitution.md`, binarios `git`/`sha256sum`/`openssl`/`dotnet`

### Contrato del script

#### Encabezado y preludio (H-03 + hardening)

- [ ] Shebang exacto: `#!/usr/bin/env bash` (primera línea).
- [ ] `set -euo pipefail` inmediatamente después.
- [ ] `IFS=$'\n\t'` antes de cualquier lógica.
- [ ] Guard de repo-root obligatorio: `[[ -f .specify/memory/constitution.md ]] || { echo "[FAIL] run from repo root"; exit 2; }`.
- [ ] Todas las expansiones de variables entrecomilladas (`"$var"`, `"${arr[@]}"`).
- [ ] Prohibido: `eval`, `curl | bash`, pipes a shell remoto, `rm -rf` sin ruta literal.
- [ ] Permisos de archivo: `0755`.
- [ ] **Documentación inline obligatoria** (cabecera `#`):
  - Uso: `bash specs/002-scaffold-src-projects/_verification/verify.sh` (desde repo-root).
  - Lista C1–C12 con una línea por check.
  - Semántica de exit codes: `0` ok (incluye `[SKIP]`/`[WARN]`), `1` fallo funcional, `2` precondición de entorno.
  - Nota informativa **B-01**: `# C8/C9 ejecutan 'dotnet restore' que contacta api.nuget.org para resolver SDK/workloads; el endurecimiento del feed (NuGetAudit + lockfiles) vive en spec 001`.

#### Precondiciones de entorno (ejecutadas ANTES de cualquier check, N-01 + H-03 + H-09)

Orden estricto de precondiciones — todas fallan a `exit 2` con mensaje estructurado:

- [ ] **P1 — repo-root**: `[[ -f .specify/memory/constitution.md ]] || { echo "[FAIL] run from repo root"; exit 2; }`.
- [ ] **P2 — git presente (N-01)**: `command -v git >/dev/null 2>&1 || { echo "[FAIL] git not in PATH"; exit 2; }`.
- [ ] **P3 — hashing tool (H-03)**: detectar en orden `command -v sha256sum` → `command -v openssl` → si ninguno, `echo "[FAIL] no sha256 tool available (install coreutils or openssl)"; exit 2`. Guardar el binario elegido en variable `HASH_BIN` (`sha256sum` | `openssl`).
- [ ] **P4 — hashes.txt presente (H-09)**: `[[ -f specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt ]] || { echo "[FAIL] missing claude-md-hashes.txt — run T-01 first"; exit 2; }`.

#### Globbing — `nullglob` obligatorio (R-01, bloqueante)

- [ ] Inmediatamente tras P4 (antes del primer check que expande glob), ejecutar literalmente:
  ```bash
  # R-01: sin nullglob, un glob sin matches se expande al patrón literal (1 elemento erróneo);
  # con nullglob, se expande a un array vacío. Requisito para régimen B y C.
  shopt -s nullglob
  ```
- [ ] **Todas** las expansiones de glob del script operan bajo `nullglob` activo: `src/TallerPro.*/*.csproj`, `src/TallerPro.*/`, cualquier otra. No se desactiva durante la ejecución.
- [ ] Alternativa aceptada (si el implementador prefiere no usar `nullglob`): migrar cada iteración a `find src -maxdepth 2 -name '*.csproj' -print0 | while IFS= read -r -d '' csproj; do ...; done` con guard explícito de "no matches". Preferencia canónica: `nullglob` por simplicidad.
- [ ] Validación inmediata post-P4: `existing_csprojs=( src/TallerPro.*/*.csproj ); echo "bootstrap: ${#existing_csprojs[@]} .csproj detected"` — con fixture vacía, debe imprimir `bootstrap: 0 .csproj detected`, nunca `bootstrap: 1 .csproj detected` (patrón literal).

#### Wrapper de hashing — contrato de salida (N-05, bloqueante)

- [ ] Función `sha256_of <file>` emite **exclusivamente** el hash hex de **64 caracteres en minúsculas**, sin filename, sin prefijo `*`, sin espacios leading/trailing, sin newline extra. Implementación canónica obligatoria:
  ```bash
  sha256_of() {
    if [[ "$HASH_BIN" == "sha256sum" ]]; then
      sha256sum "$1" | awk '{print $1}'
    else
      openssl dgst -sha256 -r "$1" | awk '{print $1}'
    fi
  }
  ```
- [ ] La comparación de C7 usa SOLO el hash (vía `awk '{print $1}'` sobre la línea correspondiente de `claude-md-hashes.txt`); nunca compara la línea completa (evita mismatch por formato de filename entre binarios).
- [ ] Si el binario elegido falla en runtime (p. ej. archivo inaccesible): `set -e` + `pipefail` propagan el fallo y el check correspondiente imprime `[FAIL] Cxx — sha256_of failed for <file>`; nunca `[PASS]` silencioso.

#### Régimen de ejecución según `count` de `.csproj` (N-02 + M-01 + M-02)

Clasificación **única y consolidada** de los checks (sustituye cualquier lista previa):

| Check | Depende del glob `src/TallerPro.*/*.csproj`? | Régimen |
|---|---|---|
| C1 | — (computa el conteo) | Siempre ejecuta |
| C7 | No (depende de `claude-md-hashes.txt` y de los `CLAUDE.md`, que preexisten a la feature) | Siempre ejecuta |
| C10 | No (depende de `git status` fuera de `src/`) | Siempre ejecuta |
| C2, C3, C4, C5, C6, C8, C9, C11, C12 | Sí | Depende de `count` (ver reglas abajo) |

Reglas para los 9 checks dependientes del glob, aplicadas **tras** computar `count` en C1:

- [ ] **Régimen A — `count == 13`** (todos los `.csproj` presentes): los 9 checks ejecutan su lógica completa sobre los 13 archivos. Emiten `[PASS]`/`[FAIL]` normales.
- [ ] **Régimen B — `0 < count < 13`** (**inspección parcial**, M-01 + R-04): los 9 checks **iteran sobre el subset presente** (los `count` `.csproj` existentes en `src/TallerPro.*/`, expandidos bajo `nullglob`) y emiten `[PASS]`/`[FAIL]` por archivo inspeccionado. Los archivos ausentes **no** generan `[FAIL]` en C2–C12 (el faltante ya lo reportó C1). En particular:
  - **C2, C3, C4, C5, C11, C12**: iteran sobre `existing_csprojs=( src/TallerPro.*/*.csproj )`. Si el array es más corto que 13, se procesan los presentes; los demás ya los contabilizó C1.
  - **C5** con Hybrid ausente → omite las 3 cláusulas de Hybrid; igual para Analyzers. Si el archivo está presente, las verifica íntegras.
  - **C6 (R-04 alineado)**: itera sobre `existing_proj_dirs=( src/TallerPro.*/TallerPro.*.csproj )` para obtener los dirs **con `.csproj` presente** (mismo subset que C2–C5/C11/C12); extrae `dir=$(dirname "$csproj")` y valida su contenido. Dirs que solo tengan `CLAUDE.md` (proyecto pendiente, `.csproj` aún no creado) **NO** son enumerados por C6; su ausencia ya la reportó C1.
  - **C8**: restaura serial solo los no-Hybrid presentes (`existing_no_hybrid=( $(printf '%s\n' "${existing_csprojs[@]}" | grep -v '/TallerPro.Hybrid/' || true) )`). Los faltantes ya están en C1.
  - **C9**: ejecuta solo si `src/TallerPro.Hybrid/TallerPro.Hybrid.csproj` existe; si no, emite `[SKIP] C9 — Hybrid not yet created`.
  - Emitir una **línea de contexto** una sola vez, **después de C1 y antes de C7** (ver §Orden determinista de emisión): `[INFO] partial inspection: count=<N>/13; inspecting present subset`.
- [ ] **Régimen C — `count == 0`**: los 9 checks emiten `[SKIP] Cxx — no .csproj to inspect (count=0)` sin iterar. Única línea SKIP por check.

- [ ] `[SKIP]` **no** cuenta como `passed` ni como `failed`; cuenta como `skipped`. `[INFO]` no cuenta en ningún contador.
- [ ] `[FAIL]` durante inspección parcial (régimen B) **sí** cuenta en `failed` y se suma al `[FAIL] C1`, produciendo exit 1 con información accionable sobre el subset.
- [ ] Resumen final distingue 4 categorías: `<N> passed, <M> failed, <K> skipped, <W> warnings`.
- [ ] Exit code:
  - `1` si `M > 0`.
  - `0` si `M == 0` (incluye régimen A limpio o escenarios con solo SKIP/WARN, aunque éste último nunca debería pasar en régimen A-B con C1 fallando).
  - `2` solo para precondiciones P1–P4 fallidas (abortado antes de C1).

#### Helper `should_run_check` — contrato canónico (R-05, requisito)

- [ ] Al final de C1, derivar `REGIME` de `count`:
  ```bash
  case "$count" in
    0)  REGIME=c ;;
    13) REGIME=a ;;
    *)  REGIME=b ;;
  esac
  ```
- [ ] Definir el helper con firma y cuerpo literal:
  ```bash
  # Decide si un check dependiente del glob debe ejecutar en el régimen actual.
  # Uso canónico: should_run_check C2 || { emit_skip C2 "no .csproj to inspect (count=0)"; continue; }
  should_run_check() {
    local check_id="$1"   # C2..C12 (solo los dependientes del glob)
    case "$REGIME" in
      a|b) return 0 ;;    # ejecuta (lógica completa en A, subset presente en B)
      c)   return 1 ;;    # skip global
      *)   echo "[FAIL] internal: unknown regime $REGIME" >&2; exit 2 ;;
    esac
  }
  ```
- [ ] Helper complementario `emit_skip <check_id> <detalle>` (emite `[SKIP] Cxx — <detalle>` e incrementa el contador `skipped`).
- [ ] Helper complementario `mark_fail <check_id> <detalle>` (emite `[FAIL] Cxx — <detalle>`, incrementa contador `failed`; no ejecuta strings dinámicos ni `eval`).
- [ ] **Estructura canónica obligatoria (C-01)**: cada uno de los 9 checks dependientes del glob (C2, C3, C4, C5, C6, C8, C9, C11, C12) se implementa como **función bash nombrada `check_Cxx()`** (ej. `check_C2`, `check_C11`) que comienza con el short-circuit literal:
  ```bash
  check_C2() {
    should_run_check C2 || { emit_skip C2 "no .csproj to inspect (count=0)"; return 0; }
    # … lógica de C2 …
  }
  ```
  El main del script invoca las funciones en el orden determinista (ver §Orden determinista de emisión). No se permite `case $REGIME` replicado inline en cada check (anti-copy-paste). No se permite `continue` (no hay loop externo) — únicamente `return 0`.
- [ ] `set -euo pipefail` sigue activo. Los fallos individuales de `grep -Ec` se traducen a `[FAIL]` vía `|| mark_fail ...` controlado, no vía trap global, para preservar granularidad.

#### Checks estructurales (C1–C9, cubren quickstart §Pasos 2-9)

- [ ] **C1 — conteo**: `count=$(ls -1 src/*/*.csproj 2>/dev/null | wc -l)`; si `count != 13` → `[FAIL] C1 — expected 13 .csproj, found <count>`. Si `count == 13` → `[PASS] C1 — 13 .csproj found`.
- [ ] **C2 — props comunes (grep anclado ERE, N-04)**: para cada `"$csproj"` en `src/TallerPro.*/*.csproj`:
  - `grep -Ec '^[[:space:]]*<Nullable>enable</Nullable>[[:space:]]*$' "$csproj"` debe devolver `1`.
  - `grep -Ec '^[[:space:]]*<ImplicitUsings>enable</ImplicitUsings>[[:space:]]*$' "$csproj"` debe devolver `1`.
  - Fallo → `[FAIL] C2 — missing Nullable/ImplicitUsings in <csproj>`.
- [ ] **C3 — referencias prohibidas (RF-15, N-04)**: `grep -El '<(PackageReference|ProjectReference)\b' src/TallerPro.*/*.csproj` debe ser vacío. Salida no vacía → `[FAIL] C3 — forbidden reference in <archivo>`.
- [ ] **C4 — SDK por banda (N-04, grep anclado a atributo `Sdk=`)**:
  - **Validación por archivo, no por contador** (O-01): cada `.csproj` se inspecciona individualmente contra el SDK esperado según su nombre de proyecto. Los totales (9/1/3) describen el estado final de régimen A; **en régimen B se iteran los presentes y se valida cada uno por su SDK esperado, sin comparar contra ningún umbral numérico**. En régimen C se emite `[SKIP]` global.
  - Mapa proyecto → SDK esperado (aplica en A y B sobre el subset presente):
    - `Microsoft.NET.Sdk` (9 en A): Domain, Application, Infrastructure, Shared, LocalDb, Observability, Security, Hybrid, Analyzers. Comando: `grep -Ec '^[[:space:]]*<Project[[:space:]]+Sdk="Microsoft\.NET\.Sdk">[[:space:]]*$' "$csproj" == 1`.
    - `Microsoft.NET.Sdk.Razor` (1 en A): Components. Idem con `Microsoft\.NET\.Sdk\.Razor`.
    - `Microsoft.NET.Sdk.Web` (3 en A): Api, Web, Admin. Idem con `Microsoft\.NET\.Sdk\.Web`.
  - Fallo → `[FAIL] C4 — wrong SDK in <csproj>: expected <X>, got <Y>`.
- [ ] **C5 — TargetFramework anclado (H-04 → CA-05/CA-06, N-04 flags fijadas)**: NO usar substring `grep 'net9.0'`. Comandos literales obligatorios con `grep -Ec`:
  - **11 `.csproj` de banda net9.0** (7 genéricos + Components + 3 Web): `grep -Ec '^[[:space:]]*<TargetFramework>net9\.0</TargetFramework>[[:space:]]*$' "$csproj"` debe devolver exactamente `1`.
  - **Hybrid** (`src/TallerPro.Hybrid/TallerPro.Hybrid.csproj`):
    - `grep -Ec '^[[:space:]]*<TargetFrameworks>net9\.0-android;net9\.0-windows10\.0\.19041\.0</TargetFrameworks>[[:space:]]*$' "$hybrid"` debe devolver `1`.
    - `! grep -Eq '<TargetFramework>' "$hybrid"` (sin singular).
    - `! grep -Eq 'net9\.0-(ios|maccatalyst|tizen)' "$hybrid"` (sin targets prohibidos).
  - **Analyzers** (`src/TallerPro.Analyzers/TallerPro.Analyzers.csproj`):
    - `grep -Ec '^[[:space:]]*<TargetFramework>netstandard2\.0</TargetFramework>[[:space:]]*$' "$analyzers"` debe devolver `1`.
    - `! grep -Eq 'net9\.0' "$analyzers"` (sin net9.0 en ninguna parte).
  - Fallo → `[FAIL] C5 — TargetFramework mismatch in <csproj>`.
- [ ] **C6 — contenido de carpeta**: para cada `dir` en `src/TallerPro.*/`, validar que `ls -A1 "$dir" | sort` es exactamente `CLAUDE.md\nTallerPro.<Proj>.csproj\n` (ignora `.` y `..`; considera ocultos). Cualquier entrada extra → `[FAIL] C6 — unexpected entries in <dir>: <extras>`.
- [ ] **C7 — hashes `CLAUDE.md` (CA-08, N-05 comparación solo-hash)**: para cada línea de `claude-md-hashes.txt`:
  - Extraer hash esperado: `expected=$(awk '{print $1}' <<< "$line")`.
  - Extraer ruta: `path=$(awk '{for(i=2;i<=NF;++i) printf "%s%s",$i,(i<NF?" ":"")}' <<< "$line")` (respeta espacios si los hubiera, aunque T-01 los prohíbe).
  - `actual=$(sha256_of "$path")`.
  - `[[ "$expected" == "$actual" ]]` o `[FAIL] C7 — hash mismatch for <path>: expected <exp>, got <act>`.
- [ ] **C8 — restore no-Hybrid serial (N-08)**: iterar serial sobre los 12 `.csproj` no-Hybrid (en orden lexicográfico de ruta) ejecutando `dotnet restore "$csproj" --nologo --verbosity quiet`. Cualquier exit ≠ 0 → `[FAIL] C8 — restore failed for <csproj>`. NO paralelo para mantener logs deterministas.
- [ ] **C9 — restore Hybrid tolerante (N-09 detección anclada)**: `if dotnet workload list 2>/dev/null | grep -Eq '^[[:space:]]*maui\b'; then dotnet restore src/TallerPro.Hybrid/TallerPro.Hybrid.csproj --nologo --verbosity quiet && echo "[PASS] C9 — Hybrid restore ok" || echo "[FAIL] C9 — Hybrid restore failed"; else echo "[WARN] C9 — Hybrid restore skipped: MAUI workload missing"; fi`. Emitir exactamente una línea (una de las tres variantes), sin duplicados.

#### Checks de hardening adicionales (H-02, H-05, H-07)

- [ ] **C10 — intocabilidad fuera de `src/` (H-02 → CA-09 + RF-17, N-07 documentado)**: comando literal exacto (globbing delegado a git, rutas relativas al repo-root):
  ```bash
  out=$(git status --porcelain -- tests/ build/ '*.sln' .editorconfig Directory.Build.props Directory.Packages.props global.json .github/)
  [[ -z "$out" ]] || { echo "[FAIL] C10 — unexpected changes outside src/:"; echo "$out"; mark_fail; }
  ```
  - `'*.sln'` con comillas simples → git recibe el patrón crudo y lo expande contra el índice/working-tree desde el repo-root (cubre `.sln` en cualquier nivel).
  - Éxito → `[PASS] C10 — no changes outside src/`.
- [ ] **C11 — props prohibidas (H-05 → RF-14, N-04 flags explícitas)**: para cada prop prohibida en `(TreatWarningsAsErrors|LangVersion|AnalysisLevel)`:
  - `grep -El "<(TreatWarningsAsErrors|LangVersion|AnalysisLevel)\b" src/TallerPro.*/*.csproj` debe ser vacío.
  - Salida no vacía → `[FAIL] C11 — forbidden property in <archivo>: <línea>`.
  - Éxito → `[PASS] C11 — no forbidden properties`.
- [ ] **C12 — EOL=LF (H-07, N-04 flags explícitas)**: comando literal: `out=$(grep -UIl $'\r' src/TallerPro.*/*.csproj || true)`.
  - Flags: `-U` binary mode (necesario para detectar CR literal), `-I` skip binary files, `-l` solo nombres.
  - `[[ -z "$out" ]]` → `[PASS] C12 — all .csproj use LF`.
  - `out` no vacío → `[FAIL] C12 — CRLF detected in:` seguido de las rutas.

#### Formato de salida (N-06) y códigos de retorno

- [ ] Cada check emite **una** línea con formato `[<NIVEL>] <ID> — <detalle>` donde `<NIVEL>` ∈ `{PASS, FAIL, WARN, SKIP, INFO}` e `<ID>` ∈ `{C1..C12}` (o sin ID para `[INFO]`). Em-dash ` — ` (U+2014, espacios a cada lado) obligatorio.
- [ ] Resumen final: literal `"<N> passed, <M> failed, <K> skipped, <W> warnings"`. `[INFO]` no cuenta en ningún contador.
- [ ] Exit codes:
  - `0` si `M == 0` (incluye runs con `K > 0` o `W > 0`, p. ej. MAUI missing).
  - `1` si `M > 0`.
  - `2` si falla precondición P1–P4 (abortado antes de llegar a los checks).
- [ ] Encabezado del script documenta: uso, lista C1–C12, semántica de los 3 exit codes, nota B-01 sobre NuGet.

#### Orden determinista de emisión (R-03)

El script emite las líneas en este orden **estricto**, sin reordenación:

1. Precondiciones P1–P4 (solo si alguna falla, y entonces exit 2 inmediato; si pasan, silencio).
2. `[FAIL|PASS] C1 — <detalle>` (siempre primero tras precondiciones).
3. Si `REGIME == b`: `[INFO] partial inspection: count=<N>/13; inspecting present subset` (una sola vez, inmediatamente después de C1, antes de C7).
4. `[PASS|FAIL] C7 — <detalle>` (siempre ejecuta).
5. `[PASS|FAIL] C10 — <detalle>` (siempre ejecuta).
6. C2 → C3 → C4 → C5 → C6 → C8 → C9 → C11 → C12 en **orden numérico**, cada uno emitiendo `[PASS]`/`[FAIL]`/`[SKIP]`/`[WARN]` según régimen y resultado.
7. Resumen final: `<N> passed, <M> failed, <K> skipped, <W> warnings`.

> El orden es verificable por `test_verify.sh` (T-02b) mediante diff línea-a-línea contra una salida esperada fija por régimen.

### Criterio de hecho

- [ ] `verify.sh` existe en `specs/002-scaffold-src-projects/_verification/verify.sh`, permisos `0755`, EOL `LF`, sin BOM.
- [ ] Encabezado literal cumple contrato: shebang + `set -euo pipefail` + `IFS=$'\n\t'` + guards P1–P4 (repo-root, git, hashing tool, hashes.txt).
- [ ] Wrapper `sha256_of` implementado exactamente como el bloque canónico; salida verificable: `sha256_of /dev/null` produce `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` (64 hex lowercase, sin newline extra ni prefijo), **idéntico** con `HASH_BIN=sha256sum` y `HASH_BIN=openssl` (N-05).
- [ ] Fallback de hashing verificable: simular `PATH=/tmp` (sin `sha256sum` ni `openssl`) produce exit 2 con `[FAIL] no sha256 tool available...`, nunca `[PASS]` silencioso.
- [ ] Precondiciones verificables: simular `PATH` sin `git` produce `[FAIL] git not in PATH` exit 2 (N-01).
- [ ] **Régimen C, antes de T-04** (`count == 0`, pero T-01 ya corrió → `claude-md-hashes.txt` tiene 13 líneas apuntando a `CLAUDE.md` preexistentes): `[FAIL] C1 — expected 13 .csproj, found 0` + `[PASS] C7 — all 13 CLAUDE.md hashes match` (C7 ejecuta lógica real; los `CLAUDE.md` existen desde antes de la feature) + `[PASS] C10 — no changes outside src/` + `[SKIP] Cxx — no .csproj to inspect (count=0)` para C2/C3/C4/C5/C6/C8/C9/C11/C12; resumen literal `2 passed, 1 failed, 9 skipped, 0 warnings`; exit 1.
- [ ] **Régimen B, durante T-04…T-08** (fixture literal alineada con el flujo real: `count == 7`, **los 7 genéricos creados por T-04** — Domain, Application, Infrastructure, Shared, LocalDb, Observability, Security — todos byte-exact al bloque canónico "Banda genérica" del plan §Componentes, todos con `Sdk="Microsoft.NET.Sdk"` + `net9.0` + `Nullable=enable` + `ImplicitUsings=enable`; Components/Api/Web/Admin/Hybrid/Analyzers aún no creados): salida literal esperada en orden determinista (R-03):
  ```
  [FAIL] C1 — expected 13 .csproj, found 7
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
  9 passed, 1 failed, 1 skipped, 0 warnings
  ```
  exit 1. Esta fixture corresponde al estado real del repo al cerrar T-04 (antes de T-05/T-06/T-07/T-08). Si cualquiera de los 7 viola contrato (p. ej. `PackageReference` intrusa), el correspondiente Cxx emite `[FAIL]` con detalle del archivo y el contador `failed` crece → resumen variante (p. ej. `8 passed, 2 failed, 1 skipped, 0 warnings`), exit 1. La inspección parcial **detecta regresiones sobre el subset presente** (M-01). qa-residual cerrado con fixture real.
- [ ] **Régimen A, tras T-04…T-08** (`count == 13`): todos los checks C1–C12 emiten `[PASS]` (o `[WARN] C9` si falta workload MAUI); resumen `12 passed, 0 failed, 0 skipped, 0 warnings` (o `11 passed, 0 failed, 0 skipped, 1 warnings`); exit 0.
- [ ] Si `claude-md-hashes.txt` no existe al arrancar: exit 2 con `[FAIL] missing claude-md-hashes.txt — run T-01 first` (H-09).
- [ ] Los grep de C2/C3/C4/C5/C11 están anclados al elemento XML completo usando `grep -E` y `grep -Ec ... == N` (o `! grep -Eq` para negativos) — NO substring (verificable leyendo el fuente, N-04/H-04).
- [ ] C12 usa literalmente `grep -UIl $'\r'` con las 3 flags presentes (N-04).
- [ ] C10 usa literalmente `git status --porcelain -- tests/ build/ '*.sln' .editorconfig Directory.Build.props Directory.Packages.props global.json .github/` (N-07).
- [ ] C8 itera serial en orden lexicográfico (N-08).
- [ ] C9 detección MAUI anclada con `grep -Eq '^[[:space:]]*maui\b'` (N-09).
- [ ] Todas las líneas de salida respetan formato `[NIVEL] Cxx — <detalle>` con em-dash (N-06); resumen final con 4 contadores (N-02).
- [ ] Encabezado del script incluye la nota informativa B-01 sobre NuGet.
- [ ] Comprobación de hashes (C7) lee `_verification/claude-md-hashes.txt` y compara solo la columna 1 (hash hex 64 chars) vía `awk '{print $1}'` — no la línea completa (N-05).
- [ ] `code-reviewer` + `security-reviewer` confirman: sin `eval`, expansiones entrecomilladas, guards de repo-root/git/hash-tool/hashes.txt, permisos `0755`, ninguna escritura fuera de stdout/stderr.
- [ ] **R-01**: el script contiene literalmente `shopt -s nullglob` entre P4 y el primer glob. Validación: con `src/TallerPro.*/` sin `.csproj`, la expansión `src/TallerPro.*/*.csproj` produce array vacío (`${#existing_csprojs[@]} == 0`), no un elemento con el patrón literal. Verificado por T-02b.
- [ ] **R-03**: el orden de emisión es literal (ver §Orden determinista de emisión). Verificable por diff línea-a-línea contra salidas esperadas de los 3 regímenes (T-02b).
- [ ] **R-04**: C6 en régimen B itera exclusivamente sobre dirs con `.csproj` presente (subset idéntico a C2–C5/C11/C12); dirs con solo `CLAUDE.md` no producen `[FAIL]` en C6.
- [ ] **R-05 + C-01**: el helper `should_run_check` está definido con el cuerpo canónico exacto; los 9 checks dependientes del glob se implementan como **funciones bash `check_Cxx()`** y invocan `should_run_check Cxx || { emit_skip Cxx "..."; return 0; }` en su primera línea antes de cualquier lógica; no hay `case $REGIME` replicado inline, no hay `continue` (solo `return 0`).
- [ ] **qa-residual**: el ejemplo del régimen B en el criterio de hecho fija valores literales exactos (no rangos abiertos) — fixture `count == 7` con contrato cumplido → resumen `9 passed, 1 failed, 1 skipped, 0 warnings`.

- **Depende de**: T-01 (formato canónico de `claude-md-hashes.txt` fijado por T-01 es prerequisito de C7)
- **Agente**: `dotnet-dev` (con revisión en paralelo de `code-reviewer` y `security-reviewer` sobre el script al cerrar la tarea, dado el hardening exigido).
- **Estado**: [x] (2026-04-29 — 596 líneas, C1–C12, régimen A/B/C, exit 0/1/2; code-reviewer + security-reviewer approve)

## T-02b — Harness de regresión `test_verify.sh` para los 3 regímenes (R-02)

> **Origen**: `analyze.md` ronda 4, bloqueante menor **R-02**. Refinado en **ronda 5** para cerrar Q-01/C-02 (fixture case B realineada a los 7 genéricos reales de T-04), Q-02 (`git status` clean asegurado post-init), Q-03 (guard pre-flight de `CLAUDE.md`), Q-04 (detección MAUI previa al construir expected), C-03 (14 líneas literales duplicadas en case B), Sec-Bajo (trap EXIT con prefix-guard `/tmp/*`), C-05 (`printf '%s\n'` en lugar de `echo` en `diff -u`). Refinado en **ronda 6** para cerrar **Q-05/S-03** (Case A-full con 2 expected literales completos embebidos, simétrico con Cases A/B), **S-01** (`git init` sin subshell → `pushd/popd` para que `exit 2` termine el padre directamente), **S-02** (`readonly HAS_MAUI` post-H4).

- **Descripción**: crear `specs/002-scaffold-src-projects/_verification/test_verify.sh`, un harness bash que usa fixtures temporales (`mktemp -d`) para construir mini-worktrees simulando los 3 regímenes A/B/C, ejecuta `verify.sh` contra cada fixture, y asserta exit code + resumen literal línea-a-línea. Es **meta-test** del script estructural — no reemplaza a T-09 (que valida el régimen A real del repo tras T-04…T-08), lo complementa con red de seguridad para regresiones durante iteraciones de `verify.sh`.
- **Archivos**:
  - crea: `specs/002-scaffold-src-projects/_verification/test_verify.sh` (permisos `0755`, EOL `LF`, sin BOM)
  - lee: `specs/002-scaffold-src-projects/_verification/verify.sh`
  - lee: `src/TallerPro.*/CLAUDE.md` (copia al fixture; los 13 deben existir para poder ejecutar el harness)

### Contrato del harness

#### Encabezado y preludio (alineado con verify.sh)

- [ ] Shebang `#!/usr/bin/env bash`, `set -euo pipefail`, `IFS=$'\n\t'`.
- [ ] Guard de repo-root idéntico al de `verify.sh` (`[[ -f .specify/memory/constitution.md ]] || exit 2`).
- [ ] **Trap EXIT defensivo (Sec-Bajo)** — literal obligatorio:
  ```bash
  trap 'if [[ -n "${TMP_ROOT:-}" && -d "$TMP_ROOT" && "$TMP_ROOT" == /tmp/* ]]; then rm -rf "$TMP_ROOT"; fi' EXIT
  ```
  El prefijo `/tmp/*` (o `$TMPDIR` canónico) blinda contra `TMP_ROOT` corrupto; `mktemp -d` ya garantiza no-vacío pero defensa en profundidad cuesta cero.
- [ ] Permisos `0755`, sin `eval`, sin `curl | bash`.

#### Precondiciones del harness (Q-03, pre-flight)

Ejecutadas **antes** de construir cualquier fixture:

- [ ] **H1 — repo-root**: idéntico al guard P1 de `verify.sh`.
- [ ] **H2 — `CLAUDE.md` presentes (Q-03)** — guard obligatorio:
  ```bash
  shopt -s nullglob
  claude_md_files=( src/TallerPro.*/CLAUDE.md )
  [[ ${#claude_md_files[@]} -eq 13 ]] || {
    echo "[FAIL] harness: expected 13 src/TallerPro.*/CLAUDE.md, found ${#claude_md_files[@]} — run T-01 first"
    exit 2
  }
  ```
  Sin este guard, un fixture con 0 `CLAUDE.md` produciría `claude-md-hashes.txt` vacío y C7 pasaría vacuamente.
- [ ] **H3 — binarios requeridos**: `command -v git`, `command -v sha256sum || command -v openssl`, `command -v dotnet` — fallos → `exit 2` con mensaje específico.
- [ ] **H4 — detección de workload MAUI (Q-04)** — ejecutar **una sola vez** al inicio y almacenar en variable:
  ```bash
  if dotnet workload list 2>/dev/null | grep -Eq '^[[:space:]]*maui\b'; then
    HAS_MAUI=1
  else
    HAS_MAUI=0
  fi
  readonly HAS_MAUI   # S-02: prevenir mutación accidental entre cases
  ```
  Se usa al construir el `expected` del Case A-full (ver abajo).

#### Estrategia de fixture

- [ ] Por cada case, el harness:
  1. Crea `TMP_ROOT=$(mktemp -d)` (valida que la ruta empieza por `/tmp/`; si no, `exit 2`).
  2. Construye el layout mínimo del repo dentro de `$TMP_ROOT`:
     - `mkdir -p .specify/memory/ && touch .specify/memory/constitution.md` (satisface P1).
     - `mkdir -p src/TallerPro.X/` para los 13 proyectos + copia de cada `CLAUDE.md` real (`cp "$REAL_REPO/src/TallerPro.X/CLAUDE.md" "$TMP_ROOT/src/TallerPro.X/CLAUDE.md"`).
     - `mkdir -p specs/002-scaffold-src-projects/_verification/` + copia de `verify.sh` real + regeneración de `claude-md-hashes.txt` contra las 13 copias del fixture (usando `sha256sum src/TallerPro.*/CLAUDE.md > specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt` ejecutado con `cd "$TMP_ROOT"`).
  3. **`git init` limpio (Q-02)** — literal obligatorio:
     ```bash
     # S-01 — sin subshell: 'exit 2' termina el script padre directamente, sin
     # depender de la propagación del exit code del subshell vía 'set -e'.
     # pushd/popd contienen el cambio de CWD sin crear proceso hijo.
     pushd "$TMP_ROOT" > /dev/null
     git init -q
     git -c user.email=harness@local -c user.name=harness add -A
     git -c user.email=harness@local -c user.name=harness commit -q -m "fixture baseline"
     # Asserts post-commit (Q-02): sin esto C10 podría emitir [FAIL] espurio
     [[ "$(git log --oneline | wc -l | tr -d ' ')" -ge 1 ]] || { echo "[FAIL] harness: fixture has no commit"; exit 2; }
     [[ -z "$(git status --porcelain)" ]] || { echo "[FAIL] harness: fixture dirty after commit"; exit 2; }
     popd > /dev/null
     ```
  4. Crea los `.csproj` según case (ver abajo) — para régimenes B y A-full, byte-exact con los bloques canónicos del plan §Componentes.
  5. Ejecuta `cd "$TMP_ROOT" && bash specs/002-scaffold-src-projects/_verification/verify.sh` capturando stdout + exit code.
  6. Compara salida contra expected literal usando `diff -u <(printf '%s\n' "$actual") <(printf '%s\n' "$expected")` (C-05: `printf` evita consumo de escapes que `echo` podría hacer).
  7. Tras completar el case, el trap EXIT limpia `$TMP_ROOT` al terminar el harness (o `rm -rf` manual entre cases si se reutiliza variable).

#### Casos de test obligatorios

- [ ] **Case A (régimen C, `count == 0`)**: fixture sin ningún `.csproj`.
  - Exit esperado: `1`.
  - Expected literal completo (comparado línea-a-línea):
    ```
    [FAIL] C1 — expected 13 .csproj, found 0
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
    2 passed, 1 failed, 9 skipped, 0 warnings
    ```
- [ ] **Case B (régimen B, `count == 7`, Q-01/C-02 fixture alineada con T-04 real)**: fixture con los 7 `.csproj` **genéricos que T-04 crea realmente** (Domain, Application, Infrastructure, Shared, LocalDb, Observability, Security), cada uno byte-exact al bloque canónico "Banda genérica" del plan §Componentes (`Sdk="Microsoft.NET.Sdk"`, `<TargetFramework>net9.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`). Components/Api/Web/Admin/Hybrid/Analyzers ausentes.
  - Exit esperado: `1`.
  - Expected literal completo (C-03, las 14 líneas duplicadas literalmente aquí; no referencia cruzada a T-02):
    ```
    [FAIL] C1 — expected 13 .csproj, found 7
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
    9 passed, 1 failed, 1 skipped, 0 warnings
    ```
- [ ] **Case A-full (régimen A, `count == 13`, todos byte-exact)**: fixture con los 13 `.csproj` byte-exact del plan §Componentes.
  - Exit esperado: `0`.
  - **Selección de expected según `HAS_MAUI` (Q-04 + Q-05)** — **2 expected literales completos** (igual tratamiento que Cases A y B, sin interpolación de líneas sueltas, C-03 simétrico):
    ```bash
    if [[ "$HAS_MAUI" -eq 1 ]]; then
      # Variante "MAUI disponible" — 13 líneas de checks + 1 resumen
      EXPECTED_A_FULL=$'[PASS] C1 — 13 .csproj found
    [PASS] C7 — all 13 CLAUDE.md hashes match
    [PASS] C10 — no changes outside src/
    [PASS] C2 — props OK on 13
    [PASS] C3 — no forbidden references
    [PASS] C4 — SDKs OK (9 Microsoft.NET.Sdk, 1 Razor, 3 Web)
    [PASS] C5 — TargetFramework OK on 13 (11 net9.0 + Hybrid multi-target + Analyzers netstandard2.0)
    [PASS] C6 — contents OK on 13
    [PASS] C8 — restore OK on 12 non-Hybrid
    [PASS] C9 — Hybrid restore ok
    [PASS] C11 — no forbidden properties
    [PASS] C12 — all 13 use LF
    12 passed, 0 failed, 0 skipped, 0 warnings'
    else
      # Variante "MAUI ausente" — 13 líneas de checks + 1 resumen; C9 emite [WARN]
      EXPECTED_A_FULL=$'[PASS] C1 — 13 .csproj found
    [PASS] C7 — all 13 CLAUDE.md hashes match
    [PASS] C10 — no changes outside src/
    [PASS] C2 — props OK on 13
    [PASS] C3 — no forbidden references
    [PASS] C4 — SDKs OK (9 Microsoft.NET.Sdk, 1 Razor, 3 Web)
    [PASS] C5 — TargetFramework OK on 13 (11 net9.0 + Hybrid multi-target + Analyzers netstandard2.0)
    [PASS] C6 — contents OK on 13
    [PASS] C8 — restore OK on 12 non-Hybrid
    [WARN] C9 — Hybrid restore skipped: MAUI workload missing
    [PASS] C11 — no forbidden properties
    [PASS] C12 — all 13 use LF
    11 passed, 0 failed, 0 skipped, 1 warnings'
    fi
    ```
  - `build_expected A-full` retorna `"$EXPECTED_A_FULL"` directamente, sin interpolación ni concatenación. Cualquier cambio futuro en líneas de C1–C12 o en el resumen exige editar **los 2 bloques** literales (mismo contrato que Cases A/B, evita drift silencioso identificado en Q-05).
- [ ] Comparación línea-a-línea con `diff -u <(printf '%s\n' "$actual") <(printf '%s\n' "$expected")` (C-05); cualquier diff produce `[CASE-X] FAIL:` seguido del diff.
- [ ] Comparación de exit code exacta; exit code inesperado → `[CASE-X] FAIL: expected exit=<E>, got exit=<G>`.

#### Helpers del harness

- [ ] `build_fixture <case_id>` — crea `$TMP_ROOT`, ejecuta H1–H4 (solo la primera vez), construye layout, hace `git init` limpio y valida Q-02 asserts.
- [ ] `seed_csprojs <case_id>` — escribe los `.csproj` según case (0 para A, 7 genéricos para B, 13 para A-full).
- [ ] `run_verify_in_fixture` — `cd "$TMP_ROOT"`, ejecuta `bash specs/002-scaffold-src-projects/_verification/verify.sh`, captura stdout + exit code.
- [ ] `build_expected <case_id>` — retorna el string literal esperado (ajustado por `HAS_MAUI` para Case A-full).
- [ ] `assert_case <case_id>` — orquesta los 4 helpers anteriores + diff + exit code check, emite `[CASE-X] PASS` o `[CASE-X] FAIL: …`.

#### Formato de salida del harness

- [ ] Por cada case: `[CASE-X] PASS` o `[CASE-X] FAIL:` seguido del diff en líneas subsiguientes.
- [ ] Resumen final: `<P> cases passed, <F> cases failed` y exit 0 si `F == 0`, exit 1 si `F > 0`.

### Criterio de hecho

- [ ] `test_verify.sh` existe en `specs/002-scaffold-src-projects/_verification/test_verify.sh`, permisos `0755`, EOL `LF`, sin BOM.
- [ ] Encabezado cumple preludio (shebang + `set -euo pipefail` + `IFS` + guard repo-root + trap EXIT defensivo con prefix-guard `/tmp/*`).
- [ ] **H2 (Q-03) verificable**: al ejecutar en un repo sin los 13 `CLAUDE.md`, el harness termina exit 2 con `[FAIL] harness: expected 13 src/TallerPro.*/CLAUDE.md, found N`.
- [ ] **H4 (Q-04) verificable**: la variable `HAS_MAUI` se computa una sola vez al inicio; el expected de Case A-full se selecciona según su valor.
- [ ] **Post-`git init` (Q-02)**: tras el commit inicial, `git log --oneline | wc -l ≥ 1` y `git status --porcelain` devuelve cadena vacía; fallo → exit 2 con mensaje específico.
- [ ] Case A (régimen C) aserta exit 1 + expected literal completo (13 líneas + resumen `2 passed, 1 failed, 9 skipped, 0 warnings`).
- [ ] Case B (régimen B, count=7, **Q-01/C-02 fixture = 7 genéricos reales**) aserta exit 1 + expected literal completo (14 líneas + resumen `9 passed, 1 failed, 1 skipped, 0 warnings`). Las 14 líneas esperadas están **embebidas literalmente** en el fuente del harness (C-03), no como cross-ref a T-02.
- [ ] **Case A-full (Q-05)** aserta exit 0 + **uno de los 2 expected literales completos** (13 líneas de checks + resumen) seleccionado según `HAS_MAUI`; no hay interpolación ni concatenación de líneas sueltas. Variante `HAS_MAUI=1`: resumen `12 passed, 0 failed, 0 skipped, 0 warnings` + línea C9 `[PASS] C9 — Hybrid restore ok`. Variante `HAS_MAUI=0`: resumen `11 passed, 0 failed, 0 skipped, 1 warnings` + línea C9 `[WARN] C9 — Hybrid restore skipped: MAUI workload missing`. Ambos bloques **embebidos literales en el fuente del harness** (simetría con Cases A/B, cierre de Q-05).
- [ ] Comparación usa `diff -u <(printf '%s\n' "$actual") <(printf '%s\n' "$expected")` — **no `echo`** (C-05).
- [ ] Ejecutar `bash specs/002-scaffold-src-projects/_verification/test_verify.sh` desde repo-root con los 13 `CLAUDE.md` presentes → exit 0 con los 3 cases en `[CASE-X] PASS`.
- [ ] Trap EXIT verificable: tras exit, `$TMP_ROOT` no existe en disco; manipular `TMP_ROOT` manualmente fuera de `/tmp/*` → el trap NO borra (prueba defensiva).
- [ ] `qa-reviewer` confirma que los 3 cases cubren los 3 regímenes y que un fallo regresivo en `verify.sh` (p. ej. quitar `shopt -s nullglob`, cambiar el orden de emisión, romper el wrapper `sha256_of`) sería detectado por este harness.

- **Depende de**: T-02 (el harness ejecuta `verify.sh`, que debe existir y cumplir contrato)
- **Agente**: `dotnet-dev` (con revisión de `qa-reviewer` al cerrar, para validar cobertura de regímenes).
- **Estado**: [x] (2026-04-30 — 3 cases PASS; fix bug MSYS2 grep -UIl en $() → grep -UIq en if-loop)

## T-03 — Verificar que `.gitattributes` normaliza `.csproj` a `LF` sin BOM

- **Descripción**: inspeccionar `.gitattributes` raíz y confirmar que `*.csproj` queda cubierto por una regla que fuerza `text eol=lf` (directa o por `* text=auto eol=lf`). Si falta, añadir una línea explícita `*.csproj text eol=lf working-tree-encoding=UTF-8` (sin BOM). Esta tarea **no** modifica `.gitattributes` si la regla ya está implícita; solo documenta el hallazgo en un comentario de la tarea y en `quickstart.md §Casos límite` (ya mencionado). Objetivo: prevenir que T-04…T-16 produzcan `.csproj` con CRLF o BOM en Windows.
- **Archivos**:
  - lee: `.gitattributes`
  - modifica (solo si falta cobertura): `.gitattributes` añadiendo 1 línea
- **Criterio**:
  - [ ] `.gitattributes` cubre `*.csproj` con `eol=lf` (directa o por wildcard).
  - [ ] Si se modifica, la línea añadida es idempotente y no altera reglas existentes (`git diff .gitattributes` muestra solo `+` de esa línea).
  - [ ] Documentado en el PR de la feature si se hizo cambio; si no, nota en el commit T-03 indicando "no-op, cobertura ya presente".
- **Depende de**: —
- **Agente**: `dotnet-dev`
- **Estado**: [x] (2026-04-29 — no-op, cobertura ya presente en `.gitattributes:4` `*.csproj    text eol=lf`)

## T-04 — Crear `.csproj` de **banda genérica (7 proyectos)**

- **Descripción**: crear byte-exact, siguiendo el bloque canónico del plan §Componentes "Banda genérica", los 7 `.csproj`: `Domain`, `Application`, `Infrastructure`, `Shared`, `LocalDb`, `Observability`, `Security`. Todos con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=net9.0`, `Nullable=enable`, `ImplicitUsings=enable`. EOL `LF`, sin BOM. Sin `PackageReference`/`ProjectReference`. Sin fuentes.
- **Archivos**:
  - crea: `src/TallerPro.Domain/TallerPro.Domain.csproj`
  - crea: `src/TallerPro.Application/TallerPro.Application.csproj`
  - crea: `src/TallerPro.Infrastructure/TallerPro.Infrastructure.csproj`
  - crea: `src/TallerPro.Shared/TallerPro.Shared.csproj`
  - crea: `src/TallerPro.LocalDb/TallerPro.LocalDb.csproj`
  - crea: `src/TallerPro.Observability/TallerPro.Observability.csproj`
  - crea: `src/TallerPro.Security/TallerPro.Security.csproj`
- **Criterio**:
  - [ ] Los 7 archivos existen.
  - [ ] Cada uno es byte-exact igual al bloque canónico del plan (único salto: el nombre del archivo, no del contenido — el `.csproj` **no** lleva nombre de proyecto dentro).
  - [ ] `dotnet restore src/TallerPro.<Proj>/TallerPro.<Proj>.csproj` → exit 0 para los 7.
  - [ ] `git diff` solo muestra creación de 7 archivos; cero cambios en cualquier `CLAUDE.md`.
  - [ ] Ejecutar `verify.sh` tras esta tarea aún falla (faltan 6 `.csproj`), pero el fallo se concentra en las bandas pendientes.
- **Depende de**: T-02b, T-03
- **Agente**: `dotnet-dev`
- **Estado**: [x] (2026-04-30 — 7 .csproj banda genérica, byte-exact, verify.sh PASS)

## T-05 — Crear `.csproj` de **`TallerPro.Components`** (banda Razor)

- **Descripción**: crear `src/TallerPro.Components/TallerPro.Components.csproj` byte-exact con `Sdk=Microsoft.NET.Sdk.Razor`, `TargetFramework=net9.0`, `Nullable=enable`, `ImplicitUsings=enable` (decisión PA-01).
- **Archivos**:
  - crea: `src/TallerPro.Components/TallerPro.Components.csproj`
- **Criterio**:
  - [ ] Archivo existe y es byte-exact al bloque "Components" del plan.
  - [ ] `dotnet restore src/TallerPro.Components/TallerPro.Components.csproj` → exit 0.
  - [ ] SDK verificado = `Microsoft.NET.Sdk.Razor` (grep directo).
  - [ ] `src/TallerPro.Components/CLAUDE.md` inalterado (sha256 igual a T-01).
- **Depende de**: T-02b, T-03
- **Agente**: `frontend-dev` (proyecto de UI; mantiene la convención de delegación aunque sea solo un `.csproj`).
- **Estado**: [x] (2026-04-30 — Components .csproj byte-exact, Sdk.Razor, verify.sh PASS)

## T-06 — Crear `.csproj` de **banda Web (3 proyectos)**: `Api`, `Web`, `Admin`

- **Descripción**: crear los 3 `.csproj` byte-exact con `Sdk=Microsoft.NET.Sdk.Web`, `TargetFramework=net9.0`, `Nullable=enable`, `ImplicitUsings=enable`. Sin `Program.cs`, sin `appsettings.json`, sin `wwwroot/`, sin `Views/`.
- **Archivos**:
  - crea: `src/TallerPro.Api/TallerPro.Api.csproj`
  - crea: `src/TallerPro.Web/TallerPro.Web.csproj`
  - crea: `src/TallerPro.Admin/TallerPro.Admin.csproj`
- **Criterio**:
  - [ ] Los 3 archivos existen y son byte-exact al bloque "Web" del plan.
  - [ ] `dotnet restore` de cada uno → exit 0 (el SDK Web resuelve referencias framework implícitas sin falla, aunque no haya código).
  - [ ] SDK = `Microsoft.NET.Sdk.Web` en los 3.
  - [ ] `CLAUDE.md` de las 3 carpetas inalterados.
- **Depende de**: T-02b, T-03
- **Agente**: `dotnet-dev` (Api) + `frontend-dev` (Web, Admin) — ejecución coordinada por el orquestador; los 3 archivos pueden crearse en paralelo ya que son independientes.
- **Estado**: [x] (2026-04-30 — Api/Web/Admin .csproj byte-exact, Sdk.Web, verify.sh PASS)

## T-07 — Crear `.csproj` de **`TallerPro.Hybrid`** (MAUI multi-target)

- **Descripción**: crear `src/TallerPro.Hybrid/TallerPro.Hybrid.csproj` byte-exact con `Sdk=Microsoft.NET.Sdk`, `TargetFrameworks=net9.0-android;net9.0-windows10.0.19041.0`, `OutputType=Exe`, `UseMaui=true`, `SingleProject=true`, `Nullable=enable`, `ImplicitUsings=enable` (decisiones PA-02, PA-03). Sin iconos, sin splash, sin `Platforms/`, sin `MauiProgram.cs`, sin `appxmanifest`.
- **Archivos**:
  - crea: `src/TallerPro.Hybrid/TallerPro.Hybrid.csproj`
- **Criterio**:
  - [ ] Archivo existe y es byte-exact al bloque "Hybrid" del plan.
  - [ ] `<TargetFrameworks>` contiene exactamente `net9.0-android;net9.0-windows10.0.19041.0` (orden incluido, sin espacios extra).
  - [ ] Ausencia de `net9.0-ios`, `net9.0-maccatalyst`, `net9.0-tizen`.
  - [ ] Si workload MAUI disponible: `dotnet restore` exit 0. Si no: fallo esperado `NETSDK1147` documentado en el log de la tarea como aceptable (no bloqueante).
  - [ ] `CLAUDE.md` inalterado.
- **Depende de**: T-02b, T-03
- **Agente**: `frontend-dev` (Hybrid es el cliente Blazor-MAUI).
- **Estado**: [x] (2026-04-30 — Hybrid .csproj byte-exact, MAUI multi-target, verify.sh PASS C9)

## T-08 — Crear `.csproj` de **`TallerPro.Analyzers`** (Roslyn netstandard2.0)

- **Descripción**: crear `src/TallerPro.Analyzers/TallerPro.Analyzers.csproj` byte-exact con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=netstandard2.0`, `IsRoslynComponent=true`, `EnforceExtendedAnalyzerRules=true`, `IncludeBuildOutput=false`, `Nullable=enable`, `ImplicitUsings=enable` (decisión PA-04). Sin `DiagnosticAnalyzer`, sin `DiagnosticDescriptor`, sin reglas `TP0001`-`TP0005` (stubs en spec 001, lógica real en spec futura).
- **Archivos**:
  - crea: `src/TallerPro.Analyzers/TallerPro.Analyzers.csproj`
- **Criterio**:
  - [ ] Archivo existe y es byte-exact al bloque "Analyzers" del plan.
  - [ ] `<TargetFramework>netstandard2.0</TargetFramework>` presente y único.
  - [ ] Las 3 props de Roslyn (`IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, `IncludeBuildOutput=false`) presentes.
  - [ ] `dotnet restore` exit 0 (netstandard2.0 no requiere workload).
  - [ ] `CLAUDE.md` inalterado.
- **Depende de**: T-02b, T-03
- **Agente**: `dotnet-dev`
- **Estado**: [x] (2026-04-30 — Analyzers .csproj byte-exact, netstandard2.0, Roslyn props, verify.sh PASS)

## T-09 — Ejecutar `verify.sh` y obtener exit 0 (ciclo verde)

- **Descripción**: ejecutar el script de verificación completo (T-02) tras T-04…T-08. Debe pasar las 9 comprobaciones: conteo = 13, `Nullable`/`ImplicitUsings` en los 13, cero `PackageReference`/`ProjectReference`, SDKs correctos, `TargetFramework(s)` por banda, contenido de carpeta exactamente `CLAUDE.md + .csproj`, hashes de `CLAUDE.md` intactos, `dotnet restore` ok en los 12 no-Hybrid. Si Hybrid falla solo por `NETSDK1147` (workload MAUI ausente), el script lo reporta como **warning** no bloqueante y aun así devuelve exit 0.
- **Archivos**:
  - lee: `specs/002-scaffold-src-projects/_verification/verify.sh`
  - lee: `src/TallerPro.*/*.csproj` (13)
  - lee: `specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt`
- **Criterio**:
  - [ ] `bash verify.sh` exit 0.
  - [ ] Output lista las 9 comprobaciones como `[PASS]`.
  - [ ] Si Hybrid sin MAUI: `[WARN] Hybrid restore skipped: MAUI workload missing` aparece una sola vez, no duplica.
  - [ ] Log de ejecución se adjunta al PR (copiar/pegar en descripción).
- **Depende de**: T-02b, T-04, T-05, T-06, T-07, T-08
- **Agente**: `qa-reviewer` (verificación final estructural).
- **Estado**: [x] (2026-04-30 — verify.sh exit 0, 12 passed, 0 failed, 0 skipped, 0 warnings)

## T-10 — Revisión cruzada: code-reviewer + qa-reviewer

- **Descripción**: al cerrar las tareas T-04…T-08, invocar en paralelo `code-reviewer` (revisar que los `.csproj` siguen convenciones, no hay props fantasma, XML válido, indentación consistente) y `qa-reviewer` (revisar cobertura del `verify.sh`, que cubre los 10 CA de spec, y que no falta ningún caso límite del quickstart). Consolidar hallazgos en `specs/002-scaffold-src-projects/review-notes.md` (opcional, solo si hay hallazgos menores; bloqueantes abren nueva tarea).
- **Archivos**:
  - lee: `src/TallerPro.*/*.csproj` (13)
  - lee: `specs/002-scaffold-src-projects/{spec,plan,quickstart}.md` + `_verification/verify.sh`
  - crea (si aplica): `specs/002-scaffold-src-projects/review-notes.md`
- **Criterio**:
  - [ ] `code-reviewer` entrega veredicto `approve` o `changes-requested`.
  - [ ] `qa-reviewer` entrega veredicto `approve` o `changes-requested`.
  - [ ] Hallazgos de severidad `alto` o superior → abrir tarea correctiva antes de cerrar.
  - [ ] Hallazgos `medio`/`bajo` → documentados en `review-notes.md` y linkeados en la spec 001 para cuando 001 recorte RF-06.
- **Depende de**: T-09
- **Agente**: `code-reviewer` + `qa-reviewer` (en paralelo, orquestados desde el flujo principal).
- **Estado**: [x] (2026-04-30 — code-reviewer: approve; qa-reviewer: approve con H-1/H-2 resueltos; verify.sh C5 extendido, quickstart.md §Paso 8 actualizado)

## T-11 — Marcar spec como `implemented`

- **Descripción**: cambiar `Estado: draft` → `Estado: implemented` en la cabecera de `specs/002-scaffold-src-projects/spec.md`. Anexar en `clarify.md §Historial de clarificaciones` (o en una nueva sección `## Cierre`) la fecha, el commit SHA, y el resultado del `verify.sh` (exit code y hashes).
- **Archivos**:
  - modifica: `specs/002-scaffold-src-projects/spec.md` (línea `Estado`)
  - modifica: `specs/002-scaffold-src-projects/clarify.md` (anexo Cierre)
- **Criterio**:
  - [ ] `Estado: implemented` en `spec.md`.
  - [ ] Sección de cierre documenta commit SHA, fecha, `verify.sh` exit 0.
  - [ ] Todas las tareas T-01…T-10 marcadas `[x]` en este archivo.
- **Depende de**: T-10
- **Agente**: orquestador principal.
- **Estado**: [x] (2026-04-30 — spec.md → implemented; clarify.md §Cierre anotado)

---

## Secuencia recomendada (orden topológico)

```
T-01 ─┐
T-03 ─┤ (pueden ejecutarse en paralelo; T-03 no depende de T-01)
      ├── T-02 ──► T-02b ──┐
                           ├── T-04 ─┐
                           ├── T-05 ─┤
                           ├── T-06 ─┤ (T-04…T-08 en paralelo, son archivos
                           ├── T-07 ─┤  independientes en carpetas distintas)
                           └── T-08 ─┘
                                     └── T-09 ──── T-10 ──── T-11
```

- **Paralelizables fase inicial**: T-01 y T-03 (no dependen entre sí).
- **Secuencia crítica**: T-01 → T-02 → T-02b → fan-out T-04…T-08.
- **Fan-out central**: T-04, T-05, T-06, T-07, T-08 una vez que T-02b+T-03 están listos. El orquestador puede lanzarlas en una sola ronda de subagentes paralelos.
- **Fan-in**: T-09 bloquea T-10 que bloquea T-11.

## Cierre

- [ ] Todas las tareas T-01, T-02, T-02b, T-03…T-11 marcadas `[x]`.
- [ ] `verify.sh` exit 0 registrado en el commit final.
- [ ] Hashes SHA-256 de los 13 `CLAUDE.md` inalterados (comparar T-01 snapshot vs estado final).
- [ ] `code-reviewer` + `qa-reviewer` → `approve`.
- [ ] ADRs: **n/a** (sin decisión arquitectónica nueva; ver plan §Decisiones).
- [ ] `spec.md` → `Estado: implemented`.
- [ ] TODO externo anotado en spec 001: recortar RF-06 y actualizar CA-01/CA-06 cuando 001 entre a `/speckit.clarify`.
