using UnityEngine;
using UnityEditor.Graphing;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "MultiLayerParallax")]
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
            return new Vector1MaterialSlot(InputDepthSlotId, GetInputSlot1Name(), kInputDepthShaderName, SlotType.Input, 0);
        }

        protected virtual MaterialSlot GetInputFadeRateSlot()
        {
            return new DynamicVectorMaterialSlot(InputFadeRateSlotId, GetInputSlot2Name(), kInputFadeRateShaderName, SlotType.Input, Vector4.zero);
        }

        protected virtual MaterialSlot GetInputLayerCountSlot()
        {
            return new DynamicVectorMaterialSlot(InputLayerCountSlotId, GetInputSlot3Name(), kInputLayerCountShaderName, SlotType.Input, Vector4.zero);
        }

        protected virtual MaterialSlot GetTextureSlot()
        {
            return new Texture2DMaterialSlot(TextureSlotId, GetTextureSlotName(), kTextureSlotShaderName, SlotType.Input);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new DynamicVectorMaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, Vector4.zero);
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

        protected virtual string GetFunctionPrototype(string depth, string fadeRate, string layerCount, string tex, string UVs, string viewTangentSpace)
        {
            var input1 = ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputDepthSlotId).concreteValueType);
            var input2 = ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputFadeRateSlotId).concreteValueType);
            var input3 = ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputLayerCountSlotId).concreteValueType);
            var output = ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType);

            return string.Format("inline {0} {1} ({2} {3}, {4} {5}, {6} {7}, " + "sampler2D {8}, {9}2 {10}, {9}3 {11})", output, GetFunctionName(), input1, depth, input2, fadeRate, input3, layerCount, tex, precision, UVs, viewTangentSpace);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputDepthSlotId, InputFadeRateSlotId, InputLayerCountSlotId, TextureSlotId }, new[] { OutputSlotId });
            string depthValue = GetSlotValue(InputDepthSlotId, generationMode);
            string fadeRateValue = GetSlotValue(InputFadeRateSlotId, generationMode);
            string layerCountValue = GetSlotValue(InputLayerCountSlotId, generationMode);
            string textureValue = GetSlotValue(TextureSlotId, generationMode);

            var output = ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2};", output, GetVariableNameForSlot(OutputSlotId), GetFunctionCallBody(depthValue, fadeRateValue, layerCountValue, textureValue)), true);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("depth", "fadeRate", "layerCount", "tex", "UVs", "viewTangentSpace"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk(precision + "2 texcoord = UVs;", false);
            outputString.AddShaderChunk(precision + "2 offset = -viewTangentSpace.xy * depth / layerCount;", false);

            var output = ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType);
            outputString.AddShaderChunk(output + " result = 0.0f;", false);
            outputString.AddShaderChunk(output + " fade = 1.0f;", false);
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
                CoordinateSpace.View.ToVariableName(InterpolatorType.Tangent) + ")";
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
}*/
