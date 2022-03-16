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
            public string functionString;
            public string blockString;
            public string shaderString;
            public Texture texture;
            public bool isShaderOutOfDate;
            public bool isRenderOutOfDate;

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
            InitializeSRPIfNeeded();
        }

        void InitializeSRPIfNeeded()
        {
            if ((Shader.globalRenderPipeline != null) && (Shader.globalRenderPipeline.Length > 0))
            {
                return;
            }

            // issue a dummy SRP render to force SRP initialization, use the master node texture
            PreviewData renderData = m_MasterPreviewData;
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

        /// <summary>
        /// Map from node names to associated preview data object
        /// </summary>
        Dictionary<string, PreviewData> m_CachedPreviewData = new();

        Dictionary<string, ShaderMessage[]> m_ShaderMessagesMap = new();

        /// <summary>
        /// Handle to the graph object we are currently generating preview data for
        /// </summary>
        GraphHandler m_GraphHandle;

        Registry m_RegistryInstance;

        MaterialPropertyBlock m_PreviewMaterialPropertyBlock = new();

        Texture2D m_ErrorTexture;
        Texture2D m_CompilingTexture;

        PreviewSceneResources m_SceneResources;

        PreviewData m_MasterPreviewData;

        MasterPreviewUserData m_MasterPreviewUserData;

        int m_MasterPreviewWidth => m_MasterPreviewData.renderTexture.width;
        int m_MasterPreviewHeight => m_MasterPreviewData.renderTexture.height;

        // TODO: Need a way to get the name of the main output context node
        const string k_MasterPreviewName = "TestContextDescriptor";

        bool masterPreviewWasResized = false;

        // Some changes made to how rendering handles uncompiled shader variants, in future will be simplified so we can request a render that also handles
        // compiling any uncompiled shader variants/passes etc and there will be an async fallback provided, and a callback for when the async compile and render is done

        /// <summary>
        /// Used to set which graph this preview manager gets its node data from.
        /// </summary>
        public void SetActiveGraph(GraphHandler activeGraphReference)
        {
            m_GraphHandle = activeGraphReference;
        }

        /// <summary>
        /// Used to set which registry instance this preview manager gets its type data from.
        /// </summary>
        public void SetActiveRegistry(Registry registryInstance)
        {
            m_RegistryInstance = registryInstance;
        }

        /// <summary>
        /// Used to change the Value of global properties like Time, Blackboard Properties, Render Pipeline Intrinsics etc.
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
            foreach (var downStreamNode in GraphTraversalUtils.GetDownstreamNodes(globalPropertyNode))
            {
                var downStreamNodeName = downStreamNode.GetName();
                var nodePreviewData = m_CachedPreviewData[downStreamNodeName];
                nodePreviewData.isShaderOutOfDate = true;

                impactedNodes.Add(downStreamNode.GetName());
            }

            return impactedNodes;
        }

        /// <summary>
        /// Used to change the Value of local properties such as node inputs/port propertyValues.
        /// </summary>
        /// <returns> List of names describing all nodes that were affected by this change </returns>
        /// <remarks> Dirties the preview render state of all nodes downstream of any references to the changed property </remarks>
        public List<string> SetLocalProperty(string nodeName, string propertyName, object newPropertyValue)
        {
            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                var nodePreviewData = m_CachedPreviewData[nodeName];
                // TODO: Remove when we have shader property promotion
                nodePreviewData.isShaderOutOfDate = true;

                var portReader = Mock_GetPortReaderForProperty(nodeName, propertyName);
                var hlslName = Mock_GetHLSLNameForLocalProperty(nodeName, propertyName);

                SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock, hlslName, newPropertyValue, portReader);

                var sourceNode = m_GraphHandle.GetNodeReader(nodeName);
                var impactedNodes = new List<string>();
                foreach (var downStreamNode in GraphTraversalUtils.GetDownstreamNodes(sourceNode))
                {
                    var downStreamNodeName = downStreamNode.GetName();
                    impactedNodes.Add(downStreamNodeName);

                    m_CachedPreviewData[downStreamNodeName].isShaderOutOfDate = true;
                }

                return impactedNodes;
            }

            Debug.Log("HeadlessPreviewManager: SetLocalProperty called on a node that hasn't been registered!");

            // Currently any change to any node needs to also dirty the master node as we don't actually have ability to traverse to master node, though in future it will and this can be removed
            m_MasterPreviewData.isShaderOutOfDate = true;

            return new List<string>();
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
                    // TODO: Will we handle node bypassing directly in GetDownstreamNodes()?

                    var previewData = m_CachedPreviewData[nodeName];
                    previewData.isShaderOutOfDate = true;

                    foreach (var downStreamNode in GraphTraversalUtils.GetDownstreamNodes(sourceNode))
                    {
                        if (m_CachedPreviewData.TryGetValue(downStreamNode.GetName(), out var downStreamNodeData))
                        {
                            downStreamNodeData.isShaderOutOfDate = true;
                        }

                        impactedNodes.Add(downStreamNode.GetName());
                    }
                }

                return impactedNodes;
            }

            Debug.Log("HeadlessPreviewManager: NotifyNodeFlowChanged called on a node that hasn't been registered!");

            return impactedNodes;
        }

        /// <summary>
        /// Used to get current preview render output of a node, and optionally at a specific preview mode provided as an argument.
        /// nodeRenderOutput is a Texture that contains the current preview output of a node, if its shaders have been compiled and ready to return
        /// </summary>
        /// <returns> Enum value that defines whether the node's render output is ready, currently being updated, or if a shader error was encountered </returns>
        public PreviewOutputState RequestNodePreviewImage(string nodeName, out Texture nodeRenderOutput, out ShaderMessage[] errorMessages, PreviewRenderMode newPreviewMode = PreviewRenderMode.Preview2D)
        {
            errorMessages = null;

            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                var previewData = m_CachedPreviewData[nodeName];
                previewData.currentRenderMode = newPreviewMode;

                // Still compiling the preview shader
                if (previewData.isShaderOutOfDate)
                {
                    UpdateShaderData(previewData);
                    UpdateRenderData(previewData);
                    nodeRenderOutput = m_CompilingTexture;

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
                    return PreviewOutputState.Complete;
                }
            }
            else
            {
                var previewData = AddNodePreviewData(nodeName);
                previewData.currentRenderMode = newPreviewMode;
                UpdateShaderData(previewData);
                UpdateRenderData(previewData);
                nodeRenderOutput = previewData.texture;
                return PreviewOutputState.Complete;
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

                // Still compiling the preview shader
                if (previewData.isShaderOutOfDate)
                {
                    UpdateShaderData(previewData);

                    // Ran into error compiling the preview shader
                    return previewData.hasShaderError ? null : previewData.material;
                }
            }
            else
            {
                var previewData = AddNodePreviewData(nodeName);
                UpdateShaderData(previewData);
                return previewData.material;
            }

            return null;
        }

        /// <summary>
        /// Used to get preview shader code associated with a node.
        /// </summary>
        /// <returns> Current preview shader generated by a node </returns>
        public string RequestNodePreviewShaderCode(string nodeName, out ShaderMessage[] shaderMessages)
        {
            RequestNodePreviewShaderCodeStrings(nodeName, out shaderMessages, out string shaderCode, out _, out _);
            return shaderCode;
        }

        /// <summary>
        /// Used to get shader code, of varying granularity, associated with a node.
        /// </summary>
        /// <param name="shaderCode">The fully generated preview shader.</param>
        /// <param name="blockCode">Shader code unique to this node's preview.</param>
        /// <param name="funcCode">The shader function generated directly by this node.</param>
        public void RequestNodePreviewShaderCodeStrings(string nodeName, out ShaderMessage[] shaderMessages, out string shaderCode, out string blockCode, out string funcCode)
        {
            shaderMessages = new ShaderMessage [] {};
            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                var previewData = m_CachedPreviewData[nodeName];

                // Still compiling the preview shader
                if (previewData.isShaderOutOfDate)
                {
                    UpdateShaderData(previewData);
                    previewData.isShaderOutOfDate = false;
                    if (previewData.hasShaderError)
                    {
                        shaderMessages = m_ShaderMessagesMap[nodeName];
                    }
                }
                blockCode = previewData.blockString;
                funcCode = previewData.functionString;
                shaderCode = previewData.shaderString;
                return;
            }
            else
            {
                var previewData = AddNodePreviewData(nodeName);
                UpdateShaderData(previewData);
                UpdateRenderData(previewData);
                blockCode = previewData.blockString;
                funcCode = previewData.functionString;
                shaderCode = previewData.shaderString;
                return;
            }
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

            Debug.Log("HeadlessPreviewManager: Node not recognized!");
        }

        /// <summary>
        /// Used to get preview material associated with the final output of the active graph.
        /// </summary>
        /// <returns> Enum value that defines whether the node's render output is ready, currently being updated, or if a shader error was encountered </returns>
        public PreviewOutputState RequestMasterPreviewMaterial(int width, int height, out Material masterPreviewMaterial, out ShaderMessage[] errorMessages)
        {
            errorMessages = null;
            masterPreviewMaterial = null;

            if (width != m_MasterPreviewWidth || height != m_MasterPreviewHeight)
            {
                // Master Preview window was resized, need to re-render at new size
                m_MasterPreviewData.renderTexture.width = height;
                m_MasterPreviewData.renderTexture.height = width;
                masterPreviewMaterial = null;
                masterPreviewWasResized = true;
                return PreviewOutputState.Updating;
            }
            else if (m_MasterPreviewData.isShaderOutOfDate)
            {
                UpdateShaderData(m_MasterPreviewData);
                m_MasterPreviewData.isShaderOutOfDate = false;
                if (m_MasterPreviewData.hasShaderError)
                {
                    masterPreviewMaterial = null;
                    errorMessages = m_ShaderMessagesMap[k_MasterPreviewName];
                    return PreviewOutputState.ShaderError;
                }
                masterPreviewMaterial = m_MasterPreviewData.material;
                return PreviewOutputState.Complete;
            }

            return PreviewOutputState.Updating;
        }

        /// <summary>
        /// Used to get preview shader code associated with the final output of the active graph.
        /// </summary>
        /// <returns> Enum value that defines whether the node's render output is ready, currently being updated, or if a shader error was encountered </returns>
        public PreviewOutputState RequestMasterPreviewShaderCode(out string masterPreviewShaderCode, out ShaderMessage[] errorMessages)
        {
            errorMessages = new ShaderMessage[] {};

            if (m_MasterPreviewData.isShaderOutOfDate)
            {
                RequestMasterPreviewMaterial(m_MasterPreviewWidth, m_MasterPreviewHeight, out var masterPreviewMaterial, out var shaderMessages);
                masterPreviewShaderCode = m_MasterPreviewData.shaderString;
                if (m_MasterPreviewData.hasShaderError)
                {
                    errorMessages = m_ShaderMessagesMap[k_MasterPreviewName];
                    return PreviewOutputState.ShaderError;
                }
                return PreviewOutputState.Complete;
            }
            else
            {
                masterPreviewShaderCode = m_MasterPreviewData.shaderString;
                return PreviewOutputState.Complete;
            }
        }

        void AddMasterPreviewData()
        {
            m_MasterPreviewData = new PreviewData()
            {
                nodeName = k_MasterPreviewName,
                renderTexture =
                    new RenderTexture(400, 400, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave },
                isShaderOutOfDate = true,
            };

            m_CachedPreviewData.Add(k_MasterPreviewName, m_MasterPreviewData);
        }

        private static Shader MakeShader(string input)
        {
            bool tmp = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            Shader output = ShaderUtil.CreateShaderAsset(input, true);
            ShaderUtil.allowAsyncCompilation = tmp;
            return output;
        }

        Shader GetNodeShaderObject(INodeReader nodeReader)
        {
            string shaderOutput = Interpreter.GetShaderForNode(nodeReader, m_GraphHandle, m_RegistryInstance);
            m_CachedPreviewData[nodeReader.GetName()].shaderString = shaderOutput;
            m_CachedPreviewData[nodeReader.GetName()].blockString = Interpreter.GetBlockCode(nodeReader, m_GraphHandle, m_RegistryInstance);
            m_CachedPreviewData[nodeReader.GetName()].functionString = Interpreter.GetFunctionCode(nodeReader, m_RegistryInstance);
            return MakeShader(shaderOutput);
        }

        Shader GetMasterPreviewShaderObject()
        {
            // TODO: Need a way to query the main context node without having a hard name dependence, from GraphDelta
            var contextNodeReader = m_GraphHandle.GetNodeReader(k_MasterPreviewName);
            string shaderOutput = Interpreter.GetShaderForNode(contextNodeReader, m_GraphHandle, m_RegistryInstance);
            m_MasterPreviewData.shaderString = shaderOutput;
            return MakeShader(shaderOutput);
        }

        PreviewData AddNodePreviewData(string nodeName)
        {
            var renderData = new PreviewData
            {
                nodeName = nodeName,
                renderTexture =
                    new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave },
                isShaderOutOfDate = true,
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

        private static readonly ProfilerMarker UpdateShadersMarker = new ProfilerMarker("UpdateShaders");

        void UpdateShaderData(PreviewData previewToUpdate)
        {
            using (UpdateShadersMarker.Auto())
            {
                // If master preview
                if (m_MasterPreviewData == previewToUpdate)
                {
                    previewToUpdate.shader = GetMasterPreviewShaderObject();
                }
                else // if node preview
                {
                    var nodeReader = m_GraphHandle.GetNodeReader(previewToUpdate.nodeName);
                    previewToUpdate.shader = GetNodeShaderObject(nodeReader);
                }

                Assert.IsNotNull(previewToUpdate.shader);

                previewToUpdate.material = new Material(previewToUpdate.shader) { hideFlags = HideFlags.HideAndDontSave };

                if (CheckForErrors(previewToUpdate))
                    previewToUpdate.hasShaderError = true;

                previewToUpdate.isShaderOutOfDate = false;
            }
        }

        void UpdateRenderData(PreviewData previewToUpdate)
        {
            Assert.IsNotNull(previewToUpdate);

            // TODO: Revisit this when we have global properties, this should be set from the GTF Preview Wrapper
            var time = Time.realtimeSinceStartup;
            var timeParameters = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);
            m_PreviewMaterialPropertyBlock.SetVector("_TimeParameters", timeParameters);

            // TODO: Revisit this when GetTargetSettings() is implemented
            //var targetSettings = m_GraphHandle.GetTargetSettings();

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

            // Master preview
            if (previewToUpdate == m_MasterPreviewData)
            {
                if (masterPreviewWasResized)
                {
                    if (m_MasterPreviewData.renderTexture != null)
                        Object.DestroyImmediate(m_MasterPreviewData.renderTexture, true);
                    m_MasterPreviewData.renderTexture = new RenderTexture((int)m_MasterPreviewWidth, (int)m_MasterPreviewHeight, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };
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
            else // Node previews
            {
                if (previewToUpdate.currentRenderMode == PreviewRenderMode.Preview2D)
                    RenderPreview(previewToUpdate, m_SceneResources.quad, Matrix4x4.identity);
                else
                    RenderPreview(previewToUpdate, m_SceneResources.sphere, Matrix4x4.identity);
            }
        }

        private static readonly ProfilerMarker RenderPreviewMarker = new ProfilerMarker("RenderPreview");

        void RenderPreview(PreviewData renderData, Mesh mesh, Matrix4x4 transform /*, PooledList<PreviewProperty> perMaterialPreviewProperties*/)
        {
            using (RenderPreviewMarker.Auto())
            {
                var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;

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
                // we seem to be using SRP for all renders now
                Unsupported.useScriptableRenderPipeline = true;
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

        // Stubbed function bodies below ----
        // Will be implemented when shader variables are treated as properties in the MPB

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

        }
    }
}
