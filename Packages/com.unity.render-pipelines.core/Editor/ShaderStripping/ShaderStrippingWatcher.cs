using System;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Notifies when shader variants have been stripped
    /// </summary>
    public static class ShaderStrippingWatcher
    {
        /// <summary>
        /// Callback when a shader has been stripped
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="shaderVariant">The variant</param>
        /// <param name="variantsOut">The output variants after the stripping process</param>
        /// <param name="stripTimeMs">The total amount of time to strip the variants</param>
        /// <typeparam name="TShader">The shader</typeparam>
        /// <typeparam name="TShaderVariant">The variant</typeparam>
        public delegate void OnShaderStrippedCallbackHandler<TShader, TShaderVariant>(TShader shader, TShaderVariant shaderVariant, uint variantsOut, double stripTimeMs)
            where TShader : UnityEngine.Object;

        /// <summary>
        /// Callback for <see cref="Shader"/>
        /// </summary>
        public static event OnShaderStrippedCallbackHandler<Shader, ShaderSnippetData> shaderProcessed;

        /// <summary>
        /// Callback for <see cref="ComputeShader"/>
        /// </summary>
        public static event OnShaderStrippedCallbackHandler<ComputeShader, string> computeShaderProcessed;

        internal static void OnShaderProcessed<TShader, TShaderVariant>(TShader shader, TShaderVariant shaderVariant, uint variantsOut, double stripTimeMs)
            where TShader : UnityEngine.Object
        {
            if (typeof(TShader) == typeof(Shader))
            {
                shaderProcessed?.Invoke((Shader)Convert.ChangeType(shader, typeof(Shader)), (ShaderSnippetData)Convert.ChangeType(shaderVariant, typeof(ShaderSnippetData)), variantsOut, stripTimeMs);
            }
            else if (typeof(TShader) == typeof(ComputeShader))
            {
                computeShaderProcessed?.Invoke((ComputeShader)Convert.ChangeType(shader, typeof(ComputeShader)), shaderVariant.ToString(), variantsOut, stripTimeMs);
            }
        }
    }
}
