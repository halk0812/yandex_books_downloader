# Yandex Books Downloader

Загрузка книг с [books.yandex.ru](https://books.yandex.ru) и сохранение их в файл формата **EPUB**,
опционально — конвертация в **MOBI** и **PDF** средствами Calibre.

Реализация: **C# / .NET 10** — веб-интерфейс с автоматическим входом через Playwright и консольная утилита.

> **Правовая информация.** Используйте инструмент только для личного пользования. Для платных книг
> нужна подписка [Яндекс Плюс](https://plus.yandex.ru/), либо книга должна быть доступна бесплатно.
> Автор инструмента не несёт ответственности за нарушение правообладателей.

## Возможности

- **Веб-интерфейс** — вход в Яндекс выполняется в реальном окне браузера (Playwright), cookie
  `Session_id` перехватывается автоматически, копировать его руками не нужно
- **CLI** — полностью автоматизированная загрузка по `bookid`
- Расшифровка метаданных книги (AES-CBC), сборка валидного EPUB
- Конвертация EPUB → MOBI/PDF через [Calibre](https://calibre-ebook.com/) (`ebook-convert`),
  если он установлен; без Calibre EPUB всё равно создаётся
- Покрытие модульными тестами (xUnit), CI (GitHub Actions)

## Как это работает

1. Запрос `GET /reader/p/api/v5/metadata_secret?lang=ru` — сервер возвращает AES-ключ в base64.
2. Запрос `GET /p/api/v5/books/{bookid}/metadata/v4` — JSON, в котором поля `container`, `opf`,
   `ncx` и др. лежат зашифрованными массивами байтов.
3. Дешифрование AES-CBC: первые 16 байт — IV, остальные — шифротекст, отбрасывается PKCS7-паддинг.
4. Сохраняются `mimetype`, `META-INF/container.xml`, `OEBPS/content.opf`, `OEBPS/toc.ncx`.
5. Из манифеста `content.opf` берутся все `<item href=...>` и каждый скачивается с
   `/p/a/4/d/{document_uuid}/contents/OEBPS/{href}`.
6. CSS-файлы обнуляются (стили Яндекса ломают вёрстку в сторонних читалках).
7. Каталог упаковывается в EPUB: запись `mimetype` идёт первой и без сжатия (STORED),
   остальные файлы — Deflate.
8. При включённой конвертации к готовому EPUB применяется `ebook-convert` из состава Calibre.

## Структура репозитория

```text
cs/
  YandexBooksDownloader.slnx      решение
  Directory.Build.props           общие настройки сборки и анализаторы
  Directory.Packages.props        централизованные версии пакетов
  Dockerfile                      Docker-образ CLI-версии
  src/
    YandexBooksDownloader.Core/   библиотека: криптография, метаданные, загрузка, EPUB, конвертация
    YandexBooksDownloader.Cli/    консольное приложение (yandex-books-downloader)
    YandexBooksDownloader.Web/    веб-интерфейс + Playwright-вход
  tests/
    YandexBooksDownloader.Tests/  модульные тесты (xUnit + FluentAssertions)
.github/workflows/dotnet.yml      CI: сборка и тесты на .NET 10
```

## Системные требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Для веб-интерфейса: графическая среда (браузер с окном) либо `YBD_HEADLESS=1`
- Опционально: [Calibre](https://calibre-ebook.com/) — для конвертации в MOBI/PDF (см. ниже)

## Calibre (конвертация в MOBI и PDF)

Конвертер не входит в состав приложения и вызывается как внешний процесс `ebook-convert`.
Приложение само находит Calibre в типовых местах установки; если он не найден,
EPUB создаётся как обычно, а вместо MOBI/PDF выдаётся предупреждение.

### Windows

- Установите инсталлятор с [calibre-ebook.com](https://calibre-ebook.com/download/windows), либо:

```powershell
winget install --id calibre.calibre
```

- Приложение ищет `ebook-convert.exe` в `%ProgramFiles%\Calibre2\` и в `PATH`.

### Linux

```bash
# Debian / Ubuntu
sudo apt install calibre

# Fedora
sudo dnf install calibre

# Arch
sudo pacman -S calibre
```

- После установки `ebook-convert` доступен в `PATH` — дополнительных настроек не нужно.

### macOS

```bash
brew install --cask calibre
```

(или установите `.dmg` с [calibre-ebook.com](https://calibre-ebook.com/download/osx))

- Приложение ищет утилиту в `/Applications/calibre.app/Contents/MacOS/ebook-convert`.
- Если Calibre установлен в другое место, укажите путь явно:

```bash
export YBD_CALIBRE=/Applications/calibre.app/Contents/MacOS/ebook-convert
```

### Переменная YBD_CALIBRE

| Переменная | По умолчанию | Значение |
|---|---|---|
| `YBD_CALIBRE` | — | полный путь к исполняемому файлу `ebook-convert` (используется в первую очередь) |

## Веб-интерфейс (рекомендуется)

```bash
cd cs
dotnet run --project src/YandexBooksDownloader.Web
```

Откройте напечатанный адрес — по умолчанию **http://localhost:5134**.

Порядок работы:

1. Нажмите **«Войти через браузер»** — откроется окно Playwright-браузера на books.yandex.ru.
   Войдите в свой аккаунт Яндекса как обычно.
2. Как только в браузере появится cookie `Session_id`, он будет перехвачен автоматически,
   а статус смены на «авторизован».
3. Введите `bookid` книги (см. ниже, как его найти), при необходимости отметьте
   **«Конвертировать в MOBI» / «Конвертировать в PDF»** и нажмите **«Скачать»**.
   Если Calibre не установлен, чекбоксы конвертации отключаются с подсказкой.
4. Дождитесь окончания и скачайте файлы по появившимся ссылкам.

Профиль браузера сохраняется в `%LOCALAPPDATA%\YandexBooksDownloader\browser-profile`
(`~/.local/share` / `~/Library` на Linux/macOS), поэтому повторно входить обычно не нужно.

Файлы книг сохраняются в папку `out/` рядом с проектом Web
(`cs/src/YandexBooksDownloader.Web/out/<bookid>.epub|.mobi|.pdf`) и доступны по ссылкам в интерфейсе.

### Переменные окружения

| Переменная | По умолчанию | Значение |
|---|---|---|
| `YBD_BROWSER` | `chromium` | `chromium`, `msedge` или `chrome` — какой браузер использовать для входа |
| `YBD_LOGIN_TIMEOUT_SECONDS` | `300` | сколько секунд ждать завершения входа в окне браузера |
| `YBD_HEADLESS` | — | `1` — запускать браузер входа без окна (для тестов/сервера) |
| `YBD_CALIBRE` | — | путь к `ebook-convert` для конвертации в MOBI/PDF |

При первом входе с `chromium` приложение автоматически скачает браузер Playwright (~200 МБ).
Если загрузка браузеров недоступна (корпоративный прокси), установите `YBD_BROWSER=msedge`
или `YBD_BROWSER=chrome` — будет использован уже установленный в системе браузер.

### HTTP API

Веб-интерфейс построен на этих эндпоинтах (можно использовать напрямую):

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/api/session` | статус входа: `{"loggedIn": true/false}` |
| `GET` | `/api/capabilities` | доступность конвертера: `{"calibre": true/false}` |
| `POST` | `/api/login` | открыть браузер для входа и перехватить `Session_id` (408 — не успели войти) |
| `POST` | `/api/logout` | забыть сохранённую сессию |
| `POST` | `/api/download` | тело `{"bookId":"<id>","mobi":false,"pdf":false}` — скачать книгу, собрать EPUB и, при запросе, сконвертировать |
| `GET` | `/api/book/{bookId}/{format}` | скачать готовый файл: `format` — `epub`, `mobi` или `pdf` |

Ответ `/api/download`: `{"formats":{"epub":"/api/book/<id>/epub",...},"warnings":[...]}` —
`warnings` содержит сообщения, если конвертация была запрошена, но не выполнена.

## CLI

```bash
cd cs
dotnet run --project src/YandexBooksDownloader.Cli -- --bookid KFHDG3bp
```

Результат — `out/KFHDG3bp.epub`.

### Параметры

| Параметр | По умолчанию | Описание |
|---|---|---|
| `--bookid <id>` | — | id книги из URL (обязательно) |
| `--outdir <dir>` | `out` | выходная папка |
| `--log <level>` | `INFO` | `TRACE`, `DEBUG`, `INFO`, `WARNING`, `ERROR`, `CRITICAL` |
| `--download <true\|false>` | `true` | скачивать файлы книги |
| `--delete_css <true\|false>` | `true` | обнулить CSS перед упаковкой |
| `--make_epub <true\|false>` | `true` | упаковать скачанное в EPUB |
| `--delete_downloaded <true\|false>` | `true` | удалить распакованный каталог после упаковки |
| `--mobi <true\|false>` | `false` | сконвертировать EPUB в MOBI (нужен Calibre) |
| `--pdf <true\|false>` | `false` | сконвертировать EPUB в PDF (нужен Calibre) |

Значения можно задавать в двух формах: `--flag value` и `--flag=value`.

### Аутентификация CLI

CLI не открывает браузер — ему нужен cookie `Session_id`:

- задайте переменную окружения `SESSION_ID=<значение cookie>`, либо
- оставьте переменную пустой — программа запросит cookie в консоли
  (как его получить — см. ниже).

### Примеры

```bash
# подробный лог, другая папка вывода
dotnet run --project src/YandexBooksDownloader.Cli -- --bookid KFHDG3bp --log DEBUG --outdir D:\books

# не удалять распакованные файлы книги (останутся в out/KFHDG3bp/)
dotnet run --project src/YandexBooksDownloader.Cli -- --bookid KFHDG3bp --delete_downloaded false

# сразу собрать EPUB + MOBI + PDF
dotnet run --project src/YandexBooksDownloader.Cli -- --bookid KFHDG3bp --mobi true --pdf true

# через переменную окружения (PowerShell)
$env:SESSION_ID = "<Session_id>"; dotnet run --project src/YandexBooksDownloader.Cli -- --bookid KFHDG3bp
```

## Сборка и тесты

```bash
cd cs
dotnet build YandexBooksDownloader.slnx -c Release
dotnet test YandexBooksDownloader.slnx -c Release
```

## Docker (CLI)

```bash
docker build -f cs/Dockerfile -t ya_books_dl .
docker run -it --mount type=bind,source=$(pwd),target=/data \
  -e SESSION_ID=<ваш_session_id> \
  ya_books_dl --bookid KFHDG3bp --log DEBUG --outdir /data
```

В Docker-образ Calibre не включён — флаги `--mobi`/`--pdf` в контейнере работают только
при установке Calibre в собственный образ поверх базового.

## Как найти bookid

1. Откройте книгу на [books.yandex.ru](https://books.yandex.ru).
2. ID находится в URL: `https://books.yandex.ru/reader/<BOOKID>/...`
   (например, `KFHDG3bp`).

## Как получить cookie Session_id (для CLI)

1. Войдите в [books.yandex.ru](https://books.yandex.ru) в браузере.
2. Откройте консоль разработчика (F12) → вкладка **Application** (Приложение).
3. Слева выберите **Cookies** → `https://books.yandex.ru`.
4. Найдите `Session_id` и скопируйте значение.

## Устранение неполадок

| Симптом | Решение |
|---|---|
| `Executable doesn't exist ...` при входе | приложение скачает Chromium само; если скачивание заблокировано сетью — `YBD_BROWSER=msedge` или `YBD_BROWSER=chrome` |
| `408` при входе в веб-интерфейсе | не успели войти в окно браузера — увеличьте `YBD_LOGIN_TIMEOUT_SECONDS` |
| `401 Not logged in` при скачивании | сессия протекла — нажмите «Выйти» и войдите заново |
| Скачивание книги падает с `403/404` | книга недоступна вашему аккаунту (нет подписки / региональные ограничения) |
| CLI: `Request ... failed with status 401` | cookie `Session_id` устарел — получите его заново |
| `Calibre ebook-convert was not found` | установите Calibre (см. выше) или задайте `YBD_CALIBRE=<путь к ebook-convert>` |
| `ebook-convert failed with exit code ...` | смотрите вывод `ebook-convert` в предупреждении; часто помогает обновление Calibre |
| EPUB не открывается в читалке | попробуйте Calibre ([calibre-ebook.com](https://calibre-ebook.com/)) для проверки и конвертации |

## Ограничения

- Скачиваются только книги, доступные вашему аккаунту (подписка Яндекс Плюс или бесплатные).
- Конвертация в MOBI/PDF требует установленного Calibre; без него создаётся только EPUB.
- Сторонние сервисы Яндекса могут менять API — при поломке проверьте, что версия актуальна.
- CSS-стили намеренно обнуляются: форматирование книг обеспечивает читалка.

## Лицензия

MIT — см. [LICENSE](LICENSE).

C# / .NET 10 версия: [halk0812](https://github.com/halk0812).
