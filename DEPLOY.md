# Wdrożenie lionsoft.pl na mikr.us

Strona działa na tym samym serwerze co offroadly.pl i afiszownia (`beata212.mikrus.xyz`) i korzysta z Caddy z projektu Offroadly:

```
Internet ──▶ Cloudflare ──(IPv6, HTTPS, cert. Origin)──▶ Caddy :443 (projekt Offroadly) ─┬─▶ offroadly   127.0.0.1:8080
                                                                                        ├─▶ afiszownia  127.0.0.1:8081
                                                                                        ├─▶ lionsoft    127.0.0.1:8082  (nginx, pliki z site/)
                                                                                        └─▶ overlanding 127.0.0.1:8083
```

| Gdzie | Co |
|---|---|
| `/opt/lionsoft/deploy/` | `docker-compose.yml`, `deploy.sh`, `.env`, `deploy.log` (z katalogu `deploy/` tego repozytorium) |
| `/opt/offroadly/deploy/sites/lionsoft.caddy` | blok domeny dla Caddy (z `deploy/lionsoft.caddy`) |
| `/opt/offroadly/deploy/certs/lionsoft*.pem` | certyfikat Cloudflare Origin dla lionsoft.pl |

Push na `main` → GitHub Actions buduje obraz `ghcr.io/jakublewandowski/lionsoft:sha-<commit>` (nginx + `site/`) →
łączy się z serwerem kluczem, który może uruchomić **tylko** `deploy.sh`. Skrypt podmienia obraz, sprawdza `/healthz`,
a gdy nowa wersja nie wstanie w 2 minuty, wraca do poprzedniej. Kontener zużywa kilka MB RAM (limit 64 MB).

## Pierwsze uruchomienie

### 1. Domena w Cloudflare

Domena nie jest jeszcze w Cloudflare. Przeniesienie DNS nie wyłącza strony ani poczty, jeśli rekordy zostaną poprawnie
przepisane.

1. Cloudflare → *Add a domain* → `lionsoft.pl`, plan Free. Cloudflare sam zaimportuje obecne rekordy DNS.
2. **Porównaj zaimportowane rekordy z panelem obecnego dostawcy DNS**, zanim zmienisz serwery nazw. Najważniejsze są
   `MX`, `TXT` (SPF, DKIM, DMARC, weryfikacje Google itp.) i `CNAME`-y poczty (`autodiscover`, `mail`). Rekordy poczty
   muszą być **DNS only** (szara chmurka), inaczej poczta przestanie działać.
3. U rejestratora domeny podmień serwery DNS na dwa podane przez Cloudflare. Zmiana zwykle trwa od kilku minut do kilku godzin.
   Cloudflare wyśle e-mail, gdy strefa będzie aktywna. Do tego czasu strona działa jak dotąd ze starego serwera.
4. **SSL/TLS → Origin Server → Create Certificate** (RSA, hosty `lionsoft.pl, *.lionsoft.pl`, 15 lat). Na serwerze:
   ```bash
   nano /opt/offroadly/deploy/certs/lionsoft.pem       # certyfikat
   nano /opt/offroadly/deploy/certs/lionsoft-key.pem   # klucz prywatny
   chmod 600 /opt/offroadly/deploy/certs/lionsoft-key.pem
   ```
5. **SSL/TLS → Edge Certificates:** włącz *Always Use HTTPS*.

Rekordy `@` i `www` przepinasz na mikr.us dopiero w kroku 6, gdy strona już działa na serwerze.

### 2. Pliki na serwerze

Z komputera (PowerShell, w katalogu repozytorium):

```powershell
ssh -p 10212 root@beata212.mikrus.xyz "mkdir -p /opt/lionsoft"
scp -P 10212 -r deploy root@beata212.mikrus.xyz:/opt/lionsoft/
```

Na serwerze:

```bash
cd /opt/lionsoft/deploy
sed -i 's/\r$//' deploy.sh && chmod +x deploy.sh      # na wypadek końców linii z Windows
cp .env.example .env && chmod 600 .env
```

