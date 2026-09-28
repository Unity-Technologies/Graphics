using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Declares a bound texture in the generated shader, along with the sampler and texel size that come with
    /// it under the names the HLSL side expects.
    /// </summary>
    /// <remarks>
    /// Everything is named by the binding key, which is what the generated code reads the texture by and what
    /// <c>VFX_SAMPLER</c> pastes <c>sampler</c> onto, so it all comes out of <see cref="WriteView"/>, the
    /// per-binding hook. There is no view struct to build over it, and nothing to put in the include file.
    /// </remarks>
    class VfxTextureDataDescriptionWriter : IDataDescriptionWriter<VfxTextureData>
    {
        public void WriteDescription(ShaderWriter shaderWriter, DataView dataView, VfxTextureData dataDescription,
            string name, CompilationContext context)
        {
        }

        public bool WriteView(ShaderWriter shaderWriter, DataView usedDataView, DataNode dataNode, string name,
            string sourceName, CompilationContext context)
        {
            var texture = (VfxTextureData)usedDataView.DataDescription;

            // A block that declared its own container for this texture (RWTexture2D, for instance) dictates
            // the declaration; otherwise it is read through the plain type, sampled rather than indexed.
            var resourceTypes = context.data.GetOrCreate<ResourceTypeContainer>();
            string declaration = resourceTypes.TryGetBufferType(usedDataView, dataNode, out var bufferType)
                ? VfxBufferTypeHlsl.GetDeclaration(bufferType)
                : null;

            shaderWriter.WriteVariableDeclaration(declaration ?? VFXExpression.TypeToCode(texture.ValueType), name);
            shaderWriter.WriteVariableDeclaration("SamplerState", GetSamplerName(name));
            shaderWriter.WriteVariableDeclaration("float4", GetTexelSizeName(name));
            return false;
        }

        /// <summary>
        /// The sampler declared alongside a texture. Must match the <c>VFX_SAMPLER</c> macro in VFXCommon.hlsl,
        /// which pastes <c>sampler</c> onto the texture's name at the point of use.
        /// </summary>
        static string GetSamplerName(string textureName) => $"sampler{textureName}";

        static string GetTexelSizeName(string textureName) => $"{textureName}_TexelSize";
    }
}
