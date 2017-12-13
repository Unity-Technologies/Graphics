using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using System.Collections.Generic;

/*namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Scene Normals")]
    public class SceneNormalsNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IGenerateProperties, IMayRequireScreenPosition
    {
        const string kUVSlotName = "UV";
        const string kOutputSlotName = "Out";

        public const int UVSlotId = 0;
        public const int OutputSlotId = 1;

        public SceneNormalsNode()
        {
            name = "Scene Normals";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        string GetFunctionName()
        {
            return "Unity_DecodeViewNormalStereo";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, OutputSlotId });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty()
            {
                name = "_CameraDepthNormalsTexture",
                propType = PropertyType.Float,
                vector4Value = new Vector4(1, 1, 1, 1),
                floatValue = 1,
                colorValue = new Vector4(1, 1, 1, 1),
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Sampler2DShaderProperty
            {
                overrideReferenceName = "_CameraDepthNormalsTexture",
                generatePropertyBlock = false
            });
        }

        string GetFunctionPrototype(string argIn, string argOut)
        {
            return string.Format("void {0} ({1}4 {2}, out {3} {4})", GetFunctionName(),
                precision, argIn,
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), argOut);
        }

        string GetFunctionCallBody(string inputValue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + outputValue + ");";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("Tex", "Out"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk(string.Format("{0}3 nn = Tex.xyz * {0}3(2.0 * 1.7777, 2.0 * 1.7777, 0) + {0}3(-1.7777, -1.7777, 1);", precision), true);
            outputString.AddShaderChunk(string.Format("{0} g = 2.0 / dot(nn.xyz, nn.xyz);", precision), true);
            outputString.AddShaderChunk(string.Format("{0}3 n;", precision), true);
            outputString.AddShaderChunk("n.xy = g * nn.xy;", true);
            outputString.AddShaderChunk("n.z = g - 1.0;", true);
            outputString.AddShaderChunk(string.Format("Out = n;", precision), true);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string uvValue = GetSlotValue(UVSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0}4 _DepthNormalsTexture = tex2D(_CameraDepthNormalsTexture, {1});", precision, uvValue), true);
            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
            visitor.AddShaderChunk(GetFunctionCallBody("_DepthNormalsTexture", outputValue), true);
            visitor.AddShaderChunk(string.Format("{1} = {1} * {0}3(1.0, 1.0, -1.0);", precision, GetVariableNameForSlot(OutputSlotId)), true);
        }

        public bool RequiresScreenPosition()
        {
            var uvSlot = FindInputSlot<MaterialSlot>(UVSlotId) as ScreenPositionMaterialSlot;
            if (uvSlot == null)
                return false;

            if (uvSlot.isConnected)
                return false;

            return uvSlot.RequiresScreenPosition();
        }
    }
}*/
