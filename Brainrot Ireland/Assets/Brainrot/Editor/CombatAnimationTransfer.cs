using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor
{
    /// <summary>Exports the owner's local clip and its import dependencies for private transfer; never uploads files.</summary>
    public static class CombatAnimationTransfer
    {
        const string SelectedAttack="HumanM@Attack2H01";
        [MenuItem("Tools/TungTung/Подготовить анимацию удара для передачи")]
        public static void ExportSelectedAttack()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorUtility.DisplayDialog("Анимация удара","Сначала остановите Play, затем повторите команду.","Хорошо");return;}
            string path=AssetDatabase.GetAssetPath(Selection.activeObject);
            var clip=Selection.activeObject as AnimationClip;
            if(clip==null&&!string.IsNullOrEmpty(path))clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__")&&c.name.IndexOf(SelectedAttack,StringComparison.OrdinalIgnoreCase)>=0);
            if(clip==null){
                var matches=AssetDatabase.FindAssets(SelectedAttack).Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileNameWithoutExtension(p).Equals(SelectedAttack,StringComparison.OrdinalIgnoreCase)).Distinct().ToArray();
                if(matches.Length==1){path=matches[0];clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__"));}
            }
            if(clip==null||string.IsNullOrEmpty(path)){EditorUtility.DisplayDialog("Анимация не выбрана","Выберите HumanM@Attack2H01 (или другой нужный клип) в Project и повторите команду.","Хорошо");return;}
            string destination=EditorUtility.SaveFilePanel("Сохранить анимацию с зависимостями","",SelectedAttack,"unitypackage");
            if(string.IsNullOrEmpty(destination))return;
            // URP shader dependencies under Packages do not belong in the owner's animation transfer.
            var dependencies=AssetDatabase.GetDependencies(path,true).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&!AssetDatabase.IsValidFolder(p)).ToArray();
            AssetDatabase.ExportPackage(dependencies,destination,ExportPackageOptions.Default);
            EditorUtility.RevealInFinder(destination);
            Debug.Log("Анимация и зависимости подготовлены для передачи файлом: "+destination+". Движение ещё не подключено к Сахуру.");
        }
    }
}
