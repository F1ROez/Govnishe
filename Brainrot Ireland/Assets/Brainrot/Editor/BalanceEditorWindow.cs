using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor {
 public sealed class BalanceEditorWindow:EditorWindow {
  GameBalance profile; UnityEditor.Editor inspector; Vector2 scroll;
  [MenuItem("Tools/TungTung/Game Settings")]
  public static void Open(){GetWindow<BalanceEditorWindow>("Game Settings").Show();}
  void OnEnable(){minSize=new Vector2(420,450);}
  void OnDisable(){if(inspector!=null)DestroyImmediate(inspector);}
  void OnGUI(){
   if(profile==null)profile=GameBuilder.EnsureBalance();
   EditorGUILayout.LabelField("Brainrot Ireland — баланс игры",EditorStyles.boldLabel);
   EditorGUILayout.HelpBox("Изменения сохраняются в профиле. Статы игрока/врагов и первый спавн — со следующего забега. Кривые спавна и награды XP читаются во время игры. Изменения самого asset в Play mode тоже сохраняются.",MessageType.Info);
   using(new EditorGUILayout.HorizontalScope()){
    if(GUILayout.Button("Показать профиль")){Selection.activeObject=profile;EditorGUIUtility.PingObject(profile);}
    if(GUILayout.Button("Стандартные значения")){
     Undo.RecordObject(profile,"Reset game balance");var defaults=CreateInstance<GameBalance>();defaults.name=profile.name;
     EditorUtility.CopySerialized(defaults,profile);DestroyImmediate(defaults);EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
    }
   }
   var spawn=profile.Spawning;
   EditorGUILayout.Space();EditorGUILayout.LabelField("XP: "+profile.RequiredXp(1)+" → "+profile.RequiredXp(2)+" → "+profile.RequiredXp(3)+" → "+profile.RequiredXp(4));
   EditorGUILayout.LabelField("Старт: "+spawn.InitialDelay.ToString("0.#")+"с; интервал "+(spawn.Interval!=null?spawn.Interval.Evaluate(0).ToString("0.#"):"5")+"с; лимит "+(spawn.AliveLimit!=null?spawn.AliveLimit.Evaluate(0).ToString("0"):"6"));
   EditorGUILayout.HelpBox("Раскройте разделы ниже. У кривых Spawning ось X — секунды забега, Y — интервал или лимит. Настройки каждого вида акулы доступны отдельно. Ctrl+Z отменяет правки.",MessageType.None);
   scroll=EditorGUILayout.BeginScrollView(scroll);
   UnityEditor.Editor.CreateCachedEditor(profile,null,ref inspector);
   EditorGUI.BeginChangeCheck();inspector.OnInspectorGUI();
   if(EditorGUI.EndChangeCheck()){EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();}
   EditorGUILayout.EndScrollView();
  }
 }
}