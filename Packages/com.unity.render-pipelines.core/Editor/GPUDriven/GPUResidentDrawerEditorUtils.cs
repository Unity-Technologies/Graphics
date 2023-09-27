using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    internal class GPUResidentDrawerEditorUtils
    {
        class DoCreateNewAssetGPUResidentDrawerResources : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<GPUResidentDrawerResources>();
                newAsset.name = Path.GetFileName(pathName);

                ResourceReloader.ReloadAllNullIn(newAsset, "Packages/com.unity.render-pipelines.core/");

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        // Hide: User aren't suppose to have to create it.
        //[MenuItem("Assets/Create/Rendering/GPU Resident Drawer Resources", priority = CoreUtils.Sections.section7 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 2)]
        static void CreateGPUResidentDrawerResources()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                ScriptableObject.CreateInstance<DoCreateNewAssetGPUResidentDrawerResources>(),
                "New GPUResidentDrawerResources.asset", icon, null);
        }
    }
}
