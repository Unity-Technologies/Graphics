using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/MultiLayerParallax")]
    public class MultiLayerParallaxNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV, IMayRequireViewDirection
    {
        protected const string kInputDepthShaderName = "Depth";
        protected const string kInputFadeRateShaderName = "FadeRate";
        protected const string kInputLayerCountShaderName = "LayerCount";
        protected const string kTextureSlotShaderName = "Texture";
        protected const string kOutputSlotShaderName = "Result";

        public const int InputDepthSlotId = 0;          // 'depth'
        public const int InputFadeRateSlotId = 1;       // 'fade_rate'
        public const int InputLayerCountSlotId = 2;     // 'layer_count'
        public const int TextureSlotId = 3;             // 'tex'
        public const int OutputSlotId = 4;              // 'result'

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                return PreviewMode.Preview3D;
            }
        }

        public MultiLayerParallaxNode()
        {
            name = "MultiLayerParallax";
            UpdateNodeAfterDeserialization();
        }

        public string GetFunctionName()
        {
            return "unity_multilayer_parallax_" + precision;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(GetInputDepthSlot());
            AddSlot(GetInputFadeRateSlot());
            AddSlot(GetInputLayerCountSlot());
            AddSlot(GetTextureSlot());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { InputDepthSlotId, InputFadeRateSlotId, InputLayerCountSlotId, TextureSlotId, OutputSlotId }; }
        }

        protected virtual MaterialSlot GetInputDepthSlot()
        {
            return new MaterialSlot(InputDepthSlotId, GetInputSlot1Name(), kInputDepthShaderName, SlotType.Input, SlotValueType.Vector1, Vector4.zero);
        }

        protected virtual MaterialSlot GetInputFadeRateSlot()
        {
            return new MaterialSlot(InputFadeRateSlotId, GetInputSlot2Name(), kInputFadeRateShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetInputLayerCountSlot()
        {
            return new MaterialSlot(InputLayerCountSlotId, GetInputSlot3Name(), kInputLayerCountShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetTextureSlot()
        {
            return new MaterialSlot(TextureSlotId, GetTextureSlotName(), kTextureSlotShaderName, SlotType.Input, SlotValueType.Texture2D, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual string GetInputSlot1Name()
        {
            return kInputDepthShaderName;
        }

        protected virtual string GetInputSlot2Name()
        {
            return kInputFadeRateShaderName;
        }

        protected virtual string GetInputSlot3Name()
        {
            return kInputLayerCountShaderName;
        }

        protected virtual string GetTextureSlotName()
        {
            return kTextureSlotShaderName;
        }

        protected virtual string GetOutputSlotName()
        {
            return kOutputSlotShaderName;
        }

        private string input1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputDepthSlotId).concreteValueType); }
        }

        private string input2Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputFadeRateSlotId).concreteValueType); }
        }
        private string input3Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputLayerCountSlotId).concreteValueType); }
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType); }
        }

        protected virtual string GetFunctionPrototype(string depth, string fadeRate, string layerCount, string tex, string UVs, string viewTangentSpace)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " (" +
                precision + input1Dimension + " " + depth + ", " +
                precision + input2Dimension + " " + fadeRate + ", " +
                precision + input3Dimension + " " + layerCount + ", " +
                "sampler2D "                      + tex + ", " +
                precision + "2 "                  + UVs + ", " +
                precision + "3 "                  + viewTangentSpace + ")";
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputDepthSlotId, InputFadeRateSlotId, InputLayerCountSlotId, TextureSlotId }, new[] { OutputSlotId });
            string depthValue = GetSlotValue(InputDepthSlotId, generationMode);
            string fadeRateValue = GetSlotValue(InputFadeRateSlotId, generationMode);
            string layerCountValue = GetSlotValue(InputLayerCountSlotId, generationMode);
            string textureValue = GetSlotValue(TextureSlotId, generationMode);

            visitor.AddShaderChunk(
                precision + outputDimension + " " + GetVariableNameForSlot(OutputSlotId) +
                " = " + GetFunctionCallBody(depthValue, fadeRateValue, layerCountValue, textureValue) + ";", true);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("depth", "fadeRate", "layerCount", "tex", "UVs", "viewTangentSpace"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk(precision + "2 texcoord = UVs;", false);
            outputString.AddShaderChunk(precision + "2 offset = -viewTangentSpace.xy * depth / layerCount;", false);

            outputString.AddShaderChunk(precision + outputDimension + " result = 0.0f;", false);
            outputString.AddShaderChunk(precision + outputDimension + " fade = 1.0f;", false);
            outputString.AddShaderChunk(precision + " alpha = 0.0f;", false);

            outputString.AddShaderChunk("for (int i = 0; i < 10; i++) {", false);
            outputString.Indent();

            outputString.AddShaderChunk("result += fade * tex2D(tex, texcoord);", false);
            outputString.AddShaderChunk("texcoord += offset;", false);
            outputString.AddShaderChunk("fade *= fadeRate;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            outputString.AddShaderChunk("return result / layerCount;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        protected virtual string GetFunctionCallBody(string depthValue, string fadeRateValue, string layerCountValue, string texValue)
        {
            return GetFunctionName() + " (" +
                depthValue + ", " +
                fadeRateValue + ", " +
                layerCountValue + ", " +
                texValue + ", " +
                UVChannel.uv0.GetUVName() + ", " +
                ShaderGeneratorNames.TangentSpaceViewDirection + ")";
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            return channel == UVChannel.uv0;
        }

        public NeededCoordinateSpace RequiresViewDirection()
        {
            return NeededCoordinateSpace.Tangent;
        }
    }
}
