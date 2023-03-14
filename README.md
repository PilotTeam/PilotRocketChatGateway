# PilotRocketChatGateway

Компонент для интеграции систем Pilot и Rocket.Chat.

Для подключения к PilotRocketChatGateway необходимо скачать официальное приложение Rocket.Chat из  Apple Store (`https://apps.apple.com/ru/app/rocket-chat/id1148741252`) или Google play (`https://play.google.com/store/apps/details?id=chat.rocket.android&hl=ru&gl=US`). Так же необходимо настроить https соединение.

## Настройка компонента

Перед запуском шлюза необходимо в appsettings.json заполнить поля:

#### Подключение к Pilot-Server
```
"PilotServer": {
    "Url": "http://localhost:5545",
    "Database": "demo",
    "LicenseCode": 103
  },
```
где:\
`Url` - адрес подключения к серверу Pilot-Server.\
`Database` - имя базы данных, к которой осуществляется подключение.\
`LicenseCode` - номер лицензии

#### Авторизация
Данные, по которым сгенерируется токен авторизации:
```
"AuthSettings": {
    "Issuer": "PilotRocketChatGatewayIssuer",
    "SecretKey": "SecretKey@30824995-BD42-4850-87ED-EE8A2AE06ACA"
  }
```
где:\
`Issuer` - имя издателя токена авторизации.\
`SecretKey` - секретный ключ для формирования токена. Должен содержать различные символы и цифры.

#### Регистрация шлюза в RocketChatCloud
Для пуш-нотификаций необходимо зарегистрировать шлюз в RocketChatCloud. Для это нужно заполнить поля:
```
"RocketChatCloud": {
    "RegistrationToken": "04c1f83b-1c00-4864-b6e5-9ffda1e64e61",
    "WorkspaceName": "",
    "WorkspaceEmail": "",
    "WorkspaceUri": ""
  }
```
где:\
`RegistrationToken` - токен регистрации; чтобы получить, нужно:
- прейти по адресу `https://cloud.rocket.chat`.
- авторизоваться.
- на вкладке Workspaces нажать на кнопку Register self-managed.

`WorkspaceName` - произвольное имя шлюза.\
`WorkspaceEmail` - почта, указанная при авторизации в RocketChatCloud.\
`WorkspaceUri` -  адрес подключения шлюза.

Также для регистрации в RocketChatCloud необходимо дать права шлюзу на запись в папку `/ProgramData` (для Win) или `/usr/share` (linux)

## Запуск на на linux

Для работы PilotRocketChatGateway на linux необходимо установить компонент libgdiplus командами:
```bash
sudo apt update
sudo apt install libgdiplus
```