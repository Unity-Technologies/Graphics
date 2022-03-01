// using System;
// using System.Collections.Generic;
// using Editor.GraphUI.Utilities;
// using UnityEditor.GraphToolsFoundation.Overdrive;
// using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
// using UnityEditor.ShaderGraph.GraphDelta;
// using UnityEditor.ShaderGraph.GraphUI.DataModel;
// using UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview;
// using UnityEngine;
// using UnityEngine.Assertions;
// using UnityEngine.GraphToolsFoundation.CommandStateObserver;
// using UnityEngine.GraphToolsFoundation.Overdrive;
// using UnityEngine.Rendering;
//
// namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
// {
//     public enum PreviewMode
//     {
//         Inherit,   // this usually means: 2D, unless a connected input node is 3D, in which case it is 3D
//         Preview2D,
//         Preview3D
//     }
//
//     class PreviewShaderData
//     {
//         public string Guid;
//         public Shader shader;
//         public Material mat;
//         public string shaderString;
//         public int passesCompiling;
//         public bool isOutOfDate;
//         public bool hasError;
//     }
//
//     class PreviewRenderData
//     {
//         public string Guid;
//         // Currently unused
//         public bool isPreviewEnabled;
//         public bool isPreviewExpanded;
//         public PreviewShaderData shaderData;
//         public RenderTexture renderTexture;
//         public Texture texture;
//         public PreviewMode previewMode;
//         public Action<Texture> OnPreviewTextureUpdated;
//         public Action OnPreviewShaderCompiling;
//
//     }
//
//     // TODO: Respond to CreateEdgeCommand and DeleteEdgeCommand
//     // TODO: When node inspector and variable inspector are fully online will also need to hook those in
//     // TODO: On-Node option changes could also affect preview data
//     // TODO: Graph settings changes will also affect preview data
//     public class GraphPreviewStateComponent : ViewStateComponent<GraphPreviewStateComponent.PreviewStateUpdater>
//     {
//         Dictionary<string, PreviewRenderData> KeyToPreviewDataMap = new ();
//
//         Dictionary<string, PortPreviewHandler> PortPreviewHandlers = new();
//
//         Dictionary<string, VariablePreviewHandler> VariablePreviewHandlers = new();
//
//         MaterialPropertyBlock m_PreviewMaterialPropertyBlock = new ();
//
//         List<string> m_TimeDependentPreviewKeys = new();
//
//         HashSet<string> m_ElementsRequiringPreviewUpdate = new();
//
//         ShaderGraphModel m_ShaderGraphModel;
//
//         Texture2D m_ErrorTexture;
//
//         PreviewSceneResources m_SceneResources;
//
//         PreviewRenderData m_MainPreviewRenderData;
//
//         public void SetGraphModel(ShaderGraphModel shaderGraphModel)
//         {
//             m_ShaderGraphModel = shaderGraphModel;
//             m_ErrorTexture = GenerateFourSquare(Color.magenta, Color.black);
//             m_SceneResources = new PreviewSceneResources();
//             m_MainPreviewRenderData = new PreviewRenderData();
//
//             foreach (var node in shaderGraphModel.NodeModels)
//             {
//                 if(node is GraphDataNodeModel { HasPreview: true } graphDataNodeModel)
//                     OnGraphDataNodeAdded(graphDataNodeModel);
//             }
//
//             // TODO: Add master preview
//         }
//
//         static Texture2D GenerateFourSquare(Color c1, Color c2)
//         {
//             var tex = new Texture2D(2, 2);
//             tex.SetPixel(0, 0, c1);
//             tex.SetPixel(0, 1, c2);
//             tex.SetPixel(1, 0, c2);
//             tex.SetPixel(1, 1, c1);
//             tex.filterMode = FilterMode.Point;
//             tex.Apply();
//             return tex;
//         }
//
//         public class PreviewStateUpdater : BaseUpdater<GraphPreviewStateComponent>
//         {
//             public void ChangePreviewExpansionState(string changedElementGuid, bool isPreviewExpanded)
//             {
//                 if(m_State.KeyToPreviewDataMap.TryGetValue(changedElementGuid, out var previewRenderData))
//                 {
//                     // Update value of flag
//                     previewRenderData.isPreviewExpanded = isPreviewExpanded;
//                     m_State.SetUpdateType(UpdateType.Partial);
//                 }
//             }
//
//             public void ChangeNodePreviewMode(string changedElementGuid, GraphDataNodeModel changedNode, PreviewMode newPreviewMode)
//             {
//                 if(m_State.KeyToPreviewDataMap.TryGetValue(changedElementGuid, out var previewRenderData))
//                 {
//                     // Also traverse upstream (i.e. left/the input nodes) through the hierarchy of this node and concretize the actual preview mode
//                     if (previewRenderData.previewMode == PreviewMode.Inherit)
//                     {
//                         foreach (var upstreamNode in m_State.m_ShaderGraphModel.GetNodesInHierarchyFromSources(new [] {changedNode}, PropagationDirection.Upstream))
//                         {
//                             if (upstreamNode.NodePreviewMode == PreviewMode.Preview3D)
//                                 previewRenderData.previewMode = PreviewMode.Preview3D;
//                         }
//                     }
//                     else // if not inherit, just directly set it
//                         previewRenderData.previewMode = newPreviewMode;
//
//                     m_State.SetUpdateType(UpdateType.Partial);
//                 }
//             }
//
//             public void UpdateNodeState(string changedElementGuid, ModelState changedNodeState)
//             {
//                 if(m_State.KeyToPreviewDataMap.TryGetValue(changedElementGuid, out var previewRenderData))
//                 {
//                     // Update value of flag
//                     previewRenderData.isPreviewEnabled = changedNodeState == ModelState.Enabled;
//                     m_State.SetUpdateType(UpdateType.Partial);
//                 }
//             }
//
//             public void MarkNodeNeedingRecompile(string elementNeedingRecompileGuid, GraphDataNodeModel changedNodeModel)
//             {
//                 if(m_State.KeyToPreviewDataMap.TryGetValue(elementNeedingRecompileGuid, out var previewRenderData))
//                 {
//                     m_State.m_ElementsRequiringPreviewUpdate.Add(elementNeedingRecompileGuid);
//
//                     // Generate shader object for node and also create material
//                     var shaderObject = m_State.m_ShaderGraphModel.GetShaderObject(changedNodeModel);
//                     previewRenderData.shaderData.shader = shaderObject;
//                     previewRenderData.shaderData.mat = new Material(shaderObject) { hideFlags = HideFlags.HideAndDontSave };
//
//                     // nodes with shader changes cause all downstream nodes to need recompilation
//                     // (since they presumably include the code for these nodes)
//                     foreach (var downStreamNode in m_State.m_ShaderGraphModel.GetNodesInHierarchyFromSources(new [] {changedNodeModel}, PropagationDirection.Downstream))
//                     {
//                         m_State.m_ElementsRequiringPreviewUpdate.Add(downStreamNode.graphDataName);
//                         // Do the same for all downstream nodes
//                         if (m_State.KeyToPreviewDataMap.TryGetValue(elementNeedingRecompileGuid, out var downStreamNodeRenderData))
//                         {
//                             var downStreamShaderObject = m_State.m_ShaderGraphModel.GetShaderObject(changedNodeModel);
//                             downStreamNodeRenderData.shaderData.shader = downStreamShaderObject;
//                             downStreamNodeRenderData.shaderData.mat = new Material(downStreamShaderObject) { hideFlags = HideFlags.HideAndDontSave };
//                         }
//                     }
//
//                     // Update value of flag
//                     previewRenderData.shaderData.isOutOfDate = true;
//                     previewRenderData.OnPreviewShaderCompiling();
//                     m_State.SetUpdateType(UpdateType.Partial);
//                 }
//             }
//
//             public void UpdateNodePortConstantValue(string changedElementGuid, object newPortConstantValue, GraphDataNodeModel changedNodeModel)
//             {
//                 if (m_State.PortPreviewHandlers.TryGetValue(changedElementGuid, out var portPreviewHandler))
//                 {
//                     // Update value of port constant
//                     portPreviewHandler.PortConstantValue = newPortConstantValue;
//                     // Marking this node as requiring re-drawing this frame
//                     m_State.m_ElementsRequiringPreviewUpdate.Add(changedNodeModel.Guid.ToString());
//                     // Also, all nodes downstream of a changed property must be redrawn (to display the updated the property value)
//                     foreach (var downStreamNode in m_State.m_ShaderGraphModel.GetNodesInHierarchyFromSources(new[] { changedNodeModel }, PropagationDirection.Downstream))
//                     {
//                         m_State.m_ElementsRequiringPreviewUpdate.Add(downStreamNode.graphDataName);
//                     }
//
//                     // TODO: Currently need to recompile even for constant value changes
//                     MarkNodeNeedingRecompile(changedNodeModel.Guid.ToString(), changedNodeModel);
//
//                     // Then set preview property in MPB from that
//                     m_State.UpdatePortPreviewPropertyBlock(portPreviewHandler);
//                     m_State.SetUpdateType(UpdateType.Partial);
//                 }
//             }
//
//             // TODO: Figure out how we're going to handle preview property gathering from property types on both the blackboard properties and their variable nodes
//             // Maybe we need a new VariablePreviewHandler, but subclassing from IPreviewHandler so preview manager can abstract away details of both
//
//             // Property/Variable nodes don't have previews, but they are connected to other nodes that do,
//             // so iterate through all ports that are connected to the property node, and update them
//             public void UpdateVariableConstantValue(object newPortConstantValue, IVariableNodeModel changedNodeModel)
//             {
//                 // TODO: Make the collection/discovery of connected ports to a variable a helper function in ShaderGraphModel
//
//                 // Iterate through the edges of the property nodes
//                 foreach (var connectedEdge in changedNodeModel.GetConnectedEdges())
//                 {
//                     var outputPort = connectedEdge.ToPort;
//                     // Update the value of the connected property on that port
//                     if(outputPort.NodeModel is GraphDataNodeModel graphDataNodeModel)
//                         this.UpdateNodePortConstantValue(outputPort.Guid.ToString(), newPortConstantValue, graphDataNodeModel);
//                 }
//             }
//
//             public void GraphDataNodeAdded(GraphDataNodeModel nodeModel)
//             {
//                 m_State.OnGraphDataNodeAdded(nodeModel);
//             }
//
//             public void GraphDataNodeRemoved(GraphDataNodeModel nodeModel)
//             {
//                 m_State.OnGraphDataNodeRemoved(nodeModel);
//             }
//
//             public void GraphWindowTick()
//             {
//                 m_State.Tick();
//             }
//         }
//
//         ~GraphPreviewStateComponent()
//         {
//             Dispose();
//         }
//
//         void UpdatePortPreviewPropertyBlock(PortPreviewHandler portPreviewHandler)
//         {
//             portPreviewHandler.SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock);
//         }
//
//         void UpdateShaders()
//         {
//             // TODO: Consider async shader compilation, maybe we provide Material reference to the interpreter so it can populate shader object on its own
//             // We'd need to wait till that material has its shader
//
//             // Currently we need to recompile for every change made as we don't have shader constants specified as properties
//             foreach (var element in m_ElementsRequiringPreviewUpdate)
//             {
//                 var previewData = KeyToPreviewDataMap[element];
//                 Assert.IsNotNull(previewData);
//
//                 // Skip any previews that are currently collapsed
//                 if (previewData.isPreviewExpanded == false)
//                     continue;
//
//             }
//         }
//
//         void Tick()
//         {
//             using (var renderList2D = PooledList<PreviewRenderData>.Get())
//             using (var renderList3D = PooledList<PreviewRenderData>.Get())
//             {
//                 UpdateTopology();
//
//                 // Unify list of elements with property changes with the ones that are time-dependent to get the final list of everything that needs to be rendered
//                 m_ElementsRequiringPreviewUpdate.UnionWith(m_TimeDependentPreviewKeys);
//
//                 // TODO: Account for custom interpolators
//                 // Need to late capture custom interpolators because of how their type changes
//                 // can have downstream impacts on dynamic slots.
//                 /*HashSet<AbstractMaterialNode> customProps = new HashSet<AbstractMaterialNode>();
//                 PropagateNodes(
//                     new HashSet<AbstractMaterialNode>(m_NodesPropertyChanged.OfType<BlockNode>().Where(b => b.isCustomBlock)),
//                     PropagationDirection.Downstream,
//                     customProps);*/
//
//                 // TODO: Account for properties from context blocks
//                 // always update properties from temporary blocks created by master node preview generation
//                 //m_NodesPropertyChanged.UnionWith(m_MasterNodeTempBlocks);
//
//                 UpdateShaders();
//
//                 var nonMPBProperties = new List<IConstant>();
//                 ProcessGraphProperties(nonMPBProperties);
//
//                 int drawPreviewCount = 0;
//                 foreach (var element in m_ElementsRequiringPreviewUpdate)
//                 {
//                     var previewData = KeyToPreviewDataMap[element];
//
//                     Assert.IsNotNull(previewData);
//
//                     // Skip any previews that are currently collapsed
//                     if (previewData.isPreviewExpanded == false)
//                         continue;
//
//                     // check that we've got shaders and materials generated
//                     // if not ,replace the rendered texture with null
//                     if (previewData.shaderData.shader == null || previewData.shaderData.mat == null)
//                     {
//                         previewData.texture = null;
//                         // Also notify the NodePreviewPart that we're currently compiling the shaders and it should replace it with a black texture
//                         previewData.OnPreviewShaderCompiling();
//                         continue;
//                     }
//
//                     if (previewData.shaderData.hasError)
//                     {
//                         previewData.texture = m_ErrorTexture;
//                         // Also notify the NodePreviewPart that we need to replace preview image with an error texture
//                         previewData.OnPreviewTextureUpdated(m_ErrorTexture);
//                     }
//
//                     // we want to render this thing, now categorize what kind of render it is
//                     /*if (previewData == m_MasterRenderData)
//                         renderMasterPreview = true;
//                     else*/ if (previewData.previewMode == PreviewMode.Preview2D)
//                         renderList2D.Add(previewData);
//                     else
//                         renderList3D.Add(previewData);
//
//                     drawPreviewCount++;
//                 }
//
//                 // if we actually don't want to render anything at all, early out here
//                 if (drawPreviewCount <= 0)
//                     return;
//
//                 var time = Time.realtimeSinceStartup;
//                 var timeParameters = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);
//                 m_PreviewMaterialPropertyBlock.SetVector("_TimeParameters", timeParameters);
//
//                 EditorUtility.SetCameraAnimateMaterialsTime(m_SceneResources.camera, time);
//
//                 m_SceneResources.light0.enabled = true;
//                 m_SceneResources.light0.intensity = 1.0f;
//                 m_SceneResources.light0.transform.rotation = Quaternion.Euler(50f, 50f, 0);
//                 m_SceneResources.light1.enabled = true;
//                 m_SceneResources.light1.intensity = 1.0f;
//                 m_SceneResources.camera.clearFlags = CameraClearFlags.Color;
//
//                 // Render 2D previews
//                 m_SceneResources.camera.transform.position = -Vector3.forward * 2;
//                 m_SceneResources.camera.transform.rotation = Quaternion.identity;
//                 m_SceneResources.camera.orthographicSize = 0.5f;
//                 m_SceneResources.camera.orthographic = true;
//
//                foreach (var renderData in renderList2D)
//                     RenderPreview(renderData, m_SceneResources.quad, Matrix4x4.identity, nonMPBProperties);
//
//                 // Render 3D previews
//                 m_SceneResources.camera.transform.position = -Vector3.forward * 5;
//                 m_SceneResources.camera.transform.rotation = Quaternion.identity;
//                 m_SceneResources.camera.orthographic = false;
//
//                 m_SceneResources.light0.enabled = false;
//                 m_SceneResources.light1.enabled = false;
//
//                 foreach (var renderData in renderList3D)
//                     RenderPreview(renderData, m_SceneResources.sphere, Matrix4x4.identity, nonMPBProperties);
//
//                 // TODO: Render master preview
//                 /*if (renderMasterPreview)
//                 {
//                     if (m_NewMasterPreviewSize.HasValue)
//                     {
//                         if (masterRenderData.renderTexture != null)
//                             Object.DestroyImmediate(masterRenderData.renderTexture, true);
//                         masterRenderData.renderTexture = new RenderTexture((int)m_NewMasterPreviewSize.Value.x, (int)m_NewMasterPreviewSize.Value.y, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };
//                         masterRenderData.renderTexture.Create();
//                         masterRenderData.texture = masterRenderData.renderTexture;
//                         m_NewMasterPreviewSize = null;
//                     }
//                     var mesh = m_Graph.previewData.serializedMesh.mesh ? m_Graph.previewData.serializedMesh.mesh : m_SceneResources.sphere;
//                     var previewTransform = Matrix4x4.Rotate(m_Graph.previewData.rotation);
//                     var scale = m_Graph.previewData.scale;
//                     previewTransform *= Matrix4x4.Scale(scale * Vector3.one * (Vector3.one).magnitude / mesh.bounds.size.magnitude);
//                     previewTransform *= Matrix4x4.Translate(-mesh.bounds.center);
//
//                     RenderPreview(masterRenderData, mesh, previewTransform, perMaterialPreviewProperties);
//                 }*/
//
//                 m_SceneResources.light0.enabled = false;
//                 m_SceneResources.light1.enabled = false;
//
//                 foreach (var renderData in renderList2D)
//                     renderData.OnPreviewTextureUpdated(renderData.texture);
//                 foreach (var renderData in renderList3D)
//                     renderData.OnPreviewTextureUpdated(renderData.texture);
//                 /*if (renderMasterPreview)
//                     masterRenderData.NotifyPreviewChanged();*/
//             }
//
//             // Empty the list every frame after updating the elements
//             m_ElementsRequiringPreviewUpdate.Clear();
//         }
//
//         void UpdateTopology()
//         {
//             using (var timedNodes = PooledHashSet<GraphDataNodeModel>.Get())
//             {
//                 m_ShaderGraphModel?.GetTimeDependentNodesOnGraph(timedNodes);
//
//                 m_TimeDependentPreviewKeys.Clear();
//
//                 // Get guids of the time dependent nodes and add to list of time-dependent nodes requiring rendering/updating this frame
//                 foreach (var nodeModel in timedNodes)
//                 {
//                     m_TimeDependentPreviewKeys.Add(nodeModel.graphDataName);
//                 }
//             }
//         }
//
//         void ProcessGraphProperties(IList<IConstant> nonMPBProperties)
//         {
//             var graphProperties = m_ShaderGraphModel.GetGraphProperties();
//
//             foreach (var property in graphProperties)
//             {
//                 // TODO: Handle virtual texture types
//                 if (/*property.InitializationModel.ObjectValue is SerializableVirtualTexture virtualTextureValue &&
//                     virtualTextureValue.layers != null*/ property.DataType == TypeHandle.Unknown)
//                 {
//                     // virtual texture assignments must be pushed to the materials themselves (MaterialPropertyBlocks not supported)
//                     nonMPBProperties.Add(property.InitializationModel);
//                 }
//                 else
//                 {
//                     // Find variable preview handler that maps to this property, and then set value on MPB
//                     var variablePreviewHandler = VariablePreviewHandlers[property.Guid.ToString()];
//                     variablePreviewHandler.SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock);
//                 }
//             }
//         }
//
//         void RenderPreview(PreviewRenderData renderData, Mesh mesh, Matrix4x4 transform, IList<IConstant> nonMPBProperties)
//         {
//             //using (RenderPreviewMarker.Auto())
//             //{
//                 var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
//                 ShaderUtil.allowAsyncCompilation = true;
//
//                 AssignPerMaterialPreviewProperties(renderData.shaderData.mat, nonMPBProperties);
//
//                 var previousRenderTexture = RenderTexture.active;
//
//                 //Temp workaround for alpha previews...
//                 var temp = RenderTexture.GetTemporary(renderData.renderTexture.descriptor);
//                 RenderTexture.active = temp;
//                 Graphics.Blit(Texture2D.whiteTexture, temp, m_SceneResources.checkerboardMaterial);
//
//                 // Mesh is invalid for VFXTarget
//                 // We should handle this more gracefully
//                 if (renderData != m_MainPreviewRenderData /*|| !m_Graph.isOnlyVFXTarget*/)
//                 {
//                     m_SceneResources.camera.targetTexture = temp;
//                     Graphics.DrawMesh(mesh, transform, renderData.shaderData.mat, 1, m_SceneResources.camera);
//                 }
//
//                 var previousUseSRP = Unsupported.useScriptableRenderPipeline;
//                 Unsupported.useScriptableRenderPipeline = (renderData == m_MainPreviewRenderData);
//                 m_SceneResources.camera.Render();
//                 Unsupported.useScriptableRenderPipeline = previousUseSRP;
//
//                 Graphics.Blit(temp, renderData.renderTexture, m_SceneResources.blitNoAlphaMaterial);
//                 RenderTexture.ReleaseTemporary(temp);
//
//                 RenderTexture.active = previousRenderTexture;
//                 renderData.texture = renderData.renderTexture;
//
//                 ShaderUtil.allowAsyncCompilation = wasAsyncAllowed;
//            // }
//         }
//
//         void AssignPerMaterialPreviewProperties(Material mat, IList<IConstant> nonMPBProperties)
//         {
//             foreach (var property in nonMPBProperties)
//             {
//                 /*switch (property.Type)
//                 {
//                     case PropertyType.VirtualTexture:
//
//                         // setup the VT textures on the material
//                         bool setAnyTextures = false;
//                         var vt = property.vtProperty.value;
//                         for (int layer = 0; layer < vt.layers.Count; layer++)
//                         {
//                             var texture = vt.layers[layer].layerTexture?.texture;
//                             int propIndex = mat.shader.FindPropertyIndex(vt.layers[layer].layerRefName);
//                             if (propIndex != -1)
//                             {
//                                 mat.SetTexture(vt.layers[layer].layerRefName, texture);
//                                 setAnyTextures = true;
//                             }
//                         }
//                         // also put in a request for the VT tiles, since preview rendering does not have feedback enabled
//                         if (setAnyTextures)
//                         {
// #if ENABLE_VIRTUALTEXTURES
//                             int stackPropertyId = Shader.PropertyToID(prop.vtProperty.referenceName);
//                             try
//                             {
//                                 // Ensure we always request the mip sized 256x256
//                                 int width, height;
//                                 UnityEngine.Rendering.VirtualTexturing.Streaming.GetTextureStackSize(mat, stackPropertyId, out width, out height);
//                                 int textureMip = (int)Math.Max(Mathf.Log(width, 2f), Mathf.Log(height, 2f));
//                                 const int baseMip = 8;
//                                 int mip = Math.Max(textureMip - baseMip, 0);
//                                 UnityEngine.Rendering.VirtualTexturing.Streaming.RequestRegion(mat, stackPropertyId, new Rect(0.0f, 0.0f, 1.0f, 1.0f), mip, UnityEngine.Rendering.VirtualTexturing.System.AllMips);
//                             }
//                             catch (InvalidOperationException)
//                             {
//                                 // This gets thrown when the system is in an indeterminate state (like a material with no textures assigned which can obviously never have a texture stack streamed).
//                                 // This is valid in this case as we're still authoring the material.
//                             }
// #endif // ENABLE_VIRTUALTEXTURES
//                         }
//                         break;
//                 }*/
//             }
//         }
//
//         void OnGraphDataNodeAdded(GraphDataNodeModel nodeModel)
//         {
//             var nodeGuid = nodeModel.Guid.ToString();
//
//             var renderData = new PreviewRenderData
//             {
//                 Guid = nodeGuid,
//                 renderTexture =
//                 new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
//                 {
//                     hideFlags = HideFlags.HideAndDontSave
//                 },
//                 OnPreviewTextureUpdated = nodeModel.OnPreviewTextureUpdated,
//                 OnPreviewShaderCompiling = nodeModel.OnPreviewShaderCompiling,
//                 isPreviewExpanded = true
//             };
//
//             var shaderData = new PreviewShaderData
//             {
//                 Guid = nodeGuid,
//                 passesCompiling = 0,
//                 isOutOfDate = true,
//                 hasError = false,
//             };
//
//             renderData.shaderData = shaderData;
//
//             bool upstream3DNode = false;
//             foreach (var upstreamNode in m_ShaderGraphModel.GetNodesInHierarchyFromSources(new [] {nodeModel}, PropagationDirection.Upstream))
//             {
//                 if (upstreamNode.NodePreviewMode == PreviewMode.Preview3D)
//                     upstream3DNode = true;
//             }
//
//             if (upstream3DNode)
//                 renderData.previewMode = PreviewMode.Preview3D;
//             else
//                 renderData.previewMode = PreviewMode.Preview2D;
//
//             CollectPreviewPropertiesFromGraphDataNode(nodeModel, ref renderData);
//
//             KeyToPreviewDataMap.Add(nodeGuid, renderData);
//         }
//
//         void OnGraphDataNodeRemoved(GraphDataNodeModel nodeModel)
//         {
//             KeyToPreviewDataMap.Remove(nodeModel.graphDataName);
//             // Also iterate through the ports on this node and remove the port preview handlers that were spawned from it
//             nodeModel.TryGetNodeReader(out var nodeReader);
//             foreach (var inputPort in nodeReader.GetInputPorts())
//             {
//                 // For each port on the node, find the matching port model in its port mappings
//                 nodeModel.PortMappings.TryGetValue(inputPort, out var matchingPortModel);
//                 if (matchingPortModel != null)
//                     PortPreviewHandlers.Remove(matchingPortModel.Guid.ToString());
//             }
//
//             // TODO: Clear any shader messages and shader objects related to this node as well
//             // And unregister any callbacks that were bound on it
//         }
//
//         void CollectPreviewPropertiesFromGraphDataNode(GraphDataNodeModel nodeModel, ref PreviewRenderData previewRenderData)
//         {
//             // For a node: Get the input ports for the node, get fields from the ports, and get values from the fields
//             var nodeInputPorts = m_ShaderGraphModel.GetInputPortsOnNode(nodeModel);
//
//             foreach (var inputPort in nodeInputPorts)
//             {
//                 var portPreviewHandler = new PortPreviewHandler(inputPort);
//                 portPreviewHandler.SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock);
//
//                 // For each port on the node, find the matching port model in its port mappings
//                 nodeModel.TryGetPortModel(inputPort, out var matchingPortModel);
//                 if (matchingPortModel != null)
//                 {
//                     PortPreviewHandlers.Add(matchingPortModel.Guid.ToString(), portPreviewHandler);
//                 }
//             }
//         }
//
//         // TODO: Once Esme integrates types, figure out how we're representing them as nodes, will we use VariableNodeModel or GraphDataNodeModel?
//         void OnVariableNodeAdded(VariableNodeModel nodeModel)
//         {
//             // Make sure the model has been assigned
//             m_ShaderGraphModel ??= nodeModel.GraphModel as ShaderGraphModel;
//
//             var nodeGuid = nodeModel.Guid.ToString();
//
//             var renderData = new PreviewRenderData
//             {
//                 Guid = nodeGuid,
//                 renderTexture =
//                     new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
//                     {
//                         hideFlags = HideFlags.HideAndDontSave
//                     },
//             };
//
//             var shaderData = new PreviewShaderData
//             {
//                 Guid = nodeGuid,
//                 passesCompiling = 0,
//                 isOutOfDate = true,
//                 hasError = false,
//             };
//
//             renderData.shaderData = shaderData;
//
//             CollectPreviewPropertiesFromVariableNode(nodeModel, ref renderData);
//
//             KeyToPreviewDataMap.Add(nodeGuid, renderData);
//         }
//
//         void CollectPreviewPropertiesFromVariableNode(VariableNodeModel nodeModel, ref PreviewRenderData previewRenderData)
//         {
//             // TODO: Collect preview properties from the newly added variable node
//             /* var nodeInputPorts = m_ShaderGraphModel.GetInputPortsOnNode(nodeModel);
//
//             foreach (var inputPort in nodeInputPorts)
//             {
//                 var portPreviewHandler = new PortPreviewHandler(inputPort);
//                 portPreviewHandler.SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock);
//
//                 // For each port on the node, find the matching port model in its port mappings
//                 nodeModel.PortMappings.TryGetValue(inputPort, out var matchingPortModel);
//                 if (matchingPortModel != null)
//                 {
//                     KeyToPreviewPropertyHandlerMap.Add(matchingPortModel.Guid.ToString(), portPreviewHandler);
//                 }
//             }*/
//         }
//
//         protected override void Dispose(bool disposing)
//         {
//             if(disposing)
//                 Debug.Log("Disposing!");
//         }
//     }
// }
