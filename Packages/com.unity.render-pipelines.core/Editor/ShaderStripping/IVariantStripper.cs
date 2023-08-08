using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Interface to be implemented for a type of shader that it's variants can be stripped
    /// </summary>
    /// <typeparam name="TShader">The shader <see cref="Shader"/> or <see cref="ComputeShader"/></typeparam>
    /// <typeparam name="TShaderVariant">The type of variant for the given type of shader can either be <see cref="ShaderSnippetData"/> or <see cref="string"/></typeparam>
    public interface IVariantStripper<TShader, TShaderVariant> : IStripper
        where TShader : UnityEngine.Object
    {
        /// <summary>
        /// Specifies if a <see cref="TShader"/> variant can be stripped
        /// </summary>
        /// <param name="shader">The <see cref="TShader"/></param>
        /// <param name="shaderVariant"><see cref="TShaderVariant"/></param>
        /// <param name="shaderCompilerData">The variant</param>
        /// <returns>true if the variant is not used and can be stripped</returns>
        bool CanRemoveVariant([DisallowNull] TShader shader, [DisallowNull] TShaderVariant shaderVariant, ShaderCompilerData shaderCompilerData);
    }

    /// <summary>
    /// Interface to allow an <see cref="IVariantStripper{TShader, TShaderVariant}"/> to skip a shader variant for processing
    /// </summary>
    /// <typeparam name="TShader">The shader <see cref="Shader"/> or <see cref="ComputeShader"/></typeparam>
    /// <typeparam name="TShaderVariant">The type of variant for the given type of shader can either be <see cref="ShaderSnippetData"/> or <see cref="string"/></typeparam>
    public interface IVariantStripperSkipper<TShader, TShaderVariant>
        where TShader : UnityEngine.Object
    {
        /// <summary>
        /// Returns if the <see cref="TShader"/> for the current <see cref="TShaderVariant"/> is skipped for stripping
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="shaderVariant">The variant</param>
        /// <returns>true, if the shader can be skipped</returns>
        bool SkipShader([DisallowNull] TShader shader, [DisallowNull] TShaderVariant shaderVariant);
    }

    /// <summary>
    /// Interface to allow an <see cref="IVariantStripper{TShader, TShaderVariant}"/> to have a callback before and after the processing of variants
    /// </summary>
    /// <typeparam name="TShader">The shader <see cref="Shader"/> or <see cref="ComputeShader"/></typeparam>
    /// <typeparam name="TShaderVariant">The type of variant for the given type of shader can either be <see cref="ShaderSnippetData"/> or <see cref="string"/></typeparam>
    public interface IVariantStripperScope<TShader, TShaderVariant>
        where TShader : UnityEngine.Object
    {
        /// <summary>
        /// Callback that will be executed before parsing variants
        /// </summary>
        /// <param name="shader">The shader</param>
        void BeforeShaderStripping(TShader shader);

        /// <summary>
        /// Callback that will be executed after parsing variants
        /// </summary>
        /// <param name="shader">The shader</param>
        void AfterShaderStripping(TShader shader);
    }

    #region Shader Helpers
    /// <summary>
    /// Helper interface to create a <see cref="IVariantStripper{TShader, TShaderVariant}"/> targeting <see cref="Shader"/>
    /// </summary>
    public interface IShaderVariantStripper : IVariantStripper<Shader, ShaderSnippetData> { }

    /// <summary>
    /// Helper interface to create a <see cref="IVariantStripperSkipper{TShader, TShaderVariant}"/> targeting <see cref="Shader"/>
    /// </summary>
    public interface IShaderVariantStripperSkipper : IVariantStripperSkipper<Shader, ShaderSnippetData> { }

    /// <summary>
    /// Helper interface to create a <see cref="IVariantStripperScope{TShader, TShaderVariant}"/> targeting <see cref="Shader"/>
    /// </summary>
    public interface IShaderVariantStripperScope : IVariantStripperScope<Shader, ShaderSnippetData> { }
    #endregion

    #region Compute Shader Helpers
    /// <summary>
    /// Helper interface to create a <see cref="IVariantStripper{TShader, TShaderVariant}"/> targeting <see cref="ComputeShader"/>
    /// </summary>
    public interface IComputeShaderVariantStripper : IVariantStripper<ComputeShader, string> { }

    /// <summary>
    /// Helper interface to create a <see cref="IVariantStripperSkipper{TShader, TShaderVariant}"/> targeting <see cref="ComputeShader"/>
    /// </summary>
    public interface IComputeShaderVariantStripperSkipper : IVariantStripperSkipper<ComputeShader, string> { }

    /// <summary>
    /// Helper interface to create a <see cref="IVariantStripperScope{TShader, TShaderVariant}"/> targeting <see cref="ComputeShader"/>
    /// </summary>
    public interface IComputeShaderVariantStripperScope : IVariantStripperScope<ComputeShader, string> { }
    #endregion
}
