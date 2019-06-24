using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.Rendering.HDPipeline.Drawing;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [Serializable]
    [Title("Master", "HDRP/Eye")]
    [FormerName("UnityEditor.ShaderGraph.EyeMasterNode")]
    class EyeMasterNode : MasterNode<IEyeSubShader>, IMayRequirePosition
    {
        public const string PositionSlotName = "Position";
        public const int PositionSlotId = 0;

        public const string SpecularOcclusionSlotName = "SpecularOcclusion";
        public const int SpecularOcclusionSlotId = 1;

        public const string AmbientOcclusionSlotName = "Occlusion";
        public const string AmbientOcclusionDisplaySlotName = "AmbientOcclusion";
        public const int AmbientOcclusionSlotId = 2;

        public const string EyeProfileHashSlotName = "EyeProfileHash";
        public const int EyeProfileHashSlotId = 3;

        public enum MaterialType
        {
            EyeGames,
            EyeCinematics
        }

        // Just for convenience of doing simple masks. We could run out of bits of course.
        [Flags]
        enum SlotMask
        {
            None = 0,
            Position = 1 << PositionSlotId,
            SpecularOcclusion = 1 << SpecularOcclusionSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            EyeProfile = 1 << EyeProfileHashSlotId,
        }

        const SlotMask EyeGamesSlotMask = SlotMask.Position | SlotMask.SpecularOcclusion | SlotMask.Occlusion | SlotMask.EyeProfile;
        const SlotMask EyeCinematicsSlotMask = SlotMask.Position | SlotMask.SpecularOcclusion | SlotMask.Occlusion | SlotMask.EyeProfile;

        // This could also be a simple array. For now, catch any mismatched data.
        SlotMask GetActiveSlotMask()
        {
            switch (materialType)
            {
                case MaterialType.EyeGames:
                    return EyeGamesSlotMask;

                case MaterialType.EyeCinematics:
                    return EyeCinematicsSlotMask;

                default:
                    return SlotMask.None;
            }
        }

        bool MaterialTypeUsesSlotMask(SlotMask mask)
        {
            SlotMask activeMask = GetActiveSlotMask();
            return (activeMask & mask) != 0;
        }

        [SerializeField]
        MaterialType m_MaterialType;

        public MaterialType materialType
        {
            get { return m_MaterialType; }
            set
            {
                if (m_MaterialType == value)
                    return;

                m_MaterialType = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_ReceivesSSR = true;
        public ToggleData receiveSSR
        {
            get { return new ToggleData(m_ReceivesSSR); }
            set
            {
                if (m_ReceivesSSR == value.isOn)
                    return;
                m_ReceivesSSR = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        SpecularOcclusionMode m_SpecularOcclusionMode;

        public SpecularOcclusionMode specularOcclusionMode
        {
            get { return m_SpecularOcclusionMode; }
            set
            {
                if (m_SpecularOcclusionMode == value)
                    return;

                m_SpecularOcclusionMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        public EyeMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return null; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Eye Master";

            List<int> validSlots = new List<int>();

            // Position
            if (MaterialTypeUsesSlotMask(SlotMask.Position))
            {
                AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
                validSlots.Add(PositionSlotId);
            }

            // Specular Occlusion
            if (MaterialTypeUsesSlotMask(SlotMask.SpecularOcclusion) && specularOcclusionMode == SpecularOcclusionMode.Custom)
            {
                AddSlot(new Vector1MaterialSlot(SpecularOcclusionSlotId, SpecularOcclusionSlotName, SpecularOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularOcclusionSlotId);
            }

            // Ambient Occlusion
            if (MaterialTypeUsesSlotMask(SlotMask.Occlusion))
            {
                AddSlot(new Vector1MaterialSlot(AmbientOcclusionSlotId, AmbientOcclusionDisplaySlotName, AmbientOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AmbientOcclusionSlotId);
            }

            // Diffusion Profile
            if (MaterialTypeUsesSlotMask(SlotMask.EyeProfile))
            {
                AddSlot(new Vector1MaterialSlot(EyeProfileHashSlotId, EyeProfileHashSlotName, EyeProfileHashSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(EyeProfileHashSlotId);
            }

            RemoveSlotsNameNotMatching(validSlots, true);
        }

        protected override VisualElement CreateCommonSettingsElement()
        {
            return new EyeSettingsView(this);
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
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
            return validSlots.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal(stageCapability));
        }

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

        public override void ProcessPreviewMaterial(Material previewMaterial)
        {
            // Fixup the material settings:
            previewMaterial.renderQueue = (int)HDRenderQueue.ChangeType(HDRenderQueue.RenderQueueType.Opaque, offset: 0);
            EyeGUI.SetupMaterialKeywordsAndPass(previewMaterial);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, false, receiveSSR.isOn);
            /*
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                surfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode),
                0,
                true,
                transparentCullMode,
                zTest,
                false
            );
            */
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, false, false);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, DoubleSidedMode.Disabled);

            base.CollectShaderProperties(collector, generationMode);
        }
    }
}
