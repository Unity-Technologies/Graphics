using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Rendering.HighDefinition.Drawing;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDUnlitMasterNode")]
    [Title("Master", "Unlit (HDRP)")]
    class HDUnlitMasterNode : MaterialMasterNode<IHDUnlitSubShader>, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string ColorSlotName = "Color";
        public const string AlphaSlotName = "Alpha";
        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const string DistortionSlotName = "Distortion";
        public const string DistortionSlotDisplayName = "Distortion Vector";
        public const string DistortionBlurSlotName = "DistortionBlur";
        public const string PositionSlotName = "Vertex Position";
        public const string PositionSlotDisplayName = "Vertex Position";
        public const string EmissionSlotName = "Emission";
        public const string VertexNormalSlotName = "Vertex Normal";
        public const string VertexTangentSlotName = "Vertex Tangent";
        public const string ShadowTintSlotName = "Shadow Tint";

        public const int ColorSlotId = 0;
        public const int AlphaSlotId = 7;
        public const int AlphaThresholdSlotId = 8;
        public const int PositionSlotId = 9;
        public const int DistortionSlotId = 10;
        public const int DistortionBlurSlotId = 11;
        public const int EmissionSlotId = 12;
        public const int VertexNormalSlotId = 13;
        public const int VertexTangentSlotId = 14;
        public const int ShadowTintSlotId = 15;

        // Don't support Multiply
        public enum AlphaModeLit
        {
            Alpha,
            Premultiply,
            Additive,
        }

        [SerializeField]
        SurfaceType m_SurfaceType;

        public SurfaceType surfaceType
        {
            get { return m_SurfaceType; }
            set
            {
                if (m_SurfaceType == value)
                    return;

                m_SurfaceType = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        AlphaMode m_AlphaMode;

        public AlphaMode alphaMode
        {
            get { return m_AlphaMode; }
            set
            {
                if (m_AlphaMode == value)
                    return;

                m_AlphaMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        HDRenderQueue.RenderQueueType m_RenderingPass = HDRenderQueue.RenderQueueType.Opaque;

        public HDRenderQueue.RenderQueueType renderingPass
        {
            get { return m_RenderingPass; }
            set
            {
                if (m_RenderingPass == value)
                    return;

                m_RenderingPass = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_TransparencyFog = true;

        public ToggleData transparencyFog
        {
            get { return new ToggleData(m_TransparencyFog); }
            set
            {
                if (m_TransparencyFog == value.isOn)
                    return;
                m_TransparencyFog = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

#pragma warning disable 649
        [SerializeField, Obsolete("Kept for data migration")]
        internal bool m_DrawBeforeRefraction;
#pragma warning restore 649

        [SerializeField]
        bool m_Distortion;

        public ToggleData distortion
        {
            get { return new ToggleData(m_Distortion); }
            set
            {
                if (m_Distortion == value.isOn)
                    return;
                m_Distortion = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        DistortionMode m_DistortionMode;

        public DistortionMode distortionMode
        {
            get { return m_DistortionMode; }
            set
            {
                if (m_DistortionMode == value)
                    return;

                m_DistortionMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_DistortionOnly = true;

        public ToggleData distortionOnly
        {
            get { return new ToggleData(m_DistortionOnly); }
            set
            {
                if (m_DistortionOnly == value.isOn)
                    return;
                m_DistortionOnly = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_DistortionDepthTest = true;

        public ToggleData distortionDepthTest
        {
            get { return new ToggleData(m_DistortionDepthTest); }
            set
            {
                if (m_DistortionDepthTest == value.isOn)
                    return;
                m_DistortionDepthTest = value.isOn;
                Dirty(ModificationScope.Graph);
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

        [SerializeField]
        int m_SortPriority;

        public int sortPriority
        {
            get { return m_SortPriority; }
            set
            {
                if (m_SortPriority == value)
                    return;
                m_SortPriority = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_DoubleSided;

        public ToggleData doubleSided
        {
            get { return new ToggleData(m_DoubleSided); }
            set
            {
                if (m_DoubleSided == value.isOn)
                    return;
                m_DoubleSided = value.isOn;
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_ZWrite = true;

        public ToggleData zWrite
        {
            get { return new ToggleData(m_ZWrite); }
            set
            {
                if (m_ZWrite == value.isOn)
                    return;
                m_ZWrite = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        TransparentCullMode m_transparentCullMode = TransparentCullMode.Back;
        public TransparentCullMode transparentCullMode
        {
            get => m_transparentCullMode;
            set
            {
                if (m_transparentCullMode == value)
                    return;

                m_transparentCullMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        CompareFunction m_ZTest = CompareFunction.LessEqual;
        public CompareFunction zTest
        {
            get => m_ZTest;
            set
            {
                if (m_ZTest == value)
                    return;

                m_ZTest = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AddPrecomputedVelocity = false;

        public ToggleData addPrecomputedVelocity
        {
            get { return new ToggleData(m_AddPrecomputedVelocity); }
            set
            {
                if (m_AddPrecomputedVelocity == value.isOn)
                    return;
                m_AddPrecomputedVelocity = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_EnableShadowMatte = false;

        public ToggleData enableShadowMatte
        {
            get { return new ToggleData(m_EnableShadowMatte); }
            set
            {
                if (m_EnableShadowMatte == value.isOn)
                    return;
                m_EnableShadowMatte = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        public HDUnlitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("Master-Node-Unlit");

        public bool HasDistortion()
        {
            return (surfaceType == SurfaceType.Transparent && distortion.isOn);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Unlit Master";

            List<int> validSlots = new List<int>();
            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotDisplayName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            validSlots.Add(PositionSlotId);
            AddSlot(new NormalMaterialSlot(VertexNormalSlotId, VertexNormalSlotName, VertexNormalSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            validSlots.Add(VertexNormalSlotId);
            AddSlot(new TangentMaterialSlot(VertexTangentSlotId, VertexTangentSlotName, VertexTangentSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            validSlots.Add(VertexTangentSlotId);
            AddSlot(new ColorRGBMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
            validSlots.Add(ColorSlotId);
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1, ShaderStageCapability.Fragment));
            validSlots.Add(AlphaSlotId);
            AddSlot(new Vector1MaterialSlot(AlphaThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            validSlots.Add(AlphaThresholdSlotId);
            AddSlot(new ColorRGBMaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
            validSlots.Add(EmissionSlotId);

            if (HasDistortion())
            {
                AddSlot(new Vector2MaterialSlot(DistortionSlotId, DistortionSlotDisplayName, DistortionSlotName, SlotType.Input, new Vector2(0.0f, 0.0f), ShaderStageCapability.Fragment));
                validSlots.Add(DistortionSlotId);

                AddSlot(new Vector1MaterialSlot(DistortionBlurSlotId, DistortionBlurSlotName, DistortionBlurSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(DistortionBlurSlotId);
            }

            if (enableShadowMatte.isOn)
            {
                AddSlot(new ColorRGBAMaterialSlot(ShadowTintSlotId, ShadowTintSlotName, ShadowTintSlotName, SlotType.Input, Color.black, ShaderStageCapability.Fragment));
                validSlots.Add(ShadowTintSlotId);
            }

            RemoveSlotsNameNotMatching(validSlots, true);
        }

        protected override VisualElement CreateCommonSettingsElement()
        {
            return new HDUnlitSettingsView(this);
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
            var slotRequirements = validSlots.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition(stageCapability));

            return slotRequirements | (transparencyFog.isOn ? NeededCoordinateSpace.World : 0);
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

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
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
            return validSlots.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresTangent(stageCapability));
        }

        public override void ProcessPreviewMaterial(Material previewMaterial)
        {
            // Fixup the material settings:
            previewMaterial.SetFloat(kSurfaceType, (int)(SurfaceType)surfaceType);
            previewMaterial.SetFloat(kDoubleSidedEnable, doubleSided.isOn ? 1.0f : 0.0f);
            previewMaterial.SetFloat(kAlphaCutoffEnabled, alphaTest.isOn ? 1 : 0);
            previewMaterial.SetFloat(kBlendMode, (int)HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode));
            previewMaterial.SetFloat(kEnableFogOnTransparent, transparencyFog.isOn ? 1.0f : 0.0f);
            previewMaterial.SetFloat(kZTestTransparent, (int)zTest);
            previewMaterial.SetFloat(kTransparentCullMode, (int)transparentCullMode);
            previewMaterial.SetFloat(kZWrite, zWrite.isOn ? 1.0f : 0.0f);
            // No sorting priority for shader graph preview
            previewMaterial.renderQueue = (int)HDRenderQueue.ChangeType(renderingPass, offset: 0, alphaTest: alphaTest.isOn);

            HDUnlitGUI.SetupMaterialKeywordsAndPass(previewMaterial);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // Trunk currently relies on checking material property "_EmissionColor" to allow emissive GI. If it doesn't find that property, or it is black, GI is forced off.
            // ShaderGraph doesn't use this property, so currently it inserts a dummy color (white). This dummy color may be removed entirely once the following PR has been merged in trunk: Pull request #74105
            // The user will then need to explicitly disable emissive GI if it is not needed.
            // To be able to automatically disable emission based on the ShaderGraph config when emission is black,
            // we will need a more general way to communicate this to the engine (not directly tied to a material property).
            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = "_EmissionColor",
                hidden = true,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });

            // ShaderGraph only property used to send the RenderQueueType to the material
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "_RenderQueueType",
                hidden = true,
                value = (int)renderingPass,
            });

            //See SG-ADDITIONALVELOCITY-NOTE
            if (addPrecomputedVelocity.isOn)
            {
                collector.AddShaderProperty(new BooleanShaderProperty
                {
                    value  = true,
                    hidden = true,
                    overrideReferenceName = kAddPrecomputedVelocity,
                });
            }

            if (enableShadowMatte.isOn)
            {
                uint mantissa = ((uint)LightFeatureFlags.Punctual | (uint)LightFeatureFlags.Directional | (uint)LightFeatureFlags.Area) & 0x007FFFFFu;
                uint exponent = 0b10000000u; // 0 as exponent
                collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    hidden = true,
                    value = HDShadowUtils.Asfloat((exponent << 23) | mantissa),
                    overrideReferenceName = HDMaterialProperties.kShadowMatteFilter
                });
            }

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, false, false);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                surfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode),
                sortPriority,
                zWrite.isOn,
                transparentCullMode,
                zTest,
                false,
                transparencyFog.isOn
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, alphaTest.isOn, false);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, doubleSided.isOn ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled);
            HDSubShaderUtilities.AddPrePostPassProperties(collector, false, false);

            base.CollectShaderProperties(collector, generationMode);
        }
    }
}
