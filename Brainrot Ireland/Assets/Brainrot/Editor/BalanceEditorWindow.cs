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
            EditorGUILayout.HelpBox("Маленький остров, акулы, осьминоги и Оркаладон. Волны имеют тихое начало и конец, пик в середине. Короткое нажатие ЛКМ — обычный удар, удержание и отпускание — сильный. Улучшения за монеты постоянны; XP отключён.", MessageType.Info);
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
            EditorGUILayout.LabelField("Волна: "+profile.Waves.Duration+" с, передышка: "+profile.Waves.Rest+" с", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Спавн: "+profile.Waves.SlowInterval+" с по краям → "+profile.Waves.PeakInterval+" с на пике", EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox("Разделы: игрок, камера, эффекты удара, волны, физика тел, враги и океан. Правки сохраняются в профиле, Ctrl+Z отменяет изменения.",MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Тестирование прогресса",EditorStyles.boldLabel);
            testCoins=EditorGUILayout.IntField("Баланс монет",testCoins);
            if(GUILayout.Button("Установить баланс")){
                if(!Application.isPlaying)SaveSystem.Load();
                testStatus=SaveSystem.SetTestCoins(testCoins)?"Баланс сохранён: "+SaveSystem.Data.Coins:"Не удалось сохранить баланс.";
            }
            if(GUILayout.Button("Сбросить весь прогресс")&&EditorUtility.DisplayDialog("Сброс прогресса","Удалить монеты, улучшения, рекорды и обучение? Настройки управления и звука сохранятся.","Сбросить","Отмена")){
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
