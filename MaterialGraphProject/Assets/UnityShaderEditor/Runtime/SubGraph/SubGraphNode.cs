using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Sub-graph/Sub-graph Node")]
    public class SubGraphNode : AbstractMaterialNode
        , IGeneratesBodyCode
        , IGeneratesFunction
        , IOnAssetEnabled
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequirePosition
        , IMayRequireVertexColor
        , IMayRequireTime
    {
        [SerializeField]
        private string m_SerializedSubGraph = string.Empty;

        [Serializable]
        private class SubGraphHelper
        {
            public MaterialSubGraphAsset subGraph;
        }


#if UNITY_EDITOR
        public MaterialSubGraphAsset subGraphAsset
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedSubGraph))
                    return null;

                var helper = new SubGraphHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerializedSubGraph, helper);
                return helper.subGraph;
            }
            set
            {
                if (subGraphAsset == value)
                    return;

                var helper = new SubGraphHelper();
                helper.subGraph = value;
                m_SerializedSubGraph = EditorJsonUtility.ToJson(helper, true);
                OnEnable();

                if (onModified != null)
                    onModified(this, ModificationScope.Topological);
            }
        }

        /*
       // SAVED FOR LATER
        if (serializedVersion<kCurrentSerializedVersion)
                    DoUpgrade();
        [SerializeField]
        private string m_SubGraphAssetGuid;

        [SerializeField]
        private int serializedVersion = 0;
        const int kCurrentSerializedVersion = 1;

        private void DoUpgrade()
        {
            var helper = new SubGraphHelper();
            if (string.IsNullOrEmpty(m_SubGraphAssetGuid))
                helper.subGraph = null;

            var path = AssetDatabase.GUIDToAssetPath(m_SubGraphAssetGuid);
            if (string.IsNullOrEmpty(path))
                helper.subGraph = null;

            helper.subGraph = AssetDatabase.LoadAssetAtPath<MaterialSubGraphAsset>(path);

            m_SerializedSubGraph = EditorJsonUtility.ToJson(helper, true);
            serializedVersion = kCurrentSerializedVersion;
            m_SubGraphAssetGuid = string.Empty;
            mark dirty damn
        }*/
#else
        public MaterialSubGraphAsset subGraphAsset {get; set; }
