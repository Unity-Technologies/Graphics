using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class SerializedUniversalRenderPipelineAsset
    {
        public SerializedProperty rendererDataProp { get; }
        public SerializedProperty defaultRendererProp { get; }

        public SerializedProperty requireDepthTextureProp { get; }
        public SerializedProperty requireOpaqueTextureProp { get; }

        public SerializedProperty hdr { get; }
        public SerializedProperty hdrColorBufferPrecisionProp { get; }
        public SerializedProperty msaa { get; }
        public SerializedProperty renderScale { get; }
        public SerializedProperty upscalingFilter { get; }
        public SerializedProperty fsrOverrideSharpness { get; }
        public SerializedProperty fsrSharpness { get; }



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

            rendererDataProp = serializedObject.FindProperty(nameof(UniversalRenderPipelineAsset.m_RendererDataList));
            defaultRendererProp = serializedObject.FindProperty(nameof(UniversalRenderPipelineAsset.m_DefaultRendererIndex));


            hdr = serializedObject.FindProperty("m_SupportsHDR");
            hdrColorBufferPrecisionProp = serializedObject.FindProperty("m_HDRColorBufferPrecision");
            msaa = serializedObject.FindProperty("m_MSAA");
            renderScale = serializedObject.FindProperty("m_RenderScale");
            upscalingFilter = serializedObject.FindProperty("m_UpscalingFilter");
            fsrOverrideSharpness = serializedObject.FindProperty("m_FsrOverrideSharpness");
            fsrSharpness = serializedObject.FindProperty("m_FsrSharpness");


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
