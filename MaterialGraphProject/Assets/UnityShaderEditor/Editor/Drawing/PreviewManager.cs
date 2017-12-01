using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PreviewManager : IDisposable
    {
        AbstractMaterialGraph m_Graph;
        Dictionary<Guid, PreviewRenderData> m_RenderDatas = new Dictionary<Guid, PreviewRenderData>();
        Dictionary<Guid, PreviewShaderData> m_ShaderDatas = new Dictionary<Guid, PreviewShaderData>();
        PreviewRenderData m_MasterRenderData;
        HashSet<Guid> m_DirtyPreviews = new HashSet<Guid>();
        HashSet<Guid> m_DirtyShaders = new HashSet<Guid>();
        HashSet<Guid> m_TimeDependentPreviews = new HashSet<Guid>();
        Material m_PreviewMaterial;
        MaterialPropertyBlock m_PreviewPropertyBlock;
        PreviewSceneResources m_SceneResources;
        Texture2D m_ErrorTexture;
        DateTime m_LastUpdate;
        const bool k_UberShaderEnabled = true;
        Shader m_UberShader;
        string m_UberShaderString;
        Dictionary<Guid, int> m_UberShaderIds;
        FloatShaderProperty m_OutputIdProperty;

        public PreviewRate previewRate { get; set; }

        public PreviewRenderData masterRenderData
        {
            get { return m_MasterRenderData; }
        }

        public PreviewManager(AbstractMaterialGraph graph)
        {
            m_Graph = graph;
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
            m_UberShaderIds = new Dictionary<Guid, int>();
            m_MasterRenderData = new PreviewRenderData
            {
                renderTexture = new RenderTexture(400, 400, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave }
            };

            foreach (var node in m_Graph.GetNodes<INode>())
                AddPreview(node);
        }

        public PreviewRenderData GetPreview(INode node)
        {
            return m_RenderDatas[node.guid];
        }

        void AddPreview(INode node)
        {
            PreviewShaderData shaderData;
            if (!m_ShaderDatas.TryGetValue(node.guid, out shaderData))
            {
                shaderData = new PreviewShaderData
                {
                    node = node
                };
                m_ShaderDatas[node.guid] = shaderData;
            }
            var previewData = new PreviewRenderData
            {
                shaderData = shaderData,
                renderTexture = new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave }
            };
            if (m_RenderDatas.ContainsKey(node.guid))
            {
                Debug.LogWarningFormat("A preview already exists for {0} {1}", node.name, node.guid);
                RemovePreview(node);
            }
            m_RenderDatas.Add(node.guid, previewData);
            m_DirtyShaders.Add(node.guid);
            node.onModified += OnNodeModified;
            if (node.RequiresTime())
                m_TimeDependentPreviews.Add(node.guid);

            var masterNode = node as IMasterNode;
            if (masterRenderData.shaderData == null && masterNode != null)
            {
                masterRenderData.shaderData = shaderData;
            }
        }

        void RemovePreview(INode node)
        {
            node.onModified -= OnNodeModified;
            m_RenderDatas.Remove(node.guid);
            m_TimeDependentPreviews.Remove(node.guid);
            m_DirtyPreviews.Remove(node.guid);
            m_DirtyShaders.Remove(node.guid);

            if (masterRenderData.shaderData != null && masterRenderData.shaderData.node == node)
                masterRenderData.shaderData = m_ShaderDatas.Values.FirstOrDefault(x => x.node is IMasterNode);
        }

        void OnNodeModified(INode node, ModificationScope scope)
        {
            if (scope >= ModificationScope.Graph)
                m_DirtyShaders.Add(node.guid);
            else if (scope == ModificationScope.Node)
                m_DirtyPreviews.Add(node.guid);

            if (node.RequiresTime())
                m_TimeDependentPreviews.Add(node.guid);
            else
                m_TimeDependentPreviews.Remove(node.guid);
        }

        Stack<Guid> m_Wavefront = new Stack<Guid>();

        void PropagateNodeSet(HashSet<Guid> nodeGuidSet, bool forward = true, IEnumerable<Guid> initialWavefront = null)
        {
            m_Wavefront.Clear();
            foreach (var guid in initialWavefront ?? nodeGuidSet)
                m_Wavefront.Push(guid);
            while (m_Wavefront.Count > 0)
            {
                var nodeGuid = m_Wavefront.Pop();
                var node = m_Graph.GetNodeFromGuid(nodeGuid);
                if (node == null)
                    continue;

                // Loop through all nodes that the node feeds into.
                foreach (var slot in forward ? node.GetOutputSlots<ISlot>() : node.GetInputSlots<ISlot>())
                {
                    foreach (var edge in m_Graph.GetEdges(slot.slotReference))
                    {
                        // We look at each node we feed into.
                        var connectedSlot = forward ? edge.inputSlot : edge.outputSlot;
                        var connectedNodeGuid = connectedSlot.nodeGuid;

                        // If the input node is already in the set of time-dependent nodes, we don't need to process it.
                        if (nodeGuidSet.Contains(connectedNodeGuid))
                            continue;

                        // Add the node to the set of time-dependent nodes, and to the wavefront such that we can process the nodes that it feeds into.
                        nodeGuidSet.Add(connectedNodeGuid);
                        m_Wavefront.Push(connectedNodeGuid);
                    }
                }
            }
        }

        HashSet<Guid> m_PropertyNodeGuids = new HashSet<Guid>();
        List<PreviewProperty> m_PreviewProperties = new List<PreviewProperty>();

        public void HandleGraphChanges()
        {
            foreach (var node in m_Graph.removedNodes)
                RemovePreview(node);

            foreach (var node in m_Graph.addedNodes)
                AddPreview(node);

            foreach (var edge in m_Graph.removedEdges)
                m_DirtyShaders.Add(edge.inputSlot.nodeGuid);

            foreach (var edge in m_Graph.addedEdges)
                m_DirtyShaders.Add(edge.inputSlot.nodeGuid);
        }

        List<PreviewRenderData> m_RenderList2D = new List<PreviewRenderData>();
        List<PreviewRenderData> m_RenderList3D = new List<PreviewRenderData>();
        HashSet<Guid> m_NodesWith3DPreview = new HashSet<Guid>();

        public void RenderPreviews()
        {
            if (previewRate == PreviewRate.Off)
                return;

            var updateTime = DateTime.Now;
            if (previewRate == PreviewRate.Throttled && (updateTime - m_LastUpdate) < TimeSpan.FromSeconds(1.0 / 10.0))
                return;

            m_LastUpdate = updateTime;

            if (m_DirtyShaders.Any())
            {
                m_NodesWith3DPreview.Clear();
                foreach (var node in m_Graph.GetNodes<AbstractMaterialNode>())
                {
                    if (node.previewMode == PreviewMode.Preview3D)
                        m_NodesWith3DPreview.Add(node.guid);
                }
                PropagateNodeSet(m_NodesWith3DPreview);
                PropagateNodeSet(m_DirtyShaders);

                var masterNodes = new List<INode>();
                var uberNodes = new List<INode>();
                foreach (var guid in m_DirtyShaders)
                {
                    var node = m_Graph.GetNodeFromGuid(guid);
                    if (node == null)
                        continue;
                    if (!k_UberShaderEnabled || node is IMasterNode)
                        masterNodes.Add(node);
                    else
                        uberNodes.Add(node);
                }
                var count = Math.Min(uberNodes.Count, 1) + masterNodes.Count;

                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var i = 0;
                    EditorUtility.DisplayProgressBar("Shader Graph", string.Format("Compiling preview shaders ({0}/{1})", i, count), 0f);
                    foreach (var node in masterNodes)
                    {
                        UpdateShader(node.guid);
                        i++;
                        EditorUtility.DisplayProgressBar("Shader Graph", string.Format("Compiling preview shaders ({0}/{1})", i, count), 0f);
                    }
                    if (uberNodes.Count > 0)
                    {
                        m_UberShaderIds.Clear();
                        m_UberShaderString = m_Graph.GetUberPreviewShader(m_UberShaderIds, out m_OutputIdProperty);
                        ShaderUtil.UpdateShaderAsset(m_UberShader, m_UberShaderString);
                        File.WriteAllText(Application.dataPath + "/../UberShader.shader", (m_UberShaderString ?? "null").Replace("UnityEngine.MaterialGraph", "Generated"));
                        var message = "RecreateUberShader: " + Environment.NewLine + m_UberShaderString;
                        if (MaterialGraphAsset.ShaderHasError(m_UberShader))
                        {
                            Debug.LogWarning(message);
                            ShaderUtil.ClearShaderErrors(m_UberShader);
                            ShaderUtil.UpdateShaderAsset(m_UberShader, k_EmptyShader);
                        }
                        else
                        {
                            Debug.Log(message);
                        }

                        foreach (var node in uberNodes)
                        {
                            PreviewShaderData shaderData;
                            if (!m_ShaderDatas.TryGetValue(node.guid, out shaderData))
                                continue;
                            shaderData.previewMode = m_NodesWith3DPreview.Contains(node.guid) ? PreviewMode.Preview3D : PreviewMode.Preview2D;
                            shaderData.shader = m_UberShader;
                        }
                        i++;
                        EditorUtility.DisplayProgressBar("Shader Graph", string.Format("Compiling preview shaders ({0}/{1})", i, count), 0f);
                    }
                    sw.Stop();
                    Debug.LogFormat("Compiled preview shaders in {0} seconds", sw.Elapsed.TotalSeconds);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                m_DirtyPreviews.UnionWith(m_DirtyShaders);
                m_DirtyShaders.Clear();
            }

            m_DirtyPreviews.UnionWith(m_TimeDependentPreviews);
            PropagateNodeSet(m_DirtyPreviews);

            // Find nodes we need properties from
            m_PropertyNodeGuids.Clear();
            foreach (var nodeGuid in m_DirtyPreviews)
                m_PropertyNodeGuids.Add(nodeGuid);
            PropagateNodeSet(m_PropertyNodeGuids, false);

            // Fill MaterialPropertyBlock
            m_PreviewPropertyBlock.Clear();
            var outputIdName = m_OutputIdProperty != null ? m_OutputIdProperty.referenceName : null;
            m_PreviewPropertyBlock.SetFloat(outputIdName, -1);
            foreach (var nodeGuid in m_PropertyNodeGuids)
            {
                var node = m_Graph.GetNodeFromGuid<AbstractMaterialNode>(nodeGuid);
                if (node == null)
                    continue;
                node.CollectPreviewMaterialProperties(m_PreviewProperties);
                foreach (var prop in m_Graph.properties)
                    m_PreviewProperties.Add(prop.GetPreviewMaterialProperty());

                foreach (var previewProperty in m_PreviewProperties)
                {
                    if (previewProperty.m_PropType == PropertyType.Texture && previewProperty.m_Texture != null)
                        m_PreviewPropertyBlock.SetTexture(previewProperty.m_Name, previewProperty.m_Texture);
                    else if (previewProperty.m_PropType == PropertyType.Cubemap && previewProperty.m_Cubemap != null)
                        m_PreviewPropertyBlock.SetTexture(previewProperty.m_Name, previewProperty.m_Cubemap);
                    else if (previewProperty.m_PropType == PropertyType.Color)
                        m_PreviewPropertyBlock.SetColor(previewProperty.m_Name, previewProperty.m_Color);
                    else if (previewProperty.m_PropType == PropertyType.Vector2)
                        m_PreviewPropertyBlock.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    else if (previewProperty.m_PropType == PropertyType.Vector3)
                        m_PreviewPropertyBlock.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    else if (previewProperty.m_PropType == PropertyType.Vector4)
                        m_PreviewPropertyBlock.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    else if (previewProperty.m_PropType == PropertyType.Float)
                        m_PreviewPropertyBlock.SetFloat(previewProperty.m_Name, previewProperty.m_Float);
                }
                m_PreviewProperties.Clear();
            }

            foreach (var nodeGuid in m_DirtyPreviews)
            {
                PreviewRenderData renderData;
                if (!m_RenderDatas.TryGetValue(nodeGuid, out renderData))
                    continue;
                if (renderData.shaderData.shader == null)
                {
                    renderData.texture = null;
                    continue;
                }
                if (MaterialGraphAsset.ShaderHasError(renderData.shaderData.shader))
                {
                    renderData.texture = m_ErrorTexture;
                    continue;
                }

                if (renderData.shaderData.previewMode == PreviewMode.Preview2D)
                    m_RenderList2D.Add(renderData);
                else
                    m_RenderList3D.Add(renderData);
            }

            if (masterRenderData.shaderData != null && m_DirtyPreviews.Contains(masterRenderData.shaderData.node.guid))
                m_RenderList3D.Add(masterRenderData);

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
            m_SceneResources.camera.orthographicSize = 1;
            m_SceneResources.camera.orthographic = true;
            foreach (var renderData in m_RenderList2D)
            {
                int outputId;
                if (m_UberShaderIds.TryGetValue(renderData.shaderData.node.guid, out outputId))
                    m_PreviewPropertyBlock.SetFloat(outputIdName, outputId);
                m_PreviewMaterial.shader = renderData.shaderData.shader;
                m_SceneResources.camera.targetTexture = renderData.renderTexture;
                var previousRenderTexure = RenderTexture.active;
                RenderTexture.active = renderData.renderTexture;
                GL.Clear(true, true, Color.black);
                Graphics.Blit(Texture2D.whiteTexture, renderData.renderTexture, m_SceneResources.checkerboardMaterial);
                Graphics.DrawMesh(m_SceneResources.quad, Matrix4x4.identity, m_PreviewMaterial, 1, m_SceneResources.camera, 0, m_PreviewPropertyBlock, ShadowCastingMode.Off, false, null, false);
                var previousUseSRP = Unsupported.useScriptableRenderPipeline;
                Unsupported.useScriptableRenderPipeline = false;
                m_SceneResources.camera.Render();
                Unsupported.useScriptableRenderPipeline = previousUseSRP;
                RenderTexture.active = previousRenderTexure;
                renderData.texture = renderData.renderTexture;
            }

            // Render 3D previews
            m_SceneResources.camera.transform.position = -Vector3.forward * 5;
            m_SceneResources.camera.transform.rotation = Quaternion.identity;
            m_SceneResources.camera.orthographic = false;
            foreach (var previewData in m_RenderList3D)
            {
                int outputId;
                if (m_UberShaderIds.TryGetValue(previewData.shaderData.node.guid, out outputId))
                    m_PreviewPropertyBlock.SetFloat(outputIdName, outputId);
                m_PreviewMaterial.shader = previewData.shaderData.shader;
                m_SceneResources.camera.targetTexture = previewData.renderTexture;
                var previousRenderTexure = RenderTexture.active;
                RenderTexture.active = previewData.renderTexture;
                GL.Clear(true, true, Color.black);
                Graphics.Blit(Texture2D.whiteTexture, previewData.renderTexture, m_SceneResources.checkerboardMaterial);
                var mesh = previewData.mesh ?? m_SceneResources.sphere;
                Graphics.DrawMesh(mesh, Matrix4x4.TRS(-mesh.bounds.center, Quaternion.identity, Vector3.one), m_PreviewMaterial, 1, m_SceneResources.camera, 0, m_PreviewPropertyBlock, ShadowCastingMode.Off, false, null, false);
                var previousUseSRP = Unsupported.useScriptableRenderPipeline;
                Unsupported.useScriptableRenderPipeline = previewData.shaderData.node is IMasterNode;
                m_SceneResources.camera.Render();
                Unsupported.useScriptableRenderPipeline = previousUseSRP;
                RenderTexture.active = previousRenderTexure;
                previewData.texture = previewData.renderTexture;
            }

            m_SceneResources.light0.enabled = false;
            m_SceneResources.light1.enabled = false;

            foreach (var previewRenderData in m_RenderList2D.Union(m_RenderList3D))
            {
                if (previewRenderData.onPreviewChanged != null)
                    previewRenderData.onPreviewChanged();
            }

            m_RenderList2D.Clear();
            m_RenderList3D.Clear();
            m_DirtyPreviews.Clear();
        }

        void UpdateShader(Guid nodeGuid)
        {
            var node = m_Graph.GetNodeFromGuid<AbstractMaterialNode>(nodeGuid);
            if (node == null)
                return;
            PreviewShaderData shaderData;
            if (!m_ShaderDatas.TryGetValue(nodeGuid, out shaderData))
                return;

            shaderData.previewMode = m_NodesWith3DPreview.Contains(nodeGuid) ? PreviewMode.Preview3D : PreviewMode.Preview2D;

            if (!(node is IMasterNode) && (!node.hasPreview || NodeUtils.FindEffectiveShaderStage(node, true) == ShaderStage.Vertex))
            {
                shaderData.shaderString = null;
            }
            else
            {
                PreviewMode mode;
                if (node is IMasterNode)
                {
                    List<PropertyCollector.TextureInfo> configuredTextures;
                    shaderData.shaderString = ((IMasterNode)node).GetShader(GenerationMode.Preview, node.name, out configuredTextures);
                }
                else
                    shaderData.shaderString = m_Graph.GetPreviewShader(node, out mode);
            }

            File.WriteAllText(Application.dataPath + "/../GeneratedShader.shader", (shaderData.shaderString ?? "null").Replace("UnityEngine.MaterialGraph", "Generated"));

            if (string.IsNullOrEmpty(shaderData.shaderString))
            {
                if (shaderData.shader != null)
                    Object.DestroyImmediate(shaderData.shader, true);
                shaderData.shader = null;
                return;
            }

            if (shaderData.shader != null && MaterialGraphAsset.ShaderHasError(shaderData.shader))
            {
                ShaderUtil.ClearShaderErrors(shaderData.shader);
                Object.DestroyImmediate(shaderData.shader, true);
                shaderData.shader = null;
            }

            if (shaderData.shader == null)
            {
                shaderData.shader = ShaderUtil.CreateShaderAsset(shaderData.shaderString);
                shaderData.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                ShaderUtil.ClearShaderErrors(shaderData.shader);
                ShaderUtil.UpdateShaderAsset(shaderData.shader, shaderData.shaderString);
            }

            // Debug output
            var message = "RecreateShader: " + node.GetVariableNameForNode() + Environment.NewLine + shaderData.shaderString;
            if (MaterialGraphAsset.ShaderHasError(shaderData.shader))
                Debug.LogWarning(message);
            else
                Debug.Log(message);
        }

        void DestroyPreview(Guid nodeGuid, PreviewRenderData previewRenderData)
        {
            if (m_RenderDatas.Remove(nodeGuid))
            {
                if (previewRenderData.shaderData.shader != null)
                    Object.DestroyImmediate(previewRenderData.shaderData.shader, true);
                if (previewRenderData.renderTexture != null)
                    Object.DestroyImmediate(previewRenderData.renderTexture, true);
                var node = m_Graph.GetNodeFromGuid(nodeGuid);
                if (node != null)
                    node.onModified -= OnNodeModified;
                m_DirtyPreviews.Remove(nodeGuid);
                m_DirtyShaders.Remove(nodeGuid);
                m_TimeDependentPreviews.Remove(nodeGuid);
                previewRenderData.shaderData.shader = null;
                previewRenderData.renderTexture = null;
                previewRenderData.texture = null;
                previewRenderData.onPreviewChanged = null;
            }
        }

        void ReleaseUnmanagedResources()
        {
            if (m_PreviewMaterial != null)
                Object.DestroyImmediate(m_PreviewMaterial, true);
            m_PreviewMaterial = null;
            if (m_SceneResources != null)
                m_SceneResources.Dispose();
            m_SceneResources = null;
            var previews = m_RenderDatas.ToList();
            foreach (var kvp in previews)
                DestroyPreview(kvp.Key, kvp.Value);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PreviewManager()
        {
            ReleaseUnmanagedResources();
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

    public delegate void OnPreviewChanged();

    public class PreviewShaderData
    {
        public INode node { get; set; }
        public Shader shader { get; set; }
        public string shaderString { get; set; }
        public PreviewMode previewMode { get; set; }
    }

    public class PreviewRenderData
    {
        public PreviewShaderData shaderData { get; set; }
        public Mesh mesh { get; set; }
        public RenderTexture renderTexture { get; set; }
        public Texture texture { get; set; }
        public OnPreviewChanged onPreviewChanged;
    }
}
