#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing shader resources needed in URP for XR.
    /// </summary>
    /// <seealso cref="Shader"/>
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

        [MenuItem("Assets/Create/Rendering/URP XR System Data", priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 1)]
        static void CreateXRSystemData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateXRSystemDataAsset>(), "CustomXRSystemData.asset", null, null);
        }

#endif

        /// <summary>
        /// Class containing shader resources used in URP for XR.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            /// <summary>
            /// XR Occlusion mesh shader.
            /// </summary>
            [Reload("Shaders/XR/XROcclusionMesh.shader")]
            public Shader xrOcclusionMeshPS;

            /// <summary>
            /// XR Mirror View shader.
            /// </summary>
            [Reload("Shaders/XR/XRMirrorView.shader")]
            public Shader xrMirrorViewPS;
        }

        /// <summary>
        /// Shader resources used in URP for XR.
        /// </summary>
        public ShaderResources shaders;
    }
}
