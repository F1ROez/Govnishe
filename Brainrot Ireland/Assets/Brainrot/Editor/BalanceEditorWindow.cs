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
            EditorGUILayout.HelpBox("Маленький остров и обычные акулы. Волны имеют тихое начало и конец, пик в середине. Короткое нажатие ЛКМ — обычный удар, удержание и отпускание — сильный. Прокачка отключена.", MessageType.Info);
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
            EditorGUILayout.LabelField("Волна: "+profile.Waves.Duration+" с, передышка: "+profile.Waves.Rest+" с");
            EditorGUILayout.LabelField("Спавн: "+profile.Waves.SlowInterval+" с по краям → "+profile.Waves.PeakInterval+" с на пике");
            EditorGUILayout.HelpBox("Разделы: игрок, бой, волны, физика тел, акула и океан. Правки сохраняются в профиле, Ctrl+Z отменяет изменения.",MessageType.None);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            UnityEditor.Editor.CreateCachedEditor(profile, null, ref inspector);
            EditorGUI.BeginChangeCheck();
            inspector.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck()) { EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets(); }
            EditorGUILayout.EndScrollView();
        }

    }
}
