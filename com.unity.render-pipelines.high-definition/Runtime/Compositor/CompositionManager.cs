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
                        if (layer.camera)
                        {
                            layer.camera.enabled = value;
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
                if (m_InputLayers.Count > 0)
                {
                    return m_InputLayers[0].aspectRatio;
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

        #region Validation
        public void ValidateLayerListOrder(int oldIndex, int newIndex)
        {
            if (m_InputLayers.Count > 1)
            {
                if (m_InputLayers[0].outputTarget == CompositorLayer.OutputTarget.CameraStack)
                {
                    var tmp = m_InputLayers[newIndex];
                    m_InputLayers.RemoveAt(newIndex);
                    m_InputLayers.Insert(oldIndex, tmp);
                }
            }
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
                    Debug.Log("Registering chroma keying pass for the HDRP pipeline");
                    hdPipeline.asset.beforePostProcessCustomPostProcesses.Add(typeof(ChromaKeying).AssemblyQualifiedName);
                }

                indx = hdPipeline.asset.beforePostProcessCustomPostProcesses.FindIndex(x => x == typeof(AlphaInjection).AssemblyQualifiedName);
                if (indx < 0)
                {
                    Debug.Log("Registering alpha injection pass for the HDRP pipeline");
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
                Debug.Log("No camera was found");
                return false;
            }

            if (m_Shader == null)
            {
                Debug.Log("The composition shader graph must be set");
                return false;
            }

            if (m_CompositionProfile == null)
            {
                Debug.Log("The composition profile was not set at runtime");
                return false;
            }

            if (m_Material == null)
            {
                Debug.Log("The composition material was Null");
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
            // delete the layer from last to first, in order to release first the camera and then the associated RT
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
                if (m_Material == null)
                {
                    m_Material = new Material(m_Shader);
                }

                m_CompositionProfile.AddPropertiesFromShaderAndMaterial(this, m_Shader, m_Material);
            }
            else
            {
                Debug.LogError("Cannot find the default composition graph. Was the installation folder corrupted?");
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
            var cameraData = m_OutputCamera.GetComponent<HDAdditionalCameraData>();
            if (cameraData && !cameraData.hasCustomRender)
            {
                cameraData.customRender += CustomRender;
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
            // We need to destroy the layers from last to first, to avoid releasing a RT that is used by a camera
            for (int i = m_InputLayers.Count - 1; i >= 0; --i)
            {
                m_InputLayers[i].Destroy();
            }

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

        public void AddNewLayer(int index, CompositorLayer.LayerType type = CompositorLayer.LayerType.Camera)
        {
            var newLayer = CompositorLayer.CreateStackLayer(type, "New SubLayer");
            
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

        void CustomRender(ScriptableRenderContext context, HDCamera camera)
        {
            if (camera == null || camera.camera == null || m_Material == null)
                return;

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
                cmd.Blit(null, BuiltinRenderTextureType.CurrentActive, m_Material, m_Material.FindPass("ForwardOnly"));
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        static public Camera GetSceceCamera()
        {
            if (Camera.main != null)
            {
                return Camera.main;
            }
            foreach (var camera in Camera.allCameras)
            {
                if (camera.name != "MainCompositorCamera")
                {
                    return camera;
                }
            }
            Debug.LogWarning("Camera not found");
            return null;
        }

        static public CompositionManager GetInstance() =>
            s_CompositorInstance ?? (s_CompositorInstance = GameObject.FindObjectOfType(typeof(CompositionManager), true) as CompositionManager);

    }
}
