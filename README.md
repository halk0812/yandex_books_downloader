# Yandex Books Downloader

Загрузка книг с [books.yandex.ru](https://books.yandex.ru) и сохранение их в файл формата **EPUB**.

Реализация: **C# / .NET 10** — веб-интерфейс с автоматическим входом через Playwright и консольная утилита.

> **Правовая информация.** Используйте инструмент только для личного пользования. Для платных книг
> нужна подписка [Яндекс Плюс](https://plus.yandex.ru/), либо книга должна быть доступна бесплатно.
> Автор инструмента не несёт ответственности за нарушение правообладателей.

## Возможности

- **Веб-интерфейс** — вход в Яндекс выполняется в реальном окне браузера (Playwright), cookie
  `Session_id` перехватывается автоматически, копировать его руками не нужно
- **CLI** — полностью автоматизированная загрузка по `bookid`
- Расшифровка метаданных книги (AES-CBC), сборка валидного EPUB
- Покрытие модульными тестами, CI (GitHub Actions)

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

## Структура репозитория

```text
cs/
  YandexBooksDownloader.slnx      решение
  Dockerfile                      Docker-образ CLI-версии
  src/
    YandexBooksDownloader.Core/   библиотека: криптография, метаданные, загрузка, EPUB
    YandexBooksDownloader.Cli/    консольное приложение (yandex-books-downloader)
    YandexBooksDownloader.Web/    веб-интерфейс + Playwright-вход
  tests/
    YandexBooksDownloader.Tests/  модульные тесты (MSTest)
.github/workflows/dotnet.yml      CI: сборка и тесты на .NET 10
```

## Системные требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Для веб-интерфейса: графическая среда (браузер с окном) либо `YBD_HEADLESS=1`

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
3. Введите `bookid` книги (см. ниже, как его найти) и нажмите **«Скачать epub»**.
4. Дождитесь окончания и скачайте EPUB по появившейся ссылке.

Профиль браузера сохраняется в `%LOCALAPPDATA%\YandexBooksDownloader\browser-profile`
(`~/.local/share` / `~/Library` на Linux/macOS), поэтому повторно входить обычно не нужно.

EPUB-файлы сохраняются в папку `out/` рядом с проектом Web
(`cs/src/YandexBooksDownloader.Web/out/<bookid>.epub`) и доступны по ссылке в интерфейсе.

### Переменные окружения

| Переменная | По умолчанию | Значение |
|---|---|---|
| `YBD_BROWSER` | `chromium` | `chromium`, `msedge` или `chrome` — какой браузер использовать для входа |
| `YBD_LOGIN_TIMEOUT_SECONDS` | `300` | сколько секунд ждать завершения входа в окне браузера |
| `YBD_HEADLESS` | — | `1` — запускать браузер входа без окна (для тестов/сервера) |

При первом входе с `chromium` приложение автоматически скачает браузер Playwright (~200 МБ).
Если загрузка браузеров недоступна (корпоративный прокси), установите `YBD_BROWSER=msedge`
или `YBD_BROWSER=chrome` — будет использован уже установленный в системе браузер.

### HTTP API

Веб-интерфейс построен на этих эндпоинтах (можно использовать напрямую):

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/api/session` | статус входа: `{"loggedIn": true/false}` |
| `POST` | `/api/login` | открыть браузер для входа и перехватить `Session_id` (408 — не успели войти) |
| `POST` | `/api/logout` | забыть сохранённую сессию |
| `POST` | `/api/download` | тело `{"bookId":"<id>"}` — скачать книгу и собрать EPUB |
| `GET` | `/api/epub/{bookId}` | скачать готовый EPUB |

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
| EPUB не открывается в читалке | попробуйте Calibre ([calibre-ebook.com](https://calibre-ebook.com/)) для проверки и конвертации |

## Ограничения

- Скачиваются только книги, доступные вашему аккаунту (подписка Яндекс Плюс или бесплатные).
- Сторонние сервисы Яндекса могут менять API — при поломке проверьте, что версия актуальна.
- CSS-стили намеренно обнуляются: форматирование книг обеспечивает читалка.

## Лицензия

MIT — см. [LICENSE](LICENSE).

C# / .NET 10 версия: [halk0812](https://github.com/halk0812).
