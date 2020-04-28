using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", SampleTextureStackNode.DefaultNodeTitle)]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNodeBase")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNode2")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNode3")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNode4")]
    class SampleTextureStackNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV, IHasSettings, IGeneratesInclude, IMayRequireTime
    {
        public const string DefaultNodeTitle = "Sample VT Stack";

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
            [InspectorName("Automatic")]
            VtLevel_Automatic = 0,
            [InspectorName("Lod Level")]
            VtLevel_Lod = 1,
            [InspectorName("Lod Bias")]
            VtLevel_Bias = 2,
            [InspectorName("Derivatives")]
            VtLevel_Derivatives = 3
        }

        public enum AddresMode
        {
            [InspectorName("Wrap")]
            VtAddressMode_Wrap = 0,
            [InspectorName("Clamp")]
            VtAddressMode_Clamp = 1,
            [InspectorName("Udim")]
            VtAddressMode_Udim = 2
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
                    // Check if the names as emitted in the shader will clash
                    if (node.GetStackName() == this.ProcessStackName(name) )
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
            True if the masternode of the graph this node is currently in supports virtual texturing.
        */ 
        private bool supportedByMasterNode
        {
            get
            {
                var masterNode = owner?.GetNodes<IMasterNode>().FirstOrDefault();
                return masterNode?.virtualTexturingEnabled ?? false;
            }
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

                ps.Add(new PropertyRow(new Label("No Feedback")), (row) =>
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

#if !ENABLE_VIRTUALTEXTURES
                ps.Add(new HelpBoxRow(MessageType.Warning), (row) => row.Add(new Label("VT is disabled, this node will do regular 2D sampling.")));
#endif
                if (!m_Node.supportedByMasterNode)
                {
                    ps.Add(new HelpBoxRow(MessageType.Warning), (row) => row.Add(new Label("The current master node does not support VT, this node will do regular 2D sampling.")));
                }

                IVirtualTexturingEnabledRenderPipeline vtRp = GraphicsSettings.currentRenderPipeline as IVirtualTexturingEnabledRenderPipeline;
                if (vtRp == null || vtRp.virtualTexturingEnabled == false)
                {
                    ps.Add(new HelpBoxRow(MessageType.Warning), (row) => row.Add(new Label("The current render pipeline does not support VT." + ((vtRp ==null) ? "(Interface not implemented by" + GraphicsSettings.currentRenderPipeline.GetType().Name + ")" : "(virtualTexturingEnabled == false)"))));
                }

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
            name = String.IsNullOrEmpty(m_StackName) ? DefaultNodeTitle : m_StackName;
        }

        // TODO: remove unnecessary
        public static bool SubGraphHasStacks(SubGraphNode node)
        {
            var asset = node.asset;
            foreach (var input in asset.inputs)
            {
                var texInput = input as Texture2DShaderProperty;
                if (texInput != null && !string.IsNullOrEmpty(texInput.textureStack))
                {
                    return true;
                }
            }
            return false;
        }

        // TODO: remove unnecessary
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

        // Texture stacks have some additional 'global' validation to do on the whole graph. This validates this.
        public static void ValidateTextureStacks(GraphData d)
        {
            var nodes = d.GetNodes<SampleTextureStackNode>();
            var subNodes = d.GetNodes<SubGraphNode>();
            var valueNameLookup = new Dictionary<string, string>();
            var nodeNames = new HashSet<string>();

            foreach (SampleTextureStackNode node in nodes)
            {
                if (nodeNames.Contains(node.GetStackName()) && !node.isProcedural)
                {
                    // Add a validation error, values need to be unique
                    node.owner.AddValidationError(node.guid, $"Some stack nodes have the same name '{node.GetStackName()}', please ensure all stack nodes have unique names.", ShaderCompilerMessageSeverity.Error);
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
                            node.owner.AddValidationError(node.guid, $"Input slot '{name}' shares it's value '{value}' with another stack '{displayName}'. Please make sure every slot has unique input textures attached to it.", ShaderCompilerMessageSeverity.Error);
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
                        node.owner.AddValidationError(node.guid, $"A stack node in a subgraph which is exposed through a texture argument has the same name '{subStack}', please ensure all stack nodes have unique names across the whole shader using them.", ShaderCompilerMessageSeverity.Error);
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
                        node.owner.AddValidationError(node.guid, $"Stack '{kvp.Value}' shares it's value '{kvp.Key}' with another stack '{stackName}'. Please make sure every slot has unique input textures attached to it.", ShaderCompilerMessageSeverity.Error);
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

        protected virtual string ProcessStackName(string potentialName)
        {
            // We do a poor man's namespacing here since the user's entered name could clash with existing symbols defined in the shaders or templates
            // adding variables in templates later could also suddenly break user's stacks so any stacks are prefixed by a "namespace".
            if (!string.IsNullOrEmpty(potentialName))
            {
                return "StackNodeNamespace_" + potentialName;
            }
            else
            {
                return string.Format("TexStack_{0}", GuidEncoder.Encode(guid));
            }
        }

        protected virtual string GetStackName()
        {
            return ProcessStackName(m_StackName);
        }

        public string GetFeedbackVariableName()
        {
            return GetVariableNameForNode() + "_fb";
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
#if defined(SHADER_STAGE_RAY_TRACING)
                        if ({0}.levelMode == VtLevel_Automatic || {0}.levelMode == VtLevel_Bias)
                        {{
                            {0}.levelMode = VtLevel_Lod;
                            {0}.lodOrOffset = 0.0f;
                        }}
#endif
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

            if (!noFeedback)
            {
                //TODO: Investigate if the feedback pass can use halfs
                string feedBackCode = string.Format("float4 {0} = GetResolveOutput({1});",
                        GetFeedbackVariableName(),
                        infoVariableName);
                sb.AppendLine(feedBackCode);
            }
        }

        public void GenerateNodeInclude(IncludeCollection registry, GenerationMode generationMode)
        {
            // This is not in templates or headers so this error only gets checked in shaders actually using the VT node
            // as vt headers get included even if there are no vt nodes yet.
            IVirtualTexturingEnabledRenderPipeline vtRp = GraphicsSettings.currentRenderPipeline as IVirtualTexturingEnabledRenderPipeline;

            /*if (!supportedByMasterNode)
            {
                // The master node is not white listed for VT just use regular textures even if vt is on for this project.
                registry.ProvideIncludeBlock("disable-vt-if-unsupported-node", "#define FORCE_VIRTUAL_TEXTURING_OFF 1");
            }
            else if (vtRp == null || vtRp.virtualTexturingEnabled == false)
            {
                // The master render pipeline does not support VT just use regular textures even if vt is on for this project.
                registry.ProvideIncludeBlock("disable-vt-if-unsupported-node", "#define FORCE_VIRTUAL_TEXTURING_OFF 1");
            }
            else
            {
                // The master node supports VT but certain keyword settings in child materials may still break it. We catch this here.
                registry.ProvideIncludeBlock("disable-vt-if-unsupported", @"#if defined(_SURFACE_TYPE_TRANSPARENT)
#define FORCE_VIRTUAL_TEXTURING_OFF 1
#error VT cannot be used on transparent surfaces.
#endif");
            }*/

            // Always include the header even if vt is off this ensures the macros providing fallbacks are existing.
            registry.Add("Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl", IncludeLocation.Pregraph);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            // this adds default properties for all of our unconnected inputs
            base.CollectShaderProperties(properties, generationMode);

            // this gets the texture variable names for each of our texture inputs
            List<string> slotNames = new List<string>();
            for (int i = 0; i < numSlots; i++)
            {
                var id = GetTextureName(i, generationMode);
                slotNames.Add(id);
            }

            string stackName = GetStackName();

            // search all properties to find any that match the texture variable names.. and flag those with the texture stack that uses them
            // this should only ever find properties that were collected above
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

            // TODO: remove these, replace with the VirtualTextureShaderProperty
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

    static class VirtualTexturingFeedbackUtils
    {
        public const string FeedbackSurfaceDescriptionVariableName = "VTPackedFeedback";

        public static int  CountFeedbackVariables(
            List<AbstractMaterialNode> activeNodesForPass)
        {
            int result = 0;

            foreach (var node in activeNodesForPass)
            {
                if (node is SampleTextureStackNode stNode)
                {
                    result += (stNode.noFeedback) ? 0 : 1;
                }

                if (node is SubGraphNode sgNode)
                {
                    if (sgNode.asset == null) continue;
                    result += sgNode.asset.vtFeedbackVariables.Count;
                }
            }

            return result;
        }

        public static void GenerateVirtualTextureFeedback(
            List<AbstractMaterialNode> downstreamNodesIncludingRoot,
            List<int>[] keywordPermutationsPerNode,
            ShaderStringBuilder surfaceDescriptionFunction,
            KeywordCollector shaderKeywords)
        {
            surfaceDescriptionFunction.AppendLine("// Collect VT feedback");

            // A note on how we handle vt feedback in combination with keywords:
            // We essentially generate a fully separate feedback path for each permutation of keywords
            // so per permutation we gather variables contribution to feedback and we generate
            // feedback gathering for each permutation individually.

            var feedbackVariablesPerPermutation = PooledList<PooledList<string>>.Get();
            try
            {
                if (shaderKeywords.permutations.Count >= 1)
                {
                    for (int i = 0; i < shaderKeywords.permutations.Count; i++)
                    {
                        feedbackVariablesPerPermutation.Add(PooledList<string>.Get());
                    }
                }
                else
                {
                    // Create a dummy single permutation
                    feedbackVariablesPerPermutation.Add(PooledList<string>.Get());
                }

                int index = 0; //for keywordPermutationsPerNode
                foreach (var node in downstreamNodesIncludingRoot)
                {
                    if (node is SampleTextureStackNode stNode)
                    {
                        if (stNode.noFeedback) continue;
                        if (keywordPermutationsPerNode[index] == null)
                        {
                            Debug.Assert(shaderKeywords.permutations.Count == 0, $"Shader has {shaderKeywords.permutations.Count} permutations but keywordPermutationsPerNode of some nodes are null." );
                            feedbackVariablesPerPermutation[0].Add(stNode.GetFeedbackVariableName());
                        }
                        else
                        {
                            foreach (int perm in keywordPermutationsPerNode[index])
                            {
                                feedbackVariablesPerPermutation[perm].Add(stNode.GetFeedbackVariableName());
                            }
                        }
                    }

                    if (node is SampleVirtualTextureNode vtNode)
                    {
                        if (vtNode.noFeedback) continue;
                        if (keywordPermutationsPerNode[index] == null)
                        {
                            Debug.Assert(shaderKeywords.permutations.Count == 0, $"Shader has {shaderKeywords.permutations.Count} permutations but keywordPermutationsPerNode of some nodes are null.");
                            feedbackVariablesPerPermutation[0].Add(vtNode.GetFeedbackVariableName());
                        }
                        else
                        {
                            foreach (int perm in keywordPermutationsPerNode[index])
                            {
                                feedbackVariablesPerPermutation[perm].Add(vtNode.GetFeedbackVariableName());
                            }
                        }
                    }

                    if (node is SubGraphNode sgNode)
                    {
                        if (sgNode.asset == null) continue;
                        if (keywordPermutationsPerNode[index] == null)
                        {
                            Debug.Assert(shaderKeywords.permutations.Count == 0, $"Shader has {shaderKeywords.permutations.Count} permutations but keywordPermutationsPerNode of some nodes are null.");
                            foreach (var feedbackSlot in sgNode.asset.vtFeedbackVariables)
                            {

                                feedbackVariablesPerPermutation[0].Add(node.GetVariableNameForNode() + "_" + feedbackSlot);
                            }
                        }
                        else
                        {
                            foreach (var feedbackSlot in sgNode.asset.vtFeedbackVariables)
                            {
                                foreach (int perm in keywordPermutationsPerNode[index])
                                {
                                    feedbackVariablesPerPermutation[perm].Add(node.GetVariableNameForNode() + "_" + feedbackSlot);
                                }
                            }
                        }
                    }

                    index++;
                }

                index = 0;
                foreach (var feedbackVariables in feedbackVariablesPerPermutation)
                {
                    // If it's a dummy single always-on permutation don't put an ifdef around the code
                    if (shaderKeywords.permutations.Count >= 1)
                    {
                        surfaceDescriptionFunction.AppendLine(KeywordUtil.GetKeywordPermutationConditional(index));
                    }

                    if (feedbackVariables.Count == 0)
                    {
                        string feedBackCode = $"surface.VTPackedFeedback = float4(1.0f,1.0f,1.0f,.0f);";
                        surfaceDescriptionFunction.AppendLine(feedBackCode);
                    }
                    else if (feedbackVariables.Count == 1)
                    {
                        string feedBackCode = $"surface.VTPackedFeedback = GetPackedVTFeedback({feedbackVariables[0]});";
                        surfaceDescriptionFunction.AppendLine(feedBackCode);
                    }
                    else if (feedbackVariables.Count > 1)
                    {
                        string arrayName = $"VTFeedback_array";
                        surfaceDescriptionFunction.AppendLine($"float4 {arrayName}[{feedbackVariables.Count}];");

                        int arrayIndex = 0;
                        foreach (var variable in feedbackVariables)
                        {
                            string code = $"{arrayName}[{arrayIndex}] = {variable};";
                            surfaceDescriptionFunction.AppendLine(code);
                            arrayIndex++;
                        }

                        string feedBackCode = $"surface.{FeedbackSurfaceDescriptionVariableName} = GetPackedVTFeedback({arrayName}[ (IN.{ShaderGeneratorNames.ScreenPosition}.x  + _FrameCount )% (uint){feedbackVariables.Count}]);";

                        surfaceDescriptionFunction.AppendLine(feedBackCode);
                    }

                    if (shaderKeywords.permutations.Count >= 1)
                    {
                        surfaceDescriptionFunction.AppendLine("#endif");
                    }

                    index++;
                }
            }
            finally
            {
                foreach (var list in feedbackVariablesPerPermutation)
                {
                    list.Dispose();
                }
                feedbackVariablesPerPermutation.Dispose();
                surfaceDescriptionFunction.AppendLine("// END Collect VT feedback");
            }
        }

        // Automatically add a  streaming feedback node and correctly connect it to stack samples are connected to it and it is connected to the master node output
        public static List<string> GetFeedbackVariables(SubGraphOutputNode masterNode)
        {
            // TODO: make use a generic interface instead of hard-coding the node types that we need to look at here
            var stackNodes = GraphUtil.FindDownStreamNodesOfType<SampleTextureStackNode>(masterNode);
            var VTNodes = GraphUtil.FindDownStreamNodesOfType<SampleVirtualTextureNode>(masterNode);
            var subGraphNodes = GraphUtil.FindDownStreamNodesOfType<SubGraphNode>(masterNode);

            List<string> result = new List<string>();

            // Early out if there are no nodes we care about in the graph
            // Debug.Log("StackNodes: " + stackNodes.Count + " VTNodes: " + VTNodes.Count + " SubGraphs: " + subGraphNodes.Count);
            if (stackNodes.Count <= 0 && subGraphNodes.Count <= 0 && VTNodes.Count <= 0)
            {
                return result;
            }

            // Add inputs to feedback node
            foreach (var node in stackNodes)
            {
                if (node.noFeedback) continue;
                result.Add(node.GetFeedbackVariableName());
            }

            foreach (var node in VTNodes)
            {
                if (node.noFeedback) continue;
                result.Add(node.GetFeedbackVariableName());
            }

            foreach (var node in subGraphNodes)
            {
                if (node.asset == null) continue;
                // TODO: subgraph.GetFeedbackVariableNames(...)
                foreach (var feedbackSlot in node.asset.vtFeedbackVariables)
                {
                    result.Add(node.GetVariableNameForNode() + "_" + feedbackSlot);
                }
            }

            return result;
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
