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
Для пуш-нотификаций необходимо зарегистрировать шлюз в RocketChatCloud. Для это нужно перерейти по адресу `https://cloud.rocket.chat` и авторизоваться, затем заполнить поля:
```
"RocketChatCloud": {
    "WorkspaceName": "",
    "WorkspaceEmail": "",
    "WorkspaceUri": "",
    "HidePushInfo": false
  }
```
где:\
`WorkspaceEmail` - почта, указанная при авторизации в RocketChatCloud.\
`WorkspaceName` - произвольное имя шлюза.\
`WorkspaceUri` -  адрес подключения шлюза.\
`HidePushInfo` - укажите значение true, если не хотите отправлять информацию о сообщении в пуш-нотификации на сервер RocketChatCloud. Значение по-умолчанию false. 

Затем необходимо необходимо дать права шлюзу на запись в папку `/ProgramData` (для Win) или `/usr/share` (linux). После этого запустите шлюз, откройте почту и подтвердите регистрацию в RocketChatCloud. 

Информация о успешной регистрации или об ошибках будет указана в лог-файле.

## Запуск на на linux

Для работы PilotRocketChatGateway на linux необходимо установить компонент libgdiplus командами:
```bash
sudo apt update
sudo apt install libgdiplus
```