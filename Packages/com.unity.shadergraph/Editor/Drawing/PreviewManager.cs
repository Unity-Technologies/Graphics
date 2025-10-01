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

        Dictionary<AbstractMaterialNode, PreviewRenderData> m_RenderDatas = new Dictionary<AbstractMaterialNode, PreviewRenderData>();  // stores all of the PreviewRendererData, mapped by node
        PreviewRenderData m_MasterRenderData;                                                               // ref to preview renderer data for the master node

        int m_MaxPreviewsCompiling = 2;                                                                     // max preview shaders we want to async compile at once

        // state trackers
        HashSet<AbstractMaterialNode> m_NodesShaderChanged = new HashSet<AbstractMaterialNode>();           // nodes whose shader code has changed, this node and nodes that read from it are put into NeedRecompile
        HashSet<AbstractMaterialNode> m_NodesPropertyChanged = new HashSet<AbstractMaterialNode>();         // nodes whose property values have changed, the properties will need to be updated and all nodes that use that property re-rendered

        HashSet<PreviewRenderData> m_PreviewsNeedsRecompile = new HashSet<PreviewRenderData>();             // previews we need to recompile the preview shader
        HashSet<PreviewRenderData> m_PreviewsCompiling = new HashSet<PreviewRenderData>();                  // previews currently being compiled
        HashSet<PreviewRenderData> m_PreviewsToDraw = new HashSet<PreviewRenderData>();                     // previews to re-render the texture (either because shader compile changed or property changed)
        HashSet<PreviewRenderData> m_TimedPreviews = new HashSet<PreviewRenderData>();                      // previews that are dependent on a time node -- i.e. animated / need to redraw every frame

        double m_LastTimedUpdateTime = 0.0f;

        bool m_TopologyDirty;                                                                               // indicates topology changed, used to rebuild timed node list and preview type (2D/3D) inheritance.

        HashSet<BlockNode> m_MasterNodeTempBlocks = new HashSet<BlockNode>();                               // temp blocks used by the most recent master node preview generation.

        // used to detect when texture assets have been modified
        HashSet<string> m_PreviewTextureGUIDs = new HashSet<string>();
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
            if (node == kMasterProxyNode ||
                node is BlockNode ||
                node == m_Graph.outputNode) // the outputNode, if it exists, is mapped to master
            {
                result = m_MasterRenderData;
            }
            else
            {
                m_RenderDatas.TryGetValue(node, out result);
            }

            return result;
        }

        void AddMasterPreview()
        {
            m_MasterRenderData = new PreviewRenderData
            {
                previewName = "Master Preview",
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
                // even though a SubGraphOutputNode can be directly mapped to master (via m_Graph.outputNode)
                // we always keep master node associated with kMasterProxyNode instead
                // just easier if the association is always dynamic
                node = kMasterProxyNode,
                passesCompiling = 0,
                isOutOfDate = true,
                hasError = false,
            };
            m_MasterRenderData.shaderData = shaderData;

            m_PreviewsNeedsRecompile.Add(m_MasterRenderData);
            m_PreviewsToDraw.Add(m_MasterRenderData);
            m_TopologyDirty = true;
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
                m_TopologyDirty = true;
            }
            else if (scope == ModificationScope.Node)
            {
                if (m_MasterRenderData != null)
                    m_PreviewsToDraw.Add(m_MasterRenderData);
            }
        }

        void AddPreview(AbstractMaterialNode node)
        {
            Assert.IsNotNull(node);

            // BlockNodes have no preview for themselves, but are mapped to the "Master" preview
            // SubGraphOutput nodes have their own previews, but will use the "Master" preview if they are the m_Graph.outputNode
            if (node is BlockNode)
            {
                node.RegisterCallback(OnNodeModified);
                UpdateMasterPreview(ModificationScope.Topological);
                m_NodesPropertyChanged.Add(node);
                return;
            }

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

            m_RenderDatas.Add(node, renderData);
            node.RegisterCallback(OnNodeModified);

            m_PreviewsNeedsRecompile.Add(renderData);
            m_NodesPropertyChanged.Add(node);
            m_TopologyDirty = true;
        }

        void OnNodeModified(AbstractMaterialNode node, ModificationScope scope)
        {
            Assert.IsNotNull(node);

            if (scope == ModificationScope.Topological ||
                scope == ModificationScope.Graph)
            {
                m_NodesShaderChanged.Add(node);     // shader code for this node changed, this will trigger m_PreviewsShaderChanged for all nodes downstream
                m_NodesPropertyChanged.Add(node);   // properties could also have changed at the same time and need to be re-collected
                m_TopologyDirty = true;
            }
            else if (scope == ModificationScope.Node)
            {
                // if we only changed a constant on the node, we don't have to recompile the shader for it, just re-render it with the updated constant
                // should instead flag m_NodesConstantChanged
                m_NodesPropertyChanged.Add(node);
            }
        }

        // temp structures that are kept around statically to avoid GC churn (not thread safe)
        static Stack<AbstractMaterialNode> m_TempNodeWave = new Stack<AbstractMaterialNode>();
        static HashSet<AbstractMaterialNode> m_TempAddedToNodeWave = new HashSet<AbstractMaterialNode>();

        // cache the Action to avoid GC
        static Action<AbstractMaterialNode> AddNextLevelNodesToWave =
            nextLevelNode =>
        {
            if (!m_TempAddedToNodeWave.Contains(nextLevelNode))
            {
                m_TempNodeWave.Push(nextLevelNode);
                m_TempAddedToNodeWave.Add(nextLevelNode);
            }
        };

        internal enum PropagationDirection
        {
            Upstream,
            Downstream
        }

        // ADDs all nodes in sources, and all nodes in the given direction relative to them, into result
        // sources and result can be the same HashSet
        private static readonly ProfilerMarker PropagateNodesMarker = new ProfilerMarker("PropagateNodes");
        internal static void PropagateNodes(HashSet<AbstractMaterialNode> sources, PropagationDirection dir, HashSet<AbstractMaterialNode> result)
        {
            using (PropagateNodesMarker.Auto())
                if (sources.Count > 0)
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

        static void ForeachConnectedNode(AbstractMaterialNode node, PropagationDirection dir, Action<AbstractMaterialNode> action)
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
                    node.owner.GetEdges(slot.slotReference, tempEdges);
                    foreach (var edge in tempEdges)
                    {
                        // We look at each node we feed into.
                        var connectedSlot = (dir == PropagationDirection.Downstream) ? edge.inputSlot : edge.outputSlot;
                        var connectedNode = connectedSlot.node;

                        action(connectedNode);
                    }
                }
            }

            // Custom Interpolator Blocks have implied connections to their Custom Interpolator Nodes...
            if (dir == PropagationDirection.Downstream && node is BlockNode bnode && bnode.isCustomBlock)
            {
                foreach (var cin in CustomInterpolatorUtils.GetCustomBlockNodeDependents(bnode))
                {
                    action(cin);
                }
            }
            // ... Just as custom Interpolator Nodes have implied connections to their custom interpolator blocks
            if (dir == PropagationDirection.Upstream && node is CustomInterpolatorNode ciNode && ciNode.e_targetBlockNode != null)
            {
                action(ciNode.e_targetBlockNode);
            }
        }

        public void ReloadChangedFiles(string ChangedFileDependencyGUIDs)
        {
            if (m_PreviewTextureGUIDs.Contains(ChangedFileDependencyGUIDs))
            {
                // have to setup the textures on the MaterialPropertyBlock again
                // easiest is to just mark everything as needing property update
                m_NodesPropertyChanged.UnionWith(m_RenderDatas.Keys);
            }
        }

        Dictionary<string, PropertyType> propertyTypeCache;
        public void HandleGraphChanges()
        {
            foreach (var node in m_Graph.addedNodes)
            {
                AddPreview(node);
                m_TopologyDirty = true;
            }

            foreach (var edge in m_Graph.addedEdges)
            {
                var node = edge.inputSlot.node;
                if (node != null)
                {
                    if ((node is BlockNode) || (node is SubGraphOutputNode))
                        UpdateMasterPreview(ModificationScope.Topological);
                    else
                        m_NodesShaderChanged.Add(node);
                    m_TopologyDirty = true;
                }
            }

            foreach (var node in m_Graph.removedNodes)
            {
                DestroyPreview(node);
                m_TopologyDirty = true;
            }

            foreach (var edge in m_Graph.removedEdges)
            {
                var node = edge.inputSlot.node;
                if ((node is BlockNode) || (node is SubGraphOutputNode))
                {
                    UpdateMasterPreview(ModificationScope.Topological);
                }

                m_NodesShaderChanged.Add(node);
                //When an edge gets deleted, if the node had the edge on creation, the properties would get out of sync and no value would get set.
                //Fix for https://fogbugz.unity3d.com/f/cases/1284033/
                m_NodesPropertyChanged.Add(node);

                m_TopologyDirty = true;
            }

            foreach (var edge in m_Graph.addedEdges)
            {
                var node = edge.inputSlot.node;
                if (node != null)
                {
                    if ((node is BlockNode) || (node is SubGraphOutputNode))
                    {
                        UpdateMasterPreview(ModificationScope.Topological);
                    }

                    m_NodesShaderChanged.Add(node);
                    m_TopologyDirty = true;
                }
            }

            bool mpbNeedsRebuild = false;
            if (propertyTypeCache == null)
                propertyTypeCache = new();
            foreach (var property in m_Graph.properties)
            {
                // if the reference name of a property type becomes associated with a different property type,
                // the property sheet will emit an error. This error is sort of a false positive, but to comply
                // we need to rebuild the MPB whenever a reference name's corresponding type is different.
                mpbNeedsRebuild |= propertyTypeCache.TryGetValue(property.referenceName, out var propType) && propType != property.propertyType;
                propertyTypeCache[property.referenceName] = property.propertyType;
            }
            if (mpbNeedsRebuild)
            {
                m_NodesPropertyChanged.UnionWith(m_Graph.GetNodes<AbstractMaterialNode>());
                m_SharedPreviewPropertyBlock = new();
            }

            // remove the nodes from the state trackers
            m_NodesShaderChanged.ExceptWith(m_Graph.removedNodes);
            m_NodesPropertyChanged.ExceptWith(m_Graph.removedNodes);

            m_Messenger.ClearNodesFromProvider(this, m_Graph.removedNodes);
        }

        private static readonly ProfilerMarker CollectPreviewPropertiesMarker = new ProfilerMarker("CollectPreviewProperties");
        void CollectPreviewProperties(IEnumerable<AbstractMaterialNode> nodesToCollect, PooledList<PreviewProperty> perMaterialPreviewProperties)
        {
            using (CollectPreviewPropertiesMarker.Auto())
            using (var tempPreviewProps = PooledList<PreviewProperty>.Get())
            {
                // collect from all of the changed nodes
                foreach (var propNode in nodesToCollect)
                    propNode.CollectPreviewMaterialProperties(tempPreviewProps);

                // also grab all graph properties (they are updated every frame)
                foreach (var prop in m_Graph.properties)
                    tempPreviewProps.Add(prop.GetPreviewMaterialProperty());

                foreach (var previewProperty in tempPreviewProps)
                {
                    previewProperty.SetValueOnMaterialPropertyBlock(m_SharedPreviewPropertyBlock);

                    // record guids for any texture properties
                    if ((previewProperty.propType >= PropertyType.Texture2D) && (previewProperty.propType <= PropertyType.Cubemap))
                    {

                        if (previewProperty.propType != PropertyType.Cubemap)
                        {
                            if (previewProperty.textureValue != null)
                                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(previewProperty.textureValue, out string guid, out long localID))
                                {
                                    // Note, this never gets cleared, so we accumulate texture GUIDs over time, if the user keeps changing textures
                                    m_PreviewTextureGUIDs.Add(guid);
                                }
                        }
                        else
                        {
                            if (previewProperty.cubemapValue != null)
                                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(previewProperty.cubemapValue, out string guid, out long localID))
                                {
                                    // Note, this never gets cleared, so we accumulate texture GUIDs over time, if the user keeps changing textures
                                    m_PreviewTextureGUIDs.Add(guid);
                                }
                        }

                    }
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
                            var texture = vt.layers[layer].layerTexture?.texture;
                            int propIndex = mat.shader.FindPropertyIndex(vt.layers[layer].layerRefName);
                            if (propIndex != -1)
                            {
                                mat.SetTexture(vt.layers[layer].layerRefName, texture);
                                setAnyTextures = true;
                            }
                        }
                        // also put in a request for the VT tiles, since preview rendering does not have feedback enabled
                        if (setAnyTextures)
                        {
#if ENABLE_VIRTUALTEXTURES
                            int stackPropertyId = Shader.PropertyToID(prop.vtProperty.referenceName);
                            try
                            {
                                // Ensure we always request the mip sized 256x256
                                int width, height;
                                UnityEngine.Rendering.VirtualTexturing.Streaming.GetTextureStackSize(mat, stackPropertyId, out width, out height);
                                int textureMip = (int)Math.Max(Mathf.Log(width, 2f), Mathf.Log(height, 2f));
                                const int baseMip = 8;
                                int mip = Math.Max(textureMip - baseMip, 0);
                                UnityEngine.Rendering.VirtualTexturing.Streaming.RequestRegion(mat, stackPropertyId, new Rect(0.0f, 0.0f, 1.0f, 1.0f), mip, UnityEngine.Rendering.VirtualTexturing.System.AllMips);
                            }
                            catch (InvalidOperationException)
                            {
                                // This gets thrown when the system is in an indeterminate state (like a material with no textures assigned which can obviously never have a texture stack streamed).
                                // This is valid in this case as we're still authoring the material.
                            }
#endif // ENABLE_VIRTUALTEXTURES
                        }
                        break;
                }
            }
        }

        bool TimedNodesShouldUpdate(EditorWindow editorWindow)
        {
            // get current screen FPS, clamp to what we consider a valid range
            // this is probably not accurate for multi-monitor.. but should be relevant to at least one of the monitors
            double monitorFPS = Screen.currentResolution.refreshRateRatio.value;
            if (Double.IsInfinity(monitorFPS) || Double.IsNaN(monitorFPS))
                monitorFPS = 60.0f;
            monitorFPS = Math.Min(monitorFPS, 144.0);
            monitorFPS = Math.Max(monitorFPS, 30.0);

            var curTime = EditorApplication.timeSinceStartup;
            var deltaTime = curTime - m_LastTimedUpdateTime;
            bool isFocusedWindow = (EditorWindow.focusedWindow == editorWindow);

            // we throttle the update rate, based on whether the window is focused and if unity is active
            const double k_AnimatedFPS_WhenNotFocused = 10.0;
            const double k_AnimatedFPS_WhenInactive = 2.0;
            double maxAnimatedFPS =
                (UnityEditorInternal.InternalEditorUtility.isApplicationActive ?
                    (isFocusedWindow ? monitorFPS : k_AnimatedFPS_WhenNotFocused) :
                    k_AnimatedFPS_WhenInactive);

            bool update = (deltaTime > (1.0 / maxAnimatedFPS));
            if (update)
                m_LastTimedUpdateTime = curTime;
            return update;
        }

        private static readonly ProfilerMarker RenderPreviewsMarker = new ProfilerMarker("RenderPreviews");
        private static int k_spriteProps = Shader.PropertyToID("unity_SpriteProps");
        private static int k_spriteColor = Shader.PropertyToID("unity_SpriteColor");
        private static int k_rendererColor = Shader.PropertyToID("_RendererColor");
        private float previewTime = 0;
        public void RenderPreviews(EditorWindow editorWindow, bool requestShaders = true)
        {
            using (RenderPreviewsMarker.Auto())
            using (var renderList2D = PooledList<PreviewRenderData>.Get())
            using (var renderList3D = PooledList<PreviewRenderData>.Get())
            using (var nodesToDraw = PooledHashSet<AbstractMaterialNode>.Get())
            using (var perMaterialPreviewProperties = PooledList<PreviewProperty>.Get())
            {
                // update topology cached data
                // including list of time-dependent previews, and the preview mode (2d/3d)
                UpdateTopology();

                if (requestShaders)
                    UpdateShaders();

                // Need to late capture custom interpolators because of how their type changes
                // can have downstream impacts on dynamic slots.
                HashSet<AbstractMaterialNode> customProps = new HashSet<AbstractMaterialNode>();
                PropagateNodes(
                    new HashSet<AbstractMaterialNode>(m_NodesPropertyChanged.OfType<BlockNode>().Where(b => b.isCustomBlock)),
                    PropagationDirection.Downstream,
                    customProps);

                m_NodesPropertyChanged.UnionWith(customProps);

                // all nodes downstream of a changed property must be redrawn (to display the updated the property value)
                PropagateNodes(m_NodesPropertyChanged, PropagationDirection.Downstream, nodesToDraw);

                // always update properties from temporary blocks created by master node preview generation
                m_NodesPropertyChanged.UnionWith(m_MasterNodeTempBlocks);

                CollectPreviewProperties(m_NodesPropertyChanged, perMaterialPreviewProperties);
                m_NodesPropertyChanged.Clear();

                // timed nodes are animated, so they should be updated regularly (but not necessarily on every update)
                // (m_TimedPreviews has been pre-propagated downstream)
                // HOWEVER they do not need to collect properties. (the only property changing is time..)
                if (TimedNodesShouldUpdate(editorWindow))
                    m_PreviewsToDraw.UnionWith(m_TimedPreviews);

                ForEachNodesPreview(nodesToDraw, p => m_PreviewsToDraw.Add(p));

                // redraw master when it is resized
                if (m_NewMasterPreviewSize.HasValue)
                    m_PreviewsToDraw.Add(m_MasterRenderData);

                // apply filtering to determine what nodes really get drawn
                bool renderMasterPreview = false;
                int drawPreviewCount = 0;
                foreach (var preview in m_PreviewsToDraw)
                {
                    Assert.IsNotNull(preview);

                    { // skip if the node doesn't have a preview expanded (unless it's master)
                        var node = preview.shaderData.node;
                        if ((node != kMasterProxyNode) && (!node.hasPreview || !node.previewExpanded))
                            continue;
                    }

                    // check that we've got shaders and materials generated
                    // if not ,replace the rendered texture with null
                    if ((preview.shaderData.shader == null) ||
                        (preview.shaderData.mat == null))
                    {
                        // avoid calling NotifyPreviewChanged repeatedly
                        if (preview.texture != null)
                        {
                            preview.texture = null;
                            preview.NotifyPreviewChanged();
                        }
                        continue;
                    }


                    if (preview.shaderData.hasError)
                    {
                        preview.texture = m_ErrorTexture;
                        preview.NotifyPreviewChanged();
                        continue;
                    }

                    // skip rendering while a preview shader is being compiled
                    if (m_PreviewsCompiling.Contains(preview))
                        continue;

                    // we want to render this thing, now categorize what kind of render it is
                    if (preview == m_MasterRenderData)
                        renderMasterPreview = true;
                    else if (preview.previewMode == PreviewMode.Preview2D)
                        renderList2D.Add(preview);
                    else
                        renderList3D.Add(preview);
                    drawPreviewCount++;
                }

                // if we actually don't want to render anything at all, early out here
                if (drawPreviewCount <= 0)
                    return;

                previewTime = Time.realtimeSinceStartup;
                var timeParameters = new Vector4(previewTime, Mathf.Sin(previewTime), Mathf.Cos(previewTime), 0.0f);
                m_SharedPreviewPropertyBlock.SetVector("_TimeParameters", timeParameters);

                EditorUtility.SetCameraAnimateMaterialsTime(m_SceneResources.camera, previewTime);

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
                    var mesh = m_Graph.previewData.serializedMesh.mesh;
                    var preventRotation = m_Graph.previewData.preventRotation;
                    if (!mesh)
                    {
                        var useSpritePreview =
                            m_Graph.activeTargets.LastOrDefault(t => t.IsActive())?.prefersSpritePreview ?? false;
                        mesh = useSpritePreview ? m_SceneResources.quad : m_SceneResources.sphere;
                        preventRotation = useSpritePreview;
                    }

                    var previewTransform = preventRotation ? Matrix4x4.identity : Matrix4x4.Rotate(m_Graph.previewData.rotation);
                    var scale = m_Graph.previewData.scale;
                    previewTransform *= Matrix4x4.Scale(scale * Vector3.one * (Vector3.one).magnitude / mesh.bounds.size.magnitude);
                    previewTransform *= Matrix4x4.Translate(-mesh.bounds.center);

                    //bugfix for some variables that need to be setup for URP Sprite material previews. Want a better isolated place to put them,
                    //but I dont believe such a place exists and would be too costly to add.
                    masterRenderData.shaderData.mat.SetVector(k_spriteProps, new Vector4(1, 1, -1, 0));
                    masterRenderData.shaderData.mat.SetVector(k_spriteColor, new Vector4(1, 1, 1, 1));
                    masterRenderData.shaderData.mat.SetVector(k_rendererColor, new Vector4(1, 1, 1, 1));
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
                            shaderData.passesCompiling = 0;
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
                        continue;
                    }

                    // Force the material to re-generate all it's shader properties, by reassigning the shader
                    renderData.shaderData.mat.shader = renderData.shaderData.shader;
                    renderData.shaderData.passesCompiling = 0;
                    renderData.shaderData.isOutOfDate = false;
                    CheckForErrors(renderData.shaderData);

                    previewsCompiled.Add(renderData);
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
                if (m_PreviewsCompiling.Count + previewsToCompile.Count < m_MaxPreviewsCompiling)
                {
                    if (m_PreviewsNeedsRecompile.Contains(m_MasterRenderData) &&
                        !m_PreviewsCompiling.Contains(m_MasterRenderData))
                    {
                        previewsToCompile.Add(m_MasterRenderData);
                        m_PreviewsNeedsRecompile.Remove(m_MasterRenderData);
                    }
                }

                // add each node to compile list if it needs a preview, is not already compiling, and we have room
                // (we don't want to double kick compiles, so wait for the first one to get back before kicking another)
                for (int i = 0; i < m_PreviewsNeedsRecompile.Count(); i++)
                {
                    if (m_PreviewsCompiling.Count + previewsToCompile.Count >= m_MaxPreviewsCompiling)
                        break;

                    var preview = m_PreviewsNeedsRecompile.ElementAt(i);
                    if (preview == m_MasterRenderData) // master preview is handled specially above
                        continue;

                    var node = preview.shaderData.node;
                    Assert.IsNotNull(node);
                    Assert.IsFalse(node is BlockNode);

                    if (node.hasPreview && node.previewExpanded && !m_PreviewsCompiling.Contains(preview))
                    {
                        previewsToCompile.Add(preview);
                    }
                }

                if (previewsToCompile.Count >= 0)
                    using (var nodesToCompile = PooledHashSet<AbstractMaterialNode>.Get())
                    {
                        // remove the selected nodes from the recompile list
                        m_PreviewsNeedsRecompile.ExceptWith(previewsToCompile);

                        // Reset error states for the UI, the shader, and all render data for nodes we're recompiling
                        nodesToCompile.UnionWith(previewsToCompile.Select(x => x.shaderData.node));
                        nodesToCompile.Remove(null);

                        // TODO: not sure if we need to clear BlockNodes when master gets rebuilt?
                        m_Messenger.ClearNodesFromProvider(this, nodesToCompile);

                        // Force async compile on
                        var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
                        ShaderUtil.allowAsyncCompilation = true;

                        // kick async compiles for all nodes in m_NodeToCompile
                        foreach (var preview in previewsToCompile)
                        {
                            if (preview == m_MasterRenderData)
                            {
                                CompileMasterNodeShader();
                                continue;
                            }

                            var node = preview.shaderData.node;
                            Assert.IsNotNull(node); // master preview is handled above

                            // Get shader code and compile
                            var generator = new Generator(node.owner, node, GenerationMode.Preview, $"hidden/preview/{node.GetVariableNameForNode()}");
                            BeginCompile(preview, generator.generatedShader);
                        }

                        ShaderUtil.allowAsyncCompilation = wasAsyncAllowed;
                    }
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
                        ForEachNodesPreview(nodesToRecompile, p => m_PreviewsNeedsRecompile.Add(p));

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
                    ShaderUtil.ClearShaderMessages(shaderData.shader);
                    ShaderUtil.UpdateShaderAsset(shaderData.shader, shaderStr, false);
                }

                // Set up the material we use for the preview
                // Due to case 1259744, we have to re-create the material to update the preview material keywords
                Object.DestroyImmediate(shaderData.mat);
                {
                    shaderData.mat = new Material(shaderData.shader) { hideFlags = HideFlags.HideAndDontSave };
                    if (renderData == m_MasterRenderData)
                    {
                        // apply active target settings to the Material
                        foreach (var target in m_Graph.activeTargets)
                        {
                            if (target.IsActive())
                                target.ProcessPreviewMaterial(renderData.shaderData.mat);
                        }
                    }
                }

                int materialPassCount = shaderData.mat.passCount;
                if (materialPassCount <= 0)
                    Debug.Log("Zero Passes ON COMPILE: " + shaderData.node.name + " (" + shaderData.passesCompiling + " passes, " + renderData.shaderData.mat.passCount + " mat passes)");
                else
                {
                    shaderData.passesCompiling = materialPassCount;
                    for (var i = 0; i < materialPassCount; i++)
                    {
                        ShaderUtil.CompilePass(shaderData.mat, i);
                    }
                    m_PreviewsCompiling.Add(renderData);
                }
            }
        }

        private void ForEachNodesPreview(
            IEnumerable<AbstractMaterialNode> nodes,
            Action<PreviewRenderData> action)
        {
            foreach (var node in nodes)
            {
                var preview = GetPreviewRenderData(node);
                if (preview != null)    // some output nodes may have no preview
                    action(preview);
            }
        }

        class NodeProcessor
        {
            // parameters
            GraphData graphData;
            Action<AbstractMaterialNode, IEnumerable<AbstractMaterialNode>> process;

            // node tracking state
            HashSet<AbstractMaterialNode> processing = new HashSet<AbstractMaterialNode>();
            HashSet<AbstractMaterialNode> processed = new HashSet<AbstractMaterialNode>();

            // iteration state stack
            Stack<AbstractMaterialNode> nodeStack = new Stack<AbstractMaterialNode>();
            Stack<int> childStartStack = new Stack<int>();
            Stack<int> curChildStack = new Stack<int>();
            Stack<int> stateStack = new Stack<int>();

            List<AbstractMaterialNode> allChildren = new List<AbstractMaterialNode>();

            public NodeProcessor(GraphData graphData, Action<AbstractMaterialNode, IEnumerable<AbstractMaterialNode>> process)
            {
                this.graphData = graphData;
                this.process = process;
            }

            public void ProcessInDependencyOrder(AbstractMaterialNode root)
            {
                // early out to skip a bit of work
                if (processed.Contains(root))
                    return;

                // push root node in the initial state
                stateStack.Push(0);
                nodeStack.Push(root);

                while (nodeStack.Count > 0)
                {
                    // check the state of the top of the stack
                    switch (stateStack.Pop())
                    {
                        case 0: // node initial state   (valid stacks:  nodeStack)
                        {
                            var node = nodeStack.Peek();
                            if (processed.Contains(node))
                            {
                                // finished with this node, pop it off the stack
                                nodeStack.Pop();
                                continue;
                            }

                            if (processing.Contains(node))
                            {
                                // not processed, but still processing.. means there was a circular dependency here
                                throw new ArgumentException("ERROR: graph contains circular wire connections");
                            }

                            processing.Add(node);

                            int childStart = allChildren.Count;
                            childStartStack.Push(childStart);

                            // add immediate children
                            ForeachConnectedNode(node, PropagationDirection.Upstream, n => allChildren.Add(n));

                            if (allChildren.Count == childStart)
                            {
                                // no children.. transition to state 2 (all children processed)
                                stateStack.Push(2);
                            }
                            else
                            {
                                // transition to state 1 (processing children)
                                stateStack.Push(1);
                                curChildStack.Push(childStart);
                            }
                        }
                        break;
                        case 1: // processing children (valid stacks:  nodeStack, childStartStack, curChildStack)
                        {
                            int curChild = curChildStack.Pop();

                            // first update our state for when we return from the cur child
                            int nextChild = curChild + 1;
                            if (nextChild < allChildren.Count)
                            {
                                // we will process the next child
                                stateStack.Push(1);
                                curChildStack.Push(nextChild);
                            }
                            else
                            {
                                // we will be done iterating children, move to state 2
                                stateStack.Push(2);
                            }

                            // then push the current child in state 0 to process it
                            stateStack.Push(0);
                            nodeStack.Push(allChildren[curChild]);
                        }
                        break;
                        case 2: // all children processed (valid stacks: nodeStack, childStartStack)
                        {
                            // read state, popping all
                            var node = nodeStack.Pop();
                            int childStart = childStartStack.Pop();

                            // process node
                            process(node, allChildren.Slice(childStart, allChildren.Count));
                            processed.Add(node);

                            // remove the children that were added in state 0
                            allChildren.RemoveRange(childStart, allChildren.Count - childStart);

                            // terminate node, stacks are popped to state of parent node
                        }
                        break;
                    }
                }
            }

            public void ProcessInDependencyOrderRecursive(AbstractMaterialNode node)
            {
                if (processed.Contains(node))
                    return; // already processed

                if (processing.Contains(node))
                    throw new ArgumentException("ERROR: graph contains circular wire connections");

                processing.Add(node);

                int childStart = allChildren.Count;

                // add immediate children
                ForeachConnectedNode(node, PropagationDirection.Upstream, n => allChildren.Add(n));

                // process children
                var children = allChildren.Slice(childStart, allChildren.Count);
                foreach (var child in children)
                    ProcessInDependencyOrderRecursive(child);

                // process self
                process(node, children);
                processed.Add(node);

                // remove the children
                allChildren.RemoveRange(childStart, allChildren.Count - childStart);
            }
        }

        // Processes all the nodes in the upstream trees of rootNodes
        // Will only process each node once, even if the trees overlap
        // Processes a node ONLY after processing all of the nodes in its upstream subtree
        void ProcessUpstreamNodesInDependencyOrder(
            IEnumerable<AbstractMaterialNode> rootNodes,                                    // root nodes can share subtrees, but cannot themselves exist in any others subtree
            Action<AbstractMaterialNode, IEnumerable<AbstractMaterialNode>> process)       // process takes the node and it's list of immediate upstream children as parameters
        {
            if (rootNodes.Any())
            {
                NodeProcessor processor = new NodeProcessor(rootNodes.First().owner, process);
                foreach (var node in rootNodes)
                    processor.ProcessInDependencyOrderRecursive(node);
            }
        }

        private static readonly ProfilerMarker UpdateTopologyMarker = new ProfilerMarker("UpdateTopology");
        void UpdateTopology()
        {
            if (!m_TopologyDirty)
                return;

            using (UpdateTopologyMarker.Auto())
            using (var timedNodes = PooledHashSet<AbstractMaterialNode>.Get())
            {
                timedNodes.UnionWith(m_Graph.GetNodes<AbstractMaterialNode>().Where(n => n.RequiresTime()));

                // we pre-propagate timed nodes downstream, to reduce amount of propagation we have to do per frame
                PropagateNodes(timedNodes, PropagationDirection.Downstream, timedNodes);

                m_TimedPreviews.Clear();
                ForEachNodesPreview(timedNodes, p => m_TimedPreviews.Add(p));
            }

            // Calculate the PreviewMode from upstream nodes
            ProcessUpstreamNodesInDependencyOrder(
                // we just pass all the nodes we care about as the roots
                m_RenderDatas.Values.Select(p => p.shaderData.node).Where(n => n != null),
                (node, children) =>
                {
                    var preview = GetPreviewRenderData(node);

                    // set preview mode based on node preference
                    preview.previewMode = node.previewMode;

                    // Inherit becomes 2D or 3D based on child state
                    if (preview.previewMode == PreviewMode.Inherit)
                    {
                        if (children.Any(child => GetPreviewRenderData(child).previewMode == PreviewMode.Preview3D))
                            preview.previewMode = PreviewMode.Preview3D;
                        else
                            preview.previewMode = PreviewMode.Preview2D;
                    }
                });

            m_TopologyDirty = false;
        }

        private static readonly ProfilerMarker RenderPreviewMarker = new ProfilerMarker("RenderPreview");
        void RenderPreview(PreviewRenderData renderData, Mesh mesh, Matrix4x4 transform, PooledList<PreviewProperty> perMaterialPreviewProperties)
        {
            using (RenderPreviewMarker.Auto())
            {
                var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = true;

                AssignPerMaterialPreviewProperties(renderData.shaderData.mat, perMaterialPreviewProperties);

                var previousRenderTexture = RenderTexture.active;

                //Temp workaround for alpha previews...
                var temp = RenderTexture.GetTemporary(renderData.renderTexture.descriptor);
                RenderTexture.active = temp;
                Graphics.Blit(Texture2D.whiteTexture, temp, m_SceneResources.checkerboardMaterial);

                // Mesh is invalid for VFXTarget
                // We should handle this more gracefully
                if (renderData != m_MasterRenderData || !m_Graph.isOnlyVFXTarget)
                {
                    m_SceneResources.camera.targetTexture = temp;
                    Graphics.DrawMesh(mesh, transform, renderData.shaderData.mat, 1, m_SceneResources.camera, 0, m_SharedPreviewPropertyBlock, ShadowCastingMode.Off, false, null, false);
                }

                var previousUseSRP = Unsupported.useScriptableRenderPipeline;
                Unsupported.useScriptableRenderPipeline = (renderData == m_MasterRenderData);
                m_SceneResources.camera.Render();
                Unsupported.useScriptableRenderPipeline = previousUseSRP;

                Graphics.Blit(temp, renderData.renderTexture, m_SceneResources.blitNoAlphaMaterial);
                RenderTexture.ReleaseTemporary(temp);

                RenderTexture.active = previousRenderTexture;
                renderData.texture = renderData.renderTexture;

                m_PreviewsToDraw.Remove(renderData);

                ShaderUtil.allowAsyncCompilation = wasAsyncAllowed;
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
                    if (shaderData.node == null)
                        return;

                    m_Messenger.AddOrAppendError(this, shaderData.node.objectId, messages[0]);
                    ShaderUtil.ClearShaderMessages(shaderData.shader);
                }
            }
        }

        void CompileMasterNodeShader()
        {
            var shaderData = masterRenderData?.shaderData;

            // Skip generation for VFXTarget
            if (!m_Graph.isOnlyVFXTarget)
            {
                var generator = new Generator(m_Graph, m_Graph.outputNode, GenerationMode.Preview, "Master");
                shaderData.shaderString = generator.generatedShader;

                // record the blocks temporarily created for missing stack blocks
                m_MasterNodeTempBlocks.Clear();
                foreach (var block in generator.temporaryBlocks)
                {
                    m_MasterNodeTempBlocks.Add(block);
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
                    ShaderUtil.ClearShaderMessages(renderData.shaderData.shader);
                    Object.DestroyImmediate(renderData.shaderData.shader, true);
                }
            }

            // Clear render textures
            if (renderData.renderTexture != null)
                Object.DestroyImmediate(renderData.renderTexture, true);
            if(renderData.texture != null)
                Object.DestroyImmediate(renderData.texture, true);

            // Clear callbacks
            renderData.onPreviewChanged = null;
            if (renderData.shaderData != null && renderData.shaderData.node != null)
                renderData.shaderData.node.UnregisterCallback(OnNodeModified);
        }

        void DestroyPreview(AbstractMaterialNode node)
        {
            if (node is BlockNode)
            {
                // block nodes don't have preview render data
                Assert.IsFalse(m_RenderDatas.ContainsKey(node));
                node.UnregisterCallback(OnNodeModified);
                UpdateMasterPreview(ModificationScope.Topological);
                return;
            }

            if (!m_RenderDatas.TryGetValue(node, out var renderData))
            {
                return;
            }

            m_PreviewsNeedsRecompile.Remove(renderData);
            m_PreviewsCompiling.Remove(renderData);
            m_PreviewsToDraw.Remove(renderData);
            m_TimedPreviews.Remove(renderData);

            DestroyRenderData(renderData);
            m_RenderDatas.Remove(node);
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
