#!/bin/sh
# Automatyczne wdrożenie z GitHub Actions (wzorowane na deploy.sh z afiszowni i Offroadly).
#
# Uruchamiane WYŁĄCZNIE przez klucz SSH GitHuba (authorized_keys: command="/opt/lionsoft/deploy/deploy.sh",
# bez powłoki i przekierowań). Serwer jest współdzielony z offroadly.pl i afiszownią, więc klucz z sekretów tego
# repozytorium nie może dawać pełnego dostępu do roota – pozwala tylko wdrożyć obraz strony.
# Wywołanie (SSH_ORIGINAL_COMMAND):  deploy <tag> <użytkownik GitHuba>
# Na stdin: jednorazowy GITHUB_TOKEN (ważny tylko w czasie zadania Actions) – do pobrania prywatnego obrazu z GHCR.
#
# Kroki: pobierz obraz → podmień kontener → poczekaj aż odpowiada → jeśli nie w 2 minuty, wróć do poprzedniej wersji.

set -eu
DIR=/opt/lionsoft/deploy
cd "$DIR"
REPO_IMAGE="ghcr.io/jakublewandowski/lionsoft"
LOG="$DIR/deploy.log"

# shellcheck disable=SC2086
set -- ${SSH_ORIGINAL_COMMAND:-}
if [ "${1:-}" != "deploy" ]; then echo "Dozwolone tylko: deploy <tag> <użytkownik>" >&2; exit 2; fi
TAG="${2:-}"
GH_USER="${3:-}"
echo "$TAG" | grep -Eq '^(latest|sha-[0-9a-f]{7,40})$' || { echo "Nieprawidłowy tag: $TAG" >&2; exit 2; }
echo "$GH_USER" | grep -Eq '^[A-Za-z0-9-]{1,39}$' || { echo "Nieprawidłowy użytkownik" >&2; exit 2; }

IMAGE="$REPO_IMAGE:$TAG"
PREVIOUS=$(grep -E '^APP_IMAGE=' .env | cut -d= -f2- || true)
log() { echo "$(date '+%F %T') $*" | tee -a "$LOG"; }

# Tylko jedno wdrożenie naraz.
exec 9>"$DIR/.deploy.lock"
flock -n 9 || { echo "Trwa inne wdrożenie" >&2; exit 3; }

# Usuwa wersje strony inne niż podane (każde wdrożenie zostawia obraz z tagiem sha-…).
remove_old_images() {
  docker images "$REPO_IMAGE" --format '{{.Repository}}:{{.Tag}}' | while read -r old; do
    case "$old" in "$1" | "${2:-}" | *:"<none>") continue ;; esac
    docker rmi "$old" >/dev/null 2>&1 && log "Usunięto stary obraz $old" || true
  done
  docker image prune -f >/dev/null 2>&1 || true
}

log "Wdrażam $IMAGE (poprzednio: ${PREVIOUS:-brak})"
remove_old_images "${PREVIOUS:-$IMAGE}" "$IMAGE"
docker login ghcr.io -u "$GH_USER" --password-stdin >/dev/null
trap 'docker logout ghcr.io >/dev/null 2>&1 || true' EXIT
docker pull -q "$IMAGE" >/dev/null
docker logout ghcr.io >/dev/null 2>&1 || true

set_image() {
  if grep -qE '^APP_IMAGE=' .env; then sed -i "s|^APP_IMAGE=.*|APP_IMAGE=$1|" .env; else echo "APP_IMAGE=$1" >> .env; fi
}
healthy() {
  i=0
  while [ $i -lt 60 ]; do
    if curl -fs -o /dev/null http://127.0.0.1:8082/healthz && curl -fs -o /dev/null http://127.0.0.1:8082/; then return 0; fi
    i=$((i + 1)); sleep 2
  done
  return 1
}

set_image "$IMAGE"
docker compose up -d >/dev/null 2>&1
if healthy; then
  log "OK: $IMAGE działa"
  # Zostają: bieżąca wersja i poprzednia (do ręcznego cofnięcia, patrz DEPLOY.md).
  remove_old_images "$IMAGE" "$PREVIOUS"
  exit 0
fi

log "BŁĄD: $IMAGE nie wstała – przywracam ${PREVIOUS:-?}"
docker compose logs --tail 60 web 2>&1 | tail -60
if [ -n "$PREVIOUS" ]; then
  set_image "$PREVIOUS"
  docker compose up -d >/dev/null 2>&1
  if healthy; then log "Przywrócono $PREVIOUS"; else log "UWAGA: poprzednia wersja też nie wstaje"; fi
fi
exit 1
