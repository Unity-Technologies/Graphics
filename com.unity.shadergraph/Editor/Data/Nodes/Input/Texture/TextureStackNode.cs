using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using System.Globalization;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample VT Stack")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNodeBase")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNode2")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNode3")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNode4")]
    class SampleTextureStackNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV, IHasSettings, IGeneratesInclude, IMayRequireTime
    {
        public const int UVInputId = 0;
        [NonSerialized]
        public readonly int[] OutputSlotIds = new int[] { 1, 2, 3, 4 };
        [NonSerialized]
        public readonly int[] TextureInputIds = new int[] { 5, 6, 7, 8 };
        public const int FeedbackSlotId = 9;
        public const int LODInputId = 10;
        public const int BiasInputId = 11;
        public const int DxInputId = 12;
        public const int DyInputId = 13;

        static string[] OutputSlotNames = { "Out", "Out2", "Out3", "Out4" };
        static string[] TextureInputNames = { "Texture", "Texture2", "Texture3", "Texture4" };
        const string UVInputNAme = "UV";
        const string FeedbackSlotName = "Feedback";
        const string LODSlotName = "Lod";
        const string BiasSlotName = "Bias";
        const string DxSlotName = "Dx";
        const string DySlotName = "Dy";

        public override bool hasPreview { get { return false; } }
        bool isProcedural;// set internally only

        // Keep these in sync with "VirtualTexturing.hlsl"
        public enum LodCalculation
        {
            VtLevel_Automatic = 0,
            VtLevel_Lod = 1,
            VtLevel_Bias = 2,
            VtLevel_Derivatives = 3
        }

        public enum AddresMode
        {
            VtAddressMode_Wrap = 0,
            VtAddressMode_Clamp = 1,
            VtAddressMode_Udim = 2
        }

        public enum FilterMode
        {
            VtFilter_Anisotropic = 0
        }

        public enum UvSpace
        {
            VtUvSpace_Regular = 0,
            VtUvSpace_PreTransformed = 1
        }

        public enum QualityMode
        {
            VtSampleQuality_Low = 0,
            VtSampleQuality_High = 1
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
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
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
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        int m_NumSlots = 1;

        public int numSlots
        {
            get
            {
                return m_NumSlots;
            }
            set
            {
                int cappedSlots = value;
                if (cappedSlots > 4) cappedSlots = 4;

                if (m_NumSlots == cappedSlots)
                    return;

                m_NumSlots = cappedSlots;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_NoFeedback;

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

                // No resolve affects the availability in the vertex shader of the node so we need to trigger a full
                // topo change.
                m_NoFeedback = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        string m_StackName = "";

        public string stackName
        {
            get
            {
                return m_StackName;
            }
            set
            {
                if (m_StackName == value)
                    return;

                m_StackName = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        protected TextureType[] m_TextureTypes = { TextureType.Default, TextureType.Default, TextureType.Default, TextureType.Default };

        // We have one normal/object space field for all layers for now, probably a nice compromise
        // between lots of settings and user flexibility?
        [SerializeField]
        private NormalMapSpace m_NormalMapSpace = NormalMapSpace.Tangent;

        public NormalMapSpace normalMapSpace
        {
            get { return m_NormalMapSpace; }
            set
            {
                if (m_NormalMapSpace == value)
                    return;

                m_NormalMapSpace = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected string CleanupStackName(string name)
        {
            // Procedural stacks allow sharing the same name. This means they will sample the same data as well.
            // This also allows sampling the same stack both from VS and PS.

            if (isProcedural) return name;

            // Make sure there is no other node with the same name
            // if there is patch the current name by adding "_" so it's unique
            var stacks = owner.GetNodes<SampleTextureStackNode>();
            int tries = stacks.Count();

            for (int i = 0; i < tries; i++)
            {
                bool conflict = false;
                foreach (var node in stacks)
                {
                    if (node == this) continue;
                    if (node.GetStackName() == name)
                    {
                        conflict = true;
                        break;
                    }
                }
                if (!conflict)
                {
                    return name;
                }
                // Try again to find a free one
                name = name + "_";
            }

            return name;
        }

        /*
            The panel behind the cogweel node settings
        */
        class TextureStackNodeSettingsView : VisualElement
        {
            SampleTextureStackNode m_Node;
            public TextureStackNodeSettingsView(SampleTextureStackNode node)
            {
                m_Node = node;

                PropertySheet ps = new PropertySheet();

                ps.Add(new PropertyRow(new Label("Stack Name")), (row) =>
                {
                    row.Add(new IdentifierField(), (field) =>
                    {
                        field.value = m_Node.stackName;
                        field.isDelayed = true;
                        field.RegisterValueChangedCallback(evt =>
                        {
                            var clean = m_Node.CleanupStackName(evt.newValue);
                            if (m_Node.stackName == clean)
                                return;

                            m_Node.owner.owner.RegisterCompleteObjectUndo("Stack Name Change");
                            m_Node.stackName = (string)clean;
                            field.SetValueWithoutNotify(m_Node.stackName);// Make sure the cleaned up name is used
                        });
                    });
                });

                ps.Add(new PropertyRow(new Label("Lod Mode")), (row) =>
                {
                    row.Add(new UIElements.EnumField(m_Node.lodCalculation), (field) =>
                    {
                        field.value = m_Node.lodCalculation;
                        field.RegisterValueChangedCallback(evt =>
                        {
                            if (m_Node.lodCalculation == (LodCalculation)evt.newValue)
                                return;

                            m_Node.owner.owner.RegisterCompleteObjectUndo("Lod Mode Change");
                            m_Node.lodCalculation = (LodCalculation)evt.newValue;
                        });
                    });
                });

                ps.Add(new PropertyRow(new Label("Quality")), (row) =>
                {
                    row.Add(new UIElements.EnumField(m_Node.sampleQuality), (field) =>
                    {
                        field.value = m_Node.sampleQuality;
                        field.RegisterValueChangedCallback(evt =>
                        {
                            if (m_Node.sampleQuality == (QualityMode)evt.newValue)
                                return;

                            m_Node.owner.owner.RegisterCompleteObjectUndo("Quality Change");
                            m_Node.sampleQuality = (QualityMode)evt.newValue;
                        });
                    });
                });

                ps.Add(new PropertyRow(new Label("Num Slots")), (row) =>
                {
                    row.Add(new UIElements.IntegerField(), (field) =>
                    {
                        field.value = m_Node.numSlots;
                        field.isDelayed = true;
                        field.RegisterValueChangedCallback(evt =>
                        {
                            if (Equals(m_Node.numSlots, evt.newValue))
                                return;

                            m_Node.owner.owner.RegisterCompleteObjectUndo("NumSlots Flow Change");
                            m_Node.numSlots = evt.newValue;
                            field.SetValueWithoutNotify(m_Node.numSlots);//This may be clamped
                        });
                    });
                });

                ps.Add(new PropertyRow(new Label("No Resolve")), (row) =>
                {
                    row.Add(new UnityEngine.UIElements.Toggle(), (field) =>
                    {
                        field.value = m_Node.noFeedback;
                        field.RegisterValueChangedCallback(evt =>
                        {
                            if (m_Node.noFeedback == evt.newValue)
                                return;

                            m_Node.owner.owner.RegisterCompleteObjectUndo("Feedback Settings Change");
                            m_Node.noFeedback = evt.newValue;
                        });
                    });
                });

                for (int i = 0; i < node.numSlots; i++)
                {
                    int currentIndex = i; //to make lambda by-ref capturing happy
                    ps.Add(new PropertyRow(new Label("Layer " + (i + 1) + " Type")), (row) =>
                      {
                          row.Add(new UIElements.EnumField(m_Node.m_TextureTypes[i]), (field) =>
                          {
                              field.value = m_Node.m_TextureTypes[i];
                              field.RegisterValueChangedCallback(evt =>
                              {
                                  if (m_Node.m_TextureTypes[currentIndex] == (TextureType)evt.newValue)
                                      return;

                                  m_Node.owner.owner.RegisterCompleteObjectUndo("Texture Type Change");
                                  m_Node.m_TextureTypes[currentIndex] = (TextureType)evt.newValue;
                                  m_Node.Dirty(ModificationScope.Graph);
                              });
                          });
                      });
                }

                ps.Add(new PropertyRow(new Label("Space")), (row) =>
                {
                    row.Add(new UIElements.EnumField(m_Node.normalMapSpace), (field) =>
                    {
                        field.value = m_Node.normalMapSpace;
                        field.RegisterValueChangedCallback(evt =>
                        {
                            if (m_Node.normalMapSpace == (NormalMapSpace)evt.newValue)
                                return;

                            m_Node.owner.owner.RegisterCompleteObjectUndo("Normal Map space Change");
                            m_Node.normalMapSpace = (NormalMapSpace)evt.newValue;
                        });
                    });
                });

                Add(ps);
            }
        }

        public VisualElement CreateSettingsElement()
        {
            return new TextureStackNodeSettingsView(this);
        }

        public SampleTextureStackNode() : this(1) { }

        public SampleTextureStackNode(int numSlots, bool procedural = false, bool isLod = false, bool noResolve = false)
        {
            isProcedural = procedural;

            if (numSlots > 4)
            {
                throw new System.Exception("Maximum 4 slots supported");
            }
            this.numSlots = numSlots;
            name = "Sample Texture Stack";

            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            List<int> usedSlots = new List<int>();
            usedSlots.Add(UVInputId);

            for (int i = 0; i < numSlots; i++)
            {
                usedSlots.Add(OutputSlotIds[i]);
                if (!isProcedural)
                    usedSlots.Add(TextureInputIds[i]);
            }

            if (!noFeedback)
            {
                usedSlots.Add(FeedbackSlotId);
            }

            // Create slots
            AddSlot(new UVMaterialSlot(UVInputId, UVInputNAme, UVInputNAme, UVChannel.UV0));

            for (int i = 0; i < numSlots; i++)
            {
                AddSlot(new Vector4MaterialSlot(OutputSlotIds[i], OutputSlotNames[i], OutputSlotNames[i], SlotType.Output, Vector4.zero, (noFeedback && m_LodCalculation == LodCalculation.VtLevel_Lod) ? ShaderStageCapability.All : ShaderStageCapability.Fragment));
            }

            if (!isProcedural)
            {
                for (int i = 0; i < numSlots; i++)
                {
                    AddSlot(new Texture2DInputMaterialSlot(TextureInputIds[i], TextureInputNames[i], TextureInputNames[i]));
                }
            }

            if (m_LodCalculation == LodCalculation.VtLevel_Lod)
            {
                var slot = new Vector1MaterialSlot(LODInputId, LODSlotName, LODSlotName, SlotType.Input, 0.0f, ShaderStageCapability.All, LODSlotName);
                usedSlots.Add(LODInputId);
                AddSlot(slot);
            }

            if (m_LodCalculation == LodCalculation.VtLevel_Bias)
            {
                var slot = new Vector1MaterialSlot(BiasInputId, BiasSlotName, BiasSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment, BiasSlotName);
                usedSlots.Add(BiasInputId);
                AddSlot(slot);
            }

            if (m_LodCalculation == LodCalculation.VtLevel_Derivatives)
            {
                var slot1 = new Vector2MaterialSlot(DxInputId, DxSlotName, DxSlotName, SlotType.Input, Vector2.one, ShaderStageCapability.All, DxSlotName);
                var slot2 = new Vector2MaterialSlot(DyInputId, DySlotName, DySlotName, SlotType.Input, Vector2.one, ShaderStageCapability.All, DySlotName);
                usedSlots.Add(DxInputId);
                usedSlots.Add(DyInputId);
                AddSlot(slot1);
                AddSlot(slot2);
            }

            if (!noFeedback)
            {
                var slot = new Vector4MaterialSlot(FeedbackSlotId, FeedbackSlotName, FeedbackSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment);
                slot.hidden = true;
                AddSlot(slot);
            }

            RemoveSlotsNameNotMatching(usedSlots, true);
            name = GetStackName();
        }

        public static bool SubGraphHasStacks(SubGraphNode node)
        {
            var asset = node.asset;
            foreach(var input in asset.inputs)
            {
                var texInput = input as Texture2DShaderProperty;
                if (texInput != null && !string.IsNullOrEmpty(texInput.textureStack))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<string> GetSubGraphInputStacks(SubGraphNode node)
        {
            // There could be more stacks in the subgraph but as they are not part of inputs they don't
            // leak out so we don't case about those.
            List<string> result = new List<string>(); //todo pooled list
            var asset = node.asset;
            foreach (var input in asset.inputs)
            {
                var texInput = input as Texture2DShaderProperty;
                if (texInput != null && !string.IsNullOrEmpty(texInput.textureStack))
                {
                    result.Add(texInput.textureStack);
                }
            }
            return result;
        }

        public static void ValidatNodes(GraphData d)
        {
            ValidatNodes(d.GetNodes<SampleTextureStackNode>(), d.GetNodes<SubGraphNode>());
        }

        public static void ValidatNodes(IEnumerable<SampleTextureStackNode> nodes, IEnumerable<SubGraphNode> subNodes)
        {
            var valueNameLookup = new Dictionary<string, string>();
            var nodeNames = new HashSet<string>();

            foreach (SampleTextureStackNode node in nodes)
            {
                if (nodeNames.Contains(node.GetStackName()) && !node.isProcedural)
                {
                    // Add a validation error, values need to be unique
                    node.owner.AddValidationError(node.tempId, $"Some stack nodes have the same name '{node.GetStackName()}', please ensure all stack nodes have unique names.", ShaderCompilerMessageSeverity.Error);
                }
                else
                {
                    // Procedural nodes are still added here as we disallow procedural and regular nodes with the same name...
                    nodeNames.Add(node.GetStackName());
                }

                for (int i = 0; i < node.numSlots; i++)
                {
                    if (!node.isProcedural)
                    {
                        string value = node.GetSlotValue(node.TextureInputIds[i], GenerationMode.ForReals);
                        string name = node.FindSlot<MaterialSlot>(node.TextureInputIds[i]).displayName;

                        // Check if there is already a slot with the same value
                        string displayName;
                        if (valueNameLookup.TryGetValue(value, out displayName))
                        {
                            // Add a validation error, values need to be unique
                            node.owner.AddValidationError(node.tempId, $"Input slot '{name}' shares it's value '{value}' with another stack '{displayName}'. Please make sure every slot has unique input textures attached to it.", ShaderCompilerMessageSeverity.Error);
                        }
                        else
                        {
                            // Save it for checking against other slots
                            valueNameLookup.Add(value, node.GetStackName() + " (slot " + name + ")");
                        }
                    }
                }
            }

            foreach (SubGraphNode node in subNodes)
            {
                var subStacks = GetSubGraphInputStacks(node);
                foreach (var subStack in subStacks)
                {
                    // Todo how to exclude procedurals in subgraphs?
                    if (nodeNames.Contains(subStack))
                    {
                        node.owner.AddValidationError(node.tempId, $"Some stack nodes in sub graphs have the same name '{subStack}', please ensure all stack nodes have unique names across the whole shader using them.", ShaderCompilerMessageSeverity.Error);
                    }
                    else
                    {
                        nodeNames.Add(subStack);
                    }
                }

                Dictionary<string, string> valueToStackLookup = node.GetValueToTextureStackDictionary(GenerationMode.ForReals);
                foreach (var kvp in valueToStackLookup)
                {
                    // Check if there is already a slot with the same value
                    string stackName;
                    if (valueNameLookup.TryGetValue(kvp.Key, out stackName))
                    {
                        // Add a validation error, values need to be unique
                        node.owner.AddValidationError(node.tempId, $"Stack '{kvp.Value}' shares it's value '{kvp.Key}' with another stack '{stackName}'. Please make sure every slot has unique input textures attached to it.", ShaderCompilerMessageSeverity.Error);
                    }
                    else
                    {
                        // Save it for checking against other slots
                        valueNameLookup.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        public override void ValidateNode()
        {
            if (isProcedural) return;

            for (int i = 0; i < numSlots; i++)
            {
                var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(TextureInputIds[i]);
                if (textureSlot != null)
                {
                    textureSlot.defaultType = (m_TextureTypes[i] == TextureType.Normal ? Texture2DShaderProperty.DefaultType.Bump : Texture2DShaderProperty.DefaultType.White);
                }
            }
            base.ValidateNode();
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }


        protected virtual string GetStackName()
        {
            if (!string.IsNullOrEmpty(m_StackName))
            {
                return m_StackName;
            }
            else
            {
                return string.Format("TexStack_{0}", GuidEncoder.Encode(guid));
            }
        }

        private string GetTextureName(int layerIndex, GenerationMode generationMode)
        {
            if (isProcedural)
            {
                return GetStackName() + "_stacks_are_not_supported_with_vt_off_" + layerIndex;
            }
            else
            {
                return GetSlotValue(TextureInputIds[layerIndex], generationMode);
            }
        }

        string MakeVtParameters(string variableName, string uvExpr, string lodExpr, string dxExpr, string dyExpr, AddresMode address, FilterMode filter, LodCalculation lod, UvSpace space, QualityMode quality)
        {
            const string VTParametersInputTemplate = @"
                        VtInputParameters {0};
                        {0}.uv = {1};
                        {0}.lodOrOffset = {2};
                        {0}.dx = {3};
                        {0}.dy = {4};
                        {0}.addressMode = {5};
                        {0}.filterMode = {6};
                        {0}.levelMode = {7};
                        {0}.uvMode = {8};
                        {0}.sampleQuality = {9};
            ";

            return string.Format(VTParametersInputTemplate,
                variableName,
                uvExpr,
                (string.IsNullOrEmpty(lodExpr)) ? "0.0f" : lodExpr,
                (string.IsNullOrEmpty(dxExpr)) ? "float2(0.0f, 0.0f)" : dxExpr,
                (string.IsNullOrEmpty(dyExpr)) ? "float2(0.0f, 0.0f)" : dyExpr,
                address.ToString(),
                filter.ToString(),
                lod.ToString(),
                space.ToString(),
                quality.ToString());
        }

        string MakeVtSample(string infoVariable, string textureName, string outputVariableName, LodCalculation lod, QualityMode quality)
        {
            const string SampleTemplate = @"$precision4 {0} = SampleStack({1}, {2}, {3}, {4});";

            return string.Format(SampleTemplate,
                outputVariableName,
                infoVariable,
                lod.ToString(),
                quality.ToString(),
                textureName);
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            // This is not in templates or headers so this error only gets checked in shaders actually using the VT node
            // as vt headers get included even if there are no vt nodes yet.
            sb.AppendLine("#if defined(_SURFACE_TYPE_TRANSPARENT)");
            sb.AppendLine("#error VT cannot be used on transparent surfaces.");
            sb.AppendLine("#endif");
            sb.AppendLine("#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)"); //SHADERPASS is not defined for preview materials so check this first.
            sb.AppendLine("#error VT cannot be used on decals. (DBuffer)");
            sb.AppendLine("#endif");
            sb.AppendLine("#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_DBUFFER_MESH)");
            sb.AppendLine("#error VT cannot be used on decals. (Mesh)");
            sb.AppendLine("#endif");
            sb.AppendLine("#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)");
            sb.AppendLine("#error VT cannot be used on decals. (Projector)");
            sb.AppendLine("#endif");
            sb.AppendLine("#if defined(FORCE_VIRTUAL_TEXTURING_OFF) && defined(VIRTUAL_TEXTURING_SHADER_ENABLED)");
            sb.AppendLine("#error FORCE_VIRTUAL_TEXTURING_OFF define was set after including TextureStack.hlsl without this define. Fix your include order.");
            sb.AppendLine("#endif");

            // Not all outputs may be connected (well one is or we wouldn't get called) so we are careful to
            // only generate code for connected outputs

            string stackName = GetStackName();
            string localVariablePrefix = GetVariableNameForNode();
            string parametersVariableNme = localVariablePrefix + "_pars";
            string infoVariableName = localVariablePrefix + "_info";

            bool anyConnected = false;
            for (int i = 0; i < numSlots; i++)
            {
                if (IsSlotConnected(OutputSlotIds[i]))
                {
                    anyConnected = true;
                    break;
                }
            }
            bool feedbackConnected = IsSlotConnected(FeedbackSlotId); ;
            anyConnected |= feedbackConnected;

            if (anyConnected)
            {
                sb.Append(MakeVtParameters(
                    parametersVariableNme,
                    GetSlotValue(UVInputId, generationMode),
                    (lodCalculation == LodCalculation.VtLevel_Lod) ? GetSlotValue(LODInputId, generationMode) : GetSlotValue(BiasInputId, generationMode),
                    GetSlotValue(DxInputId, generationMode),
                    GetSlotValue(DyInputId, generationMode),
                    AddresMode.VtAddressMode_Wrap,
                    FilterMode.VtFilter_Anisotropic,                    
                    m_LodCalculation,
                    UvSpace.VtUvSpace_Regular,
                    m_SampleQuality));

                sb.AppendLine(string.Format("StackInfo {0} = PrepareStack({1}, {2});"
                                        , infoVariableName
                                        , parametersVariableNme
                                        , stackName));
            }

            for (int i = 0; i < numSlots; i++)
            {
                if (IsSlotConnected(OutputSlotIds[i]))
                {
                    var textureName = GetTextureName(i, generationMode);
                    /*string resultLayer = string.Format("$precision4 {1} = {3}({0}_info, {2});"
                            , stackName
                            , GetVariableNameForSlot(OutputSlotIds[i])
                            , textureName
                            , GetSampleFunction());
                    sb.AppendLine(resultLayer);*/
                    sb.AppendLine(MakeVtSample(infoVariableName, textureName, GetVariableNameForSlot(OutputSlotIds[i]), m_LodCalculation, m_SampleQuality));
                }
            }

            for (int i = 0; i < numSlots; i++)
            {
                if (IsSlotConnected(OutputSlotIds[i]))
                {

                    if (m_TextureTypes[i] == TextureType.Normal)
                    {
                        if (normalMapSpace == NormalMapSpace.Tangent)
                        {
                            sb.AppendLine(string.Format("{0}.rgb = UnpackNormalmapRGorAG({0});", GetVariableNameForSlot(OutputSlotIds[i])));
                        }
                        else
                        {
                            sb.AppendLine(string.Format("{0}.rgb = UnpackNormalRGB({0});", GetVariableNameForSlot(OutputSlotIds[i])));
                        }
                    }
                }
            }

            if (!noFeedback && feedbackConnected)
            {
                //TODO: Investigate if the feedback pass can use halfs
                string feedBackCode = string.Format("float4 {0} = GetResolveOutput({1});",
                        GetVariableNameForSlot(FeedbackSlotId),
                        infoVariableName);
                sb.AppendLine(feedBackCode);
            }
        }

        public void GenerateNodeInclude(IncludeRegistry registry, GenerationMode generationMode)
        {
            // This is not in templates or headers so this error only gets checked in shaders actually using the VT node
            // as vt headers get included even if there are no vt nodes yet.
            registry.ProvideIncludeBlock("disable-vt-if-unsupported", @"#if defined(_SURFACE_TYPE_TRANSPARENT)
#warning VT cannot be used on transparent surfaces.
#define FORCE_VIRTUAL_TEXTURING_OFF 1
#endif
#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
#warning VT cannot be used on decals. (DBuffer)
#define FORCE_VIRTUAL_TEXTURING_OFF 1
#endif
#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_DBUFFER_MESH)
#warning VT cannot be used on decals. (Mesh)
#define FORCE_VIRTUAL_TEXTURING_OFF 1
#endif
#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
#warning VT cannot be used on decals. (Projector)
#define FORCE_VIRTUAL_TEXTURING_OFF 1
#endif");

            registry.ProvideInclude("Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl");
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);

            // Get names of connected textures
            List<string> slotNames = new List<string>();
            for (int i = 0; i < numSlots; i++)
            {
                var id = GetTextureName(i, generationMode);
                slotNames.Add(id);
            }

            string stackName = GetStackName();

            // Add texture stack attributes to any connected textures
            if (!isProcedural)
            {
                int found = 0;
                foreach (var prop in properties.properties.OfType<Texture2DShaderProperty>())
                {
                    int layerIdx = 0;
                    foreach (var inputTex in slotNames)
                    {
                        if (string.Compare(inputTex, prop.referenceName) == 0)
                        {
                            prop.textureStack = stackName + "(" + layerIdx + ")";
                            found++;
                        }
                        layerIdx++;
                    }
                }

                if (found != slotNames.Count)
                {
                    Debug.LogWarning("Could not find some texture properties for stack " + stackName);
                }
            }

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = stackName + "_cb",
                slotNames = slotNames
            });

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = stackName,
                slotNames = slotNames
            });
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
            return true; //HACK: This ensures we repaint in shadergraph so data that gets streamed in also becomes visible.
        }
    }

    class TextureStackAggregateFeedbackNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireRequirePixelCoordinate
    {
        public const int AggregateOutputId = 0;
        const string AggregateOutputName = "FeedbackAggregateOut";

        public const int AggregateInputFirstId = 1;

        public override bool hasPreview { get { return false; } }

        public TextureStackAggregateFeedbackNode()
        {
            name = "Feedback Aggregate";
            UpdateNodeAfterDeserialization();
        }


        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(AggregateOutputId, AggregateOutputName, AggregateOutputName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new int[] { AggregateOutputId });
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var slots = this.GetInputSlots<ISlot>();
            int numSlots = slots.Count();
            if (numSlots == 0)
            {
                return;
            }

            if (numSlots == 1)
            {
                string feedBackCode = $"float4 {GetVariableNameForSlot(AggregateOutputId)} = GetPackedVTFeedback({GetSlotValue(AggregateInputFirstId, generationMode)});";
                sb.AppendLine(feedBackCode);
            }
            else if (numSlots > 1)
            {
                string arrayName = $"{GetVariableNameForSlot(AggregateOutputId)}_array";
                sb.AppendLine($"float4 {arrayName}[{numSlots}];");

                int arrayIndex = 0;
                foreach (var slot in slots)
                {
                    string code = $"{arrayName}[{arrayIndex}] = {GetSlotValue(AggregateInputFirstId + arrayIndex, generationMode)};";
                    sb.AppendLine(code);
                    arrayIndex++;
                }

                string feedBackCode = $"float4 {GetVariableNameForSlot(AggregateOutputId)} = GetPackedVTFeedback({arrayName}[ (IN.{ShaderGeneratorNames.PixelCoordinate}.x  + _FrameCount )% (uint){numSlots}]);";

                sb.AppendLine(feedBackCode);
            }
        }

        public bool RequiresPixelCoordinate(ShaderStageCapability stageCapability)
        {
            var slots = this.GetInputSlots<ISlot>();
            int numSlots = slots.Count();
            return numSlots > 1;
        }

    }

    static class VirtualTexturingFeedback
    {
        public const int OutputSlotID = 22021982;
        public const string subgraphOutputFrefix = "VTFeedbackIn_";

        // Automatically add a  streaming feedback node and correctly connect it to stack samples are connected to it and it is connected to the master node output
        public static IMasterNode AutoInject(IMasterNode iMasterNode)
        {
            Debug.Log("Inject vt feedback");
            var masterNode = iMasterNode as AbstractMaterialNode;
            var stackNodes = GraphUtil.FindDownStreamNodesOfType<SampleTextureStackNode>(masterNode);
            var subgraphNodes = GraphUtil.FindDownStreamNodesOfType<SubGraphNode>(masterNode);
            Debug.Log("SubgraphNodes: " + subgraphNodes.Count);


            // Try to find out early if there are no nodes doing VT feedback in the graph
            // this avoids unnecessary work of copying the graph and patching it.
            if (stackNodes.Count <= 0 && subgraphNodes.Count <= 0)
            {
                return iMasterNode;
            }

            bool hasFeedback = false;
            foreach (var node in stackNodes)
            {
                if ( !node.noFeedback )
                {
                    hasFeedback = true;
                    break;
                }
            }

            if (!hasFeedback) foreach (var node in subgraphNodes)
            {
                foreach (var slot in node.GetOutputSlots<Vector4MaterialSlot>())
                {
                    if (slot.shaderOutputName.StartsWith(subgraphOutputFrefix))
                    {
                        hasFeedback = true;
                        break;
                    }
                }
            }

            if (!hasFeedback)
            {
                Debug.Log("No vt feedback early out");
                return iMasterNode;
            }

            // Duplicate the Graph so we can modify it
            var workingMasterNode = masterNode.owner.ScratchCopy().GetNodeFromGuid(masterNode.guid);

            // inject VTFeedback output slot
            var vtFeedbackSlot = new Vector4MaterialSlot(OutputSlotID, "VTPackedFeedback", "VTPackedFeedback", SlotType.Input, Vector4.one, ShaderStageCapability.Fragment);
            vtFeedbackSlot.hidden = true;
            workingMasterNode.AddSlot(vtFeedbackSlot);

            // Inject Aggregate node
            var feedbackNode = new TextureStackAggregateFeedbackNode();
            workingMasterNode.owner.AddNode(feedbackNode);

            // Add inputs to feedback node
            int i = 0;
            foreach (var node in stackNodes)
            {
                // Find feedback output slot on the vt node
                var stackFeedbackOutputSlot = (node.FindOutputSlot<ISlot>(SampleTextureStackNode.FeedbackSlotId)) as Vector4MaterialSlot;
                if (stackFeedbackOutputSlot == null)
                {
                    // Nodes which are noResolve don't have a resolve slot so just skip them 
                    continue;
                }

                // Create a new slot on the aggregate that is similar to the uv input slot
                string name = "FeedIn_" + i;
                var newSlot = new Vector4MaterialSlot(TextureStackAggregateFeedbackNode.AggregateInputFirstId + i, name, name, SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment);
                newSlot.owner = feedbackNode;
                feedbackNode.AddSlot(newSlot);

                feedbackNode.owner.Connect(stackFeedbackOutputSlot.slotReference, newSlot.slotReference);
                i++;
            }

            Debug.Log("SubgraphNodes_v2: " + subgraphNodes.Count);
            foreach (var node in subgraphNodes)
            {
                Debug.Log("SubgraphNode");
                foreach (var slot in node.GetOutputSlots< Vector4MaterialSlot>())
                {
                    Debug.Log("Slot " + slot.shaderOutputName);
                    if (!slot.shaderOutputName.StartsWith(subgraphOutputFrefix)) continue;

                    // Create a new slot on the aggregate that is similar to the uv input slot
                    string name = "FeedIn_" + i;
                    var newSlot = new Vector4MaterialSlot(TextureStackAggregateFeedbackNode.AggregateInputFirstId + i, name, name, SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment);
                    newSlot.owner = feedbackNode;
                    feedbackNode.AddSlot(newSlot);

                    feedbackNode.owner.Connect(slot.slotReference, newSlot.slotReference);
                    i++;
                }
            }

            // Add input to master node
            var feedbackInputSlot = workingMasterNode.FindInputSlot<ISlot>(OutputSlotID);
            if (feedbackInputSlot == null)
            {
                Debug.LogWarning("Could not find the VT feedback input slot on the master node.");
                return iMasterNode;
            }

            var feedbackOutputSlot = feedbackNode.FindOutputSlot<ISlot>(TextureStackAggregateFeedbackNode.AggregateOutputId);
            if (feedbackOutputSlot == null)
            {
                Debug.LogWarning("Could not find the VT feedback output slot on the aggregate node.");
                return iMasterNode;
            }

            workingMasterNode.owner.Connect(feedbackOutputSlot.slotReference, feedbackInputSlot.slotReference);
            workingMasterNode.owner.ClearChanges();

            return workingMasterNode as IMasterNode;
        }

        // Automatically add a  streaming feedback node and correctly connect it to stack samples are connected to it and it is connected to the master node output
        public static SubGraphOutputNode AutoInjectSubgraph(SubGraphOutputNode masterNode)
        {
            var stackNodes = GraphUtil.FindDownStreamNodesOfType<SampleTextureStackNode>(masterNode);

            // Early out if there are no VT nodes in the graph
            if (stackNodes.Count <= 0)
            {
                Debug.Log("No vt in subgr");
                return masterNode;
            }

            bool hasFeedback = false;
            foreach (var node in stackNodes)
            {
                if (!node.noFeedback)
                {
                    hasFeedback = true;
                }
            }
            if (!hasFeedback)
            {
                Debug.Log("No feedback in subgr");
                return masterNode;
            }

            // Duplicate the Graph so we can modify it
            Debug.Log("clonign graph");
            var workingMasterNode = masterNode.owner.ScratchCopy().GetNodeFromGuid(masterNode.guid);

            // Add inputs to feedback node
            int i = 0;
            foreach (var node in stackNodes)
            {
                // Find feedback output slot on the vt node
                var stackFeedbackOutputSlot = (node.FindOutputSlot<ISlot>(SampleTextureStackNode.FeedbackSlotId)) as Vector4MaterialSlot;
                if (stackFeedbackOutputSlot == null)
                {
                    // Nodes which are noResolve don't have a resolve slot so just skip them
                    Debug.Log("No feedback slot on node, skipping");
                    continue;
                }

                // Create a new slot on the master node for each vt feedback
                string name = subgraphOutputFrefix + i;
                var newSlot = new Vector4MaterialSlot(OutputSlotID + i, name, name, SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment);
                newSlot.owner = workingMasterNode;
                newSlot.hidden = true;
                workingMasterNode.AddSlot(newSlot);

                workingMasterNode.owner.Connect(stackFeedbackOutputSlot.slotReference, newSlot.slotReference);
                i++;

                Debug.Log("Added feedback " + name);
            }

            workingMasterNode.owner.ClearChanges();
            return workingMasterNode as SubGraphOutputNode;
        }
    }

#if PROCEDURAL_VT_IN_GRAPH
    [Title("Input", "Texture", "Sample Procedural VT Texture Stack")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackProcedural")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackProcedural2")]
    class SampleTextureStackProceduralNode : SampleTextureStackNode
    {
        public SampleTextureStackProceduralNode() : base(1, true)
        { }
    }
#endif
}
