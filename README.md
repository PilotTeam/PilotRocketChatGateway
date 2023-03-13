# PilotRocketChatGateway

Компонент для интеграции систем Pilot и Rocket.Chat.

Для подключения к PilotRocketChatGateway необходимо скачать официальное приложение Rocket.Chat из магазина телефона. Система android будет работать только с безопасным https соединением; для ios хватит http.

Перед запуском шлюза необходимо в appsettings.json заполнить поля:
- Pilot-Server: подключение к Pilot-Server;
- AuthSettings: данные для генерации токена авторизации;
- RocketChatCloud: настройка регистрации шлюза в RocketChatCloud для пуш-нотификаций; чтобы получить RegistrationToken, нужно открыть в браузере адрес https://cloud.rocket.chat, авторизоваться, затем на вкладке Workspaces нажать на кнопку Register self-managed. Также для регистрации в RocketChatCloud необходимо дать права шлюзу на запись в папку /ProgramData (для Win) или /usr/share/ (linux)

Для работы PilotRocketChatGateway на linux необходимо установить компонент libgdiplus командами:
sudo apt update
sudo apt install libgdiplus