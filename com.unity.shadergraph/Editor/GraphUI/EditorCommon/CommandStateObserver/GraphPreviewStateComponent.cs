using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    enum PreviewMode
    {
        Inherit,   // this usually means: 2D, unless a connected input node is 3D, in which case it is 3D
        Preview2D,
        Preview3D
    }

    struct PreviewShaderData
    {
        public string Guid;
        public Shader shader;
        public Material mat;
        public string shaderString;
        public int passesCompiling;
        public bool isOutOfDate;
        public bool hasError;
    }

    struct PreviewRenderData
    {
        public string Guid;
        public PreviewShaderData shaderData;
        public RenderTexture renderTexture;
        public Texture texture;
        public PreviewMode previewMode;
        public Action onPreviewChanged;

        public void NotifyPreviewChanged()
        {
            onPreviewChanged?.Invoke();
        }
    }

    public class GraphPreviewStateComponent : ViewStateComponent<GraphPreviewStateComponent.PreviewStateUpdater>
    {
        Dictionary<string, PreviewRenderData> KeyToPreviewDataMap = new ();

        ShaderGraphModel m_ShaderGraphModel;

        public void SetGraphModel(ShaderGraphModel shaderGraphModel)
        {
            m_ShaderGraphModel = shaderGraphModel;
        }

        public class PreviewStateUpdater : BaseUpdater<GraphPreviewStateComponent>
        {
            public void ChangePreviewExpansionState(string changedElementGuid, bool isPreviewExpanded)
            {
                m_State.SetUpdateType(UpdateType.Partial);
            }
        }

        ~GraphPreviewStateComponent()
        {
            Dispose();
        }

        public void OnNodeAdded(GraphDataNodeModel nodeModel)
        {
            // Make sure the model has been assigned
            m_ShaderGraphModel ??= nodeModel.GraphModel as ShaderGraphModel;

            var nodeGuid = nodeModel.Guid.ToString();

            var renderData = new PreviewRenderData
            {
                Guid = nodeGuid,
                renderTexture =
                new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                {
                    hideFlags = HideFlags.HideAndDontSave
                }
            };

            var shaderData = new PreviewShaderData
            {
                Guid = nodeGuid,
                passesCompiling = 0,
                isOutOfDate = true,
                hasError = false,
            };

            renderData.shaderData = shaderData;

            CollectPreviewPropertiesFromNode(nodeModel);

            KeyToPreviewDataMap.Add(nodeGuid, renderData);
        }

        void CollectPreviewPropertiesFromNode(GraphDataNodeModel nodeModel)
        {
            // TODO: Collect preview properties from the newly added graph element
            // For a node: Get the input ports for the node, get fields from the ports, and get values from the fields
            // For a property/keyword: Ask Esme and Liz
            var nodeInputPorts = m_ShaderGraphModel.GetInputPortsOnNode(nodeModel);
            foreach (var inputPort in nodeInputPorts)
            {
                var portFields = inputPort.GetFields();
                foreach (var portField in portFields)
                {
                    var portValue = InstantiatePreviewProperty(portField);
                }
            }
        }

        object InstantiatePreviewProperty(IFieldReader fieldReader)
        {
            // TODO: How to get the actual value of a port/field?
            // How to get the actual type of a field, is type a sub-field maybe? Speak to Esme
            // Also, what are the relevance of sub-fields?s

            // Are we meant to be communicating with GTF type handles or direct types at this level?
            // (maybe using GTF type handles which would be retrieved through some sort of registry typing abstraction)

            // Think about how we will store data regarding node preview state etc. as part of GraphStorage
            // Creating own stuct to match the PreviewRenderData, can mark structs/classes Serializable


            return null;
        }

        public void OnElementWithPreviewRemoved(string elementGuid)
        {
            KeyToPreviewDataMap.Remove(elementGuid);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
                Debug.Log("Disposing!");
        }
    }
}
