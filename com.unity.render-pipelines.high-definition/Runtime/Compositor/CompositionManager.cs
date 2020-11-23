using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Video;

using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    // The main entry point for the compositing operations. Manages the list of layers, output displays, etc.
    [ExecuteAlways]
    internal class CompositionManager : MonoBehaviour
    {
        public enum OutputDisplay
        {
            Display1 = 0,
            Display2,
            Display3,
            Display4,
            Display5,
            Display6,
            Display7,
            Display8
        }

        public enum AlphaChannelSupport
        {
            None = 0,
            Rendering,
            RenderingAndPostProcessing
        }

        [SerializeField] Material m_Material;

        [SerializeField] OutputDisplay m_OutputDisplay = OutputDisplay.Display1;

        public List<CompositorLayer> layers => m_InputLayers;

        [SerializeField] List<CompositorLayer> m_InputLayers = new List<CompositorLayer>();

        public AlphaChannelSupport alphaSupport => m_AlphaSupport;

        internal AlphaChannelSupport m_AlphaSupport = AlphaChannelSupport.RenderingAndPostProcessing;

        internal float timeSinceLastRepaint;

        public bool enableOutput
        {
            get
            {
                if (m_OutputCamera)
                {
                    return m_OutputCamera.enabled;
                }
                return false;
            }
            set
            {
                if (m_OutputCamera)
                {
                    m_OutputCamera.enabled = value;

                    // also change the layers
                    foreach(var layer in m_InputLayers)
                    {
                        if (layer.camera && layer.isUsingACameraClone)
                        {
                            layer.camera.enabled = value;
                        }
                        else
                        {
                            // The target texture was managed by the compositor, reset it so the user can se the camera output
                            if (layer.camera && value == false)
                                layer.camera.targetTexture = null;
                        }
                    }

                }
            }
        }

        public int numLayers => m_InputLayers.Count;

        public Shader shader
        {
            get => m_Shader;
            set
            {
                m_Shader = value;
            }
        }

        [SerializeField] Shader m_Shader;

        public CompositionProfile profile
        {
            get => m_CompositionProfile;
            set => m_CompositionProfile = value;
        }

        [HideInInspector, SerializeField] CompositionProfile m_CompositionProfile;
        public Camera outputCamera
        {
            get => m_OutputCamera;
            set => m_OutputCamera = value;
        }

        [SerializeField] Camera m_OutputCamera;

        public float aspectRatio
        {
            get
            {
                if (m_OutputCamera)
                {
                    return (float)m_OutputCamera.pixelWidth / m_OutputCamera.pixelHeight;
                }
                return 1.0f;
            }
        }

        public bool shaderPropertiesAreDirty
        {
            set
            {
                m_ShaderPropertiesAreDirty = true;
            }
        }

        internal bool m_ShaderPropertiesAreDirty = false;

        internal Matrix4x4 m_ViewProjMatrix;
        internal Matrix4x4 m_ViewProjMatrixFlipped;
        internal GameObject m_CompositorGameObject;

        ShaderVariablesGlobal m_ShaderVariablesGlobalCB = new ShaderVariablesGlobal();

        static private CompositionManager s_CompositorInstance;

        // Built-in Color.black has an alpha of 1, so defien here a fully transparent black 
        static Color s_TransparentBlack = new Color(0, 0, 0, 0); 

        #region Validation
        public bool ValidateLayerListOrder(int oldIndex, int newIndex)
        {
            if (m_InputLayers.Count > 1)
            {
                if (m_InputLayers[0].outputTarget == CompositorLayer.OutputTarget.CameraStack)
                {
                    var tmp = m_InputLayers[newIndex];
                    m_InputLayers.RemoveAt(newIndex);
                    m_InputLayers.Insert(oldIndex, tmp);
                    return false;
                }
            }
            return true;
        }

        public bool RuntimeCheck()
        {
            for (int i = 0; i < m_InputLayers.Count; ++i)
            {
                if (!m_InputLayers[i].Validate())
                {
                    return false;
                }
            }
            return true;
        }


        // Validates the rendering pipeline and fixes potential issues
        bool ValidatePipeline()
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                m_AlphaSupport = AlphaChannelSupport.RenderingAndPostProcessing;
                if (hdPipeline.asset.currentPlatformRenderPipelineSettings.colorBufferFormat == RenderPipelineSettings.ColorBufferFormat.R11G11B10)
                {
                    m_AlphaSupport = AlphaChannelSupport.None;
                }
                else if (hdPipeline.asset.currentPlatformRenderPipelineSettings.postProcessSettings.bufferFormat == PostProcessBufferFormat.R11G11B10)
                {
                    m_AlphaSupport = AlphaChannelSupport.Rendering;
                }

                int indx = hdPipeline.asset.beforePostProcessCustomPostProcesses.FindIndex(x => x == typeof(ChromaKeying).AssemblyQualifiedName);
                if (indx < 0)
                {
                    //Debug.Log("Registering chroma keying pass for the HDRP pipeline");
                    hdPipeline.asset.beforePostProcessCustomPostProcesses.Add(typeof(ChromaKeying).AssemblyQualifiedName);
                }

                indx = hdPipeline.asset.beforePostProcessCustomPostProcesses.FindIndex(x => x == typeof(AlphaInjection).AssemblyQualifiedName);
                if (indx < 0)
                {
                    //Debug.Log("Registering alpha injection pass for the HDRP pipeline");
                    hdPipeline.asset.beforePostProcessCustomPostProcesses.Add(typeof(AlphaInjection).AssemblyQualifiedName);
                }
                return true;
            }
            return false;
        }

        bool ValidateCompositionShader()
        {
            if (m_Shader == null)
            {
                return false;
            }

            if (m_CompositionProfile == null)
            {
                Debug.Log("A composition profile was not found. Set the composition graph from the Compositor window to create one.");
                return false;
            }

            return true;
        }

        bool ValidateProfile()
        {
            if (m_CompositionProfile)
            {
                return true;
            }
            else
            {
                Debug.LogError("No composition profile was found! Use the compositor tool to create one.");
                return false;
            }
        }

        bool ValidateMainCompositorCamera()
        {
            if (m_OutputCamera == null)
            {
                return false;
            }

            var cameraData = m_OutputCamera.GetComponent<HDAdditionalCameraData>();
            if (cameraData == null)
            {
                m_OutputCamera.gameObject.AddComponent(typeof(HDAdditionalCameraData));
                cameraData = m_OutputCamera.GetComponent<HDAdditionalCameraData>();
            }

            // Setup custom rendering (we don't want HDRP to compute anything in this camera)
            if (cameraData)
            {
                cameraData.customRender += CustomRender;
            }
            else
            {
                Debug.Log("Null additional data in compositor output");
            }
            return true;
        }

        bool ValidateAndFixRuntime()
        {
            if (m_OutputCamera == null)
            {
                return false;
            }

            if (m_Shader == null)
            {
                m_InputLayers.Clear();
                m_CompositionProfile = null;
                return false;
            }

            if (m_CompositionProfile == null)
            {
                return false;
            }

            if (m_Material == null)
            {
                SetupCompositionMaterial();
            }

            var cameraData = m_OutputCamera.GetComponent<HDAdditionalCameraData>();
            if (cameraData && !cameraData.hasCustomRender)
            {
                cameraData.customRender += CustomRender;
            }

            return true;
        }
        #endregion

        // This is called when we change camera, to remove the custom draw callback from the old camera before we set the new one
        public void DropCompositorCamera()
        {
            if (m_OutputCamera)
            {
                var cameraData = m_OutputCamera.GetComponent<HDAdditionalCameraData>();
                if (cameraData && cameraData.hasCustomRender)
                {
                    cameraData.customRender -= CustomRender;
                }
            }
        }

        public void Init()
        {
            if (ValidateCompositionShader() && ValidateProfile() && ValidateMainCompositorCamera())
            {
                UpdateDisplayNumber();

                SetupCompositionMaterial();

                SetupCompositorLayers();

                SetupGlobalCompositorVolume();

                SetupCompositorConstants();

                SetupLayerPriorities();
            }
            else
            {
                Debug.LogError("The compositor was disabled due to a validation error in the configuration.");
                enabled = false;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        void OnValidate()
        {
            if (shader == null)
            {
                m_InputLayers.Clear();
                m_CompositionProfile = null;
            }
        }

        public void OnEnable()
        {
            enableOutput = true;
            s_CompositorInstance = null;
#if UNITY_EDITOR
            //This is a work-around, to make edit and continue work when editing source code
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
#endif
        }

        public void DeleteLayerRTs()
        {
            // Delete all layer cameras first, and then the Render Targets (to avoid deleting RT that are still in use)
            for (int i = m_InputLayers.Count - 1; i >= 0; --i)
            {
                m_InputLayers[i].DestroyCameras();
            }
            
            for (int i = m_InputLayers.Count - 1; i >= 0; --i)
            {
                m_InputLayers[i].DestroyRT();
            }
        }

        public bool IsOutputLayer(int layerID)
        {
            if (layerID >= 0 && layerID < m_InputLayers.Count)
            {
                if (m_InputLayers[layerID].outputTarget == CompositorLayer.OutputTarget.CameraStack)
                {
                    return false;
                }
            }
            return true;
        }

        public void UpdateDisplayNumber()
        {
            if (m_OutputCamera)
            {
                m_OutputCamera.targetDisplay = (int)m_OutputDisplay;
            }
        }

        void SetupCompositorLayers()
        {
            for (int i = 0; i < m_InputLayers.Count; ++i)
            {
                m_InputLayers[i].Init($"Layer{i}");
            }

            SetLayerRenderTargets();
        }

        public void SetNewCompositionShader()
        {
            // When we load a new shader, we need to clear the serialized material. 
            m_Material = null;
            SetupCompositionMaterial();
        }

        public void SetupCompositionMaterial()
        {
            // Create the composition material
            if (m_Shader)
            {
                m_Material = new Material(m_Shader);

                m_CompositionProfile.AddPropertiesFromShaderAndMaterial(this, m_Shader, m_Material);

                // [case 1265631] The profile asset is auto-generated by the compositor, so do not allow the users to manually edit/reset the values in the asset because it might break things
                m_CompositionProfile.hideFlags = HideFlags.NotEditable;
            }
            else
            {
                m_CompositionProfile = null;
                m_Material = null;
            }
        }

        public void SetupLayerPriorities()
        {
            int count = 0;
            foreach (var layer in m_InputLayers)
            {
                // Set camera priority (camera's at the beginning of the list should be rendered first)
                layer.SetPriotiry(count * 1.0f);
                count++;
            }
        }

        public void OnAfterAssemblyReload()
        {
            // Bug? : After assembly reload, the customRender callback is dropped, so set it again
            if (m_OutputCamera)
            {
                var cameraData = m_OutputCamera.GetComponent<HDAdditionalCameraData>();
                if (cameraData && !cameraData.hasCustomRender)
                {
                    cameraData.customRender += CustomRender;
                }
            }
        }

        public void OnDisable()
        {
            enableOutput = false;
        }

        // Setup a global volume used for chroma keying, alpha injection etc
        void SetupGlobalCompositorVolume()
        {
            var compositorVolumes = Resources.FindObjectsOfTypeAll(typeof(CustomPassVolume));
            foreach (CustomPassVolume volume in compositorVolumes)
            {
                if(volume.isGlobal && volume.injectionPoint == CustomPassInjectionPoint.BeforeRendering)
                {
                    Debug.LogWarning($"A custom volume pass with name ${volume.name} was already registered on the BeforeRendering injection point.");
                }
            }

            // Instead of using one volume per layer/camera, we setup one global volume and we store the data in the camera
            // This way the compositor has to use only one layer/volume for N cameras (instead of N).
            m_CompositorGameObject = new GameObject("Global Composition Volume") { hideFlags = HideFlags.HideAndDontSave };
            Volume globalPPVolume = m_CompositorGameObject.AddComponent<Volume>();
            globalPPVolume.gameObject.layer = 31;
            AlphaInjection injectAlphaNode = globalPPVolume.profile.Add<AlphaInjection>();
            ChromaKeying chromaKeyingPass = globalPPVolume.profile.Add<ChromaKeying>();
            chromaKeyingPass.activate.Override(true);

            // Custom pass for "Clear to Texture"
            CustomPassVolume globalCustomPassVolume = m_CompositorGameObject.AddComponent<CustomPassVolume>();
            globalCustomPassVolume.injectionPoint = CustomPassInjectionPoint.BeforeRendering;
            globalCustomPassVolume.AddPassOfType(typeof(CustomClear));
        }

        void SetupCompositorConstants()
        {
            m_ViewProjMatrix = Matrix4x4.Scale(new Vector3(2.0f, 2.0f, 0.0f)) * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0.0f));
            m_ViewProjMatrixFlipped = Matrix4x4.Scale(new Vector3(2.0f, -2.0f, 0.0f)) * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0.0f));
        }

        public void UpdateLayerSetup()
        {
            SetupCompositorLayers();
            SetupLayerPriorities();
        }

        // Update is called once per frame
        void Update()
        {
            // TODO: move all validation calls to onValidate. Before doing it, this needs some extra testing to ensure nothing breaks
            if (ValidatePipeline() == false || ValidateAndFixRuntime() == false || RuntimeCheck() == false)
            {
                return;
            }

            UpdateDisplayNumber();

#if UNITY_EDITOR
            if (m_ShaderPropertiesAreDirty)
            {
                SetNewCompositionShader();
                m_ShaderPropertiesAreDirty = false;
                SetupCompositorLayers();//< required to allocate RT for the new layers
            }

            // Detect runtime resolution change
            foreach (var layer in m_InputLayers)
            {
                if (!layer.ValidateRTSize(m_OutputCamera.pixelWidth, m_OutputCamera.pixelHeight))
                {
                    DeleteLayerRTs();
                    SetupCompositorLayers();
                    break;
                }
            }
#endif

            if (m_CompositionProfile)
            {
                foreach (var layer in m_InputLayers)
                {
                    layer.Update();
                }
                SetLayerRenderTargets();
            }
        }

        void OnDestroy()
        {
            DeleteLayerRTs();

            if (m_CompositorGameObject != null)
            {
                CoreUtils.Destroy(m_CompositorGameObject);
                m_CompositorGameObject = null;
            }

            var compositorVolumes = Resources.FindObjectsOfTypeAll(typeof(CustomPassVolume));
            foreach (CustomPassVolume volume in compositorVolumes)
            {
                if (volume.name == "Global Composition Volume" && volume.injectionPoint == CustomPassInjectionPoint.BeforeRendering)
                {
                    CoreUtils.Destroy(volume);
                }
            }
        }

        public void AddInputFilterAtLayer(CompositionFilter filter, int index)
        {
            m_InputLayers[index].AddInputFilter(filter);
        }

        int GetBaseLayerForSubLayerAtIndex(int index)
        {
            int baseIndex = 0;
            index = (index > m_InputLayers.Count - 1) ? m_InputLayers.Count - 1 : index;
            for (int i = index; i >= 0; --i)
            {
                if (m_InputLayers[i].outputTarget == CompositorLayer.OutputTarget.CompositorLayer)
                {
                    baseIndex = i;
                    break;
                }
            }
            return baseIndex;
        }

        static string GetSubLayerName(int count)
        {
            if (count == 0)
            {
                return "New SubLayer";
            }
            else
            {
                return $"New SubLayer ({count + 1})";
            }
        }

        public string GetNewSubLayerName(int index, CompositorLayer.LayerType type = CompositorLayer.LayerType.Camera)
        {
            // First find the base layer
            int baseIndex = GetBaseLayerForSubLayerAtIndex(index - 1);

            // Get a candidate name and check if it already exists
            int count = 0;
            string candidateName = GetSubLayerName(count);
            int i = baseIndex + 1;
            while (i < m_InputLayers.Count && m_InputLayers[i].outputTarget != CompositorLayer.OutputTarget.CompositorLayer)
            {
                if (m_InputLayers[i].name == candidateName)
                {
                    // If this candidate name exists, get the next one and start again
                    candidateName = GetSubLayerName(++count);
                    i = baseIndex + 1;
                }
                else
                {
                    ++i;
                }
            }

            return candidateName;
        }

        public void AddNewLayer(int index, CompositorLayer.LayerType type = CompositorLayer.LayerType.Camera)
        {
            var newLayer = CompositorLayer.CreateStackLayer(type, GetNewSubLayerName(index, type));
            
            if (index >= 0 && index < m_InputLayers.Count)
            {
                m_InputLayers.Insert(index, newLayer);
            }
            else
            {
                m_InputLayers.Add(newLayer);
            }
        }

        int GetNumChildrenForLayerAtIndex(int indx)
        {
            if (m_InputLayers[indx].outputTarget == CompositorLayer.OutputTarget.CameraStack)
            {
                return 0;
            }

            int num = 0;
            for (int i = indx + 1; i < m_InputLayers.Count; ++i)
            {
                if (m_InputLayers[i].outputTarget == CompositorLayer.OutputTarget.CameraStack)
                {
                    num++;
                }
                else
                {
                    break;
                }
            }
            return num;
        }

        public void RemoveLayerAtIndex(int indx)
        {
            Debug.Assert(indx >= 0 && indx < m_InputLayers.Count);

            int numChildren = GetNumChildrenForLayerAtIndex(indx);
            for (int i = numChildren; i >= 0; --i)
            {
                m_InputLayers[indx + i].Destroy();
                m_InputLayers.RemoveAt(indx + i);
            }
        }

        public void SetLayerRenderTargets()
        {
            int layerPositionInStack = 0;
            CompositorLayer lastLayer = null;
            for (int i = 0; i < m_InputLayers.Count; ++i)
            {
                if (m_InputLayers[i].outputTarget != CompositorLayer.OutputTarget.CameraStack)
                {
                    lastLayer = m_InputLayers[i];

                    // [case 1265061] If this layer does not have any cameras that will clear/draw the background, set a flag so the compositor will clear it explicitly.
                    m_InputLayers[i].clearsBackGround =
                        (i + 1 < m_InputLayers.Count) ? (m_InputLayers[i + 1].outputTarget == CompositorLayer.OutputTarget.CompositorLayer) : true;
                }

                if (m_InputLayers[i].outputTarget == CompositorLayer.OutputTarget.CameraStack && i > 0)
                {
                    m_InputLayers[i].SetupLayerCamera(lastLayer, layerPositionInStack);

                    // Corner case: If the first layer in a camera stack was disabled, then it should still clear the color buffer
                    if (!m_InputLayers[i].enabled && layerPositionInStack == 0)
                    {
                        m_InputLayers[i].SetupClearColor();
                    }
                    layerPositionInStack++;
                }
                else
                {
                    layerPositionInStack = 0;
                }
            }
        }

        public void ReorderChildren(int oldIndex, int newIndex)
        {
            if (m_InputLayers[newIndex].outputTarget == CompositorLayer.OutputTarget.CompositorLayer)
            {
                if (oldIndex > newIndex)
                {
                    for (int i = 1; oldIndex + i < m_InputLayers.Count; ++i)
                    {
                        if (m_InputLayers[oldIndex + i].outputTarget == CompositorLayer.OutputTarget.CameraStack)
                        {
                            var tmp = m_InputLayers[oldIndex + i];
                            m_InputLayers.RemoveAt(oldIndex + i);
                            m_InputLayers.Insert(newIndex + i, tmp);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    while (m_InputLayers[oldIndex].outputTarget == CompositorLayer.OutputTarget.CameraStack)
                    {
                        var tmp = m_InputLayers[oldIndex];
                        m_InputLayers.RemoveAt(oldIndex);
                        m_InputLayers.Insert(newIndex, tmp);
                    }
                }
            }
        }

        public RenderTexture GetRenderTarget(int indx)
        {
            if (indx >= 0 && indx < m_InputLayers.Count)
            {
                return m_InputLayers[indx].GetRenderTarget(true, true);
            }
            return null;
        }

        public void Repaint()
        {
            for (int indx = 0; indx < m_InputLayers.Count; indx++)
            {
                if (m_InputLayers[indx].camera)
                    m_InputLayers[indx].camera.Render();
            }
        }

        void CustomRender(ScriptableRenderContext context, HDCamera camera)
        {
            if (camera == null || camera.camera == null || m_Material == null || m_Shader == null)
            {
                // If something is wrong, don't keep the previous image (clear to black) to avoid confusion
                var cmdbuff = CommandBufferPool.Get("Compositor Blit");
                cmdbuff.ClearRenderTarget(false, true, Color.black);
                return;
            }
                

            timeSinceLastRepaint = 0;

            // set shader uniforms
            m_CompositionProfile.CopyPropertiesToMaterial(m_Material);

            int layerIndex = 0;
            foreach (var layer in m_InputLayers)
            {
                if (layer.outputTarget != CompositorLayer.OutputTarget.CameraStack)  // stacked cameras are not exposed as compositor layers 
                {
                    m_Material.SetTexture(layer.name, layer.GetRenderTarget(), RenderTextureSubElement.Color);
                }
                layerIndex++;
            }

            // Blit command
            var cmd = CommandBufferPool.Get("Compositor Blit");
            {
                // fill the camera-related entries in the global constant buffer
                // (Note: we later patch the position/_ViewProjMatrix values in order to perform a full screen blit with a SG Unlit material)
                camera.UpdateShaderVariablesGlobalCB(ref m_ShaderVariablesGlobalCB, 0);

                m_ShaderVariablesGlobalCB._WorldSpaceCameraPos_Internal = new Vector3(0.0f, 0.0f, 0.0f);
                cmd.SetViewport(new Rect(0, 0, camera.camera.pixelWidth, camera.camera.pixelHeight));
                cmd.ClearRenderTarget(true, false, Color.red);

                foreach (var layer in m_InputLayers)
                {
                    if (layer.clearsBackGround)
                    {
                        cmd.SetRenderTarget(layer.GetRenderTarget());
                        cmd.ClearRenderTarget(false, true, s_TransparentBlack);
                    }
                }
            }

            if (camera.camera.targetTexture)
            {
                m_ShaderVariablesGlobalCB._ViewProjMatrix = m_ViewProjMatrixFlipped;
                ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
                cmd.Blit(null, camera.camera.targetTexture, m_Material, m_Material.FindPass("ForwardOnly"));
            }
            else
            {
                m_ShaderVariablesGlobalCB._ViewProjMatrix = m_ViewProjMatrix;
                ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
                cmd.Blit(null, BuiltinRenderTextureType.CameraTarget, m_Material, m_Material.FindPass("ForwardOnly"));
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// Helper function that indicates if a camera is shared between multiple layers
        /// </summary>
        /// <param name="camera">The input camera</param>
        /// <returns>Returns true if this camera is used to render in more than one layer</returns>
        internal bool IsThisCameraShared(Camera camera)
        {
            if (camera == null)
            {
                return false;
            }

            int count = 0;
            foreach (var layer in m_InputLayers)
            {
                
                if (layer.outputTarget == CompositorLayer.OutputTarget.CameraStack &&
                    camera.Equals(layer.sourceCamera))
                {
                    count++;
                }
            }
            // If we found the camera in more than one layer then it is shared between layers
            return count > 1;
        }

        static public Camera GetSceneCamera()
        {
            if (Camera.main != null)
            {
                return Camera.main;
            }
            foreach (var camera in Camera.allCameras)
            {
                if (camera != CompositionManager.GetInstance().outputCamera)
                {
                    return camera;
                }
            }

            return null;
        }

        static public Camera CreateCamera(string cameraName)
        {
            var newCameraGameObject = new GameObject(cameraName)
            {
                hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy | HideFlags.HideAndDontSave
            };
            var newCamera = newCameraGameObject.AddComponent<Camera>();
            newCameraGameObject.AddComponent<HDAdditionalCameraData>();

            return newCamera;
        }

        static public CompositionManager GetInstance() =>
            s_CompositorInstance ?? (s_CompositorInstance = GameObject.FindObjectOfType(typeof(CompositionManager), true) as CompositionManager);

        static public Vector4 GetAlphaScaleAndBiasForCamera(HDCamera hdCamera)
        {
            AdditionalCompositorData compositorData = null;
            hdCamera.camera.TryGetComponent<AdditionalCompositorData>(out compositorData);

            if (compositorData)
            {
                float alphaMin = compositorData.alphaMin;
                float alphaMax = compositorData.alphaMax;

                if (alphaMax == alphaMin)
                    alphaMax += 0.0001f; // Mathf.Epsilon is too small and in this case it creates precission issues 

                float alphaScale = 1.0f / (alphaMax - alphaMin);
                float alphaBias = -alphaMin * alphaScale;

                return new Vector4(alphaScale, alphaBias, 0.0f, 0.0f);
            }

            // No compositor-specific data for this camera/layer, just return the default/neutral scale and bias
            return new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
        }

        /// <summary>
        /// For stacked cameras, returns the color buffer that will be used to draw on top
        /// </summary>
        /// <param name="hdCamera">The input camera</param>
        /// <returns> The color buffer that will be used to draw on top, or null if not a stacked camera </returns>
        static internal Texture GetClearTextureForStackedCamera(HDCamera hdCamera)
        {
            AdditionalCompositorData compositorData = null;
            hdCamera.camera.TryGetComponent<AdditionalCompositorData>(out compositorData);

            if (compositorData)
            {
                return compositorData.clearColorTexture;
            }
            return null;
        }

        /// <summary>
        /// For stacked cameras, returns the depth buffer that will be used to draw on top
        /// </summary>
        /// <param name="hdCamera">The input camera</param>
        /// <returns> The depth buffer that will be used to draw on top, or null if not a stacked camera </returns>
        static internal RenderTexture GetClearDepthForStackedCamera(HDCamera hdCamera)
        {
            AdditionalCompositorData compositorData = null;
            hdCamera.camera.TryGetComponent<AdditionalCompositorData>(out compositorData);

            if (compositorData)
            {
                return compositorData.clearDepthTexture;
            }
            return null;
        }

    }
}
