using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public interface IGenerateGraphProperties
    {
        void GenerateSharedProperties(PropertyGenerator shaderProperties, ShaderGenerator propertyUsages, GenerationMode generationMode);
        IEnumerable<ShaderProperty> GetPropertiesForPropertyType(PropertyType propertyType);
    }

    public abstract class BaseMaterialGraph : Graph
    {
        private PreviewRenderUtility m_PreviewUtility;

        public PreviewRenderUtility previewUtility
        {
            get
            {
                if (m_PreviewUtility == null)
                {
                    m_PreviewUtility = new PreviewRenderUtility();
                    EditorUtility.SetCameraAnimateMaterials(m_PreviewUtility.m_Camera, true);
                }

                return m_PreviewUtility;
            }
        }

        public bool requiresRepaint
        {
            get { return isAwake && nodes.Any(x => x is IRequiresTime); }
        }

        protected abstract void RecacheActiveNodes();

        public override void RemoveEdge(Edge e)
        {
            base.RemoveEdge(e);

            var toNode = e.toSlot.node as BaseMaterialNode;
            if (toNode == null)
                return;
            
            RecacheActiveNodes();
            UpdateNodeErrorState();
            toNode.RegeneratePreviewShaders();
        }

        public override Edge Connect(Slot fromSlot, Slot toSlot)
        {
            Slot outputSlot = null;
            Slot inputSlot = null;

            // output must connect to input
            if (fromSlot.isOutputSlot)
                outputSlot = fromSlot;
            else if (fromSlot.isInputSlot)
                inputSlot = fromSlot;

            if (toSlot.isOutputSlot)
                outputSlot = toSlot;
            else if (toSlot.isInputSlot)
                inputSlot = toSlot;

            if (inputSlot == null || outputSlot == null)
                return null;

            // remove any inputs that exits before adding
            foreach (var edge in inputSlot.edges.ToArray())
            {
                Debug.Log("Removing existing edge:" + edge);
                // call base here as we DO NOT want to
                // do expensive shader regeneration
                base.RemoveEdge(edge);
            }
            
            var newEdge = base.Connect(outputSlot, inputSlot);
            
            Debug.Log("Connected edge: " + newEdge);
            var toNode = inputSlot.node as BaseMaterialNode;
            var fromNode = outputSlot.node as BaseMaterialNode;

            if (fromNode == null || toNode == null)
                return newEdge;

            RecacheActiveNodes();
            UpdateNodeErrorState();
            toNode.RegeneratePreviewShaders();
            fromNode.CollectChildNodesByExecutionOrder().ToList().ForEach(s => s.UpdatePreviewProperties());

            return newEdge;
        }

        protected abstract void UpdateNodeErrorState();

        public override void AddNode(Node node)
        {
            base.AddNode(node);
            AssetDatabase.AddObjectToAsset(node, this);

            var bmn = node as BaseMaterialNode;
            if (bmn != null && bmn.hasPreview)
                bmn.UpdatePreviewMaterial();
        }

        protected void AddMasterNodeNoAddToAsset(Node node)
        {
            base.AddNode(node);
        }

        public void GeneratePreviewShaders()
        {
            MaterialWindow.DebugMaterialGraph("Generating preview shaders on: " + name);

            // 2 passes...
            // 1 create the shaders
            foreach (var node in nodes)
            {
                var bmn = node as BaseMaterialNode;
                if (bmn != null && bmn.hasPreview)
                {
                    bmn.UpdatePreviewMaterial();
                }
            }

            // 2 set the properties
            foreach (var node in nodes)
            {
                var pNode = node as BaseMaterialNode;
                if (pNode != null && pNode.hasPreview)
                {
                    MaterialWindow.DebugMaterialGraph("Updating preview Properties on Node: " + pNode);
                    pNode.UpdatePreviewProperties();
                }
            }
        }
    }
}
