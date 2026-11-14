using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Brainrot.Editor
{
    /// <summary>Russian tuning UI without exposing obsolete wave and boss parameters.</summary>
    [CustomEditor(typeof(GameBalance))]
    public class GameBalanceInspector : UnityEditor.Editor
    {
        static readonly HashSet<string> Hidden = new HashSet<string> {
            "m_Script", "Spawning", "Bosses", "Fast", "Tank", "MiniBoss", "King", "Exploration", "Progression", "CorpseSeconds", "ChainPush", "HeavyDamageMultiplier", "HeavyCooldownMultiplier", "Lirili", "Patapim", "Cappuccino", "Crocodilo"
        };
        static readonly Dictionary<string, string> Labels = new Dictionary<string, string> {
            {"Id","Идентификатор"},{"DisplayName","Название"},{"VisualStyle","Внешний вид"},{"BodyColor","Цвет модели"},{"VisualScale","Масштаб модели"},
            {"UseCustomStats","Собственные характеристики"},{"StartingStats","Характеристики персонажа"},{"Weapon","Оружие"},{"Abilities","Особые способности"},
            {"UseCustomTiming","Собственные настройки взмаха"},{"DamageMultiplier","Множитель урона"},{"RangeMultiplier","Множитель дальности"},{"AttackIntervalMultiplier","Множитель интервала атак"},
            {"Kind","Вид улучшения"},{"Title","Название"},{"Description","Описание"},{"Rarity","Редкость"},{"MaxRank","Максимальный ранг"},{"Amount","Базовая величина"},
            {"Octopus","Синий осьминог: прыжок и захват"},{"BlueOctopus","Характеристики синего осьминога"},{"SpawnChance","Доля осьминогов среди появлений"},{"FirstWave","Первая волна появления"},{"AimDistance","Дистанция начала прицеливания"},{"AimSeconds","Длительность прицеливания, с"},{"JumpSeconds","Длительность прыжка, с"},{"JumpHeight","Высота прыжка"},{"LandingRadius","Радиус попадания при приземлении"},{"EscapeClicks","Нажатий для освобождения"},{"RetreatSpeed","Скорость возвращения в воду"},
            {"Stamina","Выносливость"},{"Maximum","Максимум выносливости"},{"DrainPerSecond","Расход при беге за секунду"},{"RestorePerSecond","Восстановление за секунду"},{"RestoreDelay","Задержка восстановления"},{"RestartThreshold","Выносливость для повторного бега"},{"DeathBodyLifetime","Время жизни тела погибшего, с"},
            {"Waves","Волны врагов"},{"Bodies","Физика и тела"},
            {"StrongDamageMultiplier","Множитель урона сильного удара"},{"StrongCooldownSeconds","Перезарядка сильного удара, с"},
            {"HeavyChargeSeconds","Заряд сильной атаки, с"},{"HeavyDamageMultiplier","Множитель урона сильной атаки"},{"HeavyCooldownMultiplier","Перезарядка сильной атаки"},
            {"Duration","Длительность волны, с"},{"Rest","Передышка между волнами, с"},{"SlowInterval","Интервал появления по краям волны, с"},{"PeakInterval","Интервал на пике волны, с"},{"InitialDelay","Задержка первого появления, с"},
            {"EdgeAliveLimit","Лимит живых врагов по краям"},{"PeakAliveLimit","Лимит на пике"},{"ExtraAlivePerWave","Прибавка лимита каждой волны"},
            {"TumbleSeconds","Время кувырка живого врага, с"},{"CorpseSeconds","Время сохранения тела, с"},{"LaunchSpeed","Скорость отбрасывания"},{"UpSpeed","Подброс вверх"},{"Spin","Сила вращения"},{"ChainPush","Передача толчка другим врагам"},{"MaxCorpsesPC","Максимум тел на ПК"},{"MaxCorpsesMobile","Максимум тел на телефоне"},
            {"Player","Игрок: начальные характеристики и управление"},
            {"Combat","Боевая система и обратная связь"},
            {"Exploration","Исследование острова и появление врагов"},
            {"Progression","Опыт и улучшения"}, {"Ocean","Океан"},
            {"Normal","Тралалеро Тралала — стартовый пляж"},
            {"Lirili","Лирили Ларила — луга"},
            {"Patapim","Брр Брр Патапим — лес"},
            {"Cappuccino","Капучино Ассасино — макаронные руины"},
            {"Crocodilo","Бомбардиро Крокодило — болото"},
            {"Damage","Урон"}, {"AttackInterval","Интервал ударов, с"},
            {"MaxHealth","Максимум здоровья"}, {"MoveSpeed","Скорость движения"},
            {"Range","Дальность удара"}, {"CritChance","Вероятность критического удара"},
            {"CameraLocked","Поворот по камере по умолчанию"}, {"CameraDistance","Расстояние до камеры"},
            {"CameraPitch","Наклон камеры"}, {"TurnSpeed","Скорость поворота"},
            {"SprintMultiplier","Множитель скорости бега"}, {"DashSpeed","Скорость рывка"},
            {"DashDuration","Длительность рывка, с"}, {"DashCooldown","Перезарядка рывка, с"},
            {"ArcDegrees","Угол сектора атаки"}, {"InnerOmniRadius","Ближний круг попадания"},
            {"Knockback","Базовая сила отбрасывания"}, {"KnockbackMultiplier","Множитель отбрасывания"},
            {"SwingDuration","Длительность взмаха, с"}, {"ImpactDelay","Задержка попадания, с"},
            {"HitFlashDuration","Длительность красной подсветки, с"},
            {"HitFlashStrength","Яркость красной подсветки"}, {"ShowAttackRange","Показывать дальность атаки"},
            {"DetectionRadius","Радиус обнаружения игрока"}, {"LoseInterestRadius","Дистанция потери интереса"},
            {"LeashDistance","Максимальное удаление от места появления"},
            {"WanderRadius","Радиус прогулки"}, {"WanderSpeed","Скорость прогулки"},
            {"IdleMin","Минимальная пауза в покое, с"}, {"IdleMax","Максимальная пауза в покое, с"},
            {"SpawnMinDistance","Минимальная дистанция появления от игрока"},
            {"SpawnMaxDistance","Максимальная дистанция появления от игрока"},
            {"SpawnIntervalMin","Минимальный интервал появления, с"},
            {"SpawnIntervalMax","Максимальный интервал появления, с"},
            {"ActiveZoneLimit","Лимит врагов в текущей зоне"},
            {"RetainedZoneLimit","Лимит сохраняемых врагов в покинутой зоне"},
            {"ZoneEntryPopulation","Начальное население при входе в зону"},
            {"DifficultyPerZone","Прибавка сложности следующей зоны"},
            {"MaxDesktop","Общий лимит врагов на ПК"}, {"MaxMobile","Общий лимит врагов на телефоне"},
            {"Health","Здоровье"}, {"Speed","Скорость преследования"},
            {"AttackRange","Дальность ближней атаки"}, {"MeleeWindup","Подготовка ближней атаки, с"},
            {"Recovery","Восстановление после атаки, с"}, {"CanCharge","Разрешить атаку рывком"},
            {"ChargeMinDistance","Минимальная дистанция для атаки рывком"},
            {"ChargeMaxDistance","Максимальная дистанция для атаки рывком"},
            {"ChargeWindup","Подготовка атаки рывком, с"}, {"ChargeSpeed","Скорость атаки рывком"},
            {"ChargeDuration","Длительность атаки рывком, с"}, {"ChargeCooldown","Перезарядка атаки рывком, с"},
            {"KnockbackResistance","Сопротивление отбрасыванию"}, {"HitStagger","Оглушение при попадании, с"},
            {"Experience","Награда опыта"}, {"FirstLevelXp","Опыт для первого повышения уровня"},
            {"XpLinearGrowth","Линейный рост цены уровня"}, {"XpQuadraticGrowth","Квадратичный рост цены уровня"},
            {"UpgradePowerMultiplier","Множитель силы улучшений"},
            {"DeepColor","Цвет глубокой воды"}, {"ShallowColor","Цвет мелководья"},
            {"WaveSpeed","Скорость волн воды"}, {"WaveScale","Масштаб волн воды"}, {"WaveStrength","Контраст волн воды"}
        };

        static readonly Dictionary<string, string> Tips = new Dictionary<string, string> {
            {"Exploration","Враги появляются только в зоне игрока. За пределами обнаружения они стоят или гуляют рядом с местом появления."},
            {"DetectionRadius","В этом радиусе враг своей зоны замечает игрока и начинает преследование."},
            {"LoseInterestRadius","При удалении игрока дальше этого значения враг возвращается к спокойному поведению."},
            {"LeashDistance","Ограничивает удаление врага от его собственного места появления."},
            {"SpawnIntervalMin","Интервал между появлениями выбирается случайно в указанном диапазоне."},
            {"ActiveZoneLimit","Мягкий предел населения текущей локации; новые враги не появляются при достижении лимита."},
            {"ZoneEntryPopulation","При входе зона заселяется до указанного количества вдали от игрока."},
            {"KnockbackMultiplier","Усиливает отбрасывание без потери ранее настроенного базового значения."},
            {"ImpactDelay","Попадание рассчитывается после начала видимого взмаха биты."},
            {"UpgradePowerMultiplier","Множитель применяется и к ранее созданным редактируемым улучшениям."},
            {"CameraLocked","Пользовательское переключение в игре имеет приоритет над этим значением."}
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            bool enter = true;
            while (property.NextVisible(enter))
            {
                enter = false;
                if (Hidden.Contains(property.name)) continue;
                DrawProperty(property);
            }
            serializedObject.ApplyModifiedProperties();
        }

        static GUIContent Label(SerializedProperty property)
        {
            string title;
            if (!Labels.TryGetValue(property.name, out title)) title = property.displayName;
            string tooltip;
            if (!Tips.TryGetValue(property.name, out tooltip)) tooltip = "";
            return new GUIContent(title, tooltip);
        }

        void DrawProperty(SerializedProperty property)
        {
            var label = Label(property);
            if (property.propertyType == SerializedPropertyType.Generic && !property.isArray)
            {
                EditorGUILayout.Space(3);
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);
                if (!property.isExpanded) return;
                EditorGUI.indentLevel++;
                var child = property.Copy();
                var end = child.GetEndProperty();
                bool enter = true;
                while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                {
                    enter = false;
                    DrawProperty(child);
                }
                EditorGUI.indentLevel--;
                return;
            }
            // Draw primitive fields directly to keep source Header/Tooltip attributes from showing English.
            // Limits are retained from the same field metadata used by Unity's standard inspector.
            FieldInfo field = FindField(property);
            var range = field != null ? Attribute.GetCustomAttribute(field, typeof(RangeAttribute)) as RangeAttribute : null;
            var min = field != null ? Attribute.GetCustomAttribute(field, typeof(MinAttribute)) as MinAttribute : null;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    float value = range != null ? EditorGUILayout.Slider(label, property.floatValue, range.min, range.max) : EditorGUILayout.FloatField(label, property.floatValue);
                    property.floatValue = min != null ? Mathf.Max(min.min, value) : value;
                    break;
                case SerializedPropertyType.Integer:
                    int integer = range != null ? EditorGUILayout.IntSlider(label, property.intValue, Mathf.CeilToInt(range.min), Mathf.FloorToInt(range.max)) : EditorGUILayout.IntField(label, property.intValue);
                    property.intValue = min != null ? Mathf.Max(Mathf.CeilToInt(min.min), integer) : integer;
                    break;
                case SerializedPropertyType.Boolean: property.boolValue = EditorGUILayout.Toggle(label, property.boolValue); break;
                case SerializedPropertyType.Color: property.colorValue = EditorGUILayout.ColorField(label, property.colorValue); break;
                default: EditorGUILayout.PropertyField(property, label, true); break;
            }
        }

        FieldInfo FindField(SerializedProperty property)
        {
            Type type = serializedObject.targetObject.GetType();
            FieldInfo field = null;
            foreach (string name in property.propertyPath.Split('.'))
            {
                field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) return null;
                type = field.FieldType;
            }
            return field;
        }
    }
}
