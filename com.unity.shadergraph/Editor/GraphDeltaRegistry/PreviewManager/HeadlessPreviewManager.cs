using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.Utils;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    /// <summary>
    /// This class encapsulates all functionality related to generating preview render and shader data from a node graph
    /// </summary>
    public class HeadlessPreviewManager
    {
        // Could we replace this by a bool?
        // We would need to handle concretization of preview mode on GTF side of things
        // prior to asking preview manager to issue updates for all downstream affected nodes
        public enum PreviewRenderMode
        {
            Inherit, // this usually means: 2D, unless a connected input node is 3D, in which case it is 3D
            Preview2D,
            Preview3D
        }

        // TODO: Where does this live?
        // Needs to be in a third place that is accessible to both the GraphUI assembly and the GraphDelta assembly so we aren't directly coupled
        public enum DefaultTextureType
        {
            White,
            Black,
            NormalMap
        }

        public enum PreviewOutputState
        {
            Updating,
            Complete,
            ShaderError
        }

        class PreviewData
        {
            public string nodeName;
            public Shader shader;
            public Material material;
            public string shaderString;
            public Texture texture;
            public bool isRenderOutOfDate;
            public bool isShaderOutOfDate;

            // Used to control whether the preview should render in 2D/3D/Inherit from upstream nodes
            public PreviewRenderMode currentRenderMode;

            // Used to control whether the preview should update or not
            public bool isPreviewEnabled;

            // Do we need to cache the render texture?
            public RenderTexture renderTexture;

            // Do we need to track how many passes are actively compiled per shader? What is it used for beyond debug log stuff?
            public int passesCompiling;

            // Same for this below...
            public bool hasShaderError;
        }

        // TODO: This should live in the GTF view model
        class MasterPreviewUserData
        {
            public SerializableMesh serializedMesh = new SerializableMesh();
            public bool preventRotation;

            [NonSerialized]
            public Quaternion rotation = Quaternion.identity;

            [NonSerialized]
            public float scale = 1f;
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

        public HeadlessPreviewManager()
        {
            m_ErrorTexture = GenerateFourSquare(Color.magenta, Color.black);
            m_CompilingTexture = GenerateFourSquare(Color.blue, Color.blue);
            m_SceneResources = new PreviewSceneResources();
            AddMasterPreviewData();
        }

        /// <summary>
        /// Map from node names to associated preview data object
        /// </summary>
        Dictionary<string, PreviewData> m_CachedPreviewData = new();

        Dictionary<string, ShaderMessage[]> m_ShaderMessagesMap = new();

        /// <summary>
        /// Handle to the graph object we are currently generating preview data for
        /// </summary>
        IGraphHandler m_GraphHandle;

        Registry.Registry m_RegistryInstance;

        MaterialPropertyBlock m_PreviewMaterialPropertyBlock = new();

        Texture2D m_ErrorTexture;
        Texture2D m_CompilingTexture;

        PreviewSceneResources m_SceneResources;

        PreviewData m_MasterPreviewData;

        MasterPreviewUserData m_MasterPreviewUserData;

        Vector2 m_MasterPreviewSize = new(400, 400);

        const string k_MasterPreviewName = "MasterPreview";

        bool masterPreviewWasResized = false;

        // Some changes made to how rendering handles uncompiled shader variants, in future will be simplified so we can request a render that also handles
        // compiling any uncompiled shader variants/passes etc and there will be an async fallback provided, and a callback for when the async compile and render is done

        /// <summary>
        /// Used to set which graph this preview manager gets its node data from.
        /// </summary>
        public void SetActiveGraph(IGraphHandler activeGraphReference)
        {
            m_GraphHandle = activeGraphReference;
        }

        /// <summary>
        /// Used to set which registry instance this preview manager gets its type data from.
        /// </summary>
        public void SetActiveRegistry(Registry.Registry registryInstance)
        {
            m_RegistryInstance = registryInstance;
        }

        /// <summary>
        /// Used to change the propertyValue of global properties like Time, Blackboard Properties, Render Pipeline Intrinsics etc.
        /// </summary>
        /// <returns> List of names describing all nodes that were affected by this change </returns>
        /// <remarks> Dirties the preview render state of all nodes downstream of any references to the changed property </remarks>
        public List<string> SetGlobalProperty(string propertyName, object newPropertyValue)
        {
            var hlslName = Mock_GetHLSLNameForGlobalProperty(propertyName);
            SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock, propertyName, newPropertyValue);

            // We're assuming that global properties/referrables will also be mapped to nodes and can be retrieved as such
            var globalPropertyNode = m_GraphHandle.GetNodeReader(propertyName);

            var impactedNodes = new List<string>();
            foreach (var downStreamNode in m_GraphHandle.GetDownstreamNodes(globalPropertyNode))
            {
                impactedNodes.Add(downStreamNode.GetName());
            }

            return impactedNodes;
        }

        /// <summary>
        /// Used to change the propertyValue of local properties such as node inputs/port propertyValues.
        /// </summary>
        /// <returns> List of names describing all nodes that were affected by this change </returns>
        /// <remarks> Dirties the preview render state of all nodes downstream of any references to the changed property </remarks>
        public List<string> SetLocalProperty(string nodeName, string propertyName, object newPropertyValue)
        {
            var portReader = Mock_GetPortReaderForProperty(nodeName, propertyName);
            var hlslName = Mock_GetHLSLNameForLocalProperty(nodeName, propertyName);

            SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock, hlslName, newPropertyValue, portReader);

            var sourceNode = m_GraphHandle.GetNodeReader(nodeName);
            var impactedNodes = new List<string>();
            foreach (var downStreamNode in m_GraphHandle.GetDownstreamNodes(sourceNode))
            {
                impactedNodes.Add(downStreamNode.GetName());
            }

            return impactedNodes;
        }

        /// <summary>
        /// Used to notify when a node has been deleted and for when connections to a node change.
        /// </summary>
        /// <returns> List of names describing all nodes that were affected by this change </returns>
        /// <remarks> Dirties the preview compile & render state of all nodes downstream of the changed node </remarks>
        public List<string> NotifyNodeFlowChanged(string nodeName)
        {
            var impactedNodes = new List<string>();

            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                var sourceNode = m_GraphHandle.GetNodeReader(nodeName);
                if (sourceNode == null)
                {
                    // Node was deleted, get rid of the preview data associated with it
                    m_CachedPreviewData.Remove(nodeName);

                    // TODO: How to get downstream nodes when the source node has been deleted? probably wont have the nodeReader hanging around after the nodes deleted right?
                }
                else
                {
                    var previewData = m_CachedPreviewData[nodeName];
                    previewData.isRenderOutOfDate = true;
                    previewData.isShaderOutOfDate = true;

                    foreach (var downStreamNode in m_GraphHandle.GetDownstreamNodes(sourceNode))
                    {
                        if (m_CachedPreviewData.TryGetValue(downStreamNode.GetName(), out var downStreamNodeData))
                        {
                            downStreamNodeData.isShaderOutOfDate = true;
                            downStreamNodeData.isRenderOutOfDate = true;
                        }

                        impactedNodes.Add(downStreamNode.GetName());
                    }
                }
            }
            return impactedNodes;
        }

        /// <summary>
        /// Used to get current preview render output of a node, and optionally at a specific preview mode provided as an argument.
        /// nodeRenderOutput is a Texture that contains the current preview output of a node, if its shaders have been compiled and ready to return
        /// </summary>
        /// <returns> Enum value that defines whether the node's render output is ready, currently being updated, or if a shader error was encountered </returns>
        public PreviewOutputState RequestNodePreviewImage(string nodeName, out Texture nodeRenderOutput, out ShaderMessage[] errorMessages, PreviewRenderMode newPreviewMode = PreviewRenderMode.Preview2D)
        {
            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                var previewData = m_CachedPreviewData[nodeName];
                previewData.currentRenderMode = newPreviewMode;

                // Still rendering the preview output
                if (previewData.isRenderOutOfDate)
                {
                    nodeRenderOutput = Texture2D.blackTexture;
                    errorMessages = null;
                    return PreviewOutputState.Updating;
                }

                // Still compiling the preview shader
                else if (previewData.isShaderOutOfDate)
                {
                    nodeRenderOutput = m_CompilingTexture;
                    errorMessages = null;
                    return PreviewOutputState.Updating;
                }

                // Ran into error compiling the preview shader
                else if (previewData.hasShaderError)
                {
                    nodeRenderOutput = m_ErrorTexture;
                    errorMessages = m_ShaderMessagesMap[previewData.nodeName];
                    return PreviewOutputState.ShaderError;
                }

                // Otherwise, the preview output has been rendered, assign it and return
                else
                {
                    nodeRenderOutput = previewData.texture;
                    errorMessages = null;
                    return PreviewOutputState.Complete;
                }
            }
            else
            {
                var previewData = AddNodePreviewData(nodeName);
                previewData.currentRenderMode = newPreviewMode;
                nodeRenderOutput = Texture2D.blackTexture;
                errorMessages = null;
                return PreviewOutputState.Updating;
            }
        }

        /// <summary>
        /// Used to get preview material associated with a node.
        /// </summary>
        /// <returns> Material that describes the current preview shader and render output of a node, if the nodes material is in a valid state to return  </returns>
        public Material RequestNodePreviewMaterial(string nodeName)
        {
            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                var previewData = m_CachedPreviewData[nodeName];

                // Still rendering the preview output
                if (previewData.isRenderOutOfDate)
                {
                    return null;
                }

                // Still compiling the preview shader
                else if (previewData.isShaderOutOfDate)
                {
                    return null;
                }

                // Ran into error compiling the preview shader
                else if (previewData.hasShaderError)
                {
                    return null;
                }

                // Otherwise, the preview output has been rendered, return material wrapper around it
                else
                {
                    return previewData.material;
                }
            }

            return null;
        }

        /// <summary>
        /// Used to get preview shader code associated with a node.
        /// </summary>
        /// <returns> Current preview shader generated by a node </returns>
        public string RequestNodePreviewShaderCode(string nodeName)
        {
            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                var previewData = m_CachedPreviewData[nodeName];

                // Still rendering the preview output
                if (previewData.isRenderOutOfDate)
                {
                    return null;
                }

                // Still compiling the preview shader
                else if (previewData.isShaderOutOfDate)
                {
                    return null;
                }

                // Ran into error compiling the preview shader
                else if (previewData.hasShaderError)
                {
                    return null;
                }

                // Otherwise, the preview output has been rendered, return material wrapper around it
                else
                {
                    return previewData.shaderString;
                }
            }

            return null;
        }

        /// <summary>
        /// Used to set whether a node preview is currently enabled or disabled for update.
        /// </summary>
        public void SetNodePreviewEnabled(string nodeName, bool shouldPreviewUpdate)
        {
            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                var previewData = m_CachedPreviewData[nodeName];
                previewData.isPreviewEnabled = shouldPreviewUpdate;
            }
        }

        /// <summary>
        /// Used to get preview material associated with the final output of the active graph.
        /// </summary>
        /// <returns> Enum value that defines whether the node's render output is ready, currently being updated, or if a shader error was encountered </returns>
        public PreviewOutputState RequestMasterPreviewMaterial(Vector2 masterPreviewSize, out Material masterPreviewMaterial, out ShaderMessage[] errorMessages)
        {
            if (masterPreviewSize != m_MasterPreviewSize)
            {
                // Master Preview window was resized, need to re-render at new size
                m_MasterPreviewSize = masterPreviewSize;
                m_MasterPreviewData.renderTexture.width = Mathf.RoundToInt(masterPreviewSize.x);
                m_MasterPreviewData.renderTexture.height = Mathf.RoundToInt(masterPreviewSize.y);
                m_MasterPreviewData.isRenderOutOfDate = true;
                masterPreviewMaterial = null;
                errorMessages = null;
                masterPreviewWasResized = true;
                return PreviewOutputState.Updating;
            }
            else if (m_MasterPreviewData.isRenderOutOfDate)
            {
                masterPreviewMaterial = null;
                errorMessages = null;
                return PreviewOutputState.Updating;
            }
            else if (m_MasterPreviewData.isShaderOutOfDate)
            {
                masterPreviewMaterial = null;
                errorMessages = null;
                return PreviewOutputState.Updating;
            }
            else if (m_MasterPreviewData.hasShaderError)
            {
                masterPreviewMaterial = null;
                errorMessages = m_ShaderMessagesMap[k_MasterPreviewName];
                return PreviewOutputState.ShaderError;
            }

            masterPreviewMaterial = null;
            errorMessages = null;
            return PreviewOutputState.Updating;
        }

        /// <summary>
        /// Used to get preview shader code associated with the final output of the active graph.
        /// </summary>
        /// <returns> Enum value that defines whether the graphs shader code output is ready, currently being compiled, or if a shader error was encountered </returns>
        public PreviewOutputState RequestMasterPreviewShaderCode(out string masterPreviewShaderCode)
        {
            if (m_MasterPreviewData.isShaderOutOfDate)
            {
                masterPreviewShaderCode = "";
                return PreviewOutputState.Updating;
            }
            else if (m_MasterPreviewData.hasShaderError)
            {
                masterPreviewShaderCode = "";
                return PreviewOutputState.ShaderError;
            }
            else
            {
                // TODO: Interpreter will also return shader code in the future, can store from there
                masterPreviewShaderCode = m_MasterPreviewData.shaderString;
                return PreviewOutputState.Complete;
            }
        }

        void AddMasterPreviewData()
        {
            int sizeX = Mathf.RoundToInt(m_MasterPreviewSize.x);
            int sizeY = Mathf.RoundToInt(m_MasterPreviewSize.y);
            m_MasterPreviewData = new PreviewData()
            {
                nodeName = k_MasterPreviewName,
                renderTexture =
                    new RenderTexture(sizeX, sizeY, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave },
                isShaderOutOfDate = true,
                isRenderOutOfDate = true
            };

            m_CachedPreviewData.Add(k_MasterPreviewName, m_MasterPreviewData);
        }

        Shader GetNodeShaderObject(INodeReader nodeReader)
        {
            return Interpreter.GetShaderForNode(nodeReader, m_GraphHandle, m_RegistryInstance);
        }

        Shader GetMasterPreviewShaderObject()
        {
            return Interpreter.GetShaderForGraph(m_GraphHandle, m_RegistryInstance);
        }

        PreviewData AddNodePreviewData(string nodeName)
        {
            var renderData = new PreviewData
            {
                nodeName = nodeName,
                renderTexture =
                    new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave },
                isShaderOutOfDate = true,
                isRenderOutOfDate = true
            };

            /* TODO: Re-enable when properties are promoted via the shader code generator
            /* Currently not needed as port values get inlined as shader constants
            var nodeReader = m_GraphHandle.GetNodeReader(nodeName);
            var nodeInputPorts = nodeReader.GetInputPorts();
            foreach (var inputPort in nodeInputPorts)
            {
                SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock, inputPort);
            }
            */

            m_CachedPreviewData.Add(nodeName, renderData);
            return renderData;
        }

        public void Update()
        {
            UpdateShaders();

            UpdateRenders();
        }

        private static readonly ProfilerMarker UpdateShadersMarker = new ProfilerMarker("UpdateShaders");
        void UpdateShaders()
        {
            using (UpdateShadersMarker.Auto())
            {
                foreach (var previewData in m_CachedPreviewData.Values)
                {
                    if(!previewData.isShaderOutOfDate)
                        continue;

                    // If master preview
                    if (m_MasterPreviewData == previewData)
                    {
                        previewData.shader = GetMasterPreviewShaderObject();
                    }
                    else // if node preview
                    {
                        var nodeReader = m_GraphHandle.GetNodeReader(previewData.nodeName);
                        previewData.shader = GetNodeShaderObject(nodeReader);
                    }

                    Assert.IsNotNull(previewData.shader);

                    previewData.material = new Material(previewData.shader) { hideFlags = HideFlags.HideAndDontSave };

                    if (CheckForErrors(previewData))
                        previewData.hasShaderError = true;

                    previewData.isShaderOutOfDate = false;
                }
            }
        }

        void UpdateRenders()
        {
            int drawPreviewCount = 0;
            bool renderMasterPreview = false;

            using (var renderList2D = PooledList<PreviewData>.Get())
            using (var renderList3D = PooledList<PreviewData>.Get())
            {
                foreach (var previewData in m_CachedPreviewData.Values)
                {
                    Assert.IsNotNull(previewData);
                    if (previewData.isRenderOutOfDate)
                    {
                        if (!previewData.isPreviewEnabled)
                            continue;

                        // Skip rendering while a preview shader is being compiled (only really a problem when we're doing async)
                        if (previewData.isShaderOutOfDate)
                            continue;

                        // we want to render this thing, now categorize what kind of render it is
                        if (previewData == m_MasterPreviewData)
                            renderMasterPreview = true;
                        else if (previewData.currentRenderMode == PreviewRenderMode.Preview2D)
                            renderList2D.Add(previewData);
                        else
                            renderList3D.Add(previewData);
                        drawPreviewCount++;
                    }
                }

                // if we actually don't want to render anything at all, early out here
                if (drawPreviewCount <= 0)
                    return;

                // TODO: Revisit this when we have global properties, this should be set from the GTF Preview Wrapper
                var time = Time.realtimeSinceStartup;
                var timeParameters = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);
                m_PreviewMaterialPropertyBlock.SetVector("_TimeParameters", timeParameters);

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

                // TODO: Revisit this when GetTargetSettings() is implemented
                //var targetSettings = m_GraphHandle.GetTargetSettings();

                if (renderMasterPreview)
                {
                    if (masterPreviewWasResized)
                    {
                        if (m_MasterPreviewData.renderTexture != null)
                            Object.DestroyImmediate(m_MasterPreviewData.renderTexture, true);
                        m_MasterPreviewData.renderTexture = new RenderTexture((int)m_MasterPreviewSize.x, (int)m_MasterPreviewSize.y, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };
                        m_MasterPreviewData.renderTexture.Create();
                        m_MasterPreviewData.texture = m_MasterPreviewData.renderTexture;
                        masterPreviewWasResized = false;
                    }

                    // TODO: Better understand how we will populate this information from the GTF view model data
                    var mesh = m_MasterPreviewUserData.serializedMesh.mesh;
                    var preventRotation = m_MasterPreviewUserData.preventRotation;
                    if (!mesh)
                    {
                        // TODO: Revisit when GetTargetSettings() is implemented
                        //var useSpritePreview = targetSettings.LastOrDefault(t => t.IsActive())?.prefersSpritePreview ?? false;
                        var useSpritePreview = false;
                        mesh = useSpritePreview ? m_SceneResources.quad : m_SceneResources.sphere;
                        preventRotation = useSpritePreview;
                    }

                    var previewTransform = preventRotation ? Matrix4x4.identity : Matrix4x4.Rotate(m_MasterPreviewUserData.rotation);
                    var scale = m_MasterPreviewUserData.scale;
                    previewTransform *= Matrix4x4.Scale(scale * Vector3.one * (Vector3.one).magnitude / mesh.bounds.size.magnitude);
                    previewTransform *= Matrix4x4.Translate(-mesh.bounds.center);

                    RenderPreview(m_MasterPreviewData, mesh, previewTransform);
                }
            }
        }

        private static readonly ProfilerMarker RenderPreviewMarker = new ProfilerMarker("RenderPreview");
        void RenderPreview(PreviewData renderData, Mesh mesh, Matrix4x4 transform/*, PooledList<PreviewProperty> perMaterialPreviewProperties*/)
        {
            using (RenderPreviewMarker.Auto())
            {
                var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = true;

                // TODO: Setting properties on materials that arent supported by the MPB like Virtual Textures etc
                // AssignPerMaterialPreviewProperties(renderData.shaderData.mat, perMaterialPreviewProperties);

                var previousRenderTexture = RenderTexture.active;

                // Temp workaround for alpha previews...
                var temp = RenderTexture.GetTemporary(renderData.renderTexture.descriptor);
                RenderTexture.active = temp;
                Graphics.Blit(Texture2D.whiteTexture, temp, m_SceneResources.checkerboardMaterial);

                // TODO: Revisit when GetTargetSettings() is implemented
                // bool isOnlyVFXTarget = m_GraphHandle.GetTargetSettings();

                // Mesh is invalid for VFXTarget
                // TODO: We should handle this more gracefully
                if (renderData != m_MasterPreviewData /*|| !isOnlyVFXTarget*/)
                {
                    m_SceneResources.camera.targetTexture = temp;
                    Graphics.DrawMesh(mesh, transform, renderData.material, 1, m_SceneResources.camera, 0, m_PreviewMaterialPropertyBlock, ShadowCastingMode.Off, false, null, false);
                }

                var previousUseSRP = Unsupported.useScriptableRenderPipeline;
                Unsupported.useScriptableRenderPipeline = (renderData == m_MasterPreviewData);
                m_SceneResources.camera.Render();
                Unsupported.useScriptableRenderPipeline = previousUseSRP;

                Graphics.Blit(temp, renderData.renderTexture, m_SceneResources.blitNoAlphaMaterial);
                RenderTexture.ReleaseTemporary(temp);

                RenderTexture.active = previousRenderTexture;
                renderData.texture = renderData.renderTexture;

                ShaderUtil.allowAsyncCompilation = wasAsyncAllowed;
            }
        }

        bool CheckForErrors(PreviewData renderData)
        {
            renderData.hasShaderError = ShaderUtil.ShaderHasError(renderData.shader);
            if (renderData.hasShaderError)
            {
                var messages = ShaderUtil.GetShaderMessages(renderData.shader);
                if (messages.Length > 0)
                {
                    // Clear any existing messages first
                    if (m_ShaderMessagesMap.ContainsKey(renderData.nodeName))
                        m_ShaderMessagesMap.Remove(renderData.nodeName);

                    m_ShaderMessagesMap.Add(renderData.nodeName, messages);
                    ShaderUtil.ClearShaderMessages(renderData.shader);
                    return true;
                }
            }

            return false;
        }

        IPortReader Mock_GetPortReaderForProperty(string nodeName, string propertyName)
        {
            var nodeReader = m_GraphHandle.GetNodeReader(nodeName);
            return nodeReader.TryGetPort(propertyName, out var portReader) ? portReader : null;
        }

        DefaultTextureType Mock_GetDefaultTextureType(IPortReader portReader)
        {
            return DefaultTextureType.White;
        }

        // This will return a uniquely qualified name for the property like:
        // Add1.InPort1
        string Mock_GetHLSLNameForLocalProperty(string nodeName, string propertyName)
        {
            return "";
        }

        string Mock_GetHLSLNameForGlobalProperty(string propertyName)
        {
            return "";
        }

        void SetValueOnMaterialPropertyBlock(MaterialPropertyBlock materialPropertyBlock, string propertyName, object propertyValue, IPortReader portReader = null)
        {
            var type = propertyValue.GetType();

            if ((type == typeof(Texture2D) /*|| propertyType == PropertyType.Texture2DArray*/ || type == typeof(Texture3D)))
            {
                if (propertyValue == null)
                {
                    // there's no way to set the texture back to NULL
                    // and no way to delete the property either
                    // so instead we set the propertyValue to what we know the default will be
                    // (all textures in ShaderGraph default to white)

                    var defaultTextureType = Mock_GetDefaultTextureType(portReader);
                    switch (defaultTextureType)
                    {
                        case DefaultTextureType.White:
                            materialPropertyBlock.SetTexture(propertyName, Texture2D.whiteTexture);
                            break;
                        case DefaultTextureType.Black:
                            materialPropertyBlock.SetTexture(propertyName, Texture2D.blackTexture);
                            break;
                        case DefaultTextureType.NormalMap:
                            materialPropertyBlock.SetTexture(propertyName, Texture2D.normalTexture);
                            break;
                    }
                }
                else
                {
                    var textureValue = propertyValue as Texture;
                    materialPropertyBlock.SetTexture(propertyName, textureValue);
                }
            }
            else if (type == typeof(Cubemap))
            {
                if (propertyValue == null)
                {
                    // there's no Cubemap.whiteTexture, but this seems to work
                    materialPropertyBlock.SetTexture(propertyName, Texture2D.whiteTexture);
                }
                else
                {
                    var cubemapValue = propertyValue as Cubemap;
                    materialPropertyBlock.SetTexture(propertyName, cubemapValue);
                }
            }
            else if (type == typeof(Color))
            {
                var colorValue = propertyValue is Color colorVal ? colorVal : default;
                materialPropertyBlock.SetColor(propertyName, colorValue);
            }
            else if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4))
            {
                var vector4Value = propertyValue is Vector4 vector4Val ? vector4Val : default;
                materialPropertyBlock.SetVector(propertyName, vector4Value);
            }
            else if (type == typeof(float))
            {
                var floatValue = propertyValue is float floatVal ? floatVal : default;
                materialPropertyBlock.SetFloat(propertyName, floatValue);
            }
            else if (type == typeof(Boolean))
            {
                var boolValue = propertyValue is Boolean boolVal ? boolVal : default;
                materialPropertyBlock.SetFloat(propertyName, boolValue ? 1 : 0);
            }
            // TODO: How to handle Matrix2/Matrix3 types?
            // Will probably be registry defined, how will we compare against them?
            else if (type == typeof(Matrix4x4)/*propertyType == PropertyType.Matrix2 || propertyType == PropertyType.Matrix3 || */)
            {
                var matValue = propertyValue is Matrix4x4 matrixValue ? matrixValue : default;
                materialPropertyBlock.SetMatrix(propertyName, matValue);
            }
            else if (type == typeof(Gradient))
            {
                var gradientValue = propertyValue as Gradient;
                materialPropertyBlock.SetFloat(string.Format("{0}_Type", propertyName), (int)gradientValue.mode);
                materialPropertyBlock.SetFloat(string.Format("{0}_ColorsLength", propertyName), gradientValue.colorKeys.Length);
                materialPropertyBlock.SetFloat(string.Format("{0}_AlphasLength", propertyName), gradientValue.alphaKeys.Length);
                for (int i = 0; i < 8; i++)
                    materialPropertyBlock.SetVector(string.Format("{0}_ColorKey{1}", propertyName, i), i < gradientValue.colorKeys.Length ? GradientUtil.ColorKeyToVector(gradientValue.colorKeys[i]) : Vector4.zero);
                for (int i = 0; i < 8; i++)
                    materialPropertyBlock.SetVector(string.Format("{0}_AlphaKey{1}", propertyName, i), i < gradientValue.alphaKeys.Length ? GradientUtil.AlphaKeyToVector(gradientValue.alphaKeys[i]) : Vector2.zero);
            }
            // TODO: Virtual textures handling
            /*else if (type == typeof(VirtualTexture))
            {
                // virtual texture assignments are not supported via the material property block, we must assign them to the directly to the material
            }*/
        }
    }

    static class GradientUtil
    {
        public static Vector4 ColorKeyToVector(GradientColorKey key)
        {
            return new Vector4(key.color.r, key.color.g, key.color.b, key.time);
        }

        public static Vector2 AlphaKeyToVector(GradientAlphaKey key)
        {
            return new Vector2(key.alpha, key.time);
        }
    }
}
