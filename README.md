# Brainrot Ireland

Unity6 URP survival arena: Tung Tung Sahur против акул Tralala.

## Как открыть

1. На этой странице нажмите **Code → Download ZIP** и распакуйте архив.
2. В Unity Hub нажмите **Add** и выберите вложенную папку **Brainrot Ireland** (в ней находятся Assets, Packages, ProjectSettings).
3. Используйте Unity **6000.3.11f1**.
4. В Unity выполните **Tools → TungTung → Build Game**.
5. Откройте **Assets/Scenes/BrainrotIreland.unity**, нажмите **Play**, затем **PLAY** в меню игры.

[Подробная инструкция](<Brainrot Ireland/README.md>) · [Дорожная карта](<Brainrot Ireland/ROADMAP.md>)

Реализованы процедурный остров, персонажи, бой, три типа акул, XP/12 улучшений, мини-боссы, KING TRALALA, меню, сохранения и мобильные контролы.

**Статическая проверка кода выполнена. Unity Editor в среде разработки отсутствует: компиляция, PlayMode tests и EXE/APK ещё не подтверждены.**

Существующие сцены и ресурсы сохранены. Игра поддерживает существующий Input System и Legacy Input Manager без изменения настроек ввода.

## Если Unity вылетает

Разместите вложенную папку проекта в `C:\UnityProjects\BrainrotIreland` и запустите **START_UNITY_DX11.bat** из неё. [Инструкция по сбою DirectX12 и длинным путям](<Brainrot Ireland/TROUBLESHOOTING.md>).

## Обновление боя и настройка баланса

**V** включает/выключает locked camera: можно отходить и продолжать бить по направлению камеры. Появились сектор дальности, подтверждение попадания, мягкая красная вспышка и отбрасывание. Акулы готовят атаки, быстрые делают прямые рывки. Стартовый спавн спокойнее, уровни требуют всё больше XP.

**Tools → TungTung → Game Settings** — редактор профиля баланса. [Инструкция](<Brainrot Ireland/GAME_SETTINGS.md>). В работающей копии достаточно объединить папку Assets/Brainrot из свежего архива со своей и дождаться компиляции; существующие Generated и Assets/Resources/Upgrades сохраняйте. Новая версия проверена статически, Unity tests ожидают запуска.
