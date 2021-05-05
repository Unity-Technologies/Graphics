using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "TriplanarSandbox")]
    class TriplanarSandboxNode : SandboxNode<TriplanarNodeDefinition>
    {
        // hax to make UI work
        [EnumControl("Type")]
        public SampleTexture2DNodeDefinition.TextureType textureType
        {
            get
            {
                return this.m_Definition?.m_TextureType ?? SampleTexture2DNodeDefinition.TextureType.Default;
            }
            set
            {
                if (this.m_Definition == null)
                    return;

                if (this.m_Definition.m_TextureType == value)
                    return;

                this.m_Definition.m_TextureType = value;
                RebuildNode();
            }
        }
    }

    [System.Serializable]
    class TriplanarNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        [SerializeField]
        public SampleTexture2DNodeDefinition.TextureType m_TextureType = SampleTexture2DNodeDefinition.TextureType.Default;

        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("TriplanarSandbox");

            // not cached (TODO: build a pure function memoizer cache)
            var shaderFunc = BuildFunction(context.GetInputConnected("Sampler"), m_TextureType);

            context.SetMainFunction(shaderFunc);
            context.SetPreviewFunction(shaderFunc, PreviewMode.Preview3D);

            if (m_TextureType == SampleTexture2DNodeDefinition.TextureType.Normal_TangentSpace)
            {
                context.HideSlot("MeshTangent");
                context.HideSlot("MeshBiTangent");
                context.HideSlot("MeshNormal");
            }
        }

        static ShaderFunction BuildFunction(bool useSeparateSamplerState, SampleTexture2DNodeDefinition.TextureType textureType)
        {
            var func = new ShaderFunction.Builder($"Unity_TriplanarSB_{textureType}_{(useSeparateSamplerState ? "sampler_" : "")}$precision");
            func.AddInput(Types._UnityTexture2D, "Texture");
            func.AddInput(Types._UnitySamplerState, "Sampler");
            func.AddInput(Types._float3, "Position", Binding.AbsoluteWorldSpacePosition);
            func.AddInput(Types._precision3, "Normal", Binding.WorldSpaceNormal);
            func.AddInput(Types._precision, "Tile", 1.0f);
            func.AddInput(Types._precision, "Blend", 1.0f);
            if (textureType == SampleTexture2DNodeDefinition.TextureType.Normal_TangentSpace)
            {
                // TODO: need to tag this as not visible inputs by default
                func.AddInput(Types._precision3, "MeshTangent", defaultValue: Binding.WorldSpaceTangent);
                func.AddInput(Types._precision3, "MeshBiTangent", Binding.WorldSpaceBitangent);
                func.AddInput(Types._precision3, "MeshNormal", Binding.WorldSpaceNormal);
            }
            func.AddOutput(Types._precision4, "Out");
            func.AddOutput(Types._precision, "R");
            func.AddOutput(Types._precision, "G");
            func.AddOutput(Types._precision, "B");
            func.AddOutput(Types._precision, "A");

            func.AddLine("$precision3 UV = Position * Tile;");
            func.AddLine("$precision3 Alpha = SafePositivePow_$precision(Normal, min(Blend, floor(log2(Min_$precision()) / log2(1 / sqrt(3)))));");
            func.AddLine("Alpha /= dot(Alpha, 1.0);");
            if (textureType == SampleTexture2DNodeDefinition.TextureType.Default)
            {
                if (useSeparateSamplerState)
                {
                    func.AddLine("$precision4 X = SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, UV.zy);");
                    func.AddLine("$precision4 Y = SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, UV.xz);");
                    func.AddLine("$precision4 Z = SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, UV.xy);");
                }
                else
                {
                    func.AddLine("$precision4 X = SAMPLE_TEXTURE2D(Texture.tex, Texture.samplerstate, UV.zy);");
                    func.AddLine("$precision4 Y = SAMPLE_TEXTURE2D(Texture.tex, Texture.samplerstate, UV.xz);");
                    func.AddLine("$precision4 Z = SAMPLE_TEXTURE2D(Texture.tex, Texture.samplerstate, UV.xy);");
                }
                func.AddLine("Out = X * Alpha.x + Y * Alpha.y + Z * Alpha.z;");
            }
            else // TextureType.Normal
            {
                // Whiteout blend method
                // https://medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
                if (useSeparateSamplerState)
                {
                    func.AddLine("$precision3 X = UnpackNormal(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, UV.zy));");
                    func.AddLine("$precision3 Y = UnpackNormal(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, UV.xz));");
                    func.AddLine("$precision3 Z = UnpackNormal(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, UV.xy));");
                }
                else
                {
                    func.AddLine("$precision3 X = UnpackNormal(SAMPLE_TEXTURE2D(Texture.tex, Texture.samplerstate, UV.zy));");
                    func.AddLine("$precision3 Y = UnpackNormal(SAMPLE_TEXTURE2D(Texture.tex, Texture.samplerstate, UV.xz));");
                    func.AddLine("$precision3 Z = UnpackNormal(SAMPLE_TEXTURE2D(Texture.tex, Texture.samplerstate, UV.xy));");
                }
                func.AddLine("X = $precision3(X.xy + Normal.zy, abs(X.z) * Normal.x);");
                func.AddLine("Y = $precision3(Y.xy + Normal.xz, abs(Y.z) * Normal.y);");
                func.AddLine("Z = $precision3(Z.xy + Normal.xy, abs(Z.z) * Normal.z);");
                func.AddLine("Out = $precision4(normalize(X.zyx * Alpha.x + Y.xzy * Alpha.y + Z.xyz * Alpha.z), 1);");

                if (textureType == SampleTexture2DNodeDefinition.TextureType.Normal_TangentSpace)
                {
                    // at this point we have world space normal -- but a standard shadergraph expects it in tangent space.. so convert spaces
                    // TODO: we could add an option to output world space normal directly, if the user has configured their input blocks to take world space normals
                    func.AddLine("$precision3x3 Transform = float3x3(MeshTangent, MeshBiTangent, MeshNormal);");
                    func.AddLine("Out.xyz = TransformWorldToTangent(Out.xyz, Transform);");
                }
            }
            func.AddLine("R = Out.r; G = Out.g; B = Out.b; A = Out.a;");

            return func.Build();
        }
    }
}
