using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    //[Title("Input", "Texture", "Sample Stack")]
    class SampleStackNodeBase : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
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

        public SampleStackNodeBase(int numSlots)
        {
            if (numSlots > 4)
            {
                throw new System.Exception("Maximum 4 slots supported");
            }
            this.numSlots = numSlots;
            name = "Sample Stack " + numSlots;

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

            for (int i = 0; i < numSlots; i++)
            {
                AddSlot(new Texture2DInputMaterialSlot(TextureInputIds[i], TextureInputNames[i], TextureInputNames[i]));
            }

            var slot = new Vector4MaterialSlot(FeedbackSlotId, FeedbackSlotName, FeedbackSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment);
            slot.hidden = true;
            AddSlot(slot);

            RemoveSlotsNameNotMatching(liveIds);
        }

        public override void ValidateNode()
        {
            for (int i = 0; i < numSlots; i++)
            {
                var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(TextureInputIds[i]);
                textureSlot.defaultType = (m_TextureTypes[i] == TextureType.Normal ? TextureShaderProperty.DefaultType.Bump : TextureShaderProperty.DefaultType.White);
            }
            base.ValidateNode();
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            // Not all outputs may be connected (well one is or we wouln't get called) so we are carefull to
            // only generate code for connected outputs
            string stackName = GetVariableNameForSlot(OutputSlotIds[0]) + "_stack";

            bool anyConnected = false;
            for (int i = 0; i < numSlots; i++)
            {
                if (IsSlotConnected(OutputSlotIds[i]))
                {
                    anyConnected = true;
                    break;
                }
            }

            if (anyConnected)
            {
                string result = string.Format("StackInfo {0}_info = PrepareStack({1}, {0});"
                        , stackName
                        , GetSlotValue(UVInputId, generationMode));
                visitor.AddShaderChunk(result, true);
            }

            for (int i = 0; i < numSlots; i++)
            {
                if (IsSlotConnected(OutputSlotIds[i]))
                {
                    var id = GetSlotValue(TextureInputIds[i], generationMode);
                    string resultLayer = string.Format("{2}4 {3} = SampleStack({0}_info, {4});"
                            , stackName
                            , GetSlotValue(UVInputId, generationMode)
                            , precision
                            , GetVariableNameForSlot(OutputSlotIds[i])
                            , id);
                    visitor.AddShaderChunk(resultLayer, true);
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
                            visitor.AddShaderChunk(string.Format("{0}.rgb = UnpackNormalmapRGorAG({0});", GetVariableNameForSlot(OutputSlotIds[i])), true);
                        }
                        else
                        {
                            visitor.AddShaderChunk(string.Format("{0}.rgb = UnpackNormalRGB({0});", GetVariableNameForSlot(OutputSlotIds[i])), true);
                        }
                    }
                }
            }

            if (IsSlotConnected(FeedbackSlotId))
            {
                string feedBackCode = string.Format("float4 {0} = ResolveStack({1}, {2});"
                        , GetVariableNameForSlot(FeedbackSlotId)
                        , GetSlotValue(UVInputId, generationMode)
                        , stackName);
                visitor.AddShaderChunk(feedBackCode, true);
            }
        }

        public void GenerateFeedbackCode(GenerationMode generationMode, string assignLValue, out string code)
        {
            string stackName = GetVariableNameForSlot(OutputSlotIds[0]) + "_stack";

            code = string.Format("{0} = ResolveStack({1}, {2});"
                    , assignLValue
                    , GetSlotValue(UVInputId, generationMode)
                    , stackName);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);

            List<string> slotNames = new List<string>();
            for (int i = 0; i < numSlots; i++)
            {
                var id = GetSlotValue(TextureInputIds[i], generationMode);
                slotNames.Add(id);
            }

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = GetVariableNameForSlot(OutputSlotIds[0]) + "_stack",
                generatePropertyBlock = true,
                modifiable = false,
                slotNames = slotNames
            });

            // If the property already exists additional calls will be ignored so we just try to add this on every VT node..
            properties.AddShaderProperty(new BooleanShaderProperty()
            {
                overrideReferenceName = "_VirtualTexturing",
                displayName = "Virtual Texturing",
                generatePropertyBlock = true,
                value = false
            });
        }

        public override void CollectShaderPragmas(PragmaCollector pragmas, GenerationMode generationMode)
        {
            pragmas.AddShaderPragma(new ShaderFeaturePragma(new string[] { "VT_ON" }));
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
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

    [Title("Input", "Texture", "Sample Stack")]
    class SampleStackNode : SampleStackNodeBase
    {
        public SampleStackNode() : base(1)
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

    [Title("Input", "Texture", "Sample Stack 2")]
    class SampleStackNode2 : SampleStackNodeBase
    {
        public SampleStackNode2() : base(2)
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

    [Title("Input", "Texture", "Sample Stack 3")]
    class SampleStackNode3 : SampleStackNodeBase
    {
        public SampleStackNode3() : base(3)
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

    [Title("Input", "Texture", "Sample Stack 4")]
    class SampleStackNode4 : SampleStackNodeBase
    {
        public SampleStackNode4() : base(4)
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

    class AggregateFeedbackNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireRequirePixelCoordinate
    {
        public const int AggregateOutputId = 0;
        const string AggregateOutputName = "FeedbackAggregateOut";

        public const int AggregateInputFirstId = 1;

        public override bool hasPreview { get { return false; } }

        public AggregateFeedbackNode()
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
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var slots = this.GetInputSlots<ISlot>();
            int numSlots = slots.Count();

            if (numSlots > 1)
            {
                string arrayName = string.Format("{0}_array", GetVariableNameForSlot(AggregateOutputId));
                visitor.AddShaderChunk(string.Format("float4 {0}[{1}];", arrayName, numSlots));

                int arrayIndex = 0;
                foreach (var slot in slots)
                {
                    string code = string.Format("{0}[{1}] = {2};"
                            , arrayName
                            , arrayIndex
                            , GetSlotValue(AggregateInputFirstId + arrayIndex, generationMode));
                    visitor.AddShaderChunk(code);
                    arrayIndex++;
                }

                string feedBackCode = string.Format("float4 {0} = {1}[IN.{2}.x%{3}];"
                        , GetVariableNameForSlot(AggregateOutputId)
                        , arrayName
                        , ShaderGeneratorNames.PixelCoordinate
                        , numSlots);

                visitor.AddShaderChunk(feedBackCode);
            }
            else if (numSlots == 1)
            {
                string feedBackCode = string.Format("float4 {0} = {1};"
                        , GetVariableNameForSlot(AggregateOutputId)
                        , GetSlotValue(AggregateInputFirstId, generationMode));

                visitor.AddShaderChunk(feedBackCode);
            }
            else
            {
                string feedBackCode = string.Format("float4 {0} = float4(1,1,1,1);"
                        , GetVariableNameForSlot(AggregateOutputId)
                        , GetSlotValue(AggregateInputFirstId, generationMode));

                visitor.AddShaderChunk(feedBackCode);
            }
        }

        public bool RequiresPixelCoordinate(ShaderStageCapability stageCapability)
        {
            // If there's only one VT slot we don't need the screen position
 
            var slots = this.GetInputSlots<ISlot>();
            int numSlots = slots.Count();

            return (numSlots > 1);
        }

        // Automatically add a  streaming feedback node and correctly connect it to stack samples are connected to it and it is connected to the master node output
        public static AggregateFeedbackNode AutoInjectFeedbackNode(AbstractMaterialNode masterNode)
        {
            var stackNodes = GraphUtil.FindDownStreamNodesOfType<SampleStackNodeBase>(masterNode);
            var feedbackNode = new AggregateFeedbackNode();
            masterNode.owner.AddNode(feedbackNode);

            // Add inputs to feedback node
            int i = 0;
            foreach (var node in stackNodes)
            {
                // Find feedback output slot on the vt node
                var stackFeedbackOutputSlot = (node.FindOutputSlot<ISlot>(node.FeedbackSlotId)) as Vector4MaterialSlot;

                // Create a new slot on the aggregate that is similar to the uv input slot
                string name = "FeedIn_" + i;
                var newSlot = new Vector4MaterialSlot(AggregateFeedbackNode.AggregateInputFirstId + i, name, name, SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment);
                newSlot.owner = feedbackNode;
                feedbackNode.AddSlot(newSlot);

                feedbackNode.owner.Connect(stackFeedbackOutputSlot.slotReference, newSlot.slotReference);
                i++;
            }

            // Add input to master node
            var feedbackInputSlot = masterNode.FindInputSlot<ISlot>(PBRMasterNode.FeedbackSlotId);
            var feedbackOutputSlot = feedbackNode.FindOutputSlot<ISlot>(AggregateFeedbackNode.AggregateOutputId);
            masterNode.owner.Connect(feedbackOutputSlot.slotReference, feedbackInputSlot.slotReference);
            masterNode.owner.ClearChanges();

            return feedbackNode;
        }
    }
}
