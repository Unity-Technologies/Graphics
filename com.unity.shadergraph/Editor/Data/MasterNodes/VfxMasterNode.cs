using System;
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
    [Serializable]
    [Title("Master", "Visual Effect")]
    sealed class VfxMasterNode : MasterNode, IMayRequirePosition
    {
        const string BaseColorSlotName = "Base Color";
        const string MetallicSlotName = "Metallic";
        const string SmoothnessSlotName = "Smoothness";
        const string NormalSlotName = "Normal";
        const string AlphaSlotName = "Alpha";
        const string EmissiveSlotName = "Emissive";
        const string ColorSlotName = "Color";
        const string AlphaThresholdSlotName = "AlphaThreshold";

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

            if ( lit.isOn)
            {
                AddSlot(new ColorRGBMaterialSlot(ShaderGraphVfxAsset.BaseColorSlotId, BaseColorSlotName, NodeUtils.GetHLSLSafeName(BaseColorSlotName), SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
                usedSlots.Add(ShaderGraphVfxAsset.BaseColorSlotId);

                AddSlot(new Vector1MaterialSlot(ShaderGraphVfxAsset.MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                usedSlots.Add(ShaderGraphVfxAsset.MetallicSlotId);

                AddSlot(new Vector1MaterialSlot(ShaderGraphVfxAsset.SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                usedSlots.Add(ShaderGraphVfxAsset.SmoothnessSlotId);

                AddSlot(new Vector3MaterialSlot(ShaderGraphVfxAsset.NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, new Vector3(0,0,1), ShaderStageCapability.Fragment));
                usedSlots.Add(ShaderGraphVfxAsset.NormalSlotId);

                AddSlot(new ColorRGBMaterialSlot(ShaderGraphVfxAsset.EmissiveSlotId, EmissiveSlotName, NodeUtils.GetHLSLSafeName(EmissiveSlotName), SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
                usedSlots.Add(ShaderGraphVfxAsset.EmissiveSlotId);
            }
            else
            {
                AddSlot(new ColorRGBMaterialSlot(ShaderGraphVfxAsset.ColorSlotId, ColorSlotName, NodeUtils.GetHLSLSafeName(ColorSlotName), SlotType.Input, Color.grey.gamma, ColorMode.HDR, ShaderStageCapability.Fragment));
                usedSlots.Add(ShaderGraphVfxAsset.ColorSlotId);
            }

            AddSlot(new Vector1MaterialSlot(ShaderGraphVfxAsset.AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1, ShaderStageCapability.Fragment));
            usedSlots.Add(ShaderGraphVfxAsset.AlphaSlotId);

            if( alphaTest.isOn)
            {
                AddSlot(new Vector1MaterialSlot(ShaderGraphVfxAsset.AlphaThresholdSlotId, AlphaThresholdSlotName, AlphaThresholdSlotName, SlotType.Input, 1, ShaderStageCapability.Fragment));
                usedSlots.Add(ShaderGraphVfxAsset.AlphaThresholdSlotId);
            }

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
            return true;
        }

        public override int GetPreviewPassIndex()
        {
            return 0;
        }
    }
}
