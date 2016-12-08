using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Remapper")]
    public class RemapMasterNode : AbstractMasterNode
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
            throw new NotImplementedException();
            var shaderTemplateLocation = ShaderGenerator.GetTemplatePath("shader.template");

            if (remapAsset == null || !File.Exists(shaderTemplateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }
            
            // Step 1: Generate properties from this node
            // remap graphs are not allowed to have subgraphs
            // or property nodes, so this is okay :)
            var shaderPropertiesVisitor = new ShaderGenerator();
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
                node.GeneratePropertyUsages(shaderPropertiesVisitor, mode);

            var subShaders = new List<string>();
            foreach (var node in activeNodeList.OfType<IMasterNode>())
                subShaders.Add(node.GetSubShader(mode));


        }

        public void GenerateNodeCode(ShaderGenerator shaderBody, GenerationMode generationMode)
        {
            var nodes = ListPool<INode>.Get();

            //Get the rest of the nodes for all the slots
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, null, NodeUtils.IncludeSelf.Exclude);
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }
            ListPool<INode>.Release(nodes);

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                var edge = owner.GetEdges(slot.slotReference).FirstOrDefault();

                if (edge != null)
                {
                    var outputRef = edge.outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                    if (fromNode == null)
                        continue;

                    shaderBody.AddShaderChunk("float4 reamapper_" + slot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
                }
                else
                {
                    shaderBody.AddShaderChunk("float4 reamapper_" + slot.shaderOutputName + " = 0;", true);
                }
            }
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
    }
}
