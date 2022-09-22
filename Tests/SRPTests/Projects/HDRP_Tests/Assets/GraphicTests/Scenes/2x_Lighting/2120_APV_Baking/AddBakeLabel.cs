using UnityEngine;

public class AddBakeLabel : MonoBehaviour
{
    public void AddBakeLabelOnActiveScene()
    {
        #if UNITY_EDITOR
        var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var scene = UnityEditor.AssetDatabase.LoadMainAssetAtPath(scenePath);
        UnityEditor.AssetDatabase.SetLabels(scene, new[] { "TestRunnerBake" });
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }
}
