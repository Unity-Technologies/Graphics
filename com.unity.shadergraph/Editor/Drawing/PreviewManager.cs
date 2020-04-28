using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    delegate void OnPrimaryMasterChanged();

    class PreviewManager : IDisposable
    {
        GraphData m_Graph;
        MessageManager m_Messenger;
        Dictionary<string, PreviewRenderData> m_RenderDatas = new Dictionary<string, PreviewRenderData>();
        PreviewRenderData m_MasterRenderData;
        HashSet<AbstractMaterialNode> m_NodesToUpdate = new HashSet<AbstractMaterialNode>();
        HashSet<AbstractMaterialNode> m_NodesToDraw = new HashSet<AbstractMaterialNode>();
        HashSet<AbstractMaterialNode> m_TimedNodes = new HashSet<AbstractMaterialNode>();
        HashSet<BlockNode> m_Blocks = new HashSet<BlockNode>();
        bool m_RefreshTimedNodes;
        bool m_UpdateMasterPreview = false;
        bool m_DrawMasterPreview = false;

        PreviewSceneResources m_SceneResources;
        Texture2D m_ErrorTexture;
        Vector2? m_NewMasterPreviewSize;

        Identifier m_MasterIdentifier;

        public PreviewRenderData masterRenderData
        {
            get { return m_MasterRenderData; }
        }

        public PreviewManager(GraphData graph, MessageManager messenger)
        {
            m_Graph = graph;
            m_Messenger = messenger;
            m_ErrorTexture = GenerateFourSquare(Color.magenta, Color.black);
            m_SceneResources = new PreviewSceneResources();
            m_MasterIdentifier = new Identifier(0);

            foreach (var node in m_Graph.GetNodes<AbstractMaterialNode>())
                AddPreview(node);

            if(!graph.isSubGraph)
            {
                AddMasterPreview();
            }
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

        public PreviewRenderData GetPreview(AbstractMaterialNode node)
        {
            return m_RenderDatas[node.objectId];
        }

        void AddMasterPreview()
        {
            var renderData = new PreviewRenderData
            {
                renderTexture =
                    new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    },
                previewMode = PreviewMode.Preview3D,
            };

            m_MasterRenderData = renderData;
            renderData.renderTexture.width = renderData.renderTexture.height = 400;
            renderData.renderTexture.Create();

            var shaderData = new PreviewShaderData
            {
                node = null,
                isCompiling = false,
                hasError = false,
                shader = ShaderUtil.CreateShaderAsset(k_EmptyShader, false),
            };
            shaderData.shader.hideFlags = HideFlags.HideAndDontSave;
            shaderData.mat = new Material(shaderData.shader) {hideFlags = HideFlags.HideAndDontSave};
            renderData.shaderData = shaderData;

            m_UpdateMasterPreview = true;
            m_RefreshTimedNodes = true;
        }

        public void UpdateMasterPreview(ModificationScope scope)
        {
            if (scope == ModificationScope.Topological ||
                scope == ModificationScope.Graph)
            {
                m_UpdateMasterPreview = true;
                m_RefreshTimedNodes = true;
            }
            else if (scope == ModificationScope.Node)
            {
                m_DrawMasterPreview = true;
            }
        }

        void AddPreview(AbstractMaterialNode node)
        {
            if(node is BlockNode)
            {
                node.RegisterCallback(OnNodeModified);
                return;
            }

            if (node is SubGraphOutputNode && masterRenderData != null)
            {
                return;
            }

            var renderData = new PreviewRenderData
            {
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
                isCompiling = false,
                hasError = false,
                shader = ShaderUtil.CreateShaderAsset(k_EmptyShader, false)
            };
            shaderData.shader.hideFlags = HideFlags.HideAndDontSave;
            shaderData.mat = new Material(shaderData.shader) {hideFlags = HideFlags.HideAndDontSave};
            renderData.shaderData = shaderData;

            m_RenderDatas.Add(node.objectId, renderData);
            node.RegisterCallback(OnNodeModified);

            if (node.RequiresTime())
            {
                m_RefreshTimedNodes = true;
            }

            MarkNodeUpdate(node);
        }

        void OnNodeModified(AbstractMaterialNode node, ModificationScope scope)
        {
            if (scope == ModificationScope.Topological ||
                scope == ModificationScope.Graph)
            {
                if(node is BlockNode blockNode)
                {
                    m_UpdateMasterPreview = true;
                    m_RefreshTimedNodes = true;
                    return;
                }

                MarkNodeUpdate(node);

                m_RefreshTimedNodes = true;
            }
            else if (scope == ModificationScope.Node)
            {
                if(node is BlockNode blockNode)
                {
                    m_DrawMasterPreview = true;
                    return;
                }

                MarkNodeDraw(node);
            }
        }

        void MarkNodeUpdate(AbstractMaterialNode node)
        {
            if (!m_Graph.ContainsNode(node))
            {
                return;
            }
            m_NodesToUpdate.Add(node);
        }

        void UnmarkNodeUpdate(AbstractMaterialNode node)
        {
            if (!m_Graph.ContainsNode(node))
            {
                return;
            }
            m_NodesToUpdate.Remove(node);
        }

        void MarkNodeDraw(AbstractMaterialNode node)
        {
            if (!m_Graph.ContainsNode(node))
            {
                return;
            }
            m_NodesToDraw.Add(node);
        }

        void UnmarkNodeDraw(AbstractMaterialNode node)
        {
            if (!m_Graph.ContainsNode(node))
            {
                return;
            }
            m_NodesToDraw.Remove(node);
        }

        Stack<AbstractMaterialNode> m_NodeWave = new Stack<AbstractMaterialNode>();
        List<IEdge> m_Edges = new List<IEdge>();
        List<MaterialSlot> m_Slots = new List<MaterialSlot>();
        List<AbstractMaterialNode> m_NextLevelNodes = new List<AbstractMaterialNode>();

        enum PropagationDirection
        {
            Upstream,
            Downstream
        }

        void PropagateNodeList<T>(T nodes, PropagationDirection dir) where T : ICollection<AbstractMaterialNode>
        {
            m_NodeWave.Clear();
            foreach (var node in nodes)
                m_NodeWave.Push(node);

            while (m_NodeWave.Count > 0)
            {
                var node = m_NodeWave.Pop();
                if (node == null)
                    continue;

                m_NextLevelNodes.Clear();
                GetConnectedNodes(node, dir, m_NextLevelNodes);
                foreach (var nextNode in m_NextLevelNodes)
                {
                    nodes.Add(nextNode);
                    m_NodeWave.Push(nextNode);
                }
            }
        }

        void GetConnectedNodes<T>(AbstractMaterialNode node, PropagationDirection dir, T connections) where T : ICollection<AbstractMaterialNode>
        {
            // Loop through all nodes that the node feeds into.
            m_Slots.Clear();
            if (dir == PropagationDirection.Downstream)
                node.GetOutputSlots(m_Slots);
            else
                node.GetInputSlots(m_Slots);
            foreach (var slot in m_Slots)
            {
                m_Edges.Clear();
                m_Graph.GetEdges(slot.slotReference, m_Edges);
                foreach (var edge in m_Edges)
                {
                    // We look at each node we feed into.
                    var connectedSlot = (dir == PropagationDirection.Downstream) ? edge.inputSlot : edge.outputSlot;
                    var connectedNode = connectedSlot.node;

                    // If the input node is already in the set, we don't need to process it.
                    if (connections.Contains(connectedNode))
                        continue;

                    // Add the node to the set, and to the wavefront such that we can process the nodes that it feeds into.
                    connections.Add(connectedNode);
                }
            }
        }

        public bool HandleGraphChanges()
        {
            foreach (var node in m_Graph.removedNodes)
            {
                if(node is BlockNode)
                {
                    node.UnregisterCallback(OnNodeModified);
                    UpdateMasterPreview(ModificationScope.Topological);
                    continue;
                }

                DestroyPreview(node.objectId);
                UnmarkNodeUpdate(node);
                UnmarkNodeDraw(node);
                m_RefreshTimedNodes = true;
            }

            m_Messenger.ClearNodesFromProvider(this, m_Graph.removedNodes);

            foreach (var node in m_Graph.addedNodes)
            {
                if(node is BlockNode)
                {
                    node.RegisterCallback(OnNodeModified);
                    UpdateMasterPreview(ModificationScope.Topological);
                    continue;
                }

                AddPreview(node);
                m_RefreshTimedNodes = true;
            }

            foreach (var edge in m_Graph.removedEdges)
            {
                var node = edge.inputSlot.node;
                if(node is BlockNode)
                {
                    UpdateMasterPreview(ModificationScope.Topological);
                    continue;
                }

                if (node != null)
                {
                    MarkNodeUpdate(node);
                    m_RefreshTimedNodes = true;
                }
            }
            foreach (var edge in m_Graph.addedEdges)
            {
                var node = edge.inputSlot.node;
                if(node is BlockNode)
                {
                    UpdateMasterPreview(ModificationScope.Topological);
                    continue;
                }

                if(node != null)
                {
                    MarkNodeUpdate(node);
                    m_RefreshTimedNodes = true;
                }
            }

            return m_NodesToUpdate.Count > 0;
        }

        List<PreviewProperty> m_PreviewProperties = new List<PreviewProperty>();
        List<AbstractMaterialNode> m_PropertyNodes = new List<AbstractMaterialNode>();

        void CollectShaderProperties(AbstractMaterialNode node, PreviewRenderData renderData)
        {
            m_PreviewProperties.Clear();
            m_PropertyNodes.Clear();

            if(node == null)
            {
                foreach(var block in m_Blocks)
                {
                    m_PropertyNodes.Add(block);
                }
            }
            else
            {
                m_PropertyNodes.Add(node);
            }

            PropagateNodeList(m_PropertyNodes, PropagationDirection.Upstream);

            foreach (var propNode in m_PropertyNodes)
            {
                propNode.CollectPreviewMaterialProperties(m_PreviewProperties);
            }

            foreach (var prop in m_Graph.properties)
                m_PreviewProperties.Add(prop.GetPreviewMaterialProperty());

            foreach (var previewProperty in m_PreviewProperties)
                renderData.shaderData.mat.SetPreviewProperty(previewProperty);
        }

        List<PreviewRenderData> m_RenderList2D = new List<PreviewRenderData>();
        List<PreviewRenderData> m_RenderList3D = new List<PreviewRenderData>();

        public void RenderPreviews()
        {
            UpdateShaders();
            UpdateTimedNodeList();

            PropagateNodeList(m_NodesToDraw, PropagationDirection.Downstream);
            m_NodesToDraw.UnionWith(m_TimedNodes);

            var time = Time.realtimeSinceStartup;
            var timeParameters = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);

            foreach (var node in m_NodesToDraw)
            {
                if(node is BlockNode)
                {
                    m_DrawMasterPreview = true;
                    continue;
                }

                if(!node.hasPreview || !node.previewExpanded)
                    continue;

                PreviewRenderData renderData = m_RenderDatas[node.objectId];
                CollectShaderProperties(node, renderData);
                renderData.shaderData.mat.SetVector("_TimeParameters", timeParameters);

                if (renderData.shaderData.shader == null)
                {
                    renderData.texture = null;
                    renderData.NotifyPreviewChanged();
                    continue;
                }
                if (renderData.shaderData.hasError)
                {
                    renderData.texture = m_ErrorTexture;
                    renderData.NotifyPreviewChanged();
                    continue;
                }

                if (renderData.previewMode == PreviewMode.Preview2D)
                    m_RenderList2D.Add(renderData);
                else
                    m_RenderList3D.Add(renderData);
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

            foreach (var renderData in m_RenderList2D)
                RenderPreview(renderData, m_SceneResources.quad, Matrix4x4.identity);

            // Render 3D previews
            m_SceneResources.camera.transform.position = -Vector3.forward * 5;
            m_SceneResources.camera.transform.rotation = Quaternion.identity;
            m_SceneResources.camera.orthographic = false;

            foreach (var renderData in m_RenderList3D)
                RenderPreview(renderData, m_SceneResources.sphere, Matrix4x4.identity);

            var renderMasterPreview = masterRenderData != null && m_DrawMasterPreview;
            if (renderMasterPreview)
            {
                masterRenderData.shaderData.mat.SetVector("_TimeParameters", timeParameters);

                m_DrawMasterPreview = false;
                CollectShaderProperties(masterRenderData.shaderData.node, masterRenderData);

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

                RenderPreview(masterRenderData, mesh, previewTransform);
                masterRenderData.NotifyPreviewChanged();
            }

            m_SceneResources.light0.enabled = false;
            m_SceneResources.light1.enabled = false;

            foreach (var renderData in m_RenderList2D)
                renderData.NotifyPreviewChanged();
            foreach (var renderData in m_RenderList3D)
                renderData.NotifyPreviewChanged();

            m_RenderList2D.Clear();
            m_RenderList3D.Clear();
            m_NodesToDraw.Clear();
        }

        public void ForceShaderUpdate()
        {
            foreach (var data in m_RenderDatas.Values)
            {
                MarkNodeUpdate(data.shaderData.node);
            }

            m_NodesToUpdate.Add(null);
        }

        void UpdateShaders()
        {
            void CompilingProcess(PreviewRenderData renderData)
            {
                if (renderData.shaderData.isCompiling)
                {
                    var isCompiled = true;
                    for (var i = 0; i < renderData.shaderData.mat.passCount; i++)
                    {
                        if (!ShaderUtil.IsPassCompiled(renderData.shaderData.mat, i))
                        {
                            isCompiled = false;
                            break;
                        }
                    }

                    if (!isCompiled)
                    {
                        return;
                    }

                    // Force the material to re-generate all it's shader properties.
                    renderData.shaderData.mat.shader = renderData.shaderData.shader;

                    renderData.shaderData.isCompiling = false;
                    CheckForErrors(renderData.shaderData);

                    if(renderData == m_MasterRenderData)
                    {
                        m_DrawMasterPreview = true;

                        // Process preview materials
                        foreach(var target in m_Graph.activeTargets)
                        {
                            if(target.IsActive())
                            {
                                target.ProcessPreviewMaterial(renderData.shaderData.mat);
                            }
                        }
                    }
                    else
                    {
                        MarkNodeDraw(renderData.shaderData.node);
                    }
                }
            }

            // Check for shaders that finished compiling and set them to redraw
            foreach (var renderData in m_RenderDatas.Values)
            {
                CompilingProcess(renderData);
            }

            // MasterRenderData is not added to m_RenderDatas
            CompilingProcess(masterRenderData);

            if (!m_UpdateMasterPreview && m_NodesToUpdate.Count == 0)
                return;

            PropagateNodeList(m_NodesToUpdate, PropagationDirection.Downstream);
            // Reset error states for the UI, the shader, and all render data for nodes we're updating
            m_Messenger.ClearNodesFromProvider(this, m_NodesToUpdate);
            var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = true;

            foreach (var node in m_NodesToUpdate)
            {
                if(node is BlockNode)
                {
                    m_UpdateMasterPreview = true;
                    continue;
                }

                if (!node.hasPreview && !(node is SubGraphOutputNode))
                    continue;

                if (!m_RenderDatas.TryGetValue(node.objectId, out var renderData))
                {
                    continue;
                }
                ShaderUtil.ClearCachedData(renderData.shaderData.shader);

                // Get shader code and compile
                var generator = new Generator(node.owner, node, GenerationMode.Preview, $"hidden/preview/{node.GetVariableNameForNode()}");
                BeginCompile(renderData, generator.generatedShader);

                // Calculate the PreviewMode from upstream nodes
                // If any upstream node is 3D that trickles downstream
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

            if(m_UpdateMasterPreview)
            {
                m_UpdateMasterPreview = false;
                UpdateMasterNodeShader();
            }

            ShaderUtil.allowAsyncCompilation = wasAsyncAllowed;
            m_NodesToUpdate.Clear();
        }

        void BeginCompile(PreviewRenderData renderData, string shaderStr)
        {
            var shaderData = renderData.shaderData;
            ShaderUtil.ClearCachedData(shaderData.shader);
            ShaderUtil.UpdateShaderAsset(shaderData.shader, shaderStr, false);
            for (var i = 0; i < shaderData.mat.passCount; i++)
            {
                ShaderUtil.CompilePass(shaderData.mat, i);
            }
            shaderData.isCompiling = true;
            renderData.NotifyPreviewChanged();
        }

        void UpdateTimedNodeList()
        {
            if (!m_RefreshTimedNodes)
                return;

            m_TimedNodes.Clear();

            foreach (var timeNode in m_Graph.GetNodes<AbstractMaterialNode>().Where(node => node.RequiresTime()))
            {
                m_TimedNodes.Add(timeNode);
            }

            PropagateNodeList(m_TimedNodes, PropagationDirection.Downstream);
            m_RefreshTimedNodes = false;
        }

        void RenderPreview(PreviewRenderData renderData, Mesh mesh, Matrix4x4 transform)
        {
            // var node = renderData.shaderData.node;
            // Assert.IsTrue((node != null && node.hasPreview && node.previewExpanded) || node == masterRenderData?.shaderData?.node);

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
            if(!m_Graph.isVFXTarget)
            {
                m_SceneResources.camera.targetTexture = temp;
                Graphics.DrawMesh(mesh, transform, renderData.shaderData.mat, 1, m_SceneResources.camera, 0, null, ShadowCastingMode.Off, false, null, false);
            }

            var previousUseSRP = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = renderData.shaderData.node == null;
            m_SceneResources.camera.Render();
            Unsupported.useScriptableRenderPipeline = previousUseSRP;

            Graphics.Blit(temp, renderData.renderTexture, m_SceneResources.blitNoAlphaMaterial);
            RenderTexture.ReleaseTemporary(temp);

            RenderTexture.active = previousRenderTexture;
            renderData.texture = renderData.renderTexture;
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
                m_Blocks.Clear();
                foreach(var block in generator.blocks)
                {
                    m_Blocks.Add(block);
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

            if (shaderData.shader == null)
            {
                shaderData.shader = ShaderUtil.CreateShaderAsset(shaderData.shaderString, false);
                shaderData.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                ShaderUtil.ClearCachedData(shaderData.shader);
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

        const string k_EmptyShader = @"
Shader ""hidden/preview""
{
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma    vertex    vert
            #pragma    fragment    frag

            #include    ""UnityCG.cginc""

            struct    appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
}";
    }

    delegate void OnPreviewChanged();

    class PreviewShaderData
    {
        public AbstractMaterialNode node { get; set; }
        public Shader shader { get; set; }
        public Material mat { get; set; }
        public string shaderString { get; set; }
        public bool isCompiling { get; set; }
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
