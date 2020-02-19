using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using System.Globalization;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering;

namespace UnityEditor.ShaderGraph
{
    //[Title("Input", "Texture", "Sample Stack")]
    class SampleTextureStackNodeBase : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int UVInputId = 0;

        [NonSerialized]
        public int[] OutputSlotIds = new int[4];

        [NonSerialized]
        public int[] TextureInputIds = new int[4];

        [NonSerialized]
        public int FeedbackSlotId;

        static string[] OutputSlotNames = { "Out", "Out2", "Out3", "Out4" };
        static string[] TextureInputNames = { "Texture", "Texture2", "Texture3", "Texture4" };
        const string UVInputNAme = "UV";
        const string FeedbackSlotName = "Feedback";

        int numSlots;
        int[] liveIds;
        bool isProcedural;

        public override bool hasPreview { get { return false; } }

        [SerializeField]
        protected TextureType[] m_TextureTypes = { TextureType.Default, TextureType.Default, TextureType.Default, TextureType.Default };

        // We have one normal/object space field for all layers for now, probably a nice compromise
        // between lots of settings and user flexibility?
        [SerializeField]
        private NormalMapSpace m_NormalMapSpace = NormalMapSpace.Tangent;

        [EnumControl("Space")]
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

        public SampleTextureStackNodeBase(int numSlots, bool procedural = false)
        {
            isProcedural = procedural;

            if (numSlots > 4)
            {
                throw new System.Exception("Maximum 4 slots supported");
            }
            this.numSlots = numSlots;
            name = "Sample Texture Stack " + numSlots;

            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            // Allocate IDs
            List<int> usedSlots = new List<int>();
            usedSlots.Add(UVInputId);

            for (int i = 0; i < numSlots; i++)
            {
                OutputSlotIds[i] = UVInputId + 1 + i;
                TextureInputIds[i] = UVInputId + 1 + numSlots + i;

                usedSlots.Add(OutputSlotIds[i]);
                if (!isProcedural)
                    usedSlots.Add(TextureInputIds[i]);
            }

            FeedbackSlotId = UVInputId + 1 + numSlots * 2;
            usedSlots.Add(FeedbackSlotId);

            liveIds = usedSlots.ToArray();

            // Create slots
            AddSlot(new UVMaterialSlot(UVInputId, UVInputNAme, UVInputNAme, UVChannel.UV0));

            for (int i = 0; i < numSlots; i++)
            {
                AddSlot(new Vector4MaterialSlot(OutputSlotIds[i], OutputSlotNames[i], OutputSlotNames[i], SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            }

            if (!isProcedural)
            {
                for (int i = 0; i < numSlots; i++)
                {
                    AddSlot(new Texture2DInputMaterialSlot(TextureInputIds[i], TextureInputNames[i], TextureInputNames[i]));
                }
            }

            var slot = new Vector4MaterialSlot(FeedbackSlotId, FeedbackSlotName, FeedbackSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment);
            slot.hidden = true;
            AddSlot(slot);

            RemoveSlotsNameNotMatching(liveIds);
        }

        public static void ValidatNodes(GraphData d)
        {
            ValidatNodes(d.GetNodes<SampleTextureStackNodeBase>());       
        }

        public static void ValidatNodes(IEnumerable<SampleTextureStackNodeBase> nodes)
        {
            List<KeyValuePair<string, string>> slotNames = new List<KeyValuePair<string, string>>();

            foreach (SampleTextureStackNodeBase node in nodes)
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
                    SampleTextureStackProcedural ssp = node as SampleTextureStackProcedural;
                    if ( ssp != null )
                    {
                        string value = ssp.GetStackName();
                        string name = ssp.GetStackName();
                        // Check if there is already a slot with the same value
                        int found = slotNames.FindIndex(elem => elem.Key == value);
                        if (found >= 0)
                        {
                            // Add a validation error, values need to be unique
                            node.owner.AddValidationError(node.tempId, $"This node has the same procedural ID as another node. Nodes need to have different procedural IDs.", ShaderCompilerMessageSeverity.Error);
                        }
                        else
                        {
                            // Save it for checking against other slots
                            slotNames.Add(new KeyValuePair<string, string>(value, name));
                        }
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
                string result = string.Format("StackInfo {0}_info = PrepareStack({1}, {0});"
                        , stackName
                        , GetSlotValue(UVInputId, generationMode));
                sb.AppendLine(result);
            }

            for (int i = 0; i < numSlots; i++)
            {
                if (IsSlotConnected(OutputSlotIds[i]))
                {
                    var id = GetTextureName(i, generationMode);
                    string resultLayer = string.Format("$precision4 {1} = SampleStack({0}_info, {2});"
                            , stackName
                            , GetVariableNameForSlot(OutputSlotIds[i])
                            , id);
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

            if (feedbackConnected)
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
                slotNames = slotNames,
                m_Batchable = true
            });

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = stackName,
                slotNames = slotNames
            });
        }

