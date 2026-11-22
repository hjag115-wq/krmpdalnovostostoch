КАК ПОЛУЧИТЬ EXE И APK (без установки программ):
1. Создайте репозиторий на github.com и загрузите в него ВСЁ содержимое этой папки (включая .github).
2. Вкладка Actions -> дождитесь зелёной галочки (3-5 минут).
3. Откройте запуск -> внизу Artifacts: DVLauncher-exe (Windows) и DVLauncher-apk (Android).

ПЕРЕД СБОРКОЙ поправьте:
- windows/Program.cs: ссылки VK/Discord, GameExe (если клиент не samp.exe)
- android/.../MainActivity.kt и AndroidManifest.xml: GAME_PACKAGE (package name клиента), DOWNLOAD_URL
