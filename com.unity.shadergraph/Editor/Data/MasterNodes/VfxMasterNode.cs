using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph
{
    sealed class VfxMasterNode : MasterNode, IMayRequirePosition
    {
        public const string PositionName = "Position";
        public const int PositionSlotId = 0;

        public const string BaseColorSlotName = "Base Color";
        public const int BaseColorSlotId = 1;

        public const string MetallicSlotName = "Metallic";
        public const int MetallicSlotId = 2;

        public const string SmoothnessSlotName = "Smoothness";
        public const int SmoothnessSlotId = 3;

        public const string AlphaSlotName = "Alpha";
        public const int AlphaSlotId = 4;

        public const string EmissiveSlotName = "Emissive";
        public const int EmissiveSlotId = 5;

        public const string ColorSlotName = "Color";
        public const int ColorSlotId = 6;

        public VfxMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }


        [SerializeField]
        bool m_Lit;

        public ToggleData lit
        {
            get { return new ToggleData(m_Lit); }
            set
            {
                if (m_Lit == value.isOn)
                    return;
                m_Lit = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_AlphaTest;

        public ToggleData alphaTest
        {
            get { return new ToggleData(m_AlphaTest); }
            set
            {
                if (m_AlphaTest == value.isOn)
                    return;
                m_AlphaTest = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();

            name = "Visual Effect Master";

            HashSet<int> usedSlots = new HashSet<int>();

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionName, PositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            usedSlots.Add(PositionSlotId);
            if ( lit.isOn)
            {
                AddSlot(new ColorRGBMaterialSlot(BaseColorSlotId, BaseColorSlotName, NodeUtils.GetHLSLSafeName(BaseColorSlotName), SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
                usedSlots.Add(BaseColorSlotId);

                AddSlot(new Vector1MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                usedSlots.Add(MetallicSlotId);

                AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                usedSlots.Add(SmoothnessSlotId);

                AddSlot(new ColorRGBMaterialSlot(EmissiveSlotId, EmissiveSlotName, NodeUtils.GetHLSLSafeName(EmissiveSlotName), SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
                usedSlots.Add(EmissiveSlotId);
            }
            else
            {
                AddSlot(new ColorRGBMaterialSlot(ColorSlotId, ColorSlotName, NodeUtils.GetHLSLSafeName(ColorSlotName), SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
                usedSlots.Add(ColorSlotId);
            }

            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1, ShaderStageCapability.Fragment));
            usedSlots.Add(AlphaSlotId);

            RemoveSlotsNameNotMatching(usedSlots);
        }

        public override void ProcessPreviewMaterial(Material previewMaterial)
        {
        }

        class SettingsView : VisualElement
        {
            readonly VfxMasterNode m_Node;
            public SettingsView(VfxMasterNode node)
            {
                m_Node = node;
                PropertySheet ps = new PropertySheet();
                ps.Add(new PropertyRow(new Label("Alpha Mask")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.alphaTest.isOn;
                        toggle.OnToggleChanged(ChangeAlphaTest);
                    });
                });
                ps.Add(new PropertyRow(new Label("Lit")), (System.Action<PropertyRow>)((row) =>
                {
                    row.Add(new Toggle(), (System.Action<Toggle>)((toggle) =>
                    {
                        toggle.value = m_Node.lit.isOn;
                        toggle.OnToggleChanged(this.ChangeLit);
                    }));
                }));
                Add(ps);
            }

            void ChangeAlphaTest(ChangeEvent<bool> e)
            {
                m_Node.alphaTest = new ToggleData(e.newValue, m_Node.alphaTest.isEnabled);
            }
            void ChangeLit(ChangeEvent<bool> e)
            {
                m_Node.lit = new ToggleData(e.newValue, m_Node.alphaTest.isEnabled);
            }
        }

        protected override VisualElement CreateCommonSettingsElement()
        {
            return new SettingsView(this);
        }

        public override bool hasPreview => false;

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetSlots(slots);

            List<MaterialSlot> validSlots = new List<MaterialSlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].stageCapability != ShaderStageCapability.All && slots[i].stageCapability != stageCapability)
                    continue;

                validSlots.Add(slots[i]);
            }
            return validSlots.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition(stageCapability));
        }

        public override string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return false;
        }

        public override int GetPreviewPassIndex()
        {
            return 0;
        }
    }
}
