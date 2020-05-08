using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Unity.Profiling;


namespace UnityEditor.ShaderGraph.Drawing
{
    delegate void OnPrimaryMasterChanged();

    class PreviewManager : IDisposable
    {
        GraphData m_Graph;
        MessageManager m_Messenger;

        MaterialPropertyBlock m_SharedPreviewPropertyBlock;         // stores preview properties (shared among ALL preview nodes)

        Dictionary<string, PreviewRenderData> m_RenderDatas = new Dictionary<string, PreviewRenderData>();  // stores all of the PreviewRendererData, mapped by node object ID
        PreviewRenderData m_MasterRenderData;                                                               // cache ref to preview renderer data for the master node

        int m_MaxPreviewsCompiling = 2;                                                                     // max preview shaders we want to async compile at once

        // state trackers
        HashSet<AbstractMaterialNode> m_NodesShaderChanged = new HashSet<AbstractMaterialNode>();           // nodes whose shader code has changed, this node and nodes that read from it are put into NeedRecompile
        HashSet<AbstractMaterialNode> m_NodesPropertyChanged = new HashSet<AbstractMaterialNode>();         // nodes whose property values have changed, the properties will need to be updated and all nodes that use that property re-rendered

        HashSet<PreviewRenderData> m_PreviewsNeedsRecompile = new HashSet<PreviewRenderData>();             // previews we need to recompile the preview shader
        HashSet<PreviewRenderData> m_PreviewsCompiling = new HashSet<PreviewRenderData>();                  // previews currently being compiled
        HashSet<PreviewRenderData> m_PreviewsToDraw = new HashSet<PreviewRenderData>();                     // previews to re-render the texture (either because shader compile changed or property changed)
        HashSet<PreviewRenderData> m_TimedPreviews = new HashSet<PreviewRenderData>();                      // previews that are dependent on a time node -- i.e. animated / need to redraw every frame
        bool m_RefreshTimedNodes;                                                                           // flag to trigger rebuilding the list of timed nodes.  ANY topological change should trigger this

        HashSet<BlockNode> m_MasterNodePreviewBlocks = new HashSet<BlockNode>();                            // all blocks used for the most recent master node preview generation. this includes temporary blocks.

        PreviewSceneResources m_SceneResources;
        Texture2D m_ErrorTexture;
        Vector2? m_NewMasterPreviewSize;

        const AbstractMaterialNode kMasterProxyNode = null;

        public PreviewRenderData masterRenderData
        {
            get { return m_MasterRenderData; }
        }

        public PreviewManager(GraphData graph, MessageManager messenger)
        {
            m_SharedPreviewPropertyBlock = new MaterialPropertyBlock();
            m_Graph = graph;
            m_Messenger = messenger;
            m_ErrorTexture = GenerateFourSquare(Color.magenta, Color.black);
            m_SceneResources = new PreviewSceneResources();

            foreach (var node in m_Graph.GetNodes<AbstractMaterialNode>())
                AddPreview(node);

            AddMasterPreview();
        }

        static Texture2D GenerateFourSquare(Color c1, Color c2)
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixel(0, 0, c1);
            tex.SetPixel(0, 1, c2);
            tex.SetPixel(1, 0, c2);
            tex.SetPixel(1, 1, c1);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }

        public void ResizeMasterPreview(Vector2 newSize)
        {
            m_NewMasterPreviewSize = newSize;
        }

        public PreviewRenderData GetPreviewRenderData(AbstractMaterialNode node)
        {
            PreviewRenderData result = null;
            if (node == kMasterProxyNode || node is BlockNode || node == m_Graph.outputNode) // TODO: node is SubGraphOutputNode ??  is there ever more than one?:   Should be caught by m_Graph.outputNode...
            {
                result = m_MasterRenderData;
            }
            else
            {
                m_RenderDatas.TryGetValue(node.objectId, out result);
            }
            
            return result;
        }

        void AddMasterPreview()
        {
            m_MasterRenderData = new PreviewRenderData
            {
                previewName = "Master:" + (m_Graph.outputNode?.name ?? ""),
                renderTexture =
                    new RenderTexture(400, 400, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    },
                previewMode = PreviewMode.Preview3D,
            };

            m_MasterRenderData.renderTexture.Create();

            var shaderData = new PreviewShaderData
            {
                node = m_Graph.outputNode,      // can be null, which means to generate with active Target
                passesCompiling = 0,
                isOutOfDate = true,
                hasError = false,
            };
            m_MasterRenderData.shaderData = shaderData;

            m_PreviewsNeedsRecompile.Add(m_MasterRenderData);
            m_RefreshTimedNodes = true;
        }

