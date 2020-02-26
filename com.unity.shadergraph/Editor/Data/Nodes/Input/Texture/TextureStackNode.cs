using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using System.Globalization;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample VT Stack")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNodeBase")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNode2")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNode3")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackNode4")]
    class SampleTextureStackNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV, IHasSettings
    {
        public const int UVInputId = 0;
        [NonSerialized]
        public readonly int[] OutputSlotIds = new int[] { 1, 2, 3, 4 };
        [NonSerialized]
        public readonly int [] TextureInputIds = new int[] { 5, 6, 7, 8 };
        public const int FeedbackSlotId = 9;
        public const int LODInputId = 10;

        static string[] OutputSlotNames = { "Out", "Out2", "Out3", "Out4" };
        static string[] TextureInputNames = { "Texture", "Texture2", "Texture3", "Texture4" };
        const string UVInputNAme = "UV";
        const string FeedbackSlotName = "Feedback";
        const string LODSlotName = "Lod";

        public override bool hasPreview { get { return false; } }
        bool isProcedural;// set internally only

        public enum LodCalculation
        {
            Automatic,
            Explicit,
            //Biased, //TODO: Add support to TextureStack.hlsl first
        }

        [SerializeField]
        LodCalculation m_LodCalculation = LodCalculation.Automatic;
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

        class TextureStackNodeSettingsView : VisualElement
        {
            SampleTextureStackNode m_Node;
            public TextureStackNodeSettingsView(SampleTextureStackNode node)
            {
                m_Node = node;

                PropertySheet ps = new PropertySheet();

                ps.Add(new PropertyRow(new Label("Lod Mode")), (row) =>
                {
                    row.Add(new UIElements.EnumField(m_Node.lodCalculation), (field) =>
                    {
                        field.value = m_Node.lodCalculation;
                        field.RegisterValueChangedCallback(ChangeLod);
                    });
                });

                ps.Add(new PropertyRow(new Label("Num Slots")), (row) =>
                {
                    row.Add(new UIElements.IntegerField(), (field) =>
                    {
                        field.value = m_Node.numSlots;
                        field.RegisterValueChangedCallback(ChangeNumSlots);
                    });
                });

                ps.Add(new PropertyRow(new Label("No Resolve")), (row) =>
                {
                    row.Add(new UnityEngine.UIElements.Toggle(), (field) =>
                    {
                        field.value = m_Node.noFeedback;
                        field.RegisterValueChangedCallback(ChangeFeedback);
                    });
                });

                for (int i=0; i<node.numSlots; i++)
                {
                    int currentIndex = i; //to make lambda by-ref capturing happy
                    ps.Add(new PropertyRow(new Label("Type " + i)), (row) =>
                    {
                        row.Add(new UIElements.EnumField(m_Node.m_TextureTypes[i]), (field) =>
                        {
                            field.value = m_Node.m_TextureTypes[i];
                            field.RegisterValueChangedCallback(evt => { ChangeTextureType(evt, currentIndex); } );
                        });
                    });
                }

                ps.Add(new PropertyRow(new Label("Space")), (row) =>
                {
                    row.Add(new UIElements.EnumField(m_Node.normalMapSpace), (field) =>
                    {
                        field.value = m_Node.normalMapSpace;
                        field.RegisterValueChangedCallback(ChangeNormalMapSpace);
                    });
                });

                Add(ps);
            }

            void ChangeNumSlots(ChangeEvent<int> evt)
            {
                if (Equals(m_Node.numSlots, evt.newValue))
                    return;

                m_Node.owner.owner.RegisterCompleteObjectUndo("NumSlots Flow Change");
                m_Node.numSlots = evt.newValue;
            }

            void ChangeLod(ChangeEvent<Enum> evt)
            {
                if (m_Node.lodCalculation == (LodCalculation)evt.newValue)
                    return;

                m_Node.owner.owner.RegisterCompleteObjectUndo("Lod Mode Change");
                m_Node.lodCalculation = (LodCalculation)evt.newValue;
            }

            void ChangeTextureType(ChangeEvent<Enum> evt, int index)
            {
                if (m_Node.m_TextureTypes[index] == (TextureType)evt.newValue)
                    return;

                m_Node.owner.owner.RegisterCompleteObjectUndo("Texture Type Change");
                m_Node.m_TextureTypes[index] = (TextureType)evt.newValue;
            }

            void ChangeFeedback(ChangeEvent<bool> evt)
            {
                if (m_Node.noFeedback == evt.newValue)
                    return;

                m_Node.owner.owner.RegisterCompleteObjectUndo("Feedback Settings Change");
                m_Node.noFeedback = evt.newValue;
            }

            void ChangeNormalMapSpace(ChangeEvent<Enum> evt)
            {
                if (m_Node.normalMapSpace == (NormalMapSpace)evt.newValue)
                    return;

                m_Node.owner.owner.RegisterCompleteObjectUndo("Normal Map space Change");
                m_Node.normalMapSpace = (NormalMapSpace)evt.newValue;
            }
        }

        public VisualElement CreateSettingsElement()
        {
            return new TextureStackNodeSettingsView(this);
        }

        public SampleTextureStackNode() : this(1) {}

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
            if (m_LodCalculation != LodCalculation.Automatic)
            {
                usedSlots.Add(LODInputId);
            }

            usedSlots.ToArray();

            // Create slots
            AddSlot(new UVMaterialSlot(UVInputId, UVInputNAme, UVInputNAme, UVChannel.UV0));

            for (int i = 0; i < numSlots; i++)
            {
                AddSlot(new Vector4MaterialSlot(OutputSlotIds[i], OutputSlotNames[i], OutputSlotNames[i], SlotType.Output, Vector4.zero, (noFeedback && m_LodCalculation == LodCalculation.Explicit) ? ShaderStageCapability.All : ShaderStageCapability.Fragment));
            }

            if (!isProcedural)
            {
                for (int i = 0; i < numSlots; i++)
                {
                    AddSlot(new Texture2DInputMaterialSlot(TextureInputIds[i], TextureInputNames[i], TextureInputNames[i]));
                }
            }

            if (m_LodCalculation != LodCalculation.Automatic)
            {
                var slot = new Vector1MaterialSlot(LODInputId, LODSlotName, LODSlotName, SlotType.Input, 0.0f, ShaderStageCapability.All, LODSlotName);
                AddSlot(slot);
            }

            if (!noFeedback)
            {
                var slot = new Vector4MaterialSlot(FeedbackSlotId, FeedbackSlotName, FeedbackSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment);
                slot.hidden = true;
                AddSlot(slot);
            }

            RemoveSlotsNameNotMatching(usedSlots, true);
        }

        public static void ValidatNodes(GraphData d)
        {
            ValidatNodes(d.GetNodes<SampleTextureStackNode>());
        }

        public static void ValidatNodes(IEnumerable<SampleTextureStackNode> nodes)
        {
            List<KeyValuePair<string, string>> slotNames = new List<KeyValuePair<string, string>>();

            foreach (SampleTextureStackNode node in nodes)
            {
                for (int i = 0; i < node.numSlots; i++)
                {
                    if (!node.isProcedural)
                    {
                        string value = node.GetSlotValue(node.TextureInputIds[i], GenerationMode.ForReals);
                        string name = node.FindSlot<MaterialSlot>(node.TextureInputIds[i]).displayName;

                        // Check if there is already a slot with the same value
                        int found = slotNames.FindIndex(elem => elem.Key == value);
                        if (found >= 0)
                        {
                            // Add a validation error, values need to be unique
                            node.owner.AddValidationError(node.tempId, $"Slot stack input slot '{value}' shares it's input with another stack input '{slotNames[found].Value}'. Please make sure every slot has unique input textures attached to it.", ShaderCompilerMessageSeverity.Error);
                        }
                        else
                        {
                            // Save it for checking against other slots
                            slotNames.Add(new KeyValuePair<string, string>(value, name));
                        }
                    }

#if PROCEDURAL_VT_IN_GRAPH
                    // Check if there is already a node with the same sampleid
                    SampleTextureStackProceduralNode ssp = node as SampleTextureStackProceduralNode;
                    if (ssp != null)
                    {
                        string value = ssp.GetStackName();
                        string name = ssp.GetStackName();
                        // Check if there is already a slot with the same value
                        //This gave errors when using multiple layers in one stack.
                        //Commented out for the moment so multiple layers are working.
                        //int found = slotNames.FindIndex(elem => elem.Key == value);
                        //if (found >= 0)
                        //{
                        //    // Add a validation error, values need to be unique
                        //    node.owner.AddValidationError(node.tempId, $"This node has the same procedural ID as another node. Nodes need to have different procedural IDs.", ShaderCompilerMessageSeverity.Error);
                        //}
                        //else
                        //{
                            // Save it for checking against other slots
                            slotNames.Add(new KeyValuePair<string, string>(value, name));
                        //}
                    }
#endif
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
            return GetVariableNameForSlot(OutputSlotIds[0]) + "_texturestack";
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

        private string GetSampleFunction()
        {
            if (m_LodCalculation != LodCalculation.Automatic)
            {
                return "SampleStackLod";
            }
            else
            {
                return "SampleStack";
            }
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

            // Not all outputs may be connected (well one is or we wouldn't get called) so we are careful to
            // only generate code for connected outputs
            string stackName = GetStackName();

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
                if (m_LodCalculation == LodCalculation.Automatic)
                {
                    string result = string.Format("StackInfo {0}_info = PrepareStack({1}, {0});"
                        , stackName
                        , GetSlotValue(UVInputId, generationMode));
                    sb.AppendLine(result);
                }
                else
                {
                    string result = string.Format("StackInfo {0}_info = PrepareStackLod({1}, {0}, {2});"
                        , stackName
                        , GetSlotValue(UVInputId, generationMode)
                        , GetSlotValue(LODInputId, generationMode));
                    sb.AppendLine(result);
                }
            }

            for (int i = 0; i < numSlots; i++)
            {
                if (IsSlotConnected(OutputSlotIds[i]))
                {
                    var id = GetTextureName(i, generationMode);
                    string resultLayer = string.Format("$precision4 {1} = {3}({0}_info, {2});"
                            , stackName
                            , GetVariableNameForSlot(OutputSlotIds[i])
                            , id
                            , GetSampleFunction());
                    sb.AppendLine(resultLayer);
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
                string feedBackCode = string.Format("float4 {0} = GetResolveOutput({1}_info);",
                        GetVariableNameForSlot(FeedbackSlotId),
                        stackName);
                sb.AppendLine(feedBackCode);
            }
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
                string feedBackCode = $"float4 {GetVariableNameForSlot(AggregateOutputId)} = {GetSlotValue(AggregateInputFirstId, generationMode)};";
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

                string feedBackCode = $"float4 {GetVariableNameForSlot(AggregateOutputId)} = {arrayName}[ (IN.{ShaderGeneratorNames.PixelCoordinate}.x  + _FrameCount )% (uint){numSlots}];";

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

        // Automatically add a  streaming feedback node and correctly connect it to stack samples are connected to it and it is connected to the master node output
        public static IMasterNode AutoInject(IMasterNode iMasterNode)
        {
            var masterNode = iMasterNode as AbstractMaterialNode;
            var stackNodes = GraphUtil.FindDownStreamNodesOfType<SampleTextureStackNode>(masterNode);

            // Early out if there are no VT nodes in the graph
            if (stackNodes.Count <= 0)
            {
                return iMasterNode;
            }

            bool hasFeedback = false;
            foreach (var node in stackNodes)
            {
                if ( !node.noFeedback )
                {
                    hasFeedback = true;
                }
            }
            if (!hasFeedback)
            {
                return iMasterNode;
            }

            // Duplicate the Graph so we can modify it
            var workingMasterNode = masterNode.owner.ScratchCopy().GetNodeFromGuid(masterNode.guid);

            // inject VTFeedback output slot
            var vtFeedbackSlot = new Vector4MaterialSlot(OutputSlotID, "VTFeedback", "VTFeedback", SlotType.Input, Vector4.one, ShaderStageCapability.Fragment);
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
    }

#if PROCEDURAL_VT_IN_GRAPH
    [Title("Input", "Texture", "Sample Procedural VT Texture Stack")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackProcedural")]
    [FormerName("UnityEditor.ShaderGraph.SampleTextureStackProcedural2")]
    class SampleTextureStackProceduralNode : SampleTextureStackNode
    {
        public SampleTextureStackProceduralNode() : base(1, true)
        { }

        [IntegerControl("Sample ID")]
        public int sampleID
        {
            get { return m_sampleId; }
            set
            {
                if (m_sampleId == value)
                    return;

                m_sampleId = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        [SerializeField]
        int m_sampleId = 0;

        protected override string GetStackName()
        {
            return "Procedural" + m_sampleId;
        }
    }
#endif
}
