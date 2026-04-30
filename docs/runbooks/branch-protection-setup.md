# Runbook: Configurar Branch Protection en GitHub

Ejecutar una sola vez tras el primer push al remote.

**Repo**: `https://github.com/DesarrollosDavidGarcia/Washer`

## Pasos en GitHub UI

1. Ir a **Settings → Branches → Add branch protection rule**.
2. Branch name pattern: `main`
3. Activar:
   - [x] **Require a pull request before merging**
     - Required approvals: `1` (aumentar a `2` cuando haya dev senior)
   - [x] **Require review from Code Owners** (aplica las reglas de `.github/CODEOWNERS`)
   - [x] **Require status checks to pass before merging**
     - Buscar y añadir: `build-and-test (ubuntu-latest)` y `build-and-test (windows-latest)`
   - [x] **Require branches to be up to date before merging**
   - [x] **Do not allow bypassing the above settings** (incluye administradores)
4. **Save changes**.

## Verificación

```bash
gh api repos/DesarrollosDavidGarcia/Washer/branches/main/protection \
  --jq '.required_pull_request_reviews.require_code_owner_reviews'
# Debe devolver: true
```

## Notas

- Cuando incorpores un segundo dev: actualizar `.github/CODEOWNERS` añadiendo `@dev-handle` a las 3 reglas, y subir el número de approvals requeridos a `2` (ADR-0001 D-7).
- El CI (`build-and-test`) aparece como status check disponible después del primer run exitoso en GitHub Actions.
