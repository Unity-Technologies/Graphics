using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", SampleVirtualTextureNode.DefaultNodeTitle)]
    class SampleVirtualTextureNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV, IMayRequireTime, IMayRequireScreenPosition
    {
        public const string DefaultNodeTitle = "Sample Virtual Texture";

        public const int kMinLayers = 1;
        public const int kMaxLayers = 4;

        // input slots
        public const int UVInputId = 0;
        public const int VirtualTextureInputId = 1;
        public const int LODInputId = 2;
        public const int BiasInputId = 3;
        public const int DxInputId = 4;
        public const int DyInputId = 5;

        // output slots
        [NonSerialized]
        public readonly int[] OutputSlotIds = { 11, 12, 13, 14 };

        const string UVInputName = "UV";
        const string VirtualTextureInputName = "VT";
        const string LODSlotName = "Lod";
        const string BiasSlotName = "Bias";
        const string DxSlotName = "Dx";
        const string DySlotName = "Dy";

        static string[] OutputSlotNames = { "Out", "Out2", "Out3", "Out4" };

        public override bool hasPreview { get { return false; } }

        // Keep these in sync with "VirtualTexturing.hlsl"
        public enum LodCalculation
        {
            [InspectorName("Automatic")]
            VtLevel_Automatic = 0,
            [InspectorName("Lod Level")]
            VtLevel_Lod = 1,
            [InspectorName("Lod Bias")]
            VtLevel_Bias = 2,
            [InspectorName("Derivatives")]
            VtLevel_Derivatives = 3
        }

        public enum AddressMode
        {
            [InspectorName("Wrap")]
            VtAddressMode_Wrap = 0,
            [InspectorName("Clamp")]
            VtAddressMode_Clamp = 1,
        }

        public enum FilterMode
        {
            [InspectorName("Anisotropic")]
            VtFilter_Anisotropic = 0
        }

        public enum UvSpace
        {
            [InspectorName("Regular")]
            VtUvSpace_Regular = 0,
            [InspectorName("Pre Transformed")]
            VtUvSpace_PreTransformed = 1
        }

        public enum QualityMode
        {
            [InspectorName("Low")]
            VtSampleQuality_Low = 0,
            [InspectorName("High")]
            VtSampleQuality_High = 1
        }

        [SerializeField]
        AddressMode m_AddressMode = AddressMode.VtAddressMode_Wrap;
        public AddressMode addressMode
        {
            get
            {
                return m_AddressMode;
            }
            set
            {
                if (m_AddressMode == value)
                    return;

                m_AddressMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        LodCalculation m_LodCalculation = LodCalculation.VtLevel_Automatic;
        public LodCalculation lodCalculation
        {
            get
            {
                return m_LodCalculation;
            }
            set
            {
                if (m_LodCalculation == value)
                    return;

                m_LodCalculation = value;
                RebuildAllSlots(true);       // LOD calculation may have associated slots that need to be updated
                Dirty(ModificationScope.Topological);   // slots ShaderStageCapability could have changed, so trigger Topo change
            }
        }

        [SerializeField]
        QualityMode m_SampleQuality = QualityMode.VtSampleQuality_High;
        public QualityMode sampleQuality
        {
            get
            {
                return m_SampleQuality;
            }
            set
            {
                if (m_SampleQuality == value)
                    return;

                m_SampleQuality = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        private bool m_EnableGlobalMipBias = true;
        public bool enableGlobalMipBias
        {
            get
            {
                return m_EnableGlobalMipBias;
            }
            set
            {
                if (m_EnableGlobalMipBias == value)
                    return;

                m_EnableGlobalMipBias = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_NoFeedback;          // aka !AutomaticStreaming
        public bool noFeedback
        {
            get
            {
                return m_NoFeedback;
            }
            set
            {
                if (m_NoFeedback == value)
                    return;

                m_NoFeedback = value;
                RebuildAllSlots(true);
                Dirty(ModificationScope.Topological);   // slots ShaderStageCapability could have changed, so trigger Topo change
            }
        }

        public SampleVirtualTextureNode() : this(false, false)
        { }

        public SampleVirtualTextureNode(bool isLod = false, bool noResolve = false)
        {
            name = "Sample Virtual Texture";
            synonyms = new string[] { "buffer" };
            UpdateNodeAfterDeserialization();
        }

        public override void Setup()
        {
            UpdateLayerOutputSlots(true);
        }

        // rebuilds the number of output slots, and also updates their ShaderStageCapability
        private int outputLayerSlotCount = 0;
        void UpdateLayerOutputSlots(bool inspectProperty, List<int> usedSlots = null)
        {
            // the default is to show all 4 slots, so we don't lose any existing connections
            int layerCount = kMaxLayers;

            if (inspectProperty)
            {
                var vtProperty = GetSlotProperty(VirtualTextureInputId) as VirtualTextureShaderProperty;
                if (vtProperty != null)
                {
                    layerCount = vtProperty?.value?.layers?.Count ?? kMaxLayers;
                }
                if (outputLayerSlotCount == layerCount)
                {
                    if (usedSlots != null)
                        for (int i = 0; i < layerCount; i++)
                            usedSlots.Add(OutputSlotIds[i]);
                    return;
                }
            }

            for (int i = 0; i < kMaxLayers; i++)
            {
                int outputID = OutputSlotIds[i];
                Vector4MaterialSlot outputSlot = FindSlot<Vector4MaterialSlot>(outputID);
                if (i < layerCount)
                {
                    // add or update it
                    if (outputSlot == null)
                    {
                        string outputName = OutputSlotNames[i];
                        outputSlot = new Vector4MaterialSlot(outputID, outputName, outputName, SlotType.Output, Vector4.zero, (noFeedback && m_LodCalculation == LodCalculation.VtLevel_Lod) ? ShaderStageCapability.All : ShaderStageCapability.Fragment);
                        AddSlot(outputSlot);
                    }
                    else
                    {
                        outputSlot.stageCapability = (noFeedback && m_LodCalculation == LodCalculation.VtLevel_Lod) ? ShaderStageCapability.All : ShaderStageCapability.Fragment;
                    }
                    if (usedSlots != null)
                        usedSlots.Add(outputID);
                }
                else
                {
                    // remove it
                    if (outputSlot != null)
                        RemoveSlot(OutputSlotIds[i]);
                }
            }
            outputLayerSlotCount = layerCount;
        }

        public void RebuildAllSlots(bool inspectProperty)
        {
            List<int> usedSlots = new List<int>();

            AddSlot(new UVMaterialSlot(UVInputId, UVInputName, UVInputName, UVChannel.UV0));
            usedSlots.Add(UVInputId);

            AddSlot(new VirtualTextureInputMaterialSlot(VirtualTextureInputId, VirtualTextureInputName, VirtualTextureInputName));
            usedSlots.Add(VirtualTextureInputId);

            // at this point we can't tell how many output slots we will have (because we can't find the VT property yet)
            // so, we create all of the possible output slots, so any edges created will connect properly
            // then we can trim down the set of slots later..
            UpdateLayerOutputSlots(inspectProperty, usedSlots);

            // Create slots

            if (m_LodCalculation == LodCalculation.VtLevel_Lod)
            {
                var slot = new Vector1MaterialSlot(LODInputId, LODSlotName, LODSlotName, SlotType.Input, 0.0f, ShaderStageCapability.All, LODSlotName);
                AddSlot(slot);
                usedSlots.Add(LODInputId);
            }

            if (m_LodCalculation == LodCalculation.VtLevel_Bias)
            {
                var slot = new Vector1MaterialSlot(BiasInputId, BiasSlotName, BiasSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment, BiasSlotName);
                AddSlot(slot);
                usedSlots.Add(BiasInputId);
            }

            if (m_LodCalculation == LodCalculation.VtLevel_Derivatives)
            {
                var slot1 = new Vector2MaterialSlot(DxInputId, DxSlotName, DxSlotName, SlotType.Input, Vector2.one, ShaderStageCapability.All, DxSlotName);
                var slot2 = new Vector2MaterialSlot(DyInputId, DySlotName, DySlotName, SlotType.Input, Vector2.one, ShaderStageCapability.All, DySlotName);
                AddSlot(slot1);
                AddSlot(slot2);
                usedSlots.Add(DxInputId);
                usedSlots.Add(DyInputId);
            }

            RemoveSlotsNameNotMatching(usedSlots, true);
        }

        public override void UpdateNodeAfterDeserialization()
        {
            RebuildAllSlots(false);
        }

        const string k_NoPropertyConnected = "A VirtualTexture property must be connected to the VT slot";
        public override void ValidateNode()
        {
            base.ValidateNode();
            if (!IsSlotConnected(VirtualTextureInputId))
            {
                owner.AddValidationError(objectId, k_NoPropertyConnected);
            }
            else
            {
                var vtProp = GetSlotProperty(VirtualTextureInputId) as VirtualTextureShaderProperty;
                if (vtProp == null)
                {
                    owner.AddValidationError(objectId, $"VT slot is not connected to a valid VirtualTexture property");
                }
            }
        }

        public string GetFeedbackVariableName()
        {
            return GetVariableNameForNode() + "_fb";
        }

        void AppendVtParameters(ShaderStringBuilder sb, string uvExpr, string lodExpr, string dxExpr, string dyExpr, AddressMode address, FilterMode filter, LodCalculation lod, UvSpace space, QualityMode quality, bool enableGlobalMipBias)
        {
            sb.AppendLine("VtInputParameters vtParams;");
            sb.AppendLine("vtParams.uv = " + uvExpr + ";");
            sb.AppendLine("vtParams.lodOrOffset = " + lodExpr + ";");
            sb.AppendLine("vtParams.dx = " + dxExpr + ";");
            sb.AppendLine("vtParams.dy = " + dyExpr + ";");
            sb.AppendLine("vtParams.addressMode = " + address + ";");
            sb.AppendLine("vtParams.filterMode = " + filter + ";");
            sb.AppendLine("vtParams.levelMode = " + lod + ";");
            sb.AppendLine("vtParams.uvMode = " + space + ";");
            sb.AppendLine("vtParams.sampleQuality = " + quality + ";");
            sb.AppendLine("vtParams.enableGlobalMipBias = " + (enableGlobalMipBias ? "1" : "0") + ";");
            sb.AppendLine("#if defined(SHADER_STAGE_RAY_TRACING)");
            sb.AppendLine("if (vtParams.levelMode == VtLevel_Automatic || vtParams.levelMode == VtLevel_Bias)");
            using (sb.BlockScope())
            {
                sb.AppendLine("vtParams.levelMode = VtLevel_Lod;");
                sb.AppendLine("vtParams.lodOrOffset = 0.0f;");
            }
            sb.AppendLine("#endif");
        }

        void AppendVtSample(ShaderStringBuilder sb, string propertiesName, string vtInputVariable, string infoVariable, int layerIndex, string outputVariableName)
        {
            sb.TryAppendIndentation();
            sb.Append(outputVariableName); sb.Append(" = ");
            sb.Append("SampleVTLayerWithTextureType(");
            sb.Append(propertiesName); sb.Append(", ");
            sb.Append(vtInputVariable); sb.Append(", ");
            sb.Append(infoVariable); sb.Append(", ");
            sb.Append(layerIndex.ToString()); sb.Append(");");
            sb.AppendNewLine();
        }

        // Node generations
        string GetFunctionName(out List<int> layerIndices)
        {
            string name = "SampleVirtualTexture_" + addressMode + "_" + lodCalculation + "_" + m_SampleQuality;
            layerIndices = new List<int>();

            if (IsSlotConnected(VirtualTextureInputId))
            {
                var vtProperty = GetSlotProperty(VirtualTextureInputId) as VirtualTextureShaderProperty;
                if (vtProperty != null)
                {
                    int layerCount = vtProperty.value.layers.Count;
                    for (int layer = 0; layer < layerCount; layer++)
                    {
                        if (IsSlotConnected(OutputSlotIds[layer]))
                        {
                            layerIndices.Add(layer);
                            name = name + "_" + layer;
                        }
                    }
                }
            }

            return name;
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            string functionName = GetFunctionName(out var layerOutputLayerIndex);

            if (layerOutputLayerIndex.Count <= 0)
                return;

            registry.ProvideFunction(functionName, s =>
            {
                string lodExpr = "0.0f";
                string dxExpr = "0.0f";
                string dyExpr = "0.0f";

                // function header
                s.TryAppendIndentation();
                s.Append("float4 ");
                s.Append(functionName);
                s.Append("(float2 uv");
                switch (lodCalculation)
                {
                    case LodCalculation.VtLevel_Lod:
                        s.Append(", float lod");
                        lodExpr = "lod";
                        break;
                    case LodCalculation.VtLevel_Bias:
                        s.Append(", float bias");
                        lodExpr = "bias";
                        break;
                    case LodCalculation.VtLevel_Derivatives:
                        s.Append(", float2 dx, float2 dy");
                        dxExpr = "dx";
                        dyExpr = "dy";
                        break;
                }
                s.Append(", VTPropertyWithTextureType vtProperty");
                for (int i = 0; i < layerOutputLayerIndex.Count; i++)
                {
                    s.Append(", out float4 Layer" + layerOutputLayerIndex[i]);
                }
                s.Append(")");
                s.AppendNewLine();

                // function body
                using (s.BlockScope())
                {
                    AppendVtParameters(
                        s,
                        "uv",
                        lodExpr,
                        dxExpr,
                        dyExpr,
                        m_AddressMode,
                        FilterMode.VtFilter_Anisotropic,
                        m_LodCalculation,
                        UvSpace.VtUvSpace_Regular,
                        m_SampleQuality,
                        m_EnableGlobalMipBias);

                    s.AppendLine("StackInfo info = PrepareVT(vtProperty.vtProperty, vtParams);");

                    for (int i = 0; i < layerOutputLayerIndex.Count; i++)
                    {
                        // sample virtual texture layer
                        int layer = layerOutputLayerIndex[i];
                        AppendVtSample(s, "vtProperty", "vtParams", "info", layer, "Layer" + layer);
                    }

                    s.AppendLine("return GetResolveOutput(info);");
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            bool success = false;
            if (IsSlotConnected(VirtualTextureInputId))
            {
                var vtProperty = GetSlotProperty(VirtualTextureInputId) as VirtualTextureShaderProperty;
                if (vtProperty != null)
                {
                    var layerOutputVariables = new List<string>();
                    int layerCount = vtProperty.value.layers.Count;
                    for (int i = 0; i < layerCount; i++)
                    {
                        if (IsSlotConnected(OutputSlotIds[i]))
                        {
                            // declare output variables up front
                            string layerOutputVariable = GetVariableNameForSlot(OutputSlotIds[i]);
                            sb.AppendLine("$precision4 " + layerOutputVariable + ";");
                            layerOutputVariables.Add(layerOutputVariable);
                        }
                    }

                    if (layerOutputVariables.Count > 0)
                    {
                        // assign feedback variable
                        sb.TryAppendIndentation();
                        if (!noFeedback)
                        {
                            sb.Append("float4 ");
                            sb.Append(GetFeedbackVariableName());
                            sb.Append(" = ");
                        }
                        sb.Append(GetFunctionName(out var unused));
                        sb.Append("(");
                        sb.Append(GetSlotValue(UVInputId, generationMode));
                        switch (lodCalculation)
                        {
                            case LodCalculation.VtLevel_Lod:
                            case LodCalculation.VtLevel_Bias:
                                sb.Append(", ");
                                sb.Append((lodCalculation == LodCalculation.VtLevel_Lod) ? GetSlotValue(LODInputId, generationMode) : GetSlotValue(BiasInputId, generationMode));
                                break;
                            case LodCalculation.VtLevel_Derivatives:
                                sb.Append(", ");
                                sb.Append(GetSlotValue(DxInputId, generationMode));
                                sb.Append(", ");
                                sb.Append(GetSlotValue(DyInputId, generationMode));
                                break;
                        }
                        sb.Append(", ");
                        sb.Append(vtProperty.referenceName);
                        foreach (string layerOutputVariable in layerOutputVariables)
                        {
                            sb.Append(", ");
                            sb.Append(layerOutputVariable);
                        }
                        sb.Append(");");
                        sb.AppendNewLine();
                        success = true;
                    }
                }
            }


            if (!success)
            {
                // set all outputs to zero
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (IsSlotConnected(OutputSlotIds[i]))
                    {
                        // declare output variables up front
                        string layerOutputVariable = GetVariableNameForSlot(OutputSlotIds[i]);
                        sb.AppendLine("$precision4 " + layerOutputVariable + " = 0;");
                    }
                }
                // TODO: should really just disable feedback in this case (need different feedback interface to do this)
                sb.AppendLine("$precision4 " + GetFeedbackVariableName() + " = 1;");
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            // this adds default properties for all of our unconnected inputs
            base.CollectShaderProperties(properties, generationMode);
        }

        public bool RequiresMeshUV(Internal.UVChannel channel, ShaderStageCapability stageCapability)
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

        public bool RequiresTime()
        {
            // HACK: This ensures we repaint in shadergraph so data that gets streamed in also becomes visible.
            return true;
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            // Feedback dithering requires screen position (and only works in Pixel Shader currently)
            return stageCapability.HasFlag(ShaderStageCapability.Fragment) && !noFeedback;
        }
    }
}
