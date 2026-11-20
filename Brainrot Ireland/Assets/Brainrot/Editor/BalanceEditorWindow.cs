using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor
{
    public sealed class BalanceEditorWindow : EditorWindow
    {
        GameBalance profile;
        UnityEditor.Editor inspector;
        Vector2 scroll;
        int testCoins;string testStatus="";

        [MenuItem("Tools/TungTung/Game Settings")]
        [MenuItem("Tools/TungTung/Настройки игры")]
        public static void Open() { GetWindow<BalanceEditorWindow>("Настройки игры").Show(); }
        void OnEnable() { minSize = new Vector2(480, 500); }
        void OnDisable() { if (inspector != null) DestroyImmediate(inspector); }

        void OnGUI()
        {
            if (profile == null) profile = GameBuilder.EnsureBalance();
            EditorGUILayout.LabelField("Брейнрот Ирландия — настройки острова", new GUIStyle(EditorStyles.boldLabel) { wordWrap = true });
            EditorGUILayout.HelpBox("Маленький остров, акулы, осьминоги и Оркаладон. Волны имеют тихое начало и конец, пик в середине. Короткое нажатие ЛКМ — обычный удар, удержание и отпускание — сильный. Монеты и улучшения действуют только в забеге; между волнами — лавка по E, новая волна по удержанию F. Каждая пятая волна — отдельный босс; XP и старые таймерные боссы отключены.", MessageType.Info);
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
            EditorGUILayout.LabelField("Волна: "+profile.Waves.Duration+" с; следующая начинается после удержания F", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Спавн: "+profile.Waves.SlowInterval+" с по краям → "+profile.Waves.PeakInterval+" с на пике", EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox("Разделы: игрок, камера, эффекты удара, волны, физика тел, враги, боссы пятой волны и океан. Правки сохраняются в профиле, Ctrl+Z отменяет изменения.",MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Тестирование прогресса",EditorStyles.boldLabel);
            testCoins=EditorGUILayout.IntField("Монеты текущего забега",testCoins);
            var current=GameManager.Instance;
            EditorGUILayout.HelpBox("Выдача доступна только в активном забеге. После его завершения монеты и улучшения исчезнут.",MessageType.None);
            using(new EditorGUI.DisabledScope(!Application.isPlaying||current==null||!current.HasRun)){
                if(GUILayout.Button("Установить баланс забега"))testStatus=SaveSystem.SetTestCoins(testCoins)?"Баланс забега: "+current.RunBalance:"Нет активного забега.";
            }
            if(GUILayout.Button("Сбросить весь прогресс")&&EditorUtility.DisplayDialog("Сброс прогресса","Завершить текущий забег и сбросить рекорды и обучение? Настройки управления и звука сохранятся.","Сбросить","Отмена")){
                var game=GameManager.Instance;if(game!=null)game.EndRun();else SaveSystem.Load();
                testStatus=SaveSystem.ResetProgress()?"Прогресс сброшен.":"Сброс не сохранён.";
                if(game!=null)game.MainMenu();
            }
            if(testStatus.Length>0)EditorGUILayout.HelpBox(testStatus,MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            UnityEditor.Editor.CreateCachedEditor(profile, null, ref inspector);
            EditorGUI.BeginChangeCheck();
            inspector.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck()) { EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets(); }
            EditorGUILayout.EndScrollView();
        }

    }
}