        public void UpdateMasterPreview(ModificationScope scope)
        {
            if (scope == ModificationScope.Topological ||
                scope == ModificationScope.Graph)
            {
                // mark the master preview for recompile if it exists
                // if not, no need to do it here, because it is always marked for recompile on creation
                if (m_MasterRenderData != null)
                    m_PreviewsNeedsRecompile.Add(m_MasterRenderData);
                m_RefreshTimedNodes = true;
            }
            else if (scope == ModificationScope.Node)
            {
                m_PreviewsToDraw.Add(m_MasterRenderData);
            }
        }

        void AddPreview(AbstractMaterialNode node)
        {
            Assert.IsNotNull(node);

            if (node is BlockNode)
            {
                node.RegisterCallback(OnNodeModified);
                UpdateMasterPreview(ModificationScope.Topological);
                return;
            }

//             if (node is SubGraphOutputNode && masterRenderData != null)
//             {
//                 return;
//             }

            var renderData = new PreviewRenderData
            {
                previewName = node.name ?? "UNNAMED NODE",
                renderTexture =
                    new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    }
            };

            renderData.renderTexture.Create();

            var shaderData = new PreviewShaderData
            {
                node = node,
                passesCompiling = 0,
                isOutOfDate = true,
                hasError = false,
            };
            renderData.shaderData = shaderData;

            m_RenderDatas.Add(node.objectId, renderData);
            node.RegisterCallback(OnNodeModified);

            if (node.RequiresTime())
            {
                m_RefreshTimedNodes = true;
            }

