using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor
{
    public sealed class BalanceEditorWindow : EditorWindow
    {
        GameBalance profile;
        UnityEditor.Editor inspector;
        Vector2 scroll;

        [MenuItem("Tools/TungTung/Game Settings")]
        [MenuItem("Tools/TungTung/Настройки игры")]
        public static void Open() { GetWindow<BalanceEditorWindow>("Настройки игры").Show(); }
        void OnEnable() { minSize = new Vector2(480, 500); }
        void OnDisable() { if (inspector != null) DestroyImmediate(inspector); }

        void OnGUI()
        {
            if (profile == null) profile = GameBuilder.EnsureBalance();
            EditorGUILayout.LabelField("Брейнрот Ирландия — настройки острова", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Все зоны открыты сразу. Враги появляются случайно в текущей локации; далёкие враги отдыхают или гуляют. Изменения профиля сохраняются, в том числе в режиме игры. Характеристики лучше проверять в новом забеге.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Показать профиль")) { Selection.activeObject = profile; EditorGUIUtility.PingObject(profile); }
                if (GUILayout.Button("Стандартные значения"))
                {
                    Undo.RecordObject(profile, "Вернуть стандартные настройки игры");
                    var defaults = CreateInstance<GameBalance>();
                    defaults.name = profile.name;
                    EditorUtility.CopySerialized(defaults, profile);
                    DestroyImmediate(defaults);
                    EditorUtility.SetDirty(profile);
                    AssetDatabase.SaveAssets();
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Опыт: " + profile.RequiredXp(1) + " → " + profile.RequiredXp(2) + " → " + profile.RequiredXp(3) + " → " + profile.RequiredXp(4));
            DrawExplorationSummary();
            EditorGUILayout.HelpBox("Раскройте нужный раздел ниже: игрок, бой, исследование, опыт, океан или конкретный вид врага. Настройки прежнего режима скрыты. Правки отменяются сочетанием Ctrl+Z.", MessageType.None);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            UnityEditor.Editor.CreateCachedEditor(profile, null, ref inspector);
            EditorGUI.BeginChangeCheck();
            inspector.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck()) { EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets(); }
            EditorGUILayout.EndScrollView();
        }

        void DrawExplorationSummary()
        {
            var data = new SerializedObject(profile);
            var exploration = data.FindProperty("Exploration");
            if (exploration == null) return;
            var limit = exploration.FindPropertyRelative("ActiveZoneLimit");
            var min = exploration.FindPropertyRelative("SpawnIntervalMin");
            var max = exploration.FindPropertyRelative("SpawnIntervalMax");
            var detect = exploration.FindPropertyRelative("DetectionRadius");
            var wander = exploration.FindPropertyRelative("WanderRadius");
            if (limit != null && min != null && max != null)
                EditorGUILayout.LabelField("В текущей зоне: до " + limit.intValue + " врагов; появление каждые " + min.floatValue.ToString("0.#") + "–" + max.floatValue.ToString("0.#") + " с");
            if (detect != null && wander != null)
                EditorGUILayout.LabelField("Обнаружение: " + detect.floatValue.ToString("0.#") + " м; прогулка около места появления: " + wander.floatValue.ToString("0.#") + " м");
        }
    }
}
