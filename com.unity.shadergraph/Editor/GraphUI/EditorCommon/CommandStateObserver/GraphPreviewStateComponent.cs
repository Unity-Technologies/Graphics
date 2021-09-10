using System;
using System.Collections.Generic;
using Editor.GraphUI.Utilities;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public enum PreviewMode
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
        public bool isPreviewEnabled;
        public bool isPreviewExpanded;
        public PreviewShaderData shaderData;
        public RenderTexture renderTexture;
        public Texture texture;
        public PreviewMode previewMode;
    }

    public class GraphPreviewStateComponent : ViewStateComponent<GraphPreviewStateComponent.PreviewStateUpdater>
    {
        Dictionary<string, PreviewRenderData> KeyToPreviewDataMap = new ();

        Dictionary<string, PortPreviewHandler> KeyToPreviewPropertyHandlerMap = new();

        MaterialPropertyBlock m_PreviewMaterialPropertyBlock = new ();

        List<string> m_TimedPreviewKeys = new();

        ShaderGraphModel m_ShaderGraphModel;

        public void SetGraphModel(ShaderGraphModel shaderGraphModel)
        {
            m_ShaderGraphModel = shaderGraphModel;
        }

        public class PreviewStateUpdater : BaseUpdater<GraphPreviewStateComponent>
        {
            public void ChangePreviewExpansionState(string changedElementGuid, bool isPreviewExpanded)
            {
                if(m_State.KeyToPreviewDataMap.TryGetValue(changedElementGuid, out var previewRenderData))
                {
                    // Update value of flag
                    previewRenderData.isPreviewExpanded = isPreviewExpanded;
                    // Make sure the struct in the state component is up-to-date also (it's a value type not a reference type as its a struct)
                    m_State.KeyToPreviewDataMap[changedElementGuid] = previewRenderData;
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            public void ChangeNodePreviewMode(string changedElementGuid, GraphDataNodeModel changedNode, PreviewMode newPreviewMode)
            {
                if(m_State.KeyToPreviewDataMap.TryGetValue(changedElementGuid, out var previewRenderData))
                {
                    // Also traverse upstream (i.e. left/the input nodes) through the hierarchy of this node and concretize the actual preview mode
                    if (previewRenderData.previewMode == PreviewMode.Inherit)
                    {
                        foreach (var upstreamNode in m_State.m_ShaderGraphModel.GetNodesInHierarchyFromSources(new [] {changedNode}, PropagationDirection.Upstream))
                        {
                            if (upstreamNode.NodePreviewMode == PreviewMode.Preview3D)
                                previewRenderData.previewMode = PreviewMode.Preview3D;
                        }
                    }
                    else // if not inherit, just directly set it
                        previewRenderData.previewMode = newPreviewMode;

                    // Make sure the struct in the state component is up-to-date also (it's a value type not a reference type as its a struct)
                    m_State.KeyToPreviewDataMap[changedElementGuid] = previewRenderData;
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            public void UpdateNodeState(string changedElementGuid, ModelState changedNodeState)
            {
                if(m_State.KeyToPreviewDataMap.TryGetValue(changedElementGuid, out var previewRenderData))
                {
                    // Update value of flag
                    previewRenderData.isPreviewEnabled = changedNodeState == ModelState.Enabled;
                    // Make sure the struct in the state component is up-to-date also (it's a value type not a reference type as its a struct)
                    m_State.KeyToPreviewDataMap[changedElementGuid] = previewRenderData;
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            public void MarkElementNeedingRecompile(string elementNeedingRecompileGuid)
            {
                if(m_State.KeyToPreviewDataMap.TryGetValue(elementNeedingRecompileGuid, out var previewRenderData))
                {
                    // Update value of flag
                    previewRenderData.shaderData.isOutOfDate = true;
                    // Make sure the struct in the state component is up-to-date also (it's a value type not a reference type as its a struct)
                    m_State.KeyToPreviewDataMap[elementNeedingRecompileGuid] = previewRenderData;
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            public void UpdatePortConstantValue(string changedElementGuid, object newPortConstantValue)
            {
                if (m_State.KeyToPreviewPropertyHandlerMap.TryGetValue(changedElementGuid, out var portPreviewHandler))
                {
                    // Update value of port constant
                    portPreviewHandler.PortConstantValue = newPortConstantValue;
                    // Make sure the struct in the state component is up-to-date also (it's a value type not a reference type as its a struct)
                    m_State.KeyToPreviewPropertyHandlerMap[changedElementGuid] = portPreviewHandler;

                    // TODO: Handle marking this element as requiring re-drawing this frame

                    // TODO: Handle Virtual Texture case
                    // Then set preview property in MPB from that
                    m_State.UpdatePortPreviewPropertyBlock(portPreviewHandler);
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            // TODO: Figure out how we're going to handle preview property gathering from property types on both the blackboard properties and their variable nodes
            // Maybe we need a new VariablePreviewHandler, but subclassing from IPreviewHandler so preview manager can abstract away details of both
            public void UpdateVariableConstantValue(string changedElementGuid, object newPortConstantValue)
            {
                if (m_State.KeyToPreviewPropertyHandlerMap.TryGetValue(changedElementGuid, out var portPreviewHandler))
                {
                    // Update value of port constant
                    portPreviewHandler.PortConstantValue = newPortConstantValue;
                    // Make sure the struct in the state component is up-to-date also (it's a value type not a reference type as its a struct)
                    m_State.KeyToPreviewPropertyHandlerMap[changedElementGuid] = portPreviewHandler;
                    // TODO: Handle Virtual Texture case
                    // Then set preview property in MPB from that
                    m_State.UpdatePortPreviewPropertyBlock(portPreviewHandler);
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            public void GraphDataNodeAdded(GraphDataNodeModel nodeModel)
            {
                m_State.OnGraphDataNodeAdded(nodeModel);
            }

            public void GraphDataNodeRemoved(GraphDataNodeModel nodeModel)
            {
                m_State.OnGraphDataNodeRemoved(nodeModel);
            }

            public void GraphWindowTick()
            {
                m_State.Tick();
            }
        }

        ~GraphPreviewStateComponent()
        {
            Dispose();
        }

        public void UpdatePortPreviewPropertyBlock(PortPreviewHandler portPreviewHandler)
        {
            portPreviewHandler.SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock);
        }

        void Tick()
        {
            UpdateTopology();

            // TODO: Skip any previews that are currently collapsed
        }

        void UpdateTopology()
        {
            using (var timedNodes = PooledHashSet<GraphDataNodeModel>.Get())
            {
                m_ShaderGraphModel?.GetTimeDependentNodesOnGraph(timedNodes);

                m_TimedPreviewKeys.Clear();

                // Get guids of the time dependent nodes and add to list of time-dependent nodes requiring rendering/updating this frame
                foreach (var nodeModel in timedNodes)
                {
                    m_TimedPreviewKeys.Add(nodeModel.graphDataName);
                }
            }


        }

        void OnGraphDataNodeAdded(GraphDataNodeModel nodeModel)
        {
            var nodeGuid = nodeModel.Guid.ToString();

            var renderData = new PreviewRenderData
            {
                Guid = nodeGuid,
                renderTexture =
                new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                {
                    hideFlags = HideFlags.HideAndDontSave
                },
            };

            var shaderData = new PreviewShaderData
            {
                Guid = nodeGuid,
                passesCompiling = 0,
                isOutOfDate = true,
                hasError = false,
            };

            renderData.shaderData = shaderData;

            CollectPreviewPropertiesFromGraphDataNode(nodeModel, ref renderData);

            KeyToPreviewDataMap.Add(nodeGuid, renderData);
        }

        void OnGraphDataNodeRemoved(GraphDataNodeModel nodeModel)
        {
            KeyToPreviewDataMap.Remove(nodeModel.graphDataName);
            // Also iterate through the ports on this node and remove the port preview handlers that were spawned from it
            nodeModel.TryGetNodeReader(out var nodeReader);
            foreach (var inputPort in nodeReader.GetInputPorts())
            {
                // For each port on the node, find the matching port model in its port mappings
                nodeModel.PortMappings.TryGetValue(inputPort, out var matchingPortModel);
                if (matchingPortModel != null)
                    KeyToPreviewPropertyHandlerMap.Remove(matchingPortModel.Guid.ToString());
            }

            // TODO: Clear any shader messages and shader objects related to this node as well
            // And unregister any callbacks that were bound on it
        }

        void CollectPreviewPropertiesFromGraphDataNode(GraphDataNodeModel nodeModel, ref PreviewRenderData previewRenderData)
        {
            // For a node: Get the input ports for the node, get fields from the ports, and get values from the fields
            var nodeInputPorts = m_ShaderGraphModel.GetInputPortsOnNode(nodeModel);

            foreach (var inputPort in nodeInputPorts)
            {
                var portPreviewHandler = new PortPreviewHandler(inputPort);
                portPreviewHandler.SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock);

                // For each port on the node, find the matching port model in its port mappings
                nodeModel.PortMappings.TryGetValue(inputPort, out var matchingPortModel);
                if (matchingPortModel != null)
                {
                    KeyToPreviewPropertyHandlerMap.Add(matchingPortModel.Guid.ToString(), portPreviewHandler);
                }
            }
        }

        // TODO: Once Esme integrates types, figure out how we're representing them as nodes, will we use VariableNodeModel or GraphDataNodeModel?
        public void OnVariableNodeAdded(VariableNodeModel nodeModel)
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
                    },
            };

            var shaderData = new PreviewShaderData
            {
                Guid = nodeGuid,
                passesCompiling = 0,
                isOutOfDate = true,
                hasError = false,
            };

            renderData.shaderData = shaderData;

            CollectPreviewPropertiesFromVariableNode(nodeModel, ref renderData);

            KeyToPreviewDataMap.Add(nodeGuid, renderData);
        }

        void CollectPreviewPropertiesFromVariableNode(VariableNodeModel nodeModel, ref PreviewRenderData previewRenderData)
        {
            // TODO: Collect preview properties from the newly added variable node
            /* var nodeInputPorts = m_ShaderGraphModel.GetInputPortsOnNode(nodeModel);

            foreach (var inputPort in nodeInputPorts)
            {
                var portPreviewHandler = new PortPreviewHandler(inputPort);
                portPreviewHandler.SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock);

                // For each port on the node, find the matching port model in its port mappings
                nodeModel.PortMappings.TryGetValue(inputPort, out var matchingPortModel);
                if (matchingPortModel != null)
                {
                    KeyToPreviewPropertyHandlerMap.Add(matchingPortModel.Guid.ToString(), portPreviewHandler);
                }
            }*/
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
