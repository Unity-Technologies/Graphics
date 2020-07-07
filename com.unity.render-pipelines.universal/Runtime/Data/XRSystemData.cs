#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    public class XRSystemData : ScriptableObject
    {
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateXRSystemDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<XRSystemData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, UniversalRenderPipelineAsset.packagePath);
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/XR System Data", priority = CoreUtils.assetCreateMenuPriority3)]
        static void CreateXRSystemData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateXRSystemDataAsset>(), "CustomXRSystemData.asset", null, null);
        }
#endif

        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Shaders/XR/XROcclusionMesh.shader")]
            public Shader xrOcclusionMeshPS;

            [Reload("Shaders/XR/XRMirrorView.shader")]
            public Shader xrMirrorViewPS;
        }

        public ShaderResources shaders;
    }
}
