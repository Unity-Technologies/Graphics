namespace UnityEditor.Rendering
{
    /// <summary>
    /// Public interface for handling a serialized object of <see cref="UnityEngine.Rendering.RenderPipelineGlobalSettings"/>
    /// </summary>
    public interface ISerializedRenderPipelineGlobalSettings
    {
        /// <summary>
        /// The <see cref="SerializedObject"/>
        /// </summary>
        SerializedObject serializedObject { get; }

        /// <summary>
        /// The shader variant log level
        /// </summary>
        SerializedProperty shaderVariantLogLevel { get; }

        /// <summary>
        /// If the shader variants needs to be exported
        /// </summary>
        SerializedProperty exportShaderVariants { get; }
    }
}
