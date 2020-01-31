using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedHDRenderPipelineAsset
    {
        public SerializedObject serializedObject;

        public SerializedProperty renderPipelineResources;
        public SerializedProperty renderPipelineRayTracingResources;
        public SerializedProperty diffusionProfileSettingsList; 
        public SerializedProperty allowShaderVariantStripping;
        public SerializedProperty enableSRPBatcher;
        public SerializedProperty shaderVariantLogLevel; 
        public SerializedRenderPipelineSettings renderPipelineSettings;
        public SerializedFrameSettings defaultFrameSettings;
        public SerializedFrameSettings defaultBakedOrCustomReflectionFrameSettings;
        public SerializedFrameSettings defaultRealtimeReflectionFrameSettings;

        //RenderPipelineResources not always exist and thus cannot be serialized normally.
        public bool editorResourceHasMultipleDifferentValues
        {
            get
            {
                var initialValue = firstEditorResources;
                for (int index = 1; index < serializedObject.targetObjects.Length; ++index)
                {
                    if (initialValue != (serializedObject.targetObjects[index] as HDRenderPipelineAsset)?.renderPipelineEditorResources)
                        return true;
                }
                return false;
            }
        }

        public HDRenderPipelineEditorResources firstEditorResources
            => (serializedObject.targetObjects[0] as HDRenderPipelineAsset)?.renderPipelineEditorResources;

        public void SetEditorResource(HDRenderPipelineEditorResources value)
        {
            for (int index = 0; index < serializedObject.targetObjects.Length; ++index)
                (serializedObject.targetObjects[index] as HDRenderPipelineAsset).renderPipelineEditorResources = value;
        }

        public SerializedHDRenderPipelineAsset(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            renderPipelineResources = serializedObject.FindProperty("m_RenderPipelineResources");
            renderPipelineRayTracingResources = serializedObject.FindProperty("m_RenderPipelineRayTracingResources");
            diffusionProfileSettingsList = serializedObject.Find((HDRenderPipelineAsset s) => s.diffusionProfileSettingsList);
            allowShaderVariantStripping = serializedObject.Find((HDRenderPipelineAsset s) => s.allowShaderVariantStripping);
            enableSRPBatcher = serializedObject.Find((HDRenderPipelineAsset s) => s.enableSRPBatcher);
            shaderVariantLogLevel = serializedObject.Find((HDRenderPipelineAsset s) => s.shaderVariantLogLevel);

            renderPipelineSettings = new SerializedRenderPipelineSettings(serializedObject.FindProperty("m_RenderPipelineSettings"));
            defaultFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RenderingPathDefaultCameraFrameSettings"), null); //no overrides in HDRPAsset
            defaultBakedOrCustomReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings"), null); //no overrides in HDRPAsset
            defaultRealtimeReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RenderingPathDefaultRealtimeReflectionFrameSettings"), null); //no overrides in HDRPAsset
        }

        public void Update()
        {
            serializedObject.Update();
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
