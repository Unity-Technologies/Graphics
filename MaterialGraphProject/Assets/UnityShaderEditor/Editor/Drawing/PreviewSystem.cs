using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class PreviewSystem : IDisposable
    {
        AbstractMaterialGraph m_Graph;
        Dictionary<Guid, PreviewData> m_Previews = new Dictionary<Guid, PreviewData>();
        HashSet<Guid> m_DirtyPreviews = new HashSet<Guid>();
        HashSet<Guid> m_DirtyShaders = new HashSet<Guid>();
        HashSet<Guid> m_TimeDependentPreviews = new HashSet<Guid>();
        Material m_PreviewMaterial;
        MaterialPropertyBlock m_PreviewPropertyBlock;
        MaterialGraphPreviewGenerator m_PreviewGenerator = new MaterialGraphPreviewGenerator();
        Texture2D m_ErrorTexture;
        DateTime m_LastUpdate;

        public PreviewRate previewRate { get; set; }

        public PreviewSystem(AbstractMaterialGraph graph)
        {
            m_Graph = graph;
            m_PreviewMaterial = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.HideInHierarchy };
            m_PreviewMaterial.hideFlags = HideFlags.HideInHierarchy;
            m_PreviewPropertyBlock = new MaterialPropertyBlock();
            m_ErrorTexture = new Texture2D(2, 2);
            m_ErrorTexture.SetPixel(0, 0, Color.magenta);
            m_ErrorTexture.SetPixel(0, 1, Color.black);
            m_ErrorTexture.SetPixel(1, 0, Color.black);
            m_ErrorTexture.SetPixel(1, 1, Color.magenta);
            m_ErrorTexture.Apply();

            foreach (var node in m_Graph.GetNodes<INode>())
                AddPreview(node);
            m_Graph.onChange += OnGraphChange;
        }

        public PreviewData GetPreview(INode node)
        {
            return m_Previews[node.guid];
        }

        void OnGraphChange(GraphChange change)
        {
            change.Match(
                nodeAdded: c => AddPreview(c.node),
                nodeRemoved: c => RemovePreview(c.node),
                edgeAdded: c => m_DirtyShaders.Add(c.edge.inputSlot.nodeGuid),
                edgeRemoved: c => m_DirtyShaders.Add(c.edge.inputSlot.nodeGuid));
        }

        void AddPreview(INode node)
        {
            var previewData = new PreviewData
            {
                renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave }
            };
            m_Previews.Add(node.guid, previewData);
            m_DirtyShaders.Add(node.guid);
            node.onModified += OnNodeModified;
            if (node.RequiresTime())
                m_TimeDependentPreviews.Add(node.guid);
        }

        void RemovePreview(INode node)
        {
            node.onModified -= OnNodeModified;
            m_Previews.Remove(node.guid);
            m_TimeDependentPreviews.Remove(node.guid);
            m_DirtyPreviews.Remove(node.guid);
            m_DirtyShaders.Remove(node.guid);
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

        public void Update()
        {
            if (previewRate == PreviewRate.Off)
                return;

            var updateTime = DateTime.Now;
            if (previewRate == PreviewRate.Throttled && (updateTime - m_LastUpdate) < TimeSpan.FromSeconds(1.0 / 10.0))
                return;

            m_LastUpdate = updateTime;

            PropagateNodeSet(m_DirtyShaders);
            foreach (var nodeGuid in m_DirtyShaders)
            {
                UpdateShader(nodeGuid);
            }
            m_DirtyPreviews.UnionWith(m_DirtyShaders);
            m_DirtyShaders.Clear();

            m_DirtyPreviews.UnionWith(m_TimeDependentPreviews);
            PropagateNodeSet(m_DirtyPreviews);

            // Find nodes we need properties from
            m_PropertyNodeGuids.Clear();
            foreach (var nodeGuid in m_DirtyPreviews)
                m_PropertyNodeGuids.Add(nodeGuid);
            PropagateNodeSet(m_PropertyNodeGuids, false);

            // Fill MaterialPropertyBlock
            m_PreviewPropertyBlock.Clear();
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

            var time = Time.realtimeSinceStartup;
            foreach (var nodeGuid in m_DirtyPreviews)
            {
                PreviewData previewData;
                if (!m_Previews.TryGetValue(nodeGuid, out previewData))
                    continue;

                if (previewData.shader == null)
                {
                    previewData.texture = null;
                    continue;
                }
                if (MaterialGraphAsset.ShaderHasError(previewData.shader))
                {
                    previewData.texture = m_ErrorTexture;
                    continue;
                }
                var node = m_Graph.GetNodeFromGuid(nodeGuid);
                m_PreviewMaterial.shader = previewData.shader;
                m_PreviewGenerator.DoRenderPreview(previewData.renderTexture, m_PreviewMaterial, previewData.previewMode, node is IMasterNode, time, m_PreviewPropertyBlock);
                previewData.texture = previewData.renderTexture;
            }

            foreach (var nodeGuid in m_DirtyPreviews)
            {
                PreviewData previewData;
                if (!m_Previews.TryGetValue(nodeGuid, out previewData))
                    continue;

                if (previewData.onPreviewChanged != null)
                    previewData.onPreviewChanged();
            }

            m_DirtyPreviews.Clear();
        }

        void UpdateShader(Guid nodeGuid)
        {
            var node = m_Graph.GetNodeFromGuid<AbstractMaterialNode>(nodeGuid);
            if (node == null)
                return;
            PreviewData previewData;
            if (!m_Previews.TryGetValue(nodeGuid, out previewData))
                return;

            if (node is IMasterNode)
            {
                var materialGraph = (AbstractMaterialGraph) node.owner;

                List<PropertyCollector.TextureInfo> defaultTextures;
                PreviewMode pmode;
                previewData.shaderString = materialGraph.GetShader(node, GenerationMode.Preview, node.guid + "_preview",  out defaultTextures, out pmode);
                previewData.previewMode = node.previewMode;
            }
            else if (!node.hasPreview || NodeUtils.FindEffectiveShaderStage(node, true) == ShaderStage.Vertex)
            {
                previewData.shaderString = null;
            }
            else
            {
                List<PropertyCollector.TextureInfo> defaultTextures;
                PreviewMode mode;
                previewData.shaderString = m_Graph.GetPreviewShader(node, out mode);
                previewData.previewMode = mode;
            }

            // Debug output
            Debug.Log("RecreateShader: " + node.GetVariableNameForNode() + Environment.NewLine + previewData.shaderString);
            File.WriteAllText(Application.dataPath + "/../GeneratedShader.shader", (previewData.shaderString ?? "null").Replace("UnityEngine.MaterialGraph", "Generated"));

            if (string.IsNullOrEmpty(previewData.shaderString))
            {
                if (previewData.shader != null)
                    Object.DestroyImmediate(previewData.shader, true);
                previewData.shader = null;
                return;
            }

            if (previewData.shader != null && MaterialGraphAsset.ShaderHasError(previewData.shader))
            {
                Object.DestroyImmediate(previewData.shader, true);
                previewData.shader = null;
            }

            if (previewData.shader == null)
            {
                previewData.shader = ShaderUtil.CreateShaderAsset(previewData.shaderString);
                previewData.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                ShaderUtil.UpdateShaderAsset(previewData.shader, previewData.shaderString);
            }

            if (MaterialGraphAsset.ShaderHasError(previewData.shader))
                Debug.LogWarningFormat("ShaderHasError: {0}\n{1}", node.GetVariableNameForNode(), previewData.shaderString);
        }

        void DestroyPreview(Guid nodeGuid, PreviewData previewData)
        {
            if (m_Previews.Remove(nodeGuid))
            {
                if (previewData.shader != null)
                    Object.DestroyImmediate(previewData.shader, true);
                if (previewData.renderTexture != null)
                    Object.DestroyImmediate(previewData.renderTexture, true);
                var node = m_Graph.GetNodeFromGuid(nodeGuid);
                if (node != null)
                    node.onModified -= OnNodeModified;
                m_DirtyPreviews.Remove(nodeGuid);
                m_DirtyShaders.Remove(nodeGuid);
                m_TimeDependentPreviews.Remove(nodeGuid);
                previewData.shader = null;
                previewData.renderTexture = null;
                previewData.texture = null;
                previewData.onPreviewChanged = null;
            }
        }

        void ReleaseUnmanagedResources()
        {
            if (m_PreviewMaterial != null)
                Object.DestroyImmediate(m_PreviewMaterial, true);
            m_PreviewMaterial = null;
            if (m_PreviewGenerator != null)
                m_PreviewGenerator.Dispose();
            m_PreviewGenerator = null;
            var previews = m_Previews.ToList();
            foreach (var kvp in previews)
                DestroyPreview(kvp.Key, kvp.Value);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PreviewSystem()
        {
            ReleaseUnmanagedResources();
        }
    }

    public delegate void OnPreviewChanged();

    public class PreviewData
    {
        public Shader shader { get; set; }
        public string shaderString { get; set; }
        public PreviewMode previewMode { get; set; }
        public RenderTexture renderTexture { get; set; }
        public Texture texture { get; set; }
        public OnPreviewChanged onPreviewChanged;
    }
}