        public bool RequiresMeshUV(Internal.UVChannel channel, ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                if (slot.RequiresMeshUV(channel))
                    return true;
            }
            return false;
        }
    }

    [Title("Input", "Texture", "Sample Texture Stack")]
    class SampleTextureStackNode : SampleTextureStackNodeBase
    {
        public SampleTextureStackNode() : base(1)
        { }

        [EnumControl("Type")]
        public TextureType textureType
        {
            get { return m_TextureTypes[0]; }
            set
            {
                if (m_TextureTypes[0] == value)
                    return;

                m_TextureTypes[0] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }
    }

    [Title("Input", "Texture", "Sample Texture Stack 2")]
    class SampleTextureStackNode2 : SampleTextureStackNodeBase
    {
        public SampleTextureStackNode2() : base(2)
        { }

        [EnumControl("Type 1")]
        public TextureType textureType
        {
            get { return m_TextureTypes[0]; }
            set
            {
                if (m_TextureTypes[0] == value)
                    return;

                m_TextureTypes[0] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        [EnumControl("Type 2")]
        public TextureType textureType2
        {
            get { return m_TextureTypes[1]; }
            set
            {
                if (m_TextureTypes[1] == value)
                    return;

                m_TextureTypes[1] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }
    }

    [Title("Input", "Texture", "Sample Texture Stack 3")]
    class SampleTextureStackNode3 : SampleTextureStackNodeBase
    {
        public SampleTextureStackNode3() : base(3)
        { }

        [EnumControl("Type 1")]
        public TextureType textureType
        {
            get { return m_TextureTypes[0]; }
            set
            {
                if (m_TextureTypes[0] == value)
                    return;

                m_TextureTypes[0] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        [EnumControl("Type 2")]
        public TextureType textureType2
        {
            get { return m_TextureTypes[1]; }
            set
            {
                if (m_TextureTypes[1] == value)
                    return;

                m_TextureTypes[1] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        [EnumControl("Type 3")]
        public TextureType textureType3
        {
            get { return m_TextureTypes[2]; }
            set
            {
                if (m_TextureTypes[2] == value)
                    return;

                m_TextureTypes[2] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }
    }

    [Title("Input", "Texture", "Sample Texture Stack 4")]
    class SampleTextureStackNode4 : SampleTextureStackNodeBase
    {
        public SampleTextureStackNode4() : base(4)
        { }

        [EnumControl("Type 1")]
        public TextureType textureType
        {
            get { return m_TextureTypes[0]; }
            set
            {
                if (m_TextureTypes[0] == value)
                    return;

                m_TextureTypes[0] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        [EnumControl("Type 2")]
        public TextureType textureType2
        {
            get { return m_TextureTypes[1]; }
            set
            {
                if (m_TextureTypes[1] == value)
                    return;

                m_TextureTypes[1] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        [EnumControl("Type 3")]
        public TextureType textureType3
        {
            get { return m_TextureTypes[2]; }
            set
            {
                if (m_TextureTypes[2] == value)
                    return;

                m_TextureTypes[2] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
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
            var stackNodes = GraphUtil.FindDownStreamNodesOfType<SampleTextureStackNodeBase>(masterNode);

            // Early out if there are no VT nodes in the graph
            if ( stackNodes.Count <= 0 )
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
                var stackFeedbackOutputSlot = (node.FindOutputSlot<ISlot>(node.FeedbackSlotId)) as Vector4MaterialSlot;
                if (stackFeedbackOutputSlot == null)
                {
                    Debug.LogWarning("Could not find the VT feedback output slot on the stack node.");
                    return iMasterNode;
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
            if ( feedbackInputSlot == null )
            {
                Debug.LogWarning("Could not find the VT feedback input slot on the master node.");
                return iMasterNode;
            }

            var feedbackOutputSlot = feedbackNode.FindOutputSlot<ISlot>(TextureStackAggregateFeedbackNode.AggregateOutputId);
            if ( feedbackOutputSlot == null )
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
    class SampleTextureStackProceduralBase : SampleTextureStackNodeBase
    {
        public SampleTextureStackProceduralBase(int numLayers) : base(numLayers, true)
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

    [Title("Input", "Texture", "Sample Texture Stack Procedural 1")]
    class SampleTextureStackProcedural : SampleTextureStackProceduralBase
    {
        public SampleTextureStackProcedural() : base(1)
        { }

        [EnumControl("Type 1")]
        public TextureType textureType
        {
            get { return m_TextureTypes[0]; }
            set
            {
                if (m_TextureTypes[0] == value)
                    return;

                m_TextureTypes[0] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }
    }

    [Title("Input", "Texture", "Sample Texture Stack Procedural 2")]
    class SampleTextureStackProcedural2 : SampleTextureStackProceduralBase
    {
        public SampleTextureStackProcedural2() : base(2)
        { }

        [EnumControl("Type 1")]
        public TextureType textureType
        {
            get { return m_TextureTypes[0]; }
            set
            {
                if (m_TextureTypes[0] == value)
                    return;

                m_TextureTypes[0] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        [EnumControl("Type 2")]
        public TextureType textureType2
        {
            get { return m_TextureTypes[1]; }
            set
            {
                if (m_TextureTypes[1] == value)
                    return;

                m_TextureTypes[1] = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }
    }
#endif
}
