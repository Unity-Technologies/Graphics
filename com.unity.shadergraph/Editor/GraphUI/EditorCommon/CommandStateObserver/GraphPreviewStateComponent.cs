using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview;
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

        MaterialPropertyBlock m_PreviewMaterialPropertyBlock = new ();

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
                var portValue = new PortPreviewHandler(inputPort);
                portValue.SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock);
                // TODO: Store for easier access and updating in future
            }
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
