# Matrix Registration Token Bot

Бот для создания токенов регистрации в Matrix-сети через команды в чате.
Таким образом реализуется регистрация через "сарафанное радио", когда пользователи приглашают своих друзей.
Работает по максимально наивному сценарию, без дополнительных проверок.

## Описание

Бот подключается к Matrix-серверу, отслеживает сообщения в комнатах и обрабатывает команды создания токенов регистрации. При получении валидной команды создаёт новый токен регистрации в Matrix Authentication Service (MAS) и отправляет его пользователю в чат.

## Возможности

- Обработка команд создания токенов: `!token`, `!t`, `!tkn` (достаточно отправить сообщение, начинающееся на одну из этих команд)
- Автоматическое создание токена регистрации через Matrix Authentication Service
- Установка срока действия токена — 24 часа
- Настраиваемый лимит использования токена
- Отправка токена пользователю в чат
- Принятие только прямых приглашений (1 на 1 без шифрования)
- Сохранение токена синхронизации для восстановления состояния
- Логирование через Serilog (консоль + файл)

## Требования

- .NET 10.0
- AOT-компиляция для оптимальной производительности
- Docker (опционально, для контейнеризации)
- Matrix Authentication Service (MAS)

## Переменные окружения

| Переменная | Описание |
|------------|----------|
| `MATRIX_HOMESERVER_URL` | URL homeserver'а (домен) |
| `MATRIX_BOT_USER_LOGIN` | Логин бота в Matrix |
| `MATRIX_BOT_USER_PASSWORD` | Пароль бота в Matrix |
| `MATRIX_BOT_BATCH_TOKEN_PATH` | Путь к файлу для сохранения токена синхронизации |
| `MATRIX_BOT_MAX_MESSAGE_AGE_MS` | Максимальный возраст сообщений для обработки (мс, по умолчанию 14400000) |
| `MATRIX_BOT_USER_TIMEOUT` | Таймаут для longpolling запросов ожидания сообщений (мс, по умолчанию 30000) |
| `MATRIX_BOT_ADMIN_BASIC_AUTH` | Basic авторизация для доступа к админке MAS |
| `MATRIX_BOT_TOKEN_USAGE_LIMIT` | Лимит использования одного токена (по умолчанию 1) |
| `MATRIX_REGISTRATION_ROOM_KEY` | Ключ комнаты для уведомлений о создании токенов |

## Использование

### Формат команд

```
!token
```

Достаточно отправить сообщение, начинающееся на `!token`, `!t` или `!tkn`. Весь остальной текст после команды игнорируется.

Бот ответит:
- Создан токен **abc123...**. Срок действия: **2026-04-04 18:01:56 UTC**.
- `abc123...` (токен отдельным сообщением для удобного копирования)

## Сборка и запуск

### Локальный запуск

```bash
dotnet run
```

### Сборка Native AOT

```bash
dotnet publish -c Release -r linux-musl-x64
```

### Docker

```bash
docker build -t matrix-registration-token-bot .
docker run -d \
  -e MATRIX_BOT_USER_LOGIN=bot_login \
  -e MATRIX_BOT_USER_PASSWORD=bot_password \
  -e MATRIX_BOT_BATCH_TOKEN_PATH=/data/token.txt \
  -e MATRIX_HOMESERVER_URL=matrix.example.com \
  -e MATRIX_BOT_USER_TIMEOUT=30000 \
  -e MATRIX_BOT_ADMIN_BASIC_AUTH=admin_password \
  -e MATRIX_BOT_TOKEN_USAGE_LIMIT=1 \
  -e MATRIX_REGISTRATION_ROOM_KEY="!room:matrix.example.com" \
  -v /path/to/data:/data \
  matrix-registration-token-bot
```

## Зависимости

- [Serilog](https://serilog.net/) — логирование
- [Polly](https://github.com/App-vNext/Polly) — обработка временных ошибок

## Лицензия

См. файл [LICENSE](LICENSE).

## Автор ридми
Qwen Code 0.13.1