### 3. Domena w Caddy (wspólnym z Offroadly)

```bash
cp /opt/lionsoft/deploy/lionsoft.caddy /opt/offroadly/deploy/sites/lionsoft.caddy
cd /opt/offroadly/deploy
docker compose exec caddy caddy reload --config /etc/caddy/Caddyfile
```

Gdy w konfiguracji jest błąd (np. brak plików certyfikatu z kroku 1.4), `reload` go zgłosi i Caddy działa dalej
na poprzedniej konfiguracji, więc pozostałe strony nie przestaną działać.

### 4. Klucz SSH dla GitHub Actions

Osobny klucz, inny niż te od Offroadly i afiszowni. Na komputerze:

```powershell
ssh-keygen -t ed25519 -f lionsoft-deploy -N '""' -C github-actions-lionsoft-deploy
```

- `lionsoft-deploy.pub` dopisz na serwerze do `/root/.ssh/authorized_keys` jako **jedną linię** z ograniczeniem:
  ```
  command="/opt/lionsoft/deploy/deploy.sh",no-port-forwarding,no-agent-forwarding,no-X11-forwarding,no-pty ssh-ed25519 AAAA… github-actions-lionsoft-deploy
  ```
- zawartość `lionsoft-deploy` (klucz prywatny) wpisz na GitHubie: repozytorium → *Settings → Environments →
  production* → *Add secret* `DEPLOY_SSH_KEY`.
- usuń oba pliki z komputera.

Bez sekretu `DEPLOY_SSH_KEY` workflow tylko buduje obraz i pomija wdrożenie, więc push przed tym krokiem jest bezpieczny.

### 5. Start

GitHub → *Actions* → **Build i deploy** → *Run workflow* (gałąź `main`). Sprawdzenie na serwerze:

```bash
cat /opt/lionsoft/deploy/deploy.log                         # „OK: ghcr.io/…:sha-… działa”
curl -s http://127.0.0.1:8082/healthz                       # ok
curl -sk --resolve lionsoft.pl:443:127.0.0.1 https://lionsoft.pl/ | head -5   # strona przez Caddy
```

### 6. Przełączenie domeny

Cloudflare → **DNS** → rekordy `@` i `www`: usuń stare `A`/`AAAA`/`CNAME` wskazujące na poprzedni hosting i dodaj
`AAAA @` oraz `AAAA www` z **tym samym IPv6 co offroadly.pl** (podgląd w strefie offroadly.pl albo
`ip -6 addr show scope global` na serwerze), oba **Proxied**. Następnie **SSL/TLS → Overview:** tryb **Full (strict)**.

Rekordy są *Proxied*, więc zmiana działa od razu. Sprawdź `https://lionsoft.pl` i `https://lionsoft.pl/en/`.
Stary hosting możesz wyłączyć po kilku dniach (poczty na nim nie ruszaj, jeśli tam jest).

## Aktualizacje i cofanie wersji

- **Automatycznie:** push na `main`. Historia: zakładka *Actions* i `/opt/lionsoft/deploy/deploy.log`.
- **Powrót do wersji** (na serwerze):
  ```bash
  cd /opt/lionsoft/deploy
  APP_IMAGE=ghcr.io/jakublewandowski/lionsoft:sha-abc1234 docker compose up -d web
  ```
- **Zmiany w `deploy/docker-compose.yml`, `deploy.sh` lub `lionsoft.caddy` nie wdrażają się same.** Wgraj plik
  `scp` jak w kroku 2 i zrób `docker compose up -d` (albo `caddy reload` jak w kroku 3).
- Cloudflare buforuje obrazki i CSS/JS. Jeśli po wdrożeniu widać starą wersję, użyj *Caching → Purge Everything*.

## Kopie zapasowe

Nie są potrzebne. Cała strona jest w repozytorium, a obraz każdej wersji w GHCR.