            m_PreviewsNeedsRecompile.Add(renderData);
            m_NodesPropertyChanged.Add(node);
        }

        void OnNodeModified(AbstractMaterialNode node, ModificationScope scope)
        {
            Assert.IsFalse(node == null);

            if (scope == ModificationScope.Topological ||
                scope == ModificationScope.Graph)
            {
                m_NodesShaderChanged.Add(node);     // this will trigger m_PreviewsShaderChanged downstream
                m_RefreshTimedNodes = true;
            }
            else if (scope == ModificationScope.Node)
            {
                // if we only changed a constant on the node, we don't have to recompile the shader for it, just re-render it with the updated constant
                // should instead flag m_NodesConstantChanged
                m_NodesPropertyChanged.Add(node);
            }
        }

        // temp structures that are kept around statically to avoid GC churn
        static Stack<AbstractMaterialNode> m_TempNodeWave = new Stack<AbstractMaterialNode>();
        static HashSet<AbstractMaterialNode> m_TempAddedToNodeWave = new HashSet<AbstractMaterialNode>();

        // cache the Action to avoid GC
        Action<AbstractMaterialNode> AddNextLevelNodesToWave =
            nextLevelNode =>
            {
                if (!m_TempAddedToNodeWave.Contains(nextLevelNode))
                {
                    m_TempNodeWave.Push(nextLevelNode);
                    m_TempAddedToNodeWave.Add(nextLevelNode);
                }
            };

        enum PropagationDirection
        {
            Upstream,
            Downstream
        }

        // ADDs all nodes in sources, and all nodes in the given direction relative to them, into result
        // sources and result can be the same HashSet
        private static readonly ProfilerMarker PropagateNodesMarker = new ProfilerMarker("PropagateNodes");
        void PropagateNodes(HashSet<AbstractMaterialNode> sources, PropagationDirection dir, HashSet<AbstractMaterialNode> result)
        {
            using (PropagateNodesMarker.Auto())
            {
                // NodeWave represents the list of nodes we still have to process and add to result
                m_TempNodeWave.Clear();
                m_TempAddedToNodeWave.Clear();
                foreach (var node in sources)
                {
                    m_TempNodeWave.Push(node);
                    m_TempAddedToNodeWave.Add(node);
                }

                while (m_TempNodeWave.Count > 0)
                {
                    var node = m_TempNodeWave.Pop();
                    if (node == null)
                        continue;

                    result.Add(node);

                    // grab connected nodes in propagation direction, add them to the node wave
                    ForeachConnectedNode(node, dir, AddNextLevelNodesToWave);
                }

                // clean up any temp data
                m_TempNodeWave.Clear();
                m_TempAddedToNodeWave.Clear();
            }
        }

        void ForeachConnectedNode(AbstractMaterialNode node, PropagationDirection dir, Action<AbstractMaterialNode> action)
        {
            using (var tempEdges = PooledList<IEdge>.Get())
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                // Loop through all nodes that the node feeds into.
                if (dir == PropagationDirection.Downstream)
                    node.GetOutputSlots(tempSlots);
                else
                    node.GetInputSlots(tempSlots);

                foreach (var slot in tempSlots)
                {
                    // get the edges out of each slot
                    tempEdges.Clear();                            // and here we serialize another list, ouch!
                    m_Graph.GetEdges(slot.slotReference, tempEdges);
                    foreach (var edge in tempEdges)
                    {
                        // We look at each node we feed into.
                        var connectedSlot = (dir == PropagationDirection.Downstream) ? edge.inputSlot : edge.outputSlot;
                        var connectedNode = connectedSlot.node;

                        action(connectedNode);
                    }
                }
            }
        }

        public void HandleGraphChanges()
        {
            foreach (var node in m_Graph.removedNodes)
            {
                DestroyPreview(node);
                m_RefreshTimedNodes = true;
            }

            // remove the nodes from the state trackers
            m_NodesShaderChanged.ExceptWith(m_Graph.removedNodes);
            m_NodesPropertyChanged.ExceptWith(m_Graph.removedNodes);

            m_Messenger.ClearNodesFromProvider(this, m_Graph.removedNodes);

            foreach (var node in m_Graph.addedNodes)
            {
                AddPreview(node);
                m_RefreshTimedNodes = true;
            }

            foreach (var edge in m_Graph.removedEdges)
            {
                var node = edge.inputSlot.node;
                if ((node is BlockNode) || (node is SubGraphOutputNode))
                {
                    UpdateMasterPreview(ModificationScope.Topological);
                    continue;
                }

                m_NodesShaderChanged.Add(node);
                m_RefreshTimedNodes = true;
            }
            foreach (var edge in m_Graph.addedEdges)
            {
                var node = edge.inputSlot.node;
                if(node != null)
                {
                    if ((node is BlockNode) || (node is SubGraphOutputNode))
                    {
                        UpdateMasterPreview(ModificationScope.Topological);
                        continue;
                    }
                    
                    m_NodesShaderChanged.Add(node);
                    m_RefreshTimedNodes = true;
                }
            }
        }

        private static readonly ProfilerMarker CollectPreviewPropertiesMarker = new ProfilerMarker("CollectPreviewProperties");
        void CollectPreviewProperties(IEnumerable<AbstractMaterialNode> nodesToCollect)
        {
            using (CollectPreviewPropertiesMarker.Auto())
            // using (var tempCollectNodes = PooledHashSet<AbstractMaterialNode>.Get())
            using (var tempPreviewProps = PooledList<PreviewProperty>.Get())
            {
                // we collect ALL properties from nodes upstream of something we want to draw

                // we could only bother to collect from nodes tagged as "m_NodesPropertyChanged"
                // but we would also have to figure out how to fully populate Material properties on newly created nodes
                // (shared MaterialPropertyBlock properties would be fine...)

                // collect properties from all nodes we need to draw
                // tempCollectNodes.UnionWith(m_PreviewsToDraw.Select(p => p.shaderData.node));

                // if we're collecting for master preview, also collect from all of the blocks and their upstream nodes
                /*
                if (m_PreviewsToDraw.Contains(m_MasterRenderData) || tempCollectNodes.Contains(null))
                {
                    tempCollectNodes.Remove(null);
                    foreach (var block in m_MasterNodePreviewBlocks)
                        tempCollectNodes.Add(block);
                }
                */

                // and also collect from all nodes upstream
                // PropagateNodes(tempCollectNodes, PropagationDirection.Upstream, tempCollectNodes);

                foreach (var propNode in nodesToCollect)
                    propNode.CollectPreviewMaterialProperties(tempPreviewProps);

                // also grab all graph properties
                foreach (var prop in m_Graph.properties)
                    tempPreviewProps.Add(prop.GetPreviewMaterialProperty());

                foreach (var previewProperty in tempPreviewProps)
                    previewProperty.SetValueOnMaterialPropertyBlock(m_SharedPreviewPropertyBlock);
            }
        }

        private static readonly ProfilerMarker RenderPreviewsMarker = new ProfilerMarker("RenderPreviews");
        private static readonly ProfilerMarker PrepareNodesMarker = new ProfilerMarker("PrepareNodesMarker");
        public void RenderPreviews(bool requestShaders = true)
        {
            using (RenderPreviewsMarker.Auto())
            using (var renderList2D = PooledList<PreviewRenderData>.Get())
            using (var renderList3D = PooledList<PreviewRenderData>.Get())
            using (var nodesToDraw = PooledHashSet<AbstractMaterialNode>.Get())
            {
                if (requestShaders)
                    UpdateShaders();

                UpdateTimedNodeList();

                if (m_NodesPropertyChanged.Count > 0)
                {
                    // all nodes downstream of a changed property must be redrawn (to display the updated the property value)
                    PropagateNodes(m_NodesPropertyChanged, PropagationDirection.Downstream, nodesToDraw);

                    // master node won't get picked up by the propagation
                    // but if any block nodes were picked up, flag master instead
                    if (nodesToDraw.RemoveWhere(n => n is BlockNode) > 0)
                        m_PreviewsToDraw.Add(m_MasterRenderData);
                }

                CollectPreviewProperties(m_NodesPropertyChanged);
                m_NodesPropertyChanged.Clear();

                // timed nodes change every frame, so must be drawn
                // (m_TimedNodes has been pre-propagated downstream)
                // HOWEVER they do not need to collect properties. (the only property changing is time..)
                m_PreviewsToDraw.UnionWith(m_TimedPreviews);

                m_PreviewsToDraw.UnionWith(nodesToDraw.Select(n => GetPreviewRenderData(n)));
                m_PreviewsToDraw.Remove(null);

                if (m_PreviewsToDraw.Count <= 0)
                    return;

                var time = Time.realtimeSinceStartup;
                var timeParameters = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);
                m_SharedPreviewPropertyBlock.SetVector("_TimeParameters", timeParameters);

                bool renderMasterPreview = false;
                using (PrepareNodesMarker.Auto())
                {
                    foreach (var preview in m_PreviewsToDraw)
                    {
                        if (preview == null)
                            continue;

                        // early out if the node doesn't have a preview expanded
                        var node = preview.shaderData.node;
                        if ((node != null) && (!node.hasPreview || !node.previewExpanded))
                            continue;

                        // check that we've got shaders and materials generated
                        var renderData = preview;
                        if ((renderData.shaderData.shader == null) || (renderData.shaderData.mat == null))
                        {
                            // avoid calling NotifyPreviewChanged repeatedly
                            if (renderData.texture != null)
                            {
                                renderData.texture = null;
                                renderData.NotifyPreviewChanged();
                            }
                            continue;
                        }

                        if (renderData.shaderData.hasError)
                        {
                            renderData.texture = m_ErrorTexture;
                            renderData.NotifyPreviewChanged();
                            continue;
                        }

                        // categorize what kind of render it is
                        if (node == kMasterProxyNode)
                            renderMasterPreview = true;
                        else if (renderData.previewMode == PreviewMode.Preview2D)
                            renderList2D.Add(renderData);
                        else
                            renderList3D.Add(renderData);
                    }
                }

                EditorUtility.SetCameraAnimateMaterialsTime(m_SceneResources.camera, time);

                m_SceneResources.light0.enabled = true;
                m_SceneResources.light0.intensity = 1.0f;
                m_SceneResources.light0.transform.rotation = Quaternion.Euler(50f, 50f, 0);
                m_SceneResources.light1.enabled = true;
                m_SceneResources.light1.intensity = 1.0f;
                m_SceneResources.camera.clearFlags = CameraClearFlags.Color;

                // Render 2D previews
                m_SceneResources.camera.transform.position = -Vector3.forward * 2;
                m_SceneResources.camera.transform.rotation = Quaternion.identity;
                m_SceneResources.camera.orthographicSize = 0.5f;
                m_SceneResources.camera.orthographic = true;

                foreach (var renderData in renderList2D)
                    RenderPreview(renderData, m_SceneResources.quad, Matrix4x4.identity);

                // Render 3D previews
                m_SceneResources.camera.transform.position = -Vector3.forward * 5;
                m_SceneResources.camera.transform.rotation = Quaternion.identity;
                m_SceneResources.camera.orthographic = false;

                foreach (var renderData in renderList3D)
                    RenderPreview(renderData, m_SceneResources.sphere, Matrix4x4.identity);

                if (renderMasterPreview)
                {
                    if (m_NewMasterPreviewSize.HasValue)
                    {
                        if (masterRenderData.renderTexture != null)
                            Object.DestroyImmediate(masterRenderData.renderTexture, true);
                        masterRenderData.renderTexture = new RenderTexture((int)m_NewMasterPreviewSize.Value.x, (int)m_NewMasterPreviewSize.Value.y, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };
                        masterRenderData.renderTexture.Create();
                        masterRenderData.texture = masterRenderData.renderTexture;
                        m_NewMasterPreviewSize = null;
                    }
                    var mesh = m_Graph.previewData.serializedMesh.mesh ? m_Graph.previewData.serializedMesh.mesh : m_SceneResources.sphere;
                    var previewTransform = Matrix4x4.Rotate(m_Graph.previewData.rotation);
                    var scale = m_Graph.previewData.scale;
                    previewTransform *= Matrix4x4.Scale(scale * Vector3.one * (Vector3.one).magnitude / mesh.bounds.size.magnitude);
                    previewTransform *= Matrix4x4.Translate(-mesh.bounds.center);

                    RenderPreview(masterRenderData, mesh, previewTransform);
                }

                m_SceneResources.light0.enabled = false;
                m_SceneResources.light1.enabled = false;

                foreach (var renderData in renderList2D)
                    renderData.NotifyPreviewChanged();
                foreach (var renderData in renderList3D)
                    renderData.NotifyPreviewChanged();
                if (renderMasterPreview)
                    masterRenderData.NotifyPreviewChanged();

                m_PreviewsToDraw.Clear();
            }
        }

        private static readonly ProfilerMarker ProcessCompletedShaderCompilationsMarker = new ProfilerMarker("ProcessCompletedShaderCompilations");
        private int compileFailRekicks = 0;
        void ProcessCompletedShaderCompilations()
        {
            // Check for shaders that finished compiling and set them to redraw
            using (ProcessCompletedShaderCompilationsMarker.Auto())
            using (var previewsCompiled = PooledHashSet<PreviewRenderData>.Get())
            {
                foreach (var preview in m_PreviewsCompiling)
                {
                    {
                        var node = preview.shaderData.node;
                        Assert.IsFalse(node is BlockNode);
                    }

                    PreviewRenderData renderData = preview;
                    PreviewShaderData shaderData = renderData.shaderData;

                    // Assert.IsTrue(shaderData.passesCompiling > 0);
                    if (shaderData.passesCompiling <= 0)
                    {
                        Debug.Log("Zero Passes: " + preview.previewName + " (" + shaderData.passesCompiling + " passes, " + renderData.shaderData.mat.passCount + " mat passes)");
                    }

                    if (shaderData.passesCompiling != renderData.shaderData.mat.passCount)
                    {
                        // attempt to re-kick the compilation a few times
                        Debug.Log("Rekicking Compiling: " + preview.previewName + " (" + shaderData.passesCompiling + " passes, " + renderData.shaderData.mat.passCount + " mat passes)");
                        compileFailRekicks++;
                        if (compileFailRekicks <= 3)
                        {
                            renderData.shaderData.passesCompiling = 0;
                            previewsCompiled.Add(renderData);
                            m_PreviewsNeedsRecompile.Add(renderData);
                            continue;
                        }
                        else if (compileFailRekicks == 4)
                        {
                            Debug.LogWarning("Unexpected error in compiling preview shaders: some previews might not update. You can try to re-open the Shader Graph window, or select <b>Help > Report a Bug</b> in the menu and report this bug.");
                        }
                    }

                    // check that all passes have compiled
                    var allPassesCompiled = true;
                    for (var i = 0; i < renderData.shaderData.mat.passCount; i++)
                    {
                        if (!ShaderUtil.IsPassCompiled(renderData.shaderData.mat, i))
                        {
                            allPassesCompiled = false;
                            break;
                        }
                    }

                    if (!allPassesCompiled)
                    {
                        // keep waiting
                        return;
                    }

                    // Force the material to re-generate all it's shader properties, by reassigning the shader
                    renderData.shaderData.mat.shader = renderData.shaderData.shader;
                    renderData.shaderData.passesCompiling = 0;
                    renderData.shaderData.isOutOfDate = false;
                    CheckForErrors(renderData.shaderData);

                    previewsCompiled.Add(renderData);

                    if (renderData == m_MasterRenderData)
                    {
                        // Process preview materials
                        foreach (var target in m_Graph.activeTargets)
                        {
                            if (target.IsActive())
                            {
                                target.ProcessPreviewMaterial(renderData.shaderData.mat);
                            }
                        }
                    }
                }

                // removed compiled nodes from compiling list
                m_PreviewsCompiling.ExceptWith(previewsCompiled);

                // and add them to the draw list to display updated shader (note this will only redraw specifically this node, not any others)
                m_PreviewsToDraw.UnionWith(previewsCompiled);
            }
        }

        private static readonly ProfilerMarker KickOffShaderCompilationsMarker = new ProfilerMarker("KickOffShaderCompilations");
        void KickOffShaderCompilations()
        {
            // Start compilation for nodes that need to recompile
            using (KickOffShaderCompilationsMarker.Auto())
            using (var previewsToCompile = PooledHashSet<PreviewRenderData>.Get())
            {
                // master node compile is first in the priority list, as it takes longer than the other previews
                if ((m_PreviewsCompiling.Count + previewsToCompile.Count < m_MaxPreviewsCompiling) &&
                    ((Shader.globalRenderPipeline != null) && (Shader.globalRenderPipeline.Length > 0)))    // master node requires an SRP
                {
                    if (m_MasterRenderData.shaderData.node != m_Graph.outputNode)
                        Debug.Log("MasterRenderData mismatch! " + m_MasterRenderData.shaderData.node + " != " + m_Graph.outputNode);

                    if (m_PreviewsNeedsRecompile.Contains(m_MasterRenderData) &&
                        !m_PreviewsCompiling.Contains(m_MasterRenderData))
                    {
                        Assert.IsTrue(m_MasterRenderData != null);
                        previewsToCompile.Add(m_MasterRenderData);
                        m_PreviewsNeedsRecompile.Remove(m_MasterRenderData);
                    }
                }

                // add each node to compile list if it needs a preview, is not already compiling, and we have room
                // (we don't want to double kick compiles, so wait for the first one to get back before kicking another)
                for(int i = 0; i < m_PreviewsNeedsRecompile.Count(); i++)
                {
                    if (m_PreviewsCompiling.Count + previewsToCompile.Count >= m_MaxPreviewsCompiling)
                        break;

                    var preview = m_PreviewsNeedsRecompile.ElementAt(i);
                    if (preview == m_MasterRenderData) // handled specially above
                        continue;

                    var node = preview.shaderData.node;
                    Assert.IsFalse((node == null) || (node is BlockNode));

                    if (node.hasPreview && node.previewExpanded && !m_PreviewsCompiling.Contains(preview))
                    {
                        previewsToCompile.Add(preview);
                    }
                }

                // remove the selected nodes from the recompile list
                m_PreviewsNeedsRecompile.ExceptWith(previewsToCompile);

                // Reset error states for the UI, the shader, and all render data for nodes we're recompiling
                var nodesToCompile = new HashSet<AbstractMaterialNode>();
                nodesToCompile.UnionWith(previewsToCompile.Select(x => x.shaderData.node));
                nodesToCompile.Remove(null);
                m_Messenger.ClearNodesFromProvider(this, nodesToCompile);               // not sure if it needs notification for BlockNodes when master rebuilds?

                // Force async compile on
                var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = true;

                // kick async compiles for all nodes in m_NodeToCompile
                foreach (var preview in previewsToCompile)
                {
                    if (preview == m_MasterRenderData)
                    {
                        UpdateMasterNodeShader();
                        continue;
                    }

                    var node = preview.shaderData.node;


                    if (node == null)
                        Debug.Log("ERROR:  null node preview compilation for node: " + preview.previewName);

                    Assert.IsFalse(!node.hasPreview && !(node is SubGraphOutputNode));

                    var renderData = preview; // GetPreviewRenderData(node);

                    // Get shader code and compile
                    var generator = new Generator(node.owner, node, GenerationMode.Preview, $"hidden/preview/{node.GetVariableNameForNode()}");
                    BeginCompile(renderData, generator.generatedShader);

                    // Calculate the PreviewMode from upstream nodes
                    // If any upstream node is 3D that trickles downstream
                    // TODO: not sure why this code exists here
                    // it would make more sense in HandleGraphChanges and/or RenderPreview
                    List<AbstractMaterialNode> upstreamNodes = new List<AbstractMaterialNode>();
                    NodeUtils.DepthFirstCollectNodesFromNode(upstreamNodes, node, NodeUtils.IncludeSelf.Include);
                    renderData.previewMode = PreviewMode.Preview2D;
                    foreach (var pNode in upstreamNodes)
                    {
                        if (pNode.previewMode == PreviewMode.Preview3D)
                        {
                            renderData.previewMode = PreviewMode.Preview3D;
                            break;
                        }
                    }
                }

                ShaderUtil.allowAsyncCompilation = wasAsyncAllowed;
            }
        }

        private static readonly ProfilerMarker UpdateShadersMarker = new ProfilerMarker("UpdateShaders");
        void UpdateShaders()
        {
            using (UpdateShadersMarker.Auto())
            {
                ProcessCompletedShaderCompilations();

                if (m_NodesShaderChanged.Count > 0)
                {
                    // nodes with shader changes cause all downstream nodes to need recompilation
                    // (since they presumably include the code for these nodes)
                    using (var nodesToRecompile = PooledHashSet<AbstractMaterialNode>.Get())
                    {
                        PropagateNodes(m_NodesShaderChanged, PropagationDirection.Downstream, nodesToRecompile);
                        foreach (var node in nodesToRecompile)
                        {
                            var preview = GetPreviewRenderData(node);
                            if (preview != null) // non-active output nodes can have NULL render data (no preview)
                                m_PreviewsNeedsRecompile.Add(preview);
                        }
                        m_NodesShaderChanged.Clear();
                    }
                }

                // if there's nothing to update, or if too many nodes are still compiling, then just return
                if ((m_PreviewsNeedsRecompile.Count == 0) || (m_PreviewsCompiling.Count >= m_MaxPreviewsCompiling))
                    return;

                // flag all nodes in m_PreviewsNeedsRecompile as having out of date textures, and redraw them
                foreach (var preview in m_PreviewsNeedsRecompile)
                {
                    Assert.IsNotNull(preview);
                    if (!preview.shaderData.isOutOfDate)
                    {
                        preview.shaderData.isOutOfDate = true;
                        preview.NotifyPreviewChanged();
                    }
                }

                InitializeSRPIfNeeded();    // SRP must be initialized to compile master node previews

                KickOffShaderCompilations();
            }
        }

        private static readonly ProfilerMarker BeginCompileMarker = new ProfilerMarker("BeginCompile");
        void BeginCompile(PreviewRenderData renderData, string shaderStr)
        {
            using (BeginCompileMarker.Auto())
            {
                var shaderData = renderData.shaderData;

                // want to ensure this so we don't get confused with multiple compile versions in flight
                Assert.IsTrue(shaderData.passesCompiling == 0);

                if (shaderData.shader == null)
                {
                    shaderData.shader = ShaderUtil.CreateShaderAsset(shaderStr, false);
                    shaderData.shader.hideFlags = HideFlags.HideAndDontSave;
                }
                else
                {
                    ShaderUtil.ClearCachedData(shaderData.shader);
                    ShaderUtil.UpdateShaderAsset(shaderData.shader, shaderStr, false);
                }

                if (shaderData.mat == null)
                {
                    shaderData.mat = new Material(shaderData.shader) { hideFlags = HideFlags.HideAndDontSave };
                }

                if (shaderData.mat.passCount <= 0)
                    Debug.Log("WTF Zero Passes ON COMPILE: " + shaderData.node.name + " (" + shaderData.passesCompiling + " passes, " + renderData.shaderData.mat.passCount + " mat passes)");
                else
                {
                    shaderData.passesCompiling = shaderData.mat.passCount;
                    for (var i = 0; i < shaderData.mat.passCount; i++)
                    {
                        ShaderUtil.CompilePass(shaderData.mat, i);
                    }
                    m_PreviewsCompiling.Add(renderData);
                }
            }
        }

        private static readonly ProfilerMarker UpdateTimedNodeListMarker = new ProfilerMarker("RenderPreviews");
        void UpdateTimedNodeList()
        {
            if (!m_RefreshTimedNodes)
                return;

            using (UpdateTimedNodeListMarker.Auto())
            using (var timedNodes = PooledHashSet<AbstractMaterialNode>.Get())
            {
                timedNodes.UnionWith(m_Graph.GetNodes<AbstractMaterialNode>().Where(n => n.RequiresTime()));

                // timed nodes are pre-propagated downstream, to reduce amount of propagation we have to do per frame
                PropagateNodes(timedNodes, PropagationDirection.Downstream, timedNodes);

                m_TimedPreviews.Clear();
                foreach (var node in timedNodes)
                {
                    var preview = GetPreviewRenderData(node);
                    if (preview != null)
                        m_TimedPreviews.Add(preview);
                }

                m_RefreshTimedNodes = false;
            }
        }

        private static readonly ProfilerMarker RenderPreviewMarker = new ProfilerMarker("RenderPreview");
        void RenderPreview(PreviewRenderData renderData, Mesh mesh, Matrix4x4 transform)
        {
            using (RenderPreviewMarker.Auto())
            {
                var node = renderData.shaderData.node;
                Assert.IsTrue((node != null && node.hasPreview && node.previewExpanded) || node == masterRenderData?.shaderData?.node);

                if (renderData.shaderData.hasError)
                {
                    renderData.texture = m_ErrorTexture;
                    return;
                }

                var previousRenderTexture = RenderTexture.active;

                //Temp workaround for alpha previews...
                var temp = RenderTexture.GetTemporary(renderData.renderTexture.descriptor);
                RenderTexture.active = temp;
                Graphics.Blit(Texture2D.whiteTexture, temp, m_SceneResources.checkerboardMaterial);

                // Mesh is invalid for VFXTarget
                // We should handle this more gracefully
                if(renderData != m_MasterRenderData || !m_Graph.isVFXTarget)
                {
                    m_SceneResources.camera.targetTexture = temp;
                    Graphics.DrawMesh(mesh, transform, renderData.shaderData.mat, 1, m_SceneResources.camera, 0, m_SharedPreviewPropertyBlock, ShadowCastingMode.Off, false, null, false);
                }

                var previousUseSRP = Unsupported.useScriptableRenderPipeline;
                Unsupported.useScriptableRenderPipeline = renderData == m_MasterRenderData;
                m_SceneResources.camera.Render();
                Unsupported.useScriptableRenderPipeline = previousUseSRP;

                Graphics.Blit(temp, renderData.renderTexture, m_SceneResources.blitNoAlphaMaterial);
                RenderTexture.ReleaseTemporary(temp);

                RenderTexture.active = previousRenderTexture;
                renderData.texture = renderData.renderTexture;
            }
        }

        void InitializeSRPIfNeeded()
        {
            if ((Shader.globalRenderPipeline != null) && (Shader.globalRenderPipeline.Length > 0))
            {
                return;
            }

            // issue a dummy SRP render to force SRP initialization, use the master node texture
            PreviewRenderData renderData = m_MasterRenderData;

            var previousRenderTexture = RenderTexture.active;

            //Temp workaround for alpha previews...
            var temp = RenderTexture.GetTemporary(renderData.renderTexture.descriptor);
            RenderTexture.active = temp;
            Graphics.Blit(Texture2D.whiteTexture, temp, m_SceneResources.checkerboardMaterial);

            m_SceneResources.camera.targetTexture = temp;

            var previousUseSRP = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = true;
            m_SceneResources.camera.Render();
            Unsupported.useScriptableRenderPipeline = previousUseSRP;

            RenderTexture.ReleaseTemporary(temp);

            RenderTexture.active = previousRenderTexture;
        }

        void CheckForErrors(PreviewShaderData shaderData)
        {
            shaderData.hasError = ShaderUtil.ShaderHasError(shaderData.shader);
            if (shaderData.hasError)
            {
                var messages = ShaderUtil.GetShaderMessages(shaderData.shader);
                if (messages.Length > 0)
                {
                    // TODO: Where to add errors to the stack??
                    if(shaderData.node == null)
                        return;
                    
                    m_Messenger.AddOrAppendError(this, shaderData.node.objectId, messages[0]);
                }
            }
        }

        void UpdateMasterNodeShader()
        {
            var shaderData = masterRenderData?.shaderData;
            
            // Skip generation for VFXTarget
            if(!m_Graph.isVFXTarget)
            {
                var generator = new Generator(m_Graph, shaderData?.node, GenerationMode.Preview, "Master");
                shaderData.shaderString = generator.generatedShader;

                // Blocks from the generation include those temporarily created for missing stack blocks
                // We need to hold on to these to set preview property values during CollectShaderProperties
                m_MasterNodePreviewBlocks.Clear();
                foreach(var block in generator.blocks)
                {
                    m_MasterNodePreviewBlocks.Add(block);
                }
            }

            if (string.IsNullOrEmpty(shaderData.shaderString))
            {
                if (shaderData.shader != null)
                {
                    ShaderUtil.ClearShaderMessages(shaderData.shader);
                    Object.DestroyImmediate(shaderData.shader, true);
                    shaderData.shader = null;
                }
                return;
            }

            BeginCompile(masterRenderData, shaderData.shaderString);
        }

        void DestroyRenderData(PreviewRenderData renderData)
        {
            if (renderData.shaderData != null)
            {
                if (renderData.shaderData.mat != null)
                {
                    Object.DestroyImmediate(renderData.shaderData.mat, true);
                }
                if (renderData.shaderData.shader != null)
                {
                    Object.DestroyImmediate(renderData.shaderData.shader, true);
                }
            }

            if (renderData.renderTexture != null)
                Object.DestroyImmediate(renderData.renderTexture, true);

            if (renderData.shaderData != null && renderData.shaderData.node != null)
                renderData.shaderData.node.UnregisterCallback(OnNodeModified);
        }

        void DestroyPreview(AbstractMaterialNode node)
        {
            string nodeId = node.objectId;

            if ((node is BlockNode) || (node is SubGraphOutputNode))
            {
                // block nodes don't have preview render data
                Assert.IsFalse(m_RenderDatas.ContainsKey(node.objectId));
                node.UnregisterCallback(OnNodeModified);
                UpdateMasterPreview(ModificationScope.Topological);
                return;
            }

            if (!m_RenderDatas.TryGetValue(nodeId, out var renderData))
            {
                return;
            }

            m_PreviewsNeedsRecompile.Remove(renderData);
            m_PreviewsCompiling.Remove(renderData);
            m_PreviewsToDraw.Remove(renderData);
            m_TimedPreviews.Remove(renderData);

            DestroyRenderData(renderData);
            m_RenderDatas.Remove(nodeId);
        }

        void ReleaseUnmanagedResources()
        {
            if (m_ErrorTexture != null)
            {
                Object.DestroyImmediate(m_ErrorTexture);
                m_ErrorTexture = null;
            }
            if (m_SceneResources != null)
            {
                m_SceneResources.Dispose();
                m_SceneResources = null;
            }
            foreach (var renderData in m_RenderDatas.Values)
                DestroyRenderData(renderData);
            m_RenderDatas.Clear();
            m_SharedPreviewPropertyBlock.Clear();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PreviewManager()
        {
            throw new Exception("PreviewManager was not disposed of properly.");
        }
    }

    delegate void OnPreviewChanged();

    class PreviewShaderData
    {
        public AbstractMaterialNode node;
        public Shader shader;
        public Material mat;
        public string shaderString;
        public int passesCompiling;
        public bool isOutOfDate;
        public bool hasError;
    }

    class PreviewRenderData
    {
        public string previewName;
        public PreviewShaderData shaderData;
        public RenderTexture renderTexture;
        public Texture texture;
        public PreviewMode previewMode;
        public OnPreviewChanged onPreviewChanged;

        public void NotifyPreviewChanged()
        {
            if (onPreviewChanged != null)
                onPreviewChanged();
        }
    }
}
