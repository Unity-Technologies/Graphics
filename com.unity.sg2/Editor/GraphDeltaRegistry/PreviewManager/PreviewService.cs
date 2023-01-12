using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using Unity.Profiling;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.Utils;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    /// <summary>
    /// This class encapsulates all functionality related to generating preview render and shader data from a node graph
    /// TODO: Pack all the render info. in each Request() function into a struct so that its more visible what data is needed and function calls are slimmer
    /// </summary>
    public class PreviewService
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

        internal class PreviewData
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
            public List<(string, Texture)> defaultTextures = new();

            // Used to control whether the preview should render in 2D/3D/Inherit from upstream nodes
            public PreviewRenderMode currentRenderMode;

            // Do we need to cache the render texture?
            public RenderTexture renderTexture;

            // Do we need to track how many passes are actively compiled per shader? What is it used for beyond debug log stuff?
            public int passesCompiling;

            // Same for this below...
            public bool hasShaderError;
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

        IPreviewUpdateReceiver m_PreviewUpdateReceiver;
        MaterialPropertyBlock m_PreviewMaterialPropertyBlock;
        Texture2D m_ErrorTexture;
        Texture2D m_CompilingTexture;
        PreviewSceneResources m_SceneResources;

        #region MainPreviewData
        PreviewData m_MainPreviewData;
        Mesh m_MainPreviewMesh;
        Quaternion m_MainPreviewRotation = Quaternion.identity;
        bool m_PreventMainPreviewRotation;
        float m_MainPreviewScale = 1.0f;
        #endregion

        int mainPreviewWidth => m_MainPreviewData.renderTexture.width;
        int mainPreviewHeight => m_MainPreviewData.renderTexture.height;
        string m_OutputContextNodeName;

        Target m_GlobalTarget;
        Target m_NodePreviewTarget;
        List<PreviewData> m_PreviewsCompiling;  // list of PreviewData that are being compiled (see UpdateShaderCompilationStatus)

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

        internal static Target CreateUniversalTarget() // Temp code.
        {
            var targetTypes = TypeCache.GetTypesDerivedFrom<Target>();
            foreach (var type in targetTypes)
            {
                if (type.IsAbstract || type.IsGenericType || !type.IsClass || type.Name != "UniversalTarget")
                    continue;

                var target = (Target)Activator.CreateInstance(type);
                if (!target.isHidden)
                    return target;
            }
            return null;
        }

        internal static SubTarget GetUniversalSubTarget(string subTargetName) // Temp code.
        {
            var subtargetTypes = TypeCache.GetTypesDerivedFrom<SubTarget>();
            foreach (var type in subtargetTypes)
            {
                if (type.Name != subTargetName)
                    continue;

                var subTarget = (SubTarget)Activator.CreateInstance(type);
                return subTarget;
            }
            return null;
        }

        internal static Target ConfigureURPUnlit(Target urpTarget) // Temp code.
        {
            var unlitSubtarget = GetUniversalSubTarget("UniversalUnlitSubTarget");
            var methodInfo = urpTarget.GetType().GetMethod("TrySetActiveSubTarget", BindingFlags.Public | BindingFlags.Instance);
            methodInfo.Invoke(urpTarget, new object[] { unlitSubtarget.GetType() });

            return urpTarget;
        }

        public void Initialize(string contextNodeName, Vector2 mainPreviewSize)
        {
            m_PreviewsCompiling = new List<PreviewData>();
            m_ErrorTexture = GenerateFourSquare(Color.magenta, Color.black);
            m_CompilingTexture = GenerateFourSquare(Color.blue, Color.blue);
            m_SceneResources = new PreviewSceneResources();

            m_PreviewMaterialPropertyBlock = new();
            AddMainPreviewData(contextNodeName, mainPreviewSize);

            // Also create and store a URP unlit target for rendering node previews
            m_NodePreviewTarget = CreateUniversalTarget();
            ConfigureURPUnlit(m_NodePreviewTarget);

            InitializeSRPIfNeeded();
        }

        public void Cleanup()
        {
            if (m_ErrorTexture != null)
            {
                Object.DestroyImmediate(m_ErrorTexture);
                m_ErrorTexture = null;
            }

            if (m_CompilingTexture != null)
            {
                Object.DestroyImmediate(m_CompilingTexture);
                m_CompilingTexture = null;
            }

            if (m_SceneResources != null)
            {
                m_SceneResources.Dispose();
                m_SceneResources = null;
            }

            m_CachedPreviewData.Clear();
            m_ShaderMessagesMap.Clear();
            m_PreviewMaterialPropertyBlock.Clear();
        }

        void InitializeSRPIfNeeded()
        {
            if ((Shader.globalRenderPipeline != null) && (Shader.globalRenderPipeline.Length > 0))
            {
                return;
            }

            // issue a dummy SRP render to force SRP initialization, use the main preview texture
            PreviewData renderData = m_MainPreviewData;
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

        internal void SetActiveTarget(Target target)
        {
            m_GlobalTarget = target;
        }

        internal void SetPreviewUpdateReceiver(IPreviewUpdateReceiver previewUpdateReceiver)
        {
            m_PreviewUpdateReceiver = previewUpdateReceiver;
        }

        /// <summary>
        /// Used to change the Value of global properties like Time, Blackboard Properties, Render Pipeline Intrinsics etc.
        /// </summary>
        /// <returns> List of names describing all nodes that were affected by this change </returns>
        /// <remarks> Dirties the preview render state of all nodes downstream of any references to the changed property </remarks>
        public List<string> SetGlobalProperty(string propertyName, object newPropertyValue, IEnumerable<string> variableNodeList)
        {
            var hlslName = Mock_GetHLSLNameForGlobalProperty(propertyName);
            SetValueOnMaterialPropertyBlock(m_PreviewMaterialPropertyBlock, propertyName, newPropertyValue);

            var impactedNodes = new List<string>();

            // Property nodes/referrables are mapped to nodes and can be retrieved as such
            foreach (var variableNode in variableNodeList)
            {
                var propertyNode = m_GraphHandle.GetNode(variableNode);

                foreach (var downStreamNode in GraphTraversalUtils.GetDownstreamNodes(propertyNode))
                {
                    var downStreamNodeName = downStreamNode.ID.LocalPath;
                    if (m_CachedPreviewData.TryGetValue(downStreamNodeName, out var nodePreviewData))
                    {
                        nodePreviewData.isShaderOutOfDate = true;
                        impactedNodes.Add(downStreamNode.ID.LocalPath);
                    }
                }
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

                var sourceNode = m_GraphHandle.GetNode(nodeName);
                var impactedNodes = new List<string>();
                foreach (var downStreamNode in GraphTraversalUtils.GetDownstreamNodes(sourceNode))
                {
                    var downStreamNodeName = downStreamNode.ID.LocalPath;
                    impactedNodes.Add(downStreamNodeName);

                    m_CachedPreviewData[downStreamNodeName].isShaderOutOfDate = true;
                }

                return impactedNodes;
            }

            Debug.Log("HeadlessPreviewManager: SetLocalProperty called on a node that hasn't been registered!");

            // Currently any change to any node needs to also dirty the main node as we don't actually have ability to traverse to main node, though in future it will and this can be removed
            m_MainPreviewData.isShaderOutOfDate = true;

            return new List<string>();
        }

        /// <summary>
        /// Used to notify when a node has been deleted and for when connections to a node change.
        /// </summary>
        /// <returns> List of names describing all nodes that were affected by this change </returns>
        /// <remarks> Dirties the preview compile & render state of all nodes downstream of the changed node </remarks>
        public List<string> NotifyNodeFlowChanged(string nodeName, bool wasNodeDeleted = false)
        {
            var impactedNodes = new List<string>();

            var sourceNode = m_GraphHandle.GetNode(nodeName);
            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                if (wasNodeDeleted)
                {
                    // Node was deleted, get rid of the preview data associated with it
                    m_CachedPreviewData.Remove(nodeName);
                }
                else
                {
                    // TODO: Will we handle node bypassing directly in GetDownstreamNodes()?
                    var previewData = m_CachedPreviewData[nodeName];
                    previewData.isShaderOutOfDate = true;
                }
            }

            if (sourceNode != null)
            {
                foreach (var downStreamNode in GraphTraversalUtils.GetDownstreamNodes(sourceNode))
                {
                    if (m_CachedPreviewData.TryGetValue(downStreamNode.ID.LocalPath, out var downStreamNodeData))
                    {
                        downStreamNodeData.isShaderOutOfDate = true;
                    }

                    impactedNodes.Add(downStreamNode.ID.LocalPath);
                }
            }

            return impactedNodes;
        }

        /// <summary>
        /// Used to get current preview render output of a node, and optionally at a specific preview mode provided as an argument.
        /// nodeRenderOutput is a Texture that contains the current preview output of a node, if its shaders have been compiled and ready to return
        /// </summary>
        /// <returns> Enum value that defines whether the node's render output is ready, currently being updated, or if a shader error was encountered </returns>
        public PreviewOutputState RequestNodePreviewTexture(
            string nodeName,
            out Texture nodeRenderOutput,
            out ShaderMessage[] errorMessages,
            PreviewRenderMode newPreviewMode = PreviewRenderMode.Preview2D,
            bool forceRecompile = false)
        {
            errorMessages = null;

            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                var previewData = m_CachedPreviewData[nodeName];
                previewData.currentRenderMode = newPreviewMode;

                // Still compiling the preview shader
                if (previewData.isShaderOutOfDate || forceRecompile)
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

        public void RequestNodePreviewUpdate(
            string nodeName,
            PreviewRenderMode newPreviewMode = PreviewRenderMode.Preview2D,
            bool forceRerender = false)
        {
            PreviewData nodePreviewData;

            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                nodePreviewData = m_CachedPreviewData[nodeName];
                nodePreviewData.currentRenderMode = newPreviewMode;

                // Still compiling the preview shader
                if (nodePreviewData.isShaderOutOfDate)
                {
                    UpdateShaderData(nodePreviewData);
                }
                else if (nodePreviewData.isRenderOutOfDate || forceRerender)
                {
                    UpdateRenderData(nodePreviewData);
                }
            }
            else
            {
                nodePreviewData = AddNodePreviewData(nodeName);
                nodePreviewData.currentRenderMode = newPreviewMode;
                UpdateShaderData(nodePreviewData);
            }
        }

        // ==================================
        // Basic Async Implementation

        /// <summary>
        /// UpdateHandler is called by the owner of the preview service if they desire async functionality
        /// </summary>
        public void UpdateHandler()
        {
            // do update
            UpdateShaderCompilationStatus();
        }

        /// <summary>
        /// Checks the list of preview data that are currently being compiled for
        /// completion.
        /// Upon compile completion:
        ///   update material textures
        ///   check for errors
        ///   mark dirty
        /// </summary>
        void UpdateShaderCompilationStatus()
        {
            if (m_PreviewsCompiling.Count <= 0)
                return;
            List<PreviewData> completed = new List<PreviewData>();
            foreach (PreviewData previewToUpdate in m_PreviewsCompiling)
            {
                bool isCompiling = false;
                for (int i = 0; i < previewToUpdate.material.passCount; ++i)
                {
                    if (!ShaderUtil.IsPassCompiled(previewToUpdate.material, i))
                    {
                        isCompiling = true;
                        break;
                    }
                }
                if (!isCompiling)
                {
                    // add to the local completed list for update
                    completed.Add(previewToUpdate);

                    // set the material textures
                    foreach (var texDefault in previewToUpdate.defaultTextures)
                        previewToUpdate.material.SetTexture(texDefault.Item1, texDefault.Item2);

                    // check for PreviewData errors
                    if (CheckForErrors(previewToUpdate))
                        previewToUpdate.hasShaderError = true;
                }
            }
            foreach (PreviewData completedPreview in completed)
            {
                m_PreviewsCompiling.Remove(completedPreview);
                UpdateRenderData(completedPreview);
            }
        }

        // END Basic Async Implementation
        // ==================================

        public void RequestMainPreviewUpdate(
            int width,
            int height,
            Mesh meshToRender,
            float mainPreviewScale,
            bool preventRotation,
            Quaternion mainPreviewRotation,
            bool forceRerender = false)
        {
            if (width != mainPreviewWidth || height != mainPreviewHeight)
                ResizeMainPreview(width, height);

            if (meshToRender != m_MainPreviewMesh
                || !Mathf.Approximately(mainPreviewScale, m_MainPreviewScale)
                || preventRotation != m_PreventMainPreviewRotation
                || !mainPreviewRotation.Equals(m_MainPreviewRotation))
            {
                m_MainPreviewMesh = meshToRender;
                m_MainPreviewScale = mainPreviewScale;
                m_MainPreviewRotation = mainPreviewRotation;
                m_PreventMainPreviewRotation = preventRotation;
                m_MainPreviewData.isRenderOutOfDate = true;
            }

            if (m_MainPreviewData.isShaderOutOfDate)
            {
                UpdateShaderData(m_MainPreviewData);
            }
            else if (m_MainPreviewData.isRenderOutOfDate || forceRerender)
            {
                UpdateRenderData(m_MainPreviewData);
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
        /// Used to get preview material associated with the final output of the active graph.
        /// </summary>
        /// <returns> Enum value that defines whether the node's render output is ready, currently being updated, or if a shader error was encountered </returns>
        public PreviewOutputState RequestMainPreviewMaterial(
            int width,
            int height,
            out Material mainPreviewMaterial,
            out ShaderMessage[] errorMessages)
        {
            errorMessages = null;
            mainPreviewMaterial = null;

            if (width != mainPreviewWidth || height != mainPreviewHeight)
            {
                ResizeMainPreview(width, height);
                return PreviewOutputState.Updating;
            }

            if (m_MainPreviewData.isShaderOutOfDate)
            {
                UpdateShaderData(m_MainPreviewData);
                m_MainPreviewData.isShaderOutOfDate = false;
                if (m_MainPreviewData.hasShaderError)
                {
                    mainPreviewMaterial = null;
                    errorMessages = m_ShaderMessagesMap[m_OutputContextNodeName];
                    return PreviewOutputState.ShaderError;
                }
                mainPreviewMaterial = m_MainPreviewData.material;
                return PreviewOutputState.Complete;
            }

            return PreviewOutputState.Updating;
        }

        void ResizeMainPreview(int width, int height)
        {
            if (m_MainPreviewData.renderTexture != null)
                Object.DestroyImmediate(m_MainPreviewData.renderTexture, true);

            // This will queue it up for re-render
            m_MainPreviewData.isRenderOutOfDate = true;

            // Main Preview window was resized, RTs cannot be resized so create a new one
            m_MainPreviewData.renderTexture =
                new RenderTexture(
                width,
                height,
                16,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default
                ) { hideFlags = HideFlags.HideAndDontSave };

            m_MainPreviewData.renderTexture.Create();
            m_MainPreviewData.texture = m_MainPreviewData.renderTexture;
        }

        /// <summary>
        /// Used to get image associated with the final output of the active graph.
        /// </summary>
        /// <returns> Enum value that defines whether the node's render output is ready, currently being updated, or if a shader error was encountered </returns>
        public PreviewOutputState RequestMainPreviewTexture(
            int width,
            int height,
            Mesh meshToRender,
            float mainPreviewScale,
            bool preventRotation,
            Quaternion mainPreviewRotation,
            out Texture mainPreviewTexture,
            out ShaderMessage[] errorMessages)
        {
            errorMessages = null;
            mainPreviewTexture = null;

            // Assign it to the currently output texture
            mainPreviewTexture = m_MainPreviewData.texture;

            if (width != mainPreviewWidth || height != mainPreviewHeight)
                ResizeMainPreview(width, height);

            if (meshToRender != m_MainPreviewMesh
                || !Mathf.Approximately(mainPreviewScale, m_MainPreviewScale)
                || preventRotation != m_PreventMainPreviewRotation
                || !mainPreviewRotation.Equals(m_MainPreviewRotation))
            {
                m_MainPreviewMesh = meshToRender;
                m_MainPreviewScale = mainPreviewScale;
                m_MainPreviewRotation = mainPreviewRotation;
                m_PreventMainPreviewRotation = preventRotation;
                m_MainPreviewData.isRenderOutOfDate = true;
                return PreviewOutputState.Updating;
            }

            if (m_MainPreviewData.isShaderOutOfDate)
            {
                UpdateShaderData(m_MainPreviewData);
                UpdateRenderData(m_MainPreviewData);
                if (m_MainPreviewData.hasShaderError)
                {
                    mainPreviewTexture = m_ErrorTexture;
                    errorMessages = m_ShaderMessagesMap[m_OutputContextNodeName];
                    return PreviewOutputState.ShaderError;
                }
                mainPreviewTexture = m_MainPreviewData.texture;
                return PreviewOutputState.Complete;
            }
            else if (m_MainPreviewData.isRenderOutOfDate)
            {
                UpdateRenderData(m_MainPreviewData);
                if (m_MainPreviewData.hasShaderError)
                {
                    mainPreviewTexture = null;
                    errorMessages = m_ShaderMessagesMap[m_OutputContextNodeName];
                    return PreviewOutputState.ShaderError;
                }
                mainPreviewTexture = m_MainPreviewData.texture;
                return PreviewOutputState.Complete;
            }

            return PreviewOutputState.Updating;
        }

        /// <summary>
        /// Used to get preview shader code associated with the final output of the active graph.
        /// </summary>
        /// <returns> Enum value that defines whether the node's render output is ready, currently being updated, or if a shader error was encountered </returns>
        public PreviewOutputState RequestMainPreviewShaderCode(out string mainPreviewShaderCode, out ShaderMessage[] errorMessages)
        {
            errorMessages = new ShaderMessage[] {};

            if (m_MainPreviewData.isShaderOutOfDate)
            {
                RequestMainPreviewMaterial(mainPreviewWidth, mainPreviewHeight, out var mainPreviewMaterial, out var shaderMessages);
                mainPreviewShaderCode = m_MainPreviewData.shaderString;
                if (m_MainPreviewData.hasShaderError)
                {
                    errorMessages = m_ShaderMessagesMap[m_OutputContextNodeName];
                    return PreviewOutputState.ShaderError;
                }
                return PreviewOutputState.Complete;
            }
            else
            {
                mainPreviewShaderCode = m_MainPreviewData.shaderString;
                return PreviewOutputState.Complete;
            }
        }

        void AddMainPreviewData(string contextNodeName, Vector2 mainPreviewSize)
        {
            m_OutputContextNodeName = contextNodeName;

            m_MainPreviewMesh = Resources.GetBuiltinResource(typeof(Mesh), $"Sphere.fbx") as Mesh;

            m_MainPreviewData = new PreviewData()
            {
                nodeName = contextNodeName,
                renderTexture =
                    new RenderTexture(
                        Mathf.FloorToInt(mainPreviewSize.x),
                        Mathf.FloorToInt(mainPreviewSize.y),
                        16,
                        RenderTextureFormat.ARGB32,
                        RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave },
                isShaderOutOfDate = true
            };

            m_CachedPreviewData.Add(m_OutputContextNodeName, m_MainPreviewData);
        }


        Shader GetNodeShaderObject(NodeHandler nodeReader, Target targetToUse)
        {
            string shaderOutput = Interpreter.GetShaderForNode(nodeReader, m_GraphHandle, m_RegistryInstance, out m_CachedPreviewData[nodeReader.ID.LocalPath].defaultTextures, targetToUse);
            var throwAway = new List<(string, Texture)>(); // gross.
            m_CachedPreviewData[nodeReader.ID.LocalPath].shaderString = shaderOutput;
            m_CachedPreviewData[nodeReader.ID.LocalPath].blockString = Interpreter.GetBlockCode(nodeReader, m_GraphHandle, m_RegistryInstance, ref throwAway);
            m_CachedPreviewData[nodeReader.ID.LocalPath].functionString = Interpreter.GetFunctionCode(nodeReader, m_RegistryInstance);
            // create the shader asset without compiling the shader
            return ShaderUtil.CreateShaderAsset(shaderOutput, false);
        }

        Shader GetMainPreviewShaderObject()
        {
            var contextNodeReader = m_GraphHandle.GetNode(m_OutputContextNodeName);
            string shaderOutput = Interpreter.GetShaderForNode(contextNodeReader, m_GraphHandle, m_RegistryInstance, out m_MainPreviewData.defaultTextures, m_GlobalTarget);
            m_MainPreviewData.shaderString = shaderOutput;
            // create the shader asset without compiling the shader
            return ShaderUtil.CreateShaderAsset(shaderOutput, false);
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

        void UpdateShaderData(PreviewData previewToUpdate)
        {
            // UpdateShaderData executes the following steps to update the PreviewData:
            //   generate the shader code from the graph
            //   creates and sets a Shader asset
            //   creates the material for the preview
            //   compiles the Shader asset with the material
            //   updates the material texture for the preview
            //   checks the data for errors
            //   sets the PreviewData as dirty (out of date)

            // TODO (Brett) I think that all of the updates to the PreviewData
            // TODO (Brett) should happen in tasks, so that the calls to
            // TODO (Brett) the task manager can be uniform. However,
            // TODO (Brett) that takes too much state passing for now. We
            // TODO (Brett) should think about this again.
            // TODO (Brett) Task chaining would be a slick way to do this.

            // TODO (Brett) Split this into GenerateShaderCode and CreateShaderAsset
            if (previewToUpdate == m_MainPreviewData) // main preview
            {
                previewToUpdate.shader = GetMainPreviewShaderObject();
            }
            else // node preview
            {
                var nodeReader = m_GraphHandle.GetNode(previewToUpdate.nodeName);
                previewToUpdate.shader = GetNodeShaderObject(nodeReader, m_NodePreviewTarget);
            }

            // create the material for the preview
            previewToUpdate.material = new Material(previewToUpdate.shader) {
                hideFlags = HideFlags.HideAndDontSave
            };

            // set textures that were setup in GetNodeShaderObject
            foreach ((string textureName, Texture texture) in previewToUpdate.defaultTextures)
            {
                previewToUpdate.material.SetTexture(textureName, texture);
            }

            // compile the passes for the material
            bool prev = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = true;

            // start the compiling for all the passes
            for (var i = 0; i < previewToUpdate.material.passCount; ++i)
            {
                ShaderUtil.CompilePass(previewToUpdate.material, i);
            }
            ShaderUtil.allowAsyncCompilation = prev;

            // add to update checking
            m_PreviewsCompiling.Add(previewToUpdate);

            // mark not dirty
            previewToUpdate.isShaderOutOfDate = false;
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

            // TODO: (Sai) Support for rendering 3D node previews
            // Node previews
            if (previewToUpdate != m_MainPreviewData)
            {
                Mesh renderMesh = previewToUpdate.currentRenderMode is PreviewRenderMode.Preview2D or PreviewRenderMode.Inherit
                    ? m_SceneResources.quad
                    : m_SceneResources.sphere;

                    RenderPreview(
                    previewToUpdate.renderTexture,
                    previewToUpdate.material,
                    renderMesh,
                    Matrix4x4.identity);
            }

            if (previewToUpdate.renderTexture != null)
            {
                previewToUpdate.texture = previewToUpdate.renderTexture;
                previewToUpdate.isRenderOutOfDate = false;
            }

            // Render 3D previews
            m_SceneResources.camera.transform.position = -Vector3.forward * 5;
            m_SceneResources.camera.transform.rotation = Quaternion.identity;
            m_SceneResources.camera.orthographic = false;

            // Main preview
            if (previewToUpdate == m_MainPreviewData)
            {
                var mesh = m_MainPreviewMesh;
                var preventRotation = m_PreventMainPreviewRotation;
                if (!mesh)
                {
                    // TODO: Revisit when GetTargetSettings() is implemented
                    //var useSpritePreview = targetSettings.LastOrDefault(t => t.IsActive())?.prefersSpritePreview ?? false;
                    var useSpritePreview = false;
                    mesh = useSpritePreview ? m_SceneResources.quad : m_SceneResources.sphere;
                    preventRotation = useSpritePreview;
                }

                var previewTransform = preventRotation ? Matrix4x4.identity : Matrix4x4.Rotate(m_MainPreviewRotation);
                var scale = m_MainPreviewScale;
                previewTransform *= Matrix4x4.Scale(scale * Vector3.one * (Vector3.one).magnitude / mesh.bounds.size.magnitude);
                previewTransform *= Matrix4x4.Translate(-mesh.bounds.center);

                RenderPreview(m_MainPreviewData.renderTexture, m_MainPreviewData.material, mesh, previewTransform, false);
                if (m_MainPreviewData.renderTexture != null)
                {
                    m_MainPreviewData.texture = m_MainPreviewData.renderTexture;
                    m_MainPreviewData.isRenderOutOfDate = false;
                }
            }

            m_PreviewUpdateReceiver.UpdatePreviewData(previewToUpdate.nodeName, previewToUpdate.texture);
        }

        private static readonly ProfilerMarker RenderPreviewMarker = new ProfilerMarker("RenderPreview");

        void RenderPreview(RenderTexture renderTarget, Material renderMaterial, Mesh mesh, Matrix4x4 transform, bool isNodePreview = true)
        {
            using (RenderPreviewMarker.Auto())
            {
                var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;

                // TODO: Setting properties on materials that arent supported by the MPB like Virtual Textures etc
                // AssignPerMaterialPreviewProperties(renderData.shaderData.mat, perMaterialPreviewProperties);

                var previousRenderTexture = RenderTexture.active;

                // Temp workaround for alpha previews...
                var temp = RenderTexture.GetTemporary(renderTarget.descriptor);
                RenderTexture.active = temp;
                Graphics.Blit(Texture2D.whiteTexture, temp, m_SceneResources.checkerboardMaterial);

                // TODO: Revisit when GetTargetSettings() is implemented
                // bool isOnlyVFXTarget = m_GraphHandle.GetTargetSettings();

                // Mesh is invalid for VFXTarget
                // TODO: We should handle this more gracefully
                //if (renderData != m_MainPreviewData /*|| !isOnlyVFXTarget*/)
                //{
                    m_SceneResources.camera.targetTexture = temp;
                    Graphics.DrawMesh(mesh, transform, renderMaterial, 1, m_SceneResources.camera, 0, m_PreviewMaterialPropertyBlock, ShadowCastingMode.Off, false, null, false);

                //}

                var previousUseSRP = Unsupported.useScriptableRenderPipeline;
                // we seem to be using SRP for all renders now
                Unsupported.useScriptableRenderPipeline = true;
                m_SceneResources.camera.Render();
                Unsupported.useScriptableRenderPipeline = previousUseSRP;

                Graphics.Blit(temp, renderTarget, m_SceneResources.blitNoAlphaMaterial);

                RenderTexture.ReleaseTemporary(temp);

                RenderTexture.active = previousRenderTexture;

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

        PortHandler Mock_GetPortReaderForProperty(string nodeName, string propertyName)
        {
            var nodeReader = m_GraphHandle.GetNode(nodeName);
            return nodeReader.GetPort(propertyName);
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

        void SetValueOnMaterialPropertyBlock(MaterialPropertyBlock materialPropertyBlock, string propertyName, object propertyValue, PortHandler portReader = null)
        {

        }
    }
}
