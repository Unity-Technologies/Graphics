using System;
using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UI", "Sample Element Texture")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class SampleElementTextureNode : AbstractMaterialNode, IGeneratesFunction, IGeneratesBodyCode, IMayRequireUITK
    {

        public const int UV0Id = 0;
        public const int UV1Id = 1;
        public const int UV2Id = 2;
        public const int UV3Id = 3;
        public const int Color0SlotId = 10;
        public const int Color1SlotId = 11;
        public const int Color2SlotId = 12;
        public const int Color3SlotId = 13;

        private const string kUIV0SlotName = "UV 0";
        private const string kUIV1SlotName = "UV 1";
        private const string kUIV2SlotName = "UV 2";
        private const string kUIV3SlotName = "UV 3";
        private const string kColor0SlotName = "Color 0";
        private const string kColor1SlotName = "Color 1";
        private const string kColor2SlotName = "Color 2";
        private const string kColor3SlotName = "Color 3";


        public override bool hasPreview { get { return false; } }

        public SampleElementTextureNode()
        {
            name = "Sample Element Texture";
            synonyms = new string[] { };
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector2MaterialSlot(UV0Id, kUIV0SlotName, kUIV0SlotName, SlotType.Input, Vector2.zero));
            AddSlot(new Vector2MaterialSlot(UV1Id, kUIV1SlotName, kUIV1SlotName, SlotType.Input, Vector2.zero));
            AddSlot(new Vector2MaterialSlot(UV2Id, kUIV2SlotName, kUIV2SlotName, SlotType.Input, Vector2.zero));
            AddSlot(new Vector2MaterialSlot(UV3Id, kUIV3SlotName, kUIV3SlotName, SlotType.Input, Vector2.zero));

            AddSlot(new Vector4MaterialSlot(Color0SlotId, kColor0SlotName, kColor0SlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(Color1SlotId, kColor1SlotName, kColor1SlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(Color2SlotId, kColor2SlotName, kColor2SlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(Color3SlotId, kColor3SlotName, kColor3SlotName, SlotType.Output, Vector4.zero));

            RemoveSlotsNameNotMatching(new[] {
                UV0Id,
                UV1Id,
                UV2Id,
                UV3Id,
                Color0SlotId,
                Color1SlotId,
                Color2SlotId,
                Color3SlotId,
            });
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction("UIE_SAMPLE4", sb =>
            {
                sb.AppendLine(@"#define UIE_SAMPLE4(index) \");
                sb.AppendLine(@"    c0 = UNITY_SAMPLE_TEX2D(_Texture##index, uv0); \");
                sb.AppendLine(@"    c1 = UNITY_SAMPLE_TEX2D(_Texture##index, uv1); \");
                sb.AppendLine(@"    c2 = UNITY_SAMPLE_TEX2D(_Texture##index, uv2); \");
                sb.AppendLine(@"    c3 = UNITY_SAMPLE_TEX2D(_Texture##index, uv3);");
                sb.AppendLine("");
                sb.AppendLine("void SampleTextureSlot4(half index, float2 uv0, float2 uv1, float2 uv2, float2 uv3, out float4 c0, out float4 c1, out float4 c2, out float4 c3)");
                using (sb.BlockScope())
                {
                    sb.AppendLine("UIE_BRANCH(UIE_SAMPLE4)");
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("$precision4 {0};", GetVariableNameForSlot(Color0SlotId));
            sb.AppendLine("$precision4 {0};", GetVariableNameForSlot(Color1SlotId));
            sb.AppendLine("$precision4 {0};", GetVariableNameForSlot(Color2SlotId));
            sb.AppendLine("$precision4 {0};", GetVariableNameForSlot(Color3SlotId));

            using (sb.BlockScope())
            {
                sb.AppendLine("half Unity_UIE_TextureSlotIndex = IN.typeTexSettings.y;");
                sb.AppendLine("SampleTextureSlot4(Unity_UIE_TextureSlotIndex, {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});",
                    GetSlotValue(UV0Id, generationMode),
                    GetSlotValue(UV1Id, generationMode),
                    GetSlotValue(UV2Id, generationMode),
                    GetSlotValue(UV3Id, generationMode),
                    GetVariableNameForSlot(Color0SlotId),
                    GetVariableNameForSlot(Color1SlotId),
                    GetVariableNameForSlot(Color2SlotId),
                    GetVariableNameForSlot(Color3SlotId));
            }
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
