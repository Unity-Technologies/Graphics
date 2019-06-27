#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;

namespace UnityEngine.Rendering.LWRP
{
    [ReloadGroup]
    public class PhysicalSkyData : ScriptableObject
    {
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreatePhysicalSkyDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<PhysicalSkyData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, LightweightRenderPipelineAsset.packagePath);
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/Lightweight Render Pipeline/Physical Sky Data", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreatePostProcessData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreatePhysicalSkyDataAsset>(), "CustomPhysicalSkyData.asset", null, null);
        }
#endif

        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Shaders/PhysicalSky/Precomputation.compute")]
            public ComputeShader precomputationCS;

            [Reload("Shaders/PhysicalSky/RenderSky.shader")]
            public Shader renderSkyPS;
        }

        public ShaderResources shaders;
    }
}
