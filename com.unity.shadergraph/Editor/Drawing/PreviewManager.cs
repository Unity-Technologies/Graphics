using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    class PreviewManager : IDisposable
    {
        GraphData m_Graph;
        MessageManager m_Messenger;
        List<PreviewRenderData> m_RenderDatas = new List<PreviewRenderData>();
        PreviewRenderData m_MasterRenderData;
        List<Identifier> m_Identifiers = new List<Identifier>();
        
        Material m_PreviewMaterial;
        MaterialPropertyBlock m_PreviewPropertyBlock;
        PreviewSceneResources m_SceneResources;
        Texture2D m_ErrorTexture;
        Shader m_UberShader;
        string m_OutputIdName;
        bool m_NeedShaderUpdate;
        Vector2? m_NewMasterPreviewSize;

        public PreviewRenderData masterRenderData
        {
            get { return m_MasterRenderData; }
        }

        public PreviewManager(GraphData graph, MessageManager messenger)
        {
            m_Graph = graph;
            m_Messenger = messenger;
            m_PreviewMaterial = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.HideInHierarchy };
            m_PreviewMaterial.hideFlags = HideFlags.HideAndDontSave;
            m_PreviewPropertyBlock = new MaterialPropertyBlock();
            m_ErrorTexture = new Texture2D(2, 2);
            m_ErrorTexture.SetPixel(0, 0, Color.magenta);
            m_ErrorTexture.SetPixel(0, 1, Color.black);
            m_ErrorTexture.SetPixel(1, 0, Color.black);
            m_ErrorTexture.SetPixel(1, 1, Color.magenta);
            m_ErrorTexture.filterMode = FilterMode.Point;
            m_ErrorTexture.Apply();
            m_SceneResources = new PreviewSceneResources();
            m_UberShader = ShaderUtil.CreateShaderAsset(k_EmptyShader);
            m_UberShader.hideFlags = HideFlags.HideAndDontSave;
            m_MasterRenderData = new PreviewRenderData
            {
                renderTexture = new RenderTexture(400, 400, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave }
            };
            m_MasterRenderData.renderTexture.Create();

            foreach (var node in m_Graph.GetNodes<AbstractMaterialNode>())
                AddPreview(node);
        }

        public void ResizeMasterPreview(Vector2 newSize)
        {
            m_NewMasterPreviewSize = newSize;
        }

        public PreviewRenderData GetPreview(AbstractMaterialNode node)
        {
            return m_RenderDatas[node.tempId.index];
        }

        void AddPreview(AbstractMaterialNode node)
        {
            var shaderData = new PreviewShaderData
            {
                node = node,
                shader = m_UberShader
            };
            var renderData = new PreviewRenderData
            {
                shaderData = shaderData,
                renderTexture = new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave },
                
            };
            renderData.renderTexture.Create();
            Set(m_Identifiers, node.tempId, node.tempId);
            Set(m_RenderDatas, node.tempId, renderData);
            node.RegisterCallback(OnNodeModified);

            var isMaster = node is IMasterNode;
            if (masterRenderData.shaderData == null &&
                (isMaster || node is SubGraphOutputNode))
            {
                masterRenderData.shaderData = shaderData;
                // If it's actually the master, clear the shader since it will be assigned
                // later. SubGraphOutputNode still needs the UberShader.
                if (isMaster)
                {
                    masterRenderData.shaderData.shader = null;
                }
            }

            m_NeedShaderUpdate = true;
        }

        void OnNodeModified(AbstractMaterialNode node, ModificationScope scope)
        {
            m_NeedShaderUpdate |= (scope == ModificationScope.Topological || scope == ModificationScope.Graph);
        }

        Stack<AbstractMaterialNode> m_NodeWave = new Stack<AbstractMaterialNode>();
        List<IEdge> m_Edges = new List<IEdge>();
        List<MaterialSlot> m_Slots = new List<MaterialSlot>();

        void PropagateNodeList(ICollection<AbstractMaterialNode> nodes, bool forward)
        {
            m_NodeWave.Clear();
            foreach (var node in nodes)
                m_NodeWave.Push(node);

            while (m_NodeWave.Count > 0)
            {
                var node = m_NodeWave.Pop();
                if (node == null)
                    continue;

                // Loop through all nodes that the node feeds into.
                m_Slots.Clear();
                if (forward)
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
                        var connectedSlot = forward ? edge.inputSlot : edge.outputSlot;
                        var connectedNodeGuid = connectedSlot.nodeGuid;
                        var connectedNode = m_Graph.GetNodeFromGuid(connectedNodeGuid);

                        // If the input node is already in the set, we don't need to process it.
                        if (nodes.Contains(connectedNode))
                            continue;

                        // Add the node to the set, and to the wavefront such that we can process the nodes that it feeds into.
                        nodes.Add(connectedNode);
                        m_NodeWave.Push(connectedNode);
                    }
                }
            }
        }

        public void HandleGraphChanges()
        {
            foreach (var node in m_Graph.removedNodes)
            {
                DestroyPreview(node.tempId);
            }

            m_Messenger.ClearNodesFromProvider(this, m_Graph.removedNodes);

            foreach (var node in m_Graph.addedNodes)
            {
                AddPreview(node);
            }

            m_NeedShaderUpdate |= (m_Graph.removedEdges.Any() || m_Graph.removedNodes.Any() ||
                                  m_Graph.addedEdges.Any() || m_Graph.addedNodes.Any());
        }

        List<PreviewProperty> m_PreviewProperties = new List<PreviewProperty>();
        List<PreviewRenderData> m_RenderList2D = new List<PreviewRenderData>();
        List<PreviewRenderData> m_RenderList3D = new List<PreviewRenderData>();

        public void RenderPreviews()
        {
            UpdateShaders();

            m_PreviewPropertyBlock.Clear();
            m_PreviewPropertyBlock.SetFloat(m_OutputIdName, -1);
            foreach (var node in m_Graph.GetNodes<AbstractMaterialNode>())
            {
                var renderData = GetRenderData(node.tempId);
                renderData.previewMode = PreviewMode.Preview3D;
                if (node.previewMode == PreviewMode.Preview2D)
                {
                    renderData.previewMode = PreviewMode.Preview2D;
                }

                node.CollectPreviewMaterialProperties(m_PreviewProperties);
                foreach (var prop in m_Graph.properties)
                    m_PreviewProperties.Add(prop.GetPreviewMaterialProperty());

                foreach (var previewProperty in m_PreviewProperties)
                    m_PreviewPropertyBlock.SetPreviewProperty(previewProperty);
                m_PreviewProperties.Clear();
                
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

            var time = Time.realtimeSinceStartup;
            EditorUtility.SetCameraAnimateMaterialsTime(m_SceneResources.camera, time);

            m_SceneResources.light0.enabled = true;
            m_SceneResources.light0.intensity = 1.0f;
            m_SceneResources.light0.transform.rotation = Quaternion.Euler(50f, 50f, 0);
            m_SceneResources.light1.enabled = true;
            m_SceneResources.light1.intensity = 1.0f;
            m_SceneResources.camera.clearFlags = CameraClearFlags.Depth;

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

            var renderMasterPreview = masterRenderData.shaderData != null;
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
                var mesh = m_Graph.previewData.serializedMesh.mesh ? m_Graph.previewData.serializedMesh.mesh :  m_SceneResources.sphere;
                var previewTransform = Matrix4x4.Rotate(m_Graph.previewData.rotation);
                var scale = m_Graph.previewData.scale;
                previewTransform *= Matrix4x4.Scale(scale * Vector3.one * (Vector3.one).magnitude / mesh.bounds.size.magnitude);
                previewTransform *= Matrix4x4.Translate(-mesh.bounds.center);
                RenderPreview(masterRenderData, mesh, previewTransform);
            }

            m_SceneResources.light0.enabled = false;
            m_SceneResources.light1.enabled = false;

            foreach (var renderData in m_RenderList2D)
                renderData.NotifyPreviewChanged();
            foreach (var renderData in m_RenderList3D)
                renderData.NotifyPreviewChanged();
            if (renderMasterPreview)
                masterRenderData.NotifyPreviewChanged();

            m_RenderList2D.Clear();
            m_RenderList3D.Clear();
        }

        public void ForceShaderUpdate()
        {
            m_NeedShaderUpdate = true;
        }

        void UpdateShaders()
        {
            if (!m_NeedShaderUpdate)
                return;

            try
            {
                EditorUtility.DisplayProgressBar("Shader Graph", "Compiling preview shaders", 0f);

                foreach (var masterNode in m_Graph.GetNodes<AbstractMaterialNode>().Where(x => x is IMasterNode))
                {
                    UpdateMasterNodeShader(masterNode.tempId);
                }

                EditorUtility.DisplayProgressBar("Shader Graph", "Compiling preview shaders", 0.5f);

                // Reset error states for the UI, the shader, and all render data
                m_Messenger.ClearAllFromProvider(this);
                m_RenderDatas.ForEach(data =>
                {
                    if (data != null)
                    {
                        data.shaderData.hasError = false;
                    }
                });

                var errNodes = new HashSet<AbstractMaterialNode>();
                GenerationResults results;
                var uberShaderHasError = GenerateUberShader(errNodes, out results);
                
                if (uberShaderHasError)
                {
                    errNodes = ProcessUberErrors(results);
                    // Also collect any nodes that had validation errors because they cause the uber shader to fail without
                    // putting valid entries in the source map so ProcessUberErrors doesn't find them.
                    errNodes.UnionWith( m_Graph.GetNodes<AbstractMaterialNode>().Where(node => node.hasError) );
                    PropagateNodeList(errNodes, true);

                    // Try generating the shader again, excluding the nodes with errors (and descendants)
                    uberShaderHasError = GenerateUberShader(errNodes, out results);
                    if (uberShaderHasError)
                    {
                        Debug.LogWarning("Shader Graph compilation failed due to multiple errors. Resolve the visible errors to reveal more.");
                    }

                    foreach (var errNode in errNodes)
                    {
                        GetRenderData(errNode.tempId).shaderData.hasError = true;
                    }
                }

                var debugOutputPath = DefaultShaderIncludes.GetDebugOutputPath();
                if (debugOutputPath != null)
                {
                    File.WriteAllText(debugOutputPath + "/ColorShader.shader",
                        (results.shader ?? "null").Replace("UnityEngine.MaterialGraph", "Generated"));
                }

                m_NeedShaderUpdate = false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        bool GenerateUberShader(ICollection<AbstractMaterialNode> errNodes, out GenerationResults results)
        {
            ShaderUtil.ClearCachedData(m_UberShader);
            results = m_Graph.GetUberColorShader(errNodes);
            m_OutputIdName = results.outputIdProperty.referenceName;
            ShaderUtil.UpdateShaderAsset(m_UberShader, results.shader);
            return ShaderUtil.ShaderHasError(m_UberShader);
        }

        HashSet<AbstractMaterialNode> ProcessUberErrors(GenerationResults results)
        {
            var errNodes = new HashSet<AbstractMaterialNode>();
            var message = new StringBuilder();
            var messages = ShaderUtil.GetShaderMessages(m_UberShader);
            message.AppendFormat(@"Preview shader for graph has {0} error{1}:\n", messages.Length, messages.Length != 1 ? "s" : "");
            foreach (var error in messages)
            {
                var node = results.sourceMap.FindNode(error.line);
                
                message.AppendFormat("Shader compilation error in {3} at line {1} (on {2}):\n{0}\n",
                    error.message, error.line, error.platform,
                    node != null ? string.Format("node {0} ({1})", node.name, node.guid) : "graph");
                message.AppendLine(error.messageDetails);
                message.AppendLine();

                if (node != null)
                {
                    m_Messenger.AddOrAppendError(this, node.tempId, error);
                    errNodes.Add(node);
                }
            }
            Debug.LogWarning(message.ToString());
            return errNodes;
        }

        void RenderPreview(PreviewRenderData renderData, Mesh mesh, Matrix4x4 transform)
        {
            if (renderData.shaderData.shader == null || renderData.shaderData.hasError)
            {
                renderData.texture = m_ErrorTexture;
                return;
            }
            m_PreviewPropertyBlock.SetFloat(m_OutputIdName, renderData.shaderData.node.tempId.index);
            if (m_PreviewMaterial.shader != renderData.shaderData.shader)
                m_PreviewMaterial.shader = renderData.shaderData.shader;
            var previousRenderTexure = RenderTexture.active;


            //Temp workaround for alpha previews...
            var temp = RenderTexture.GetTemporary(renderData.renderTexture.descriptor);
            RenderTexture.active = temp;
            GL.Clear(true, true, Color.black);
            Graphics.Blit(Texture2D.whiteTexture, temp, m_SceneResources.checkerboardMaterial);

            m_SceneResources.camera.targetTexture = temp;
            Graphics.DrawMesh(mesh, transform, m_PreviewMaterial, 1, m_SceneResources.camera, 0, m_PreviewPropertyBlock, ShadowCastingMode.Off, false, null, false);

            var previousUseSRP = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = renderData.shaderData.node is IMasterNode;
            m_SceneResources.camera.Render();
            Unsupported.useScriptableRenderPipeline = previousUseSRP;

            Graphics.Blit(temp, renderData.renderTexture, m_SceneResources.blitNoAlphaMaterial);
            RenderTexture.ReleaseTemporary(temp);

            RenderTexture.active = previousRenderTexure;
            renderData.texture = renderData.renderTexture;
        }

        void UpdateMasterNodeShader(Identifier nodeId)
        {
            var node = m_Graph.GetNodeFromTempId(nodeId);
            var masterNode = node as IMasterNode;
            var renderData = Get(m_RenderDatas, nodeId);
            var shaderData = renderData?.shaderData;

            if (masterNode == null || shaderData == null)
                return;

            List<PropertyCollector.TextureInfo> configuredTextures;
            shaderData.shaderString = masterNode.GetShader(GenerationMode.Preview, node.name, out configuredTextures);

            var debugOutputPath = DefaultShaderIncludes.GetDebugOutputPath();
            if (!string.IsNullOrEmpty(debugOutputPath))
            {
                File.WriteAllText(debugOutputPath + "/GeneratedShader.shader",
                    (shaderData.shaderString ?? "null").Replace("UnityEngine.MaterialGraph", "Generated"));
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
                shaderData.shader = ShaderUtil.CreateShaderAsset(shaderData.shaderString);
                shaderData.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                ShaderUtil.ClearCachedData(shaderData.shader);
                ShaderUtil.UpdateShaderAsset(shaderData.shader, shaderData.shaderString);
            }

            // Debug output
            shaderData.hasError = ShaderUtil.ShaderHasError(shaderData.shader);
            if(shaderData.hasError)
            {
                var messages = ShaderUtil.GetShaderMessages(shaderData.shader);
                foreach (var message in messages)
                {
                    Debug.LogFormat("Compilation error in {3} at line {1} (on {2}):\n{0}", message.message,
                        message.line, message.platform, "graph");
                }

                if (!string.IsNullOrEmpty(debugOutputPath))
                {
                    var AMNode = masterNode as AbstractMaterialNode;
                    var message = "RecreateShader: " + AMNode?.GetVariableNameForNode() + Environment.NewLine + shaderData.shaderString;
                    Debug.LogWarning(message);
                }
                ShaderUtil.ClearShaderMessages(shaderData.shader);
                Object.DestroyImmediate(shaderData.shader, true);
                shaderData.shader = null;
            }
        }

        void DestroyRenderData(PreviewRenderData renderData)
        {
            if (renderData.shaderData != null
                && renderData.shaderData.shader != null
                && renderData.shaderData.shader != m_UberShader)
                Object.DestroyImmediate(renderData.shaderData.shader, true);
            if (renderData.renderTexture != null)
                Object.DestroyImmediate(renderData.renderTexture, true);

            if (renderData.shaderData != null && renderData.shaderData.node != null)
                renderData.shaderData.node.UnregisterCallback(OnNodeModified);
        }

        void DestroyPreview(Identifier nodeId)
        {
            var renderData = Get(m_RenderDatas, nodeId);
            if (renderData != null)
            {
                // Check if we're destroying the shader data used by the master preview
                if (masterRenderData != null && masterRenderData.shaderData != null && masterRenderData.shaderData == renderData.shaderData)
                    masterRenderData.shaderData = m_RenderDatas.Where(x => x != null && x.shaderData.node is IMasterNode && x != renderData).Select(x => x.shaderData).FirstOrDefault();

                DestroyRenderData(renderData);

                Set(m_RenderDatas, nodeId, null);
                Set(m_Identifiers, nodeId, default(Identifier));
            }
        }

        void ReleaseUnmanagedResources()
        {
            if (m_UberShader != null)
            {
                Object.DestroyImmediate(m_UberShader, true);
                m_UberShader = null;
            }
            if (m_ErrorTexture != null)
            {
                Object.DestroyImmediate(m_ErrorTexture);
                m_ErrorTexture = null;
            }
            if (m_PreviewMaterial != null)
            {
                Object.DestroyImmediate(m_PreviewMaterial, true);
                m_PreviewMaterial = null;
            }
            if (m_SceneResources != null)
            {
                m_SceneResources.Dispose();
                m_SceneResources = null;
            }
            if (m_MasterRenderData != null)
                DestroyRenderData(m_MasterRenderData);
            foreach (var renderData in m_RenderDatas.ToList().Where(x => x != null))
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

        T Get<T>(List<T> list, Identifier id)
        {
            var existingId = Get(m_Identifiers, id.index);
            if (existingId.valid && existingId.version != id.version)
                throw new Exception("Identifier version mismatch");
            return Get(list, id.index);
        }

        static T Get<T>(List<T> list, int index)
        {
            return index < list.Count ? list[index] : default(T);
        }

        void Set<T>(List<T> list, Identifier id, T value)
        {
            var existingId = Get(m_Identifiers, id.index);
            if (existingId.valid && existingId.version != id.version)
                throw new Exception("Identifier version mismatch");
            Set(list, id.index, value);
        }

        static void Set<T>(List<T> list, int index, T value)
        {
            // Make sure the list is large enough for the index
            for (var i = list.Count; i <= index; i++)
                list.Add(default(T));
            list[index] = value;
        }

        PreviewRenderData GetRenderData(Identifier id)
        {
            var value = Get(m_RenderDatas, id);
            if (value != null && value.shaderData.node.tempId.version != id.version)
                throw new Exception("Trying to access render data of a previous version of a node");
            return value;
        }
    }

    delegate void OnPreviewChanged();

    class PreviewShaderData
    {
        public AbstractMaterialNode node { get; set; }
        public Shader shader { get; set; }
        public string shaderString { get; set; }
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
