# Strona lionsoft.pl – statyczne pliki z site/ serwowane przez nginx (ok. 5 MB RAM).
# Budowany przez GitHub Actions (.github/workflows/deploy.yml), wdrażany przez deploy/deploy.sh (DEPLOY.md).
FROM nginx:1.27-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY site/ /usr/share/nginx/html/
# Sprawdzenie konfiguracji już przy budowaniu – błąd zatrzymuje wdrożenie.
RUN nginx -t
