using UnityEditorInternal;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class SerializedUniversalRenderPipelineAsset
    {
        public SerializedProperty rendererDataProp { get; }
        public SerializedProperty defaultRendererProp { get; }

        public SerializedProperty requireDepthTextureProp { get; }
        public SerializedProperty requireOpaqueTextureProp { get; }
        public SerializedProperty opaqueDownsamplingProp { get; }

        public SerializedProperty hdr { get; }
        public SerializedProperty msaa { get; }
        public SerializedProperty renderScale { get; }



        public SerializedProperty srpBatcher { get; }
        public SerializedProperty supportsDynamicBatching { get; }

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
        public SerializedProperty useAdaptivePerformance { get; }
#endif
        public UniversalRenderPipelineAsset asset { get; }
        public SerializedObject serializedObject { get; }

        public SerializedUniversalRenderPipelineAsset(SerializedObject serializedObject)
        {
            asset = serializedObject.targetObject as UniversalRenderPipelineAsset;
            this.serializedObject = serializedObject;

            requireDepthTextureProp = serializedObject.FindProperty("m_RequireDepthTexture");
            requireOpaqueTextureProp = serializedObject.FindProperty("m_RequireOpaqueTexture");
            opaqueDownsamplingProp = serializedObject.FindProperty("m_OpaqueDownsampling");

            hdr = serializedObject.FindProperty("m_SupportsHDR");
            msaa = serializedObject.FindProperty("m_MSAA");
            renderScale = serializedObject.FindProperty("m_RenderScale");


            srpBatcher = serializedObject.FindProperty("m_UseSRPBatcher");
            supportsDynamicBatching = serializedObject.FindProperty("m_SupportsDynamicBatching");

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            useAdaptivePerformance = serializedObject.FindProperty("m_UseAdaptivePerformance");
#endif
        }

        /// <summary>
        /// Refreshes the serialized object
        /// </summary>
        public void Update()
        {
            serializedObject.Update();
        }

        /// <summary>
        /// Applies the modified properties of the serialized object
        /// </summary>
        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
