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

    class PreviewShaderData
    {
        public string Guid;
        public Shader shader;
        public Material mat;
        public string shaderString;
        public int passesCompiling;
        public bool isOutOfDate;
        public bool hasError;
    }

    class PreviewRenderData
    {
        public string Guid;
        public bool isPreviewEnabled;
        public bool isPreviewExpanded;
        public PreviewShaderData shaderData;
        public RenderTexture renderTexture;
        public Texture texture;
        public PreviewMode previewMode;
    }

    // TODO: Respond to CreateEdgeCommand and DeleteEdgeCommand
    // TODO: When node inspector and variable inspector are fully online will also need to hook those in
    // TODO: On-Node option changes could also affect preview data
    // TODO: Graph settings changes will also affect preview data
    public class GraphPreviewStateComponent : ViewStateComponent<GraphPreviewStateComponent.PreviewStateUpdater>
    {
        Dictionary<string, PreviewRenderData> KeyToPreviewDataMap = new ();

        Dictionary<string, PortPreviewHandler> KeyToPreviewPropertyHandlerMap = new();

        MaterialPropertyBlock m_PreviewMaterialPropertyBlock = new ();

        List<string> m_TimeDependentPreviewKeys = new();

        HashSet<string> m_ElementsRequiringPreviewUpdate = new();

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

                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            public void UpdateNodeState(string changedElementGuid, ModelState changedNodeState)
            {
                if(m_State.KeyToPreviewDataMap.TryGetValue(changedElementGuid, out var previewRenderData))
                {
                    // Update value of flag
                    previewRenderData.isPreviewEnabled = changedNodeState == ModelState.Enabled;
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            // TODO: Supply node/variable being affected so we can iterate through all the affected nodes as well to mark for recompile
            public void MarkElementNeedingRecompile(string elementNeedingRecompileGuid)
            {
                // TODO: Also iterate through all linked nodes
                if(m_State.KeyToPreviewDataMap.TryGetValue(elementNeedingRecompileGuid, out var previewRenderData))
                {
                    // Update value of flag
                    previewRenderData.shaderData.isOutOfDate = true;
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            public void UpdateNodePortConstantValue(string changedElementGuid, object newPortConstantValue, GraphDataNodeModel changedNodeModel)
            {
                if (m_State.KeyToPreviewPropertyHandlerMap.TryGetValue(changedElementGuid, out var portPreviewHandler))
                {
                    // Update value of port constant
                    portPreviewHandler.PortConstantValue = newPortConstantValue;
                    // Marking this element as requiring re-drawing this frame
                    m_State.m_ElementsRequiringPreviewUpdate.Add(changedElementGuid);
                    // Also, all nodes downstream of a changed property must be redrawn (to display the updated the property value)
                    foreach (var downStreamNode in m_State.m_ShaderGraphModel.GetNodesInHierarchyFromSources(new[] { changedNodeModel }, PropagationDirection.Downstream))
                    {
                        m_State.m_ElementsRequiringPreviewUpdate.Add(downStreamNode.graphDataName);
                    }

                    // TODO: Handle Virtual Texture case

                    // Then set preview property in MPB from that
                    m_State.UpdatePortPreviewPropertyBlock(portPreviewHandler);
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            // TODO: Figure out how we're going to handle preview property gathering from property types on both the blackboard properties and their variable nodes
            // Maybe we need a new VariablePreviewHandler, but subclassing from IPreviewHandler so preview manager can abstract away details of both

            // Property/Variable nodes don't have previews, but they are connected to other nodes that do,
            // so iterate through all ports that are connected to the property node, and update them
            public void UpdateVariableConstantValue(object newPortConstantValue, IVariableNodeModel changedNodeModel)
            {
                // TODO: Make the collection/discovery of connected ports to a variable a helper function in ShaderGraphModel

                // Iterate through the edges of the property nodes
                foreach (var connectedEdge in changedNodeModel.GetConnectedEdges())
                {
                    var outputPort = connectedEdge.ToPort;
                    // Update the value of the connected property on that port
                    if(outputPort.NodeModel is GraphDataNodeModel graphDataNodeModel)
                        this.UpdateNodePortConstantValue(outputPort.Guid.ToString(), newPortConstantValue, graphDataNodeModel);
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

        void UpdatePortPreviewPropertyBlock(PortPreviewHandler portPreviewHandler)
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
                m_ShaderGraphModel.GetTimeDependentNodesOnGraph(timedNodes);

                m_TimeDependentPreviewKeys.Clear();

                // Get guids of the time dependent nodes and add to list of time-dependent nodes requiring rendering/updating this frame
                foreach (var nodeModel in timedNodes)
                {
                    m_TimeDependentPreviewKeys.Add(nodeModel.graphDataName);
                }
            }

            // Unify list of elements with property changes with the ones that are time-dependent to get the final list of everything that needs to be rendered
            m_ElementsRequiringPreviewUpdate.UnionWith(m_TimeDependentPreviewKeys);
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
                nodeModel.TryGetPortModel(inputPort, out var matchingPortModel);
                if (matchingPortModel != null)
                {
                    KeyToPreviewPropertyHandlerMap.Add(matchingPortModel.Guid.ToString(), portPreviewHandler);
                }
            }
        }

        // TODO: Once Esme integrates types, figure out how we're representing them as nodes, will we use VariableNodeModel or GraphDataNodeModel?
        void OnVariableNodeAdded(VariableNodeModel nodeModel)
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

        void OnElementRequiringPreviewChanged()
        {

        }

        void OnElementWithPreviewRemoved(string elementGuid)
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
