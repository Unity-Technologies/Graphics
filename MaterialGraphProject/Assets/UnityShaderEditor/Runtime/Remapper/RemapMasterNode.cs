using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Remapper")]
    public class RemapMasterNode : AbstractMasterNode
        , IGeneratesBodyCode
        , IGeneratesFunction
        , IOnAssetEnabled
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequireWorldPosition
        , IMayRequireVertexColor
    {
        [SerializeField]
        private string m_SerialziedRemapGraph = string.Empty;

        [Serializable]
        private class RemapGraphHelper
        {
            public MaterialRemapAsset subGraph;
        }

        public override bool allowedInRemapGraph
        {
            get { return false; }
        }
        
        public override string GetFullShader(GenerationMode mode, out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            var shaderTemplateLocation = ShaderGenerator.GetTemplatePath("shader.template");

            if (remapAsset == null || !File.Exists(shaderTemplateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }
            
            // Step 1: Generate properties from this node
            // remap graphs are not allowed to have subgraphs
            // or property nodes, so this is okay :)
            var shaderPropertiesVisitor = new PropertyGenerator();
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
                node.GeneratePropertyBlock(shaderPropertiesVisitor, mode);

            // Step 2: Set this node as the remap target
            var subShaders = remapAsset.masterRemapGraph.GetSubShadersFor(this, mode);

            var templateText = File.ReadAllText(shaderTemplateLocation);
            var resultShader = templateText.Replace("${ShaderName}", GetType() + guid.ToString());
            resultShader = resultShader.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${SubShader}", subShaders.Aggregate((i, j) => i + Environment.NewLine + j));
            configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();
            return Regex.Replace(resultShader, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }

        public override string GetSubShader(GenerationMode mode)
        {
            throw new NotImplementedException();
        }

#if UNITY_EDITOR
        public MaterialRemapAsset remapAsset
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerialziedRemapGraph))
                    return null;

                var helper = new RemapGraphHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerialziedRemapGraph, helper);
                return helper.subGraph;
            }
            set
            {
                if (remapAsset == value)
                    return;
                    
                var helper = new RemapGraphHelper();
                helper.subGraph = value;
                m_SerialziedRemapGraph = EditorJsonUtility.ToJson(helper, true);
                OnEnable();

                if (onModified != null)
                    onModified(this, ModificationScope.Graph);
            }
        }
#else
        public MaterialSubGraphAsset subGraphAsset {get; set; }
#endif

        private MasterRemapGraph masterRemapGraph
        {
            get
            {
                if (remapAsset == null)
                    return null;

                return remapAsset.masterRemapGraph;
            }
        }
        
        public override PreviewMode previewMode
        {
            get
            {
                if (remapAsset == null)
                    return PreviewMode.Preview2D;

                return PreviewMode.Preview3D;
            }
        }

        public RemapMasterNode()
        {
            name = "Remapper";
        }

        public void OnEnable()
        {
            var validNames = new List<int>();
            if (remapAsset == null)
            {
                RemoveSlotsNameNotMatching(validNames);
                return;
            }

            var inputNode = remapAsset.masterRemapGraph.inputNode;
            foreach (var slot in inputNode.GetOutputSlots<MaterialSlot>())
            {
                AddSlot(new MaterialSlot(slot.id, slot.displayName, slot.shaderOutputName, SlotType.Input, slot.valueType, slot.defaultValue));
                validNames.Add(slot.id);
            }
            RemoveSlotsNameNotMatching(validNames);
        }

        public void GenerateNodeCode(ShaderGenerator shaderBody, GenerationMode generationMode)
        {
            var nodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, null, NodeUtils.IncludeSelf.Exclude);
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }
            ListPool<INode>.Release(nodes);

            if (remapAsset == null)
                return;

            var inputNode = remapAsset.masterRemapGraph.inputNode;
            foreach (var mappedSlot in inputNode.GetOutputSlots<MaterialSlot>())
            {
                var edge = owner.GetEdges(new SlotReference(guid, mappedSlot.id)).FirstOrDefault();
                if (edge != null)
                {
                    var outputRef = edge.outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                    if (fromNode == null)
                        continue;

                    shaderBody.AddShaderChunk("float4 " + inputNode.GetVariableNameForSlot(mappedSlot.id) + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
                }
                else
                {
                    shaderBody.AddShaderChunk("float4 " + inputNode.GetVariableNameForSlot(mappedSlot.id) + " = " + inputNode.FindSlot<MaterialSlot>(mappedSlot.id).GetDefaultValue(GenerationMode.ForReals) + ";", true);
                }
            }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            foreach (var node in activeNodeList.OfType<IGenerateProperties>())
                node.GeneratePropertyBlock(visitor, generationMode);
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            foreach (var node in activeNodeList.OfType<IGenerateProperties>())
                node.GeneratePropertyUsages(visitor, generationMode);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            foreach (var node in activeNodeList.OfType<IGeneratesFunction>())
                node.GenerateNodeFunction(visitor, generationMode);
        }

        public bool RequiresNormal()
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            return activeNodeList.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal());
        }

        public bool RequiresTangent()
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            return activeNodeList.OfType<IMayRequireTangent>().Any(x => x.RequiresTangent());
        }

        public bool RequiresBitangent()
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            return activeNodeList.OfType<IMayRequireBitangent>().Any(x => x.RequiresBitangent());
        }

        public bool RequiresMeshUV()
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            return activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV());
        }

        public bool RequiresScreenPosition()
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            return activeNodeList.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
        }

        public bool RequiresViewDirection()
        {

            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            return activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection());
        }

        public bool RequiresWorldPosition()
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            return activeNodeList.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition());
        }

        public bool RequiresVertexColor()
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, null, NodeUtils.IncludeSelf.Exclude);
            return activeNodeList.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor());
        }
    }
}
