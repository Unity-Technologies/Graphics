using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Flipbook")]
    class FlipbookNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV
    {
        public FlipbookNode()
        {
            name = "Flipbook";
            synonyms = new string[] { "atlas", "animation" };
            UpdateNodeAfterDeserialization();
        }

        const int UVSlotId = 0;
        const int WidthSlotId = 1;
        const int HeightSlotId = 2;
        const int TileSlotId = 3;
        const int OutputSlotId = 4;
        const string kUVSlotName = "UV";
        const string kWidthSlotName = "Width";
        const string kHeightSlotName = "Height";
        const string kTileSlotName = "Tile";
        const string kOutputSlotName = "Out";

        public override bool hasPreview
        {
            get { return true; }
        }

        string GetFunctionName()
        {
            string invertText = string.Empty;

            if (m_InvertX && m_InvertY)
                invertText = "InvertXY_";
            else if (m_InvertX)
                invertText = "InvertX_";
            else if (m_InvertY)
                invertText = "InvertY_";

            return $"Unity_Flipbook_{invertText}$precision";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(WidthSlotId, kWidthSlotName, kWidthSlotName, SlotType.Input, 1));
            AddSlot(new Vector1MaterialSlot(HeightSlotId, kHeightSlotName, kHeightSlotName, SlotType.Input, 1));
            AddSlot(new Vector1MaterialSlot(TileSlotId, kTileSlotName, kTileSlotName, SlotType.Input, 0));
            AddSlot(new Vector2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector2.zero));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, WidthSlotId, HeightSlotId, TileSlotId, OutputSlotId });
        }

        [SerializeField]
        private bool m_InvertX = false;

        [ToggleControl("Invert X")]
        public ToggleData invertX
        {
            get { return new ToggleData(m_InvertX); }
            set
            {
                if (m_InvertX == value.isOn)
                    return;
                m_InvertX = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        private bool m_InvertY = true;

        [ToggleControl("Invert Y")]
        public ToggleData invertY
        {
            get { return new ToggleData(m_InvertY); }
            set
            {
                if (m_InvertY == value.isOn)
                    return;
                m_InvertY = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var uvValue = GetSlotValue(UVSlotId, generationMode);
            var widthValue = GetSlotValue(WidthSlotId, generationMode);
            var heightValue = GetSlotValue(HeightSlotId, generationMode);
            var tileValue = GetSlotValue(TileSlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);

            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSlotId));
            if (!generationMode.IsPreview())
            {
                sb.AppendLine("$precision2 _{0}_Invert = $precision2 ({1}, {2});", GetVariableNameForNode(), invertX.isOn ? 1 : 0, invertY.isOn ? 1 : 0);
            }
            sb.AppendLine("{0}({1}, {2}, {3}, {4}, _{5}_Invert, {6});", GetFunctionName(), uvValue, widthValue, heightValue, tileValue, GetVariableNameForNode(), outputValue);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            properties.Add(new PreviewProperty(PropertyType.Vector2)
            {
                name = string.Format("_{0}_Invert", GetVariableNameForNode()),
                vector4Value = new Vector2(invertX.isOn ? 1 : 0, invertY.isOn ? 1 : 0)
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            properties.AddShaderProperty(new Vector2ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_Invert", GetVariableNameForNode()),
                generatePropertyBlock = false
            });
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("void {0} ({1} UV, {2} Width, {3} Height, {4} Tile, $precision2 Invert, out {5} Out)",
                    GetFunctionName(),
                    FindInputSlot<MaterialSlot>(UVSlotId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(WidthSlotId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(HeightSlotId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(TileSlotId).concreteValueType.ToShaderString(),
                    FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString());
                using (s.BlockScope())
                {
                    s.AppendLine("Tile = floor(fmod(Tile + $precision(0.00001), Width*Height));");
                    s.AppendLine("$precision2 tileCount = $precision2(1.0, 1.0) / $precision2(Width, Height);");

                    AppendInvertSpecificLines(s);

                    s.AppendLine("Out = (UV + $precision2(tileX, tileY)) * tileCount;");
                }
            });
        }

        private void AppendInvertSpecificLines(ShaderStringBuilder stringBuilder)
        {
            if (m_InvertX)
            {
                stringBuilder.AppendLine("$precision tileX = (Invert.x * Width - ((Tile - Width * floor(Tile * tileCount.x)) + Invert.x * 1));");
            }
            else
            {
                stringBuilder.AppendLine("$precision tileX = (Tile - Width * floor(Tile * tileCount.x));");
            }

            if (m_InvertY)
            {
                stringBuilder.AppendLine("$precision tileY = (Invert.y * Height - (floor(Tile * tileCount.x) + Invert.y * 1));");
            }
            else
            {
                stringBuilder.AppendLine("$precision tileY = (floor(Tile * tileCount.x));");
            }
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                        return true;
                }

                return false;
            }
        }
    }
}
