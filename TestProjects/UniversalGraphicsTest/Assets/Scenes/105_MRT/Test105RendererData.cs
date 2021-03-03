#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, ReloadGroup]
    public class Test105RendererData : ScriptableRendererData
    {
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateTest105RendererAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<Test105RendererData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, UniversalRenderPipelineAsset.packagePath);
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/Tests/Test 105 Renderer", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateTest105RendererData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateTest105RendererAsset>(), "Test105RendererData.asset", null, null);
        }
#endif

        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            //[Reload("TestProjects/UniversalGraphicsTest/Assets/Scenes/105_MRT/OutputColorsToMRTs.shader")]
            // Path outside of scope supported by ResourceReloader - we manually load this one
            public Shader colorToMrtPS;

            //[Reload("TestProjects/UniversalGraphicsTest/Assets/Scenes/105_MRT/CopyToViewport.shader")]
            // Path outside of scope supported by ResourceReloader - we manually load this one
            public Shader copyToViewportPS;

            [Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;
        }

        public ShaderResources shaders = null;

        protected override ScriptableRenderer Create()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ResourceReloader.ReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
            }
#endif
            shaders.colorToMrtPS = Shader.Find("Test/OutputColorsToMRTs");
            shaders.copyToViewportPS = Shader.Find("Test/CopyToViewport");

            return new Test105Renderer(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Upon asset creation, OnEnable is called and `shaders` reference is not yet initialized
            // We need to call the OnEnable for data migration when updating from old versions of LWRP that
            // serialized resources in a different format. Early returning here when OnEnable is called
            // upon asset creation is fine because we guarantee new assets get created with all resources initialized.
            if (shaders == null)
                return;

#if UNITY_EDITOR
            try
            {
                ResourceReloader.ReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
                shaders.colorToMrtPS = Shader.Find("Test/OutputColorsToMRTs");
                shaders.copyToViewportPS = Shader.Find("Test/CopyToViewport");
            }
            catch {}
#endif
        }
    }
}