#endif

        private SubGraph subGraph
        {
            get
            {
                if (subGraphAsset == null)
                    return null;

                return subGraphAsset.subGraph;
            }
        }

        public override bool hasPreview
        {
            get { return subGraphAsset != null; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                if (subGraphAsset == null)
                    return PreviewMode.Preview2D;

                return PreviewMode.Preview3D;
            }
        }

        public SubGraphNode()
        {
            name = "SubGraph";
        }

        public void OnEnable()
        {
            var validNames = new List<int>();
            if (subGraphAsset == null)
            {
                RemoveSlotsNameNotMatching(validNames);
                return;
            }

            var props = subGraph.properties;
            foreach (var prop in props)
            {
                var propType = prop.propertyType;
                SlotValueType slotType;

                switch (propType)
                {
                    case PropertyType.Color:
                        slotType = SlotValueType.Vector4;
                        break;
                    case PropertyType.Texture:
                        slotType = SlotValueType.Texture2D;
                        break;
                    case PropertyType.Float:
                        slotType = SlotValueType.Vector1;
                        break;
                    case PropertyType.Vector2:
                        slotType = SlotValueType.Vector2;
                        break;
                    case PropertyType.Vector3:
                        slotType = SlotValueType.Vector3;
                        break;
                    case PropertyType.Vector4:
                        slotType = SlotValueType.Vector4;
                        break;
                    case PropertyType.Matrix2:
                        slotType = SlotValueType.Matrix2;
                        break;
                    case PropertyType.Matrix3:
                        slotType = SlotValueType.Matrix3;
                        break;
                    case PropertyType.Matrix4:
                        slotType = SlotValueType.Matrix4;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var id = prop.guid.GetHashCode();
                AddSlot(new MaterialSlot(id, prop.name, prop.name, SlotType.Input, slotType, prop.defaultValue));
                validNames.Add(id);
            }

            var subGraphOutputNode = subGraphAsset.subGraph.outputNode;
            foreach (var slot in subGraphOutputNode.GetInputSlots<MaterialSlot>())
            {
                AddSlot(new MaterialSlot(slot.id, slot.displayName, slot.shaderOutputName, SlotType.Output, slot.valueType, slot.defaultValue));
                validNames.Add(slot.id);
            }

            RemoveSlotsNameNotMatching(validNames);
        }

        public void GenerateNodeCode(ShaderGenerator shaderBodyVisitor, GenerationMode generationMode)
        {
            if (subGraphAsset == null)
                return;

            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("// Subgraph for node " + GetVariableNameForNode(), false);

            // Step 1...
            // find out which output slots are actually used
            //TODO: Be smarter about this and only output ones that are actually USED, not just connected
            //var validOutputSlots = NodeUtils.GetSlotsThatOutputToNodeRecurse(this, (graph as BaseMaterialGraph).masterNode);
            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                var outDimension = ConvertConcreteSlotValueTypeToString(slot.concreteValueType);

                outputString.AddShaderChunk(
                    "float"
                    + outDimension
                    + " "
                    + GetVariableNameForSlot(slot.id)
                    + " = 0;", false);
            }

            // Step 2...
            // Go into the subgraph
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            // Step 3...
            // For each input that is used and connects through we want to generate code.
            // First we assign the input variables to the subgraph
            // we do this by renaming the properties to be the names of where the variables come from
            // weird, but works.
            var sSubGraph = SerializationHelper.Serialize<SubGraph>(subGraphAsset.subGraph);
            var subGraph = SerializationHelper.Deserialize<SubGraph>(sSubGraph, null);

            var subGraphInputs = subGraph.properties;


            //todo:
            // copy whole subgraph
            // rename properties to match what we want (external scope)
            // then generate graph
            var propertyGen = new PropertyCollector();
            subGraph.CollectShaderProperties(propertyGen, GenerationMode.ForReals);

            foreach (var prop in subGraphInputs)
            {
                var inSlotId = prop.guid.GetHashCode();
                var inSlot = FindInputSlot<MaterialSlot>(inSlotId);

                var edges = owner.GetEdges(inSlot.slotReference).ToArray();

                string varValue = inSlot.GetDefaultValue(generationMode);
                if (edges.Any())
                {
                    var fromSocketRef = edges[0].outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(fromSocketRef.nodeGuid);
                    if (fromNode != null)
                    {
                        var slot = fromNode.FindOutputSlot<MaterialSlot>(fromSocketRef.slotId);
                        if (slot != null)
                            prop.name = fromNode.GetSlotValue(slot.id, generationMode);
                    }
                }
                else if (inSlot.concreteValueType == ConcreteSlotValueType.Texture2D)
                {
                    prop.name = MaterialSlot.DefaultTextureName;
                }
                else
                {
                    var varName = prop.name;
                    outputString.AddShaderChunk(
                        ConvertConcreteSlotValueTypeToString(precision, inSlot.concreteValueType)
                        + " "
                        + varName
                        + " = "
                        + varValue
                        + ";", false);
                }
            }

            // Step 4...
            // Using the inputs we can now generate the shader body :)
            var bodyGenerator = new ShaderGenerator();
            subGraph.GenerateNodeCode(bodyGenerator, GenerationMode.ForReals);
            var subGraphOutputNode = subGraph.outputNode;
            outputString.AddShaderChunk(bodyGenerator.GetShaderString(0), false);

            // Step 5...
            // Copy the outputs to the parent context name);
            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                var inputValue = subGraphOutputNode.GetSlotValue(slot.id, GenerationMode.ForReals);

                outputString.AddShaderChunk(
                    GetVariableNameForSlot(slot.id)
                    + " = "
                    + inputValue
                    + ";", false);
            }

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.AddShaderChunk("// Subgraph ends", false);

            shaderBodyVisitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public override void CollectShaderProperties(PropertyCollector visitor, GenerationMode generationMode)
        {
            base.CollectShaderProperties(visitor, generationMode);

            if (subGraph == null)
                return;

            subGraph.CollectShaderProperties(visitor, GenerationMode.ForReals);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            if (subGraph == null)
                return;

            properties.AddRange(subGraph.GetPreviewProperties());
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (subGraph == null)
                return;

            subGraph.GenerateNodeFunction(visitor, GenerationMode.ForReals);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (subGraph == null)
                return;

            subGraph.GenerateVertexShaderBlock(visitor, GenerationMode.ForReals);
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (subGraph == null)
                return;

            subGraph.GenerateVertexToFragmentBlock(visitor, GenerationMode.ForReals);
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresNormal();
                return mask;
            });
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel));
        }

        public bool RequiresScreenPosition()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
        }

        public NeededCoordinateSpace RequiresViewDirection()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequireViewDirection>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresViewDirection();
                return mask;
            });
        }


        public NeededCoordinateSpace RequiresPosition()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresPosition();
                return mask;
            });
        }

        public NeededCoordinateSpace RequiresTangent()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresTangent();
                return mask;
            });
        }

        public bool RequiresTime()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireTime>().Any(x => x.RequiresTime());
        }

        public NeededCoordinateSpace RequiresBitangent()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequireBitangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresBitangent();
                return mask;
            });
        }

        public bool RequiresVertexColor()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor());
        }
    }
}
