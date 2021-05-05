using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample Texture 2D Sandbox")]
    class SampleTexture2DSandboxNode : SandboxNode<SampleTexture2DNodeDefinition>
    {
        [EnumControl("Type")]
        public SampleTexture2DNodeDefinition.TextureType textureType
        {
            get { return m_Definition?.textureType ?? SampleTexture2DNodeDefinition.TextureType.Default; }
            set
            {
                if (this.m_Definition == null)
                    return;

                if (m_Definition.textureType == value)
                    return;

                m_Definition.textureType = value;
                RebuildNode();
            }
        }
    }

    class SampleTexture2DNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        internal enum TextureType
        {
            Default,
            Normal_TangentSpace,
            Normal_WorldSpace
        };

        [SerializeField]
        public TextureType textureType = TextureType.Default;

        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("Sample Texture 2D Sandbox");

            bool useSamplerInput = context.GetInputConnected("Sampler");

            // not cached (TODO: build a pure function memoizer cache)
            var shaderFunc = BuildFunction(textureType, useSamplerInput);

            context.SetMainFunction(shaderFunc);
            context.SetPreviewFunction(shaderFunc);

            if (!useSamplerInput)
                context.AddInputSlot(Types._UnitySamplerState, "Sampler");
        }

        // statically cached function definition
        static ShaderFunction BuildFunction(TextureType textureType, bool useSamplerInput)
        {
            var func = new ShaderFunction.Builder($"Unity_SampleTexture2DSB_{textureType}_{(useSamplerInput ? "Sampler_" : "")}$precision");

            func.AddInput(Types._UnityTexture2D, "Texture");
            func.AddInput(Types._precision2, "UV", Binding.MeshUV0);

            if (useSamplerInput)
                func.AddInput(Types._UnitySamplerState, "Sampler");         // how to specify possible variants with or without this function?

            func.AddOutput(Types._precision4, "RGBA");
            func.AddOutput(Types._precision, "R");
            func.AddOutput(Types._precision, "G");
            func.AddOutput(Types._precision, "B");
            func.AddOutput(Types._precision, "A");

            func.AddLine("RGBA = SAMPLE_TEXTURE2D(Texture.tex, ", useSamplerInput ? "Sampler" : "Texture", ".samplerstate, UV);");

            if (textureType == TextureType.Normal_TangentSpace)
                func.AddLine("RGBA.rgb = UnpackNormal(RGBA);");
            else if (textureType == TextureType.Normal_WorldSpace)
                func.AddLine("RGBA.rgb = UnpackNormalRGB(RGBA);");

            // alpha is always straight through from the raw sampler, even in normal mode.  Not ideal, but that's what the old node did...

            func.AddLine("R = RGBA.r; G = RGBA.g; B = RGBA.b; A = RGBA.a;");

            return func.Build();
        }
    }
}
