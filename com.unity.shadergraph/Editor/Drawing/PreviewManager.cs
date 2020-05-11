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

        int m_MaxNodesCompiling = 2;                                                                        // max preview shaders we want to async compile at once

        // state trackers
        HashSet<AbstractMaterialNode> m_NodesShaderChanged = new HashSet<AbstractMaterialNode>();           // nodes whose shader code has changed, this node and nodes that read from it are put into NeedRecompile
        HashSet<AbstractMaterialNode> m_NodesNeedsRecompile = new HashSet<AbstractMaterialNode>();           // nodes we need to recompile the preview shader
        HashSet<AbstractMaterialNode> m_NodesCompiling = new HashSet<AbstractMaterialNode>();               // nodes currently being compiled
        HashSet<AbstractMaterialNode> m_NodesToDraw = new HashSet<AbstractMaterialNode>();                  // nodes to rebuild the texture for
        HashSet<AbstractMaterialNode> m_TimedNodes = new HashSet<AbstractMaterialNode>();                   // nodes that are dependent on a time node -- i.e. animated -- need to redraw every frame
        bool m_RefreshTimedNodes;                                                                           // flag to trigger rebuilding the list of timed nodes

        PreviewSceneResources m_SceneResources;
        Texture2D m_ErrorTexture;
        Vector2? m_NewMasterPreviewSize;

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
        }

        public OnPrimaryMasterChanged onPrimaryMasterChanged;

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
            if (node != null)
            {
                m_RenderDatas.TryGetValue(node.objectId, out result);
            }
            return result;
        }

        void AddPreview(AbstractMaterialNode node)
        {
            var isMaster = false;

            if (node is IMasterNode || node is SubGraphOutputNode)
            {
                // we don't build preview render data for output nodes that aren't the active output node
                if (masterRenderData != null || (node is IMasterNode && node != node.owner.outputNode))
                {
                    return;
                }

                isMaster = true;
            }

            var renderData = new PreviewRenderData
            {
                renderTexture =
                    new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    }
            };

            if (isMaster)
            {
                m_MasterRenderData = renderData;
                renderData.renderTexture.width = renderData.renderTexture.height = 400;
            }

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

            if (m_MasterRenderData == renderData && onPrimaryMasterChanged != null)
            {
                onPrimaryMasterChanged();
            }

            m_NodesNeedsRecompile.Add(node);
        }

        void OnNodeModified(AbstractMaterialNode node, ModificationScope scope)
        {
            if (scope == ModificationScope.Topological ||
                scope == ModificationScope.Graph)
            {
                m_NodesShaderChanged.Add(node);
                m_RefreshTimedNodes = true;
            }
            else if (scope == ModificationScope.Node)
            {
                // if we only changed a constant on the node, we don't have to recompile the shader for it, just re-render it with the updated constant
                m_NodesToDraw.Add(node);
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
            if (m_Graph.didActiveOutputNodeChange)
            {
                DestroyPreview(masterRenderData.shaderData.node.objectId);
            }

            foreach (var node in m_Graph.removedNodes)
            {
                DestroyPreview(node.objectId);
                m_RefreshTimedNodes = true;
            }

            // remove the nodes from the state trackers
            m_NodesShaderChanged.ExceptWith(m_Graph.removedNodes);
            m_NodesNeedsRecompile.ExceptWith(m_Graph.removedNodes);
            m_NodesCompiling.ExceptWith(m_Graph.removedNodes);
            m_NodesToDraw.ExceptWith(m_Graph.removedNodes);
            m_TimedNodes.ExceptWith(m_Graph.removedNodes);

            m_Messenger.ClearNodesFromProvider(this, m_Graph.removedNodes);

            foreach (var node in m_Graph.addedNodes)
            {
                AddPreview(node);
                m_RefreshTimedNodes = true;
            }

            foreach (var edge in m_Graph.removedEdges)
            {
                var node = edge.inputSlot.node;
                if (node != null)
                {
                    m_NodesShaderChanged.Add(node);
                    m_RefreshTimedNodes = true;
                }
            }
            foreach (var edge in m_Graph.addedEdges)
            {
                var node = edge.inputSlot.node;
                if(node != null)
                {
                    m_NodesShaderChanged.Add(node);
                    m_RefreshTimedNodes = true;
                }
            }
        }

        private static readonly ProfilerMarker CollectPreviewPropertiesMarker = new ProfilerMarker("CollectPreviewProperties");
        void CollectPreviewProperties(PooledList<PreviewProperty> perMaterialPreviewProperties)
        {
            using (CollectPreviewPropertiesMarker.Auto())
            using (var tempCollectNodes = PooledHashSet<AbstractMaterialNode>.Get())
            using (var tempPreviewProps = PooledList<PreviewProperty>.Get())
            {
                // we only collect properties from nodes upstream of something we want to draw
                // TODO: we could go a step farther and only collect properties from nodes we know have changed their value
                // but that's not something we currently track...
                PropagateNodes(m_NodesToDraw, PropagationDirection.Upstream, tempCollectNodes);

                foreach (var propNode in tempCollectNodes)
                    propNode.CollectPreviewMaterialProperties(tempPreviewProps);

                foreach (var prop in m_Graph.properties)
                    tempPreviewProps.Add(prop.GetPreviewMaterialProperty());

                foreach (var previewProperty in tempPreviewProps)
                {
                    previewProperty.SetValueOnMaterialPropertyBlock(m_SharedPreviewPropertyBlock);

                    // virtual texture assignments must be pushed to the materials themselves (MaterialPropertyBlocks not supported)
                    if ((previewProperty.propType == PropertyType.VirtualTexture) &&
                        (previewProperty.vtProperty?.value?.layers != null))
                    {
                        perMaterialPreviewProperties.Add(previewProperty);
                    }
                }
            }
        }

        void AssignPerMaterialPreviewProperties(Material mat, List<PreviewProperty> perMaterialPreviewProperties)
        {
            #if ENABLE_VIRTUALTEXTURES
            foreach (var prop in perMaterialPreviewProperties)
            {
                switch (prop.propType)
                {
                    case PropertyType.VirtualTexture:

                        // setup the VT textures on the material
                        bool setAnyTextures = false;
                        var vt = prop.vtProperty.value;
                        for (int layer = 0; layer < vt.layers.Count; layer++)
                        {
                            if (vt.layers[layer].layerTexture.texture != null)
                            {
                                int propIndex = mat.shader.FindPropertyIndex(vt.layers[layer].layerRefName);
                                if (propIndex != -1)
                                {
                                    mat.SetTexture(vt.layers[layer].layerRefName, vt.layers[layer].layerTexture.texture);
                                    setAnyTextures = true;
                                }
                            }
                        }

                        // also put in a request for the VT tiles, since preview rendering does not have feedback enabled
                        if (setAnyTextures)
                        {
                            int stackPropertyId = Shader.PropertyToID(prop.vtProperty.referenceName);
                            try
                            {
                                // Ensure we always request the mip sized 256x256
                                int width, height;
                                UnityEngine.Rendering.VirtualTexturing.System.GetTextureStackSize(mat, stackPropertyId, out width, out height);
                                int textureMip = (int)Math.Max(Mathf.Log(width, 2f), Mathf.Log(height, 2f));
                                const int baseMip = 8;
                                int mip = Math.Max(textureMip - baseMip, 0);
                                UnityEngine.Rendering.VirtualTexturing.System.RequestRegion(mat, stackPropertyId, new Rect(0.0f, 0.0f, 1.0f, 1.0f), mip, UnityEngine.Rendering.VirtualTexturing.System.AllMips);
                            }
                            catch (InvalidOperationException)
                            {
                                // This gets thrown when the system is in an indeterminate state (like a material with no textures assigned which can obviously never have a texture stack streamed).
                                // This is valid in this case as we're still authoring the material.
                            }
                        }
                        break;
                }
            }
        #endif // ENABLE_VIRTUALTEXTURES
        }

        private static readonly ProfilerMarker RenderPreviewsMarker = new ProfilerMarker("RenderPreviews");
        private static readonly ProfilerMarker PrepareNodesMarker = new ProfilerMarker("PrepareNodesMarker");
        public void RenderPreviews(bool requestShaders = true)
        {
            using (RenderPreviewsMarker.Auto())
            using (var renderList2D = PooledList<PreviewRenderData>.Get())
            using (var renderList3D = PooledList<PreviewRenderData>.Get())
            using (var perMaterialPreviewProperties = PooledList<PreviewProperty>.Get())
            {
                if (requestShaders)
                    UpdateShaders();

                UpdateTimedNodeList();

                PropagateNodes(m_NodesToDraw, PropagationDirection.Downstream, m_NodesToDraw);
                m_NodesToDraw.UnionWith(m_TimedNodes);

                if (m_NodesToDraw.Count <= 0)
                    return;

                CollectPreviewProperties(perMaterialPreviewProperties);

                // setup other global properties
                var time = Time.realtimeSinceStartup;
                var timeParameters = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);
                m_SharedPreviewPropertyBlock.SetVector("_TimeParameters", timeParameters);

                using (PrepareNodesMarker.Auto())
                {
                    foreach (var node in m_NodesToDraw)
                    {
                        if (node == null || !node.hasPreview || !node.previewExpanded)
                            continue;

                        var renderData = GetPreviewRenderData(node);
                        if (renderData == null) // non-active output nodes can have NULL render data (no preview)
                            continue;

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

                        if (renderData.previewMode == PreviewMode.Preview2D)
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
                    RenderPreview(renderData, m_SceneResources.quad, Matrix4x4.identity, perMaterialPreviewProperties);

                // Render 3D previews
                m_SceneResources.camera.transform.position = -Vector3.forward * 5;
                m_SceneResources.camera.transform.rotation = Quaternion.identity;
                m_SceneResources.camera.orthographic = false;

                foreach (var renderData in renderList3D)
                    RenderPreview(renderData, m_SceneResources.sphere, Matrix4x4.identity, perMaterialPreviewProperties);

                var renderMasterPreview = masterRenderData != null && m_NodesToDraw.Contains(masterRenderData.shaderData.node);
                if (renderMasterPreview && masterRenderData.shaderData.mat != null)
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
                    var mesh = m_Graph.previewData.serializedMesh.mesh ? m_Graph.previewData.serializedMesh.mesh :  m_SceneResources.sphere;
                    var previewTransform = Matrix4x4.Rotate(m_Graph.previewData.rotation);
                    var scale = m_Graph.previewData.scale;
                    previewTransform *= Matrix4x4.Scale(scale * Vector3.one * (Vector3.one).magnitude / mesh.bounds.size.magnitude);
                    previewTransform *= Matrix4x4.Translate(-mesh.bounds.center);

                    RenderPreview(masterRenderData, mesh, previewTransform, perMaterialPreviewProperties);
                }

                m_SceneResources.light0.enabled = false;
                m_SceneResources.light1.enabled = false;

                foreach (var renderData in renderList2D)
                    renderData.NotifyPreviewChanged();
                foreach (var renderData in renderList3D)
                    renderData.NotifyPreviewChanged();
                if (renderMasterPreview)
                    masterRenderData.NotifyPreviewChanged();

                m_NodesToDraw.Clear();
            }
        }

        public void ForceShaderUpdate()
        {
            foreach (var data in m_RenderDatas.Values)
            {
                m_NodesNeedsRecompile.Add(data.shaderData.node);
            }
        }

        private static readonly ProfilerMarker ProcessCompletedShaderCompilationsMarker = new ProfilerMarker("ProcessCompletedShaderCompilations");
        private int compileFailRekicks = 0;
        void ProcessCompletedShaderCompilations()
        {
            // Check for shaders that finished compiling and set them to redraw
            using (ProcessCompletedShaderCompilationsMarker.Auto())
            using (var nodesCompiled = PooledHashSet<AbstractMaterialNode>.Get())
            {
                foreach (var node in m_NodesCompiling)
                {
                    PreviewRenderData renderData = GetPreviewRenderData(node);
                    PreviewShaderData shaderData = renderData.shaderData;
                    Assert.IsTrue(shaderData.passesCompiling > 0);

                    if (shaderData.passesCompiling != renderData.shaderData.mat.passCount)
                    {
                        // attempt to re-kick the compilation a few times
                        compileFailRekicks++;
                        if (compileFailRekicks <= 3)
                        {
                            renderData.shaderData.passesCompiling = 0;
                            m_NodesNeedsRecompile.Add(node);
                            nodesCompiled.Add(node);
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
                        continue;
                    }

                    // Force the material to re-generate all it's shader properties, by reassigning the shader
                    renderData.shaderData.mat.shader = renderData.shaderData.shader;
                    renderData.shaderData.passesCompiling = 0;
                    renderData.shaderData.isOutOfDate = false;
                    CheckForErrors(renderData.shaderData);

                    nodesCompiled.Add(renderData.shaderData.node);

                    var masterNode = renderData.shaderData.node as IMasterNode;
                    masterNode?.ProcessPreviewMaterial(renderData.shaderData.mat);
                }

                // removed compiled nodes from compiling list
                m_NodesCompiling.ExceptWith(nodesCompiled);

                // and add them to the draw list
                m_NodesToDraw.UnionWith(nodesCompiled);
            }
        }

        private static readonly ProfilerMarker KickOffShaderCompilationsMarker = new ProfilerMarker("KickOffShaderCompilations");
        void KickOffShaderCompilations()
        {
            // Start compilation for nodes that need to recompile
            using (KickOffShaderCompilationsMarker.Auto())
            using (var nodesToCompile = PooledHashSet<AbstractMaterialNode>.Get())
            {
                // master node compile is first in the priority list, as it takes longer than the other previews
                if ((m_NodesCompiling.Count + nodesToCompile.Count < m_MaxNodesCompiling) &&
                    m_NodesNeedsRecompile.Contains(m_MasterRenderData.shaderData.node) &&
                    !m_NodesCompiling.Contains(m_MasterRenderData.shaderData.node) &&
                    ((Shader.globalRenderPipeline != null) && (Shader.globalRenderPipeline.Length > 0)))    // master node requires an SRP
                {
                    var renderData = GetPreviewRenderData(m_MasterRenderData.shaderData.node);
                    Assert.IsTrue(renderData != null);
                    nodesToCompile.Add(m_MasterRenderData.shaderData.node);
                }

                // add each node to compile list if it needs a preview, is not already compiling, and we have room
                // (we don't want to double kick compiles, so wait for the first one to get back before kicking another)
                foreach (var node in m_NodesNeedsRecompile)
                {
                    if (m_NodesCompiling.Count + nodesToCompile.Count >= m_MaxNodesCompiling)
                        break;

                    if (node.hasPreview && node.previewExpanded && !m_NodesCompiling.Contains(node))
                    {
                        var renderData = GetPreviewRenderData(node);
                        if (renderData == null) // non-active output nodes can have NULL render data (no preview)
                            continue;

                        nodesToCompile.Add(node);
                    }
                }

                // remove the selected nodes from the recompile list
                m_NodesNeedsRecompile.ExceptWith(nodesToCompile);

                // Reset error states for the UI, the shader, and all render data for nodes we're recompiling
                m_Messenger.ClearNodesFromProvider(this, nodesToCompile);

                // Force async compile on
                var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = true;

                // kick async compiles for all nodes in m_NodeToCompile
                foreach (var node in nodesToCompile)
                {
                    if (node is IMasterNode && masterRenderData != null && node == masterRenderData.shaderData.node && !(node is VfxMasterNode))
                    {
                        UpdateMasterNodeShader();
                        continue;
                    }

                    Assert.IsFalse(!node.hasPreview && !(node is SubGraphOutputNode || node is VfxMasterNode));

                    var renderData = GetPreviewRenderData(node);

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
                    PropagateNodes(m_NodesShaderChanged, PropagationDirection.Downstream, m_NodesNeedsRecompile);
                    m_NodesShaderChanged.Clear();
                }

                // if there's nothing to update, or if too many nodes are still compiling, then just return
                if ((m_NodesNeedsRecompile.Count == 0) || (m_NodesCompiling.Count >= m_MaxNodesCompiling))
                    return;

                // flag all nodes in m_NodesNeedRecompile as having out of date textures, and redraw them
                foreach (var node in m_NodesNeedsRecompile)
                {
                    PreviewRenderData previewRendererData = GetPreviewRenderData(node);
                    if ((previewRendererData != null) && !previewRendererData.shaderData.isOutOfDate)
                    {
                        previewRendererData.shaderData.isOutOfDate = true;
                        previewRendererData.NotifyPreviewChanged();
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

                shaderData.passesCompiling = shaderData.mat.passCount;
                for (var i = 0; i < shaderData.mat.passCount; i++)
                {
                    ShaderUtil.CompilePass(shaderData.mat, i);
                }
                m_NodesCompiling.Add(shaderData.node);
            }
        }

        private static readonly ProfilerMarker UpdateTimedNodeListMarker = new ProfilerMarker("RenderPreviews");
        void UpdateTimedNodeList()
        {
            if (!m_RefreshTimedNodes)
                return;

            using (UpdateTimedNodeListMarker.Auto())
            {
                m_TimedNodes.Clear();
                foreach (var timeNode in m_Graph.GetNodes<AbstractMaterialNode>().Where(node => node.RequiresTime()))
                {
                    m_TimedNodes.Add(timeNode);
                }
                PropagateNodes(m_TimedNodes, PropagationDirection.Downstream, m_TimedNodes);

                m_RefreshTimedNodes = false;
            }
        }

       private static readonly ProfilerMarker RenderPreviewMarker = new ProfilerMarker("RenderPreview");
        void RenderPreview(PreviewRenderData renderData, Mesh mesh, Matrix4x4 transform, PooledList<PreviewProperty> perMaterialPreviewProperties)
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

                AssignPerMaterialPreviewProperties(renderData.shaderData.mat, perMaterialPreviewProperties);

                var previousRenderTexture = RenderTexture.active;

                //Temp workaround for alpha previews...
                var temp = RenderTexture.GetTemporary(renderData.renderTexture.descriptor);
                RenderTexture.active = temp;
                Graphics.Blit(Texture2D.whiteTexture, temp, m_SceneResources.checkerboardMaterial);

                m_SceneResources.camera.targetTexture = temp;
                Graphics.DrawMesh(mesh, transform, renderData.shaderData.mat, 1, m_SceneResources.camera, 0, m_SharedPreviewPropertyBlock, ShadowCastingMode.Off, false, null, false);

                var previousUseSRP = Unsupported.useScriptableRenderPipeline;
                Unsupported.useScriptableRenderPipeline = renderData.shaderData.node is IMasterNode;
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
                    m_Messenger.AddOrAppendError(this, shaderData.node.objectId, messages[0]);
                }
            }
        }

        void UpdateMasterNodeShader()
        {
            var shaderData = masterRenderData?.shaderData;
            var masterNode = shaderData?.node as IMasterNode;

            if (masterNode == null)
                return;

            var generator = new Generator(m_Graph, shaderData?.node, GenerationMode.Preview, shaderData?.node.name);
            shaderData.shaderString = generator.generatedShader;

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

        void DestroyPreview(string nodeId)
        {
            if (!m_RenderDatas.TryGetValue(nodeId, out var renderData))
            {
                return;
            }

            DestroyRenderData(renderData);
            m_RenderDatas.Remove(nodeId);

            // Check if we're destroying the shader data used by the master preview
            if (masterRenderData == renderData)
            {
                m_MasterRenderData = null;
                if (!m_Graph.isSubGraph && renderData.shaderData.node != m_Graph.outputNode)
                {
                    AddPreview(m_Graph.outputNode);
                }

                if (onPrimaryMasterChanged != null)
                    onPrimaryMasterChanged();
            }
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
        public AbstractMaterialNode node { get; set; }
        public Shader shader { get; set; }
        public Material mat { get; set; }
        public string shaderString { get; set; }
        public int passesCompiling { get; set; }
        public bool isOutOfDate { get; set; }
        public bool hasError { get; set; }
    }

    class PreviewRenderData
    {
        public PreviewShaderData shaderData { get; set; }
        public RenderTexture renderTexture { get; set; }
        public Texture texture { get; set; }
        public PreviewMode previewMode { get; set; }
        public OnPreviewChanged onPreviewChanged;

        public void NotifyPreviewChanged()
        {
            if (onPreviewChanged != null)
                onPreviewChanged();
        }
    }
}
