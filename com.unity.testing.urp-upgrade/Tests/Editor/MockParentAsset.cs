using UnityEditor;
using UnityObject = UnityEngine.Object;

namespace UnityEngine.Rendering.Tests
{
    class MockParentAsset : ScriptableObject
    {
        [MenuItem("Test/Create Mock Parent Asset")]
        static void Create()
        {
            if (Selection.count == 0)
                return;

            var assetPath =
                EditorUtility.SaveFilePanelInProject("Save New Mock Parent", "MockParent", "asset", "Select location");

            if (string.IsNullOrEmpty(assetPath))
                return;

            var parent = CreateInstance<MockParentAsset>();
            AssetDatabase.CreateAsset(parent, assetPath);

            foreach (var asset in Selection.objects)
                AssetDatabase.AddObjectToAsset(Instantiate(asset), assetPath);

            AssetDatabase.SaveAssets();

            Selection.activeObject = parent;
        }
    }
}
