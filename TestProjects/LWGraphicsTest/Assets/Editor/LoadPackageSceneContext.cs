using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class LoadPackageSceneContext : Editor
{
    [MenuItem("Assets/Load Package Scene", false, -10)]
    private static void LoadPackageScene()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            
        EditorSceneManager.OpenScene(path); 
    }
    
    [MenuItem("Assets/Load Package Scene", true)]
    private static bool ValidatePackageScene()
    {
        return Selection.activeObject is SceneAsset;
    }
}
