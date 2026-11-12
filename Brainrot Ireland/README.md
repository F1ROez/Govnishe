# Brainrot Ireland

Исходный Unity6 URP проект для Windows и Android, интегрированный в F1ROez/Govnishe. Существующие сцены, настройки URP и пользовательские ресурсы сохранены. Игра поддерживает существующий Input System и Legacy Input Manager; настройки ввода сохранены.

## Быстрый запуск

1. Распакуйте проект и в Unity Hub выберите Add → папку с Assets, Packages и ProjectSettings.
2. Установите Unity **6000.3.11f1**, Windows Build Support (IL2CPP или Mono); для Android также Android Build Support, SDK/NDK/OpenJDK.
3. Откройте проект и дождитесь восстановления пакетов. При первом импорте Editor автоматически создаёт URP pipeline.
4. Выполните **Tools → TungTung → Build Game**. Команда создаёт/обновляет сцену, prefab, материалы и12 ScriptableObject улучшений; повторный запуск не добавляет дубликаты. Сохраните свои изменения сцены по запросу Unity.
5. Откройте **Assets/Scenes/BrainrotIreland.unity**, нажмите **Play**, затем **PLAY** в меню.

Сцена уже входит в проект и содержит bootstrap; остров, модели и UI создаются при Play, поэтому в Edit mode сцена выглядит пустой.

WASD — движение; мышь — камера; ЛКМ — бита; Shift — бег; Space — рывок; Escape — пауза. На Android появляются сенсорные контролы, ориентация landscape.

Обычные/быстрые/танки появляются из океана. Мини-босс на2-й минуте, KING TRALALA на5-й. XP выдаётся напрямую за убийства;12 улучшений и очередь выбора. Огонь пока реализован как бонус урона и оранжевые попадания, без отдельного горения во времени. Рекорды автоматически сохраняются после смерти.

## Проверки и сборки

**Здесь Unity не запускался. Компиляция, PlayMode и реальные сборки ещё не подтверждены.**

Откройте Window → General → Test Runner → PlayMode → Run All. Есть тесты убийства/XP/pooling, очереди уровней, паузы и нового забега. Тесты очищают тестовую сцену — не запускайте их на несохранённой рабочей сцене.

Статическая проверка (Python3):
```
python -m pip install tree-sitter tree-sitter-c-sharp
python Tools/validate_source.py
```

Batchmode из PowerShell (замените путь к Unity):
```
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe'
& $unity -batchmode -projectPath $PWD -executeMethod Brainrot.Editor.GameBuilder.Validate -quit -logFile validation.log
& $unity -batchmode -projectPath $PWD -runTests -testPlatform PlayMode -testResults results.xml -logFile tests.log
& $unity -batchmode -projectPath $PWD -buildTarget Win64 -executeMethod Brainrot.Editor.GameBuilder.BuildWindows -quit -logFile windows-build.log
```
В Unity доступны **Build Windows** и **Build Android** в Tools/TungTung. Результаты: Builds/Windows/BrainrotIreland.exe или Builds/Android/BrainrotIreland.apk. Android требует платформенный модуль и установленный SDK. После переключения платформы дождитесь reimport.

Ручной smoke test: новый забег → убийства → три последовательных level-up → пауза/возобновление → мини-босс120с → KING300с со всеми4 способностями → смерть → запись рекорда → перезапуск приложения → рекорд сохранён → новый забег сбрасывает врагов/статы. На Android проверьте мультитач, сворачивание, нагрев и FPS.

## Ограничения текущей версии

Процедурные временные персонажи, простая компонентная анимация и синтезированные удары. Нет импортированных ригов, музыки, полноценного art/audio polish, проверенного баланса или измеренного FPS. Вода анимируется шейдером; бесконечный океан визуально имитируется большой плоскостью и туманом. Декорации пропускают игрока для дешёвого AI без NavMesh.

Игрок поворачивается в направлении движения, атака направлена вперёд персонажа. UI на английском. Настройки качества используют доступные Unity Quality levels; отдельная настройка URP для слабых устройств — следующая задача профилирования.

## Репозиторий

Проект находится в папке `Brainrot Ireland` репозитория. Скачайте ветку через Code → Download ZIP, распакуйте и добавьте именно эту вложенную папку в Unity Hub. Существующие SampleScene и efef сохранены; игровая сцена — Assets/Scenes/BrainrotIreland.unity.

Версия Unity и package manifest оставлены из исходного репозитория: Unity6000.3.11f1 / URP17.3.0. Существующие настройки сохраняются, включая activeInputHandler: игра автоматически поддерживает выбранный backend ввода. Build Game также настраивает URP и Build Settings.

AGENTS.md — правила; ARCHITECTURE.md — связи компонентов; ROADMAP.md — готовность; DEVELOPMENT_LOG.md — фактические проверки.
