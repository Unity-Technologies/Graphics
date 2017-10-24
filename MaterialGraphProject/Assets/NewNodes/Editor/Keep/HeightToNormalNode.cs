using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Utility/Heightmap To Normalmap")]
    public class HeightToNormalNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int TextureInput = 0;
        public const int TexCoordInput = 1;
        public const int TexOffsetInput = 2;
        public const int StrengthInput = 3;
        public const int NormalOutput = 4;

        const string TextureInputName = "Texture";
        const string TexCoordInputName = "UV";
        const string TexOffsetInputName = "Offset";
        const string StrengthInputName = "Strength";
        const string NormalOutputName = "Normal";

        public HeightToNormalNode()
        {
            name = "HeightToNormal";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DMaterialSlot(TextureInput, TextureInputName, TextureInputName, SlotType.Input));
            AddSlot(new Vector2MaterialSlot(TexCoordInput, TexCoordInputName, TexCoordInputName, SlotType.Input, Vector2.zero));
            AddSlot(new Vector1MaterialSlot(TexOffsetInput, TexOffsetInputName, TexOffsetInputName, SlotType.Input, 0.005f));
            AddSlot(new Vector1MaterialSlot(StrengthInput, StrengthInputName, StrengthInputName, SlotType.Input, 8f));
            AddSlot(new Vector3MaterialSlot(NormalOutput, NormalOutputName, NormalOutputName, SlotType.Output, Vector3.zero));
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var textureInput = GetSlotValue(TextureInput, generationMode);
            var texCoordInput = RequiresMeshUV(UVChannel.uv0)
                ? string.Format("{0}.xy", UVChannel.uv0.GetUVName())
                : GetSlotValue(TexCoordInput, generationMode);
            var texOffsetInput = GetSlotValue(TexOffsetInput, generationMode);
            var strengthInput = GetSlotValue(StrengthInput, generationMode);
            var normalOutput = GetVariableNameForSlot(NormalOutput);

            visitor.AddShaderChunk(string.Format("{0}3 {1};", precision, normalOutput), true);
            visitor.AddShaderChunk("{", false);
            visitor.Indent();
            {
                visitor.AddShaderChunk(string.Format("{0}2 offsetU = float2({1}.x + {2}, {1}.y);", precision, texCoordInput, texOffsetInput), true);
                visitor.AddShaderChunk(string.Format("{0}2 offsetV = float2({1}.x, {1}.y + {2});", precision, texCoordInput, texOffsetInput), true);

                visitor.AddShaderChunk(string.Format("{0} normalSample = UNITY_SAMPLE_TEX2D({1}, {2});", precision, textureInput, texCoordInput), true);
                visitor.AddShaderChunk(string.Format("{0} uSample = UNITY_SAMPLE_TEX2D({1}, offsetU);", precision, textureInput), true);
                visitor.AddShaderChunk(string.Format("{0} vSample = UNITY_SAMPLE_TEX2D({1}, offsetV);", precision, textureInput), true);

                visitor.AddShaderChunk(string.Format("{0}3 va = float3(1, 0, (uSample - normalSample) * {1});", precision, strengthInput), true);
                visitor.AddShaderChunk(string.Format("{0}3 vb = float3(0, 1, (vSample - normalSample) * {1});", precision, strengthInput), true);
                visitor.AddShaderChunk(string.Format("{0} = cross(va, vb);", normalOutput), true);
            }
            visitor.Deindent();
            visitor.AddShaderChunk("}", false);
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            if (channel != UVChannel.uv0)
            {
                return false;
            }

            var uvSlot = FindInputSlot<MaterialSlot>(TexCoordInput);
            if (uvSlot == null)
                return true;

            var edges = owner.GetEdges(uvSlot.slotReference).ToList();
            return edges.Count == 0;
        }
    }
}
