using System.Collections;
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
    // Defines a single compositor layer and it's runtime properties. These are scene specific and are not saved in the asset file.
    [System.Serializable]
    internal class CompositorLayer
    {
        public enum LayerType
        {
            Camera = 0,
            Video = 1,
            Image = 2
        };

        // The graphics format options exposed in the UI
        public enum UIColorBufferFormat
        {
            R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,
            R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat,
            R32G32B32A32 = GraphicsFormat.R32G32B32A32_SFloat
        };

        // Specifies if this layer will be used in the compositor or a camera stack
        public enum OutputTarget
        {
            CompositorLayer = 0,
            CameraStack
        }

        public enum ResolutionScale
        {
            Full = 1,
            Half = 2,
            Quarter = 4
        }
        public string name => m_LayerName;

        [SerializeField] string m_LayerName;

        public OutputTarget outputTarget => m_OutputTarget;

        [SerializeField] OutputTarget m_OutputTarget; // Specifies if this layer will be used in the compositor or a camera stack
        [SerializeField] bool m_ClearDepth = false;   // Specifies if the depth will be cleared when stacking this camera over the previous one (for overlays)
        [SerializeField] bool m_ClearAlpha = true;    // Specifies if the Alpha channel will be cleared when stacking this camera over the previous one (for overlays)
        [SerializeField] Renderer m_OutputRenderer = null; // Specifies the output surface/renderer
        [SerializeField] LayerType m_Type;

        public Camera sourceCamera => m_Camera;
        [SerializeField] Camera m_Camera = null;      // The source camera for the layer (were we get the default properties). The actual rendering, with overridden properties is done by the m_LayerCamera
        [SerializeField] VideoPlayer m_InputVideo = null;
        [SerializeField] Texture m_InputTexture = null;
        [SerializeField] BackgroundFitMode m_BackgroundFit = BackgroundFitMode.Stretch;
        [SerializeField] ResolutionScale m_ResolutionScale = ResolutionScale.Full;
        [SerializeField] UIColorBufferFormat m_ColorBufferFormat = UIColorBufferFormat.R16G16B16A16;

        // Layer overrides
        [SerializeField] bool m_OverrideAntialiasing = false;
        [SerializeField] HDAdditionalCameraData.AntialiasingMode m_Antialiasing;

        [SerializeField] bool m_OverrideClearMode = false;
        [SerializeField] HDAdditionalCameraData.ClearColorMode m_ClearMode = HDAdditionalCameraData.ClearColorMode.Color;

        [SerializeField] bool m_OverrideCullingMask = false;
        [SerializeField] LayerMask m_CullingMask;

        [SerializeField] bool m_OverrideVolumeMask = false;
        [SerializeField] LayerMask m_VolumeMask;

        public bool hasLayerOverrides => m_OverrideAntialiasing || m_OverrideCullingMask || m_OverrideVolumeMask || m_OverrideClearMode;

        [SerializeField] int m_LayerPositionInStack = 0;

        // Layer filters
        [SerializeField] List<CompositionFilter> m_InputFilters = new List<CompositionFilter>();

        // AOVs
        [SerializeField] MaterialSharedProperty m_AOVBitmask = 0;

        [SerializeField] Dictionary<string, int> m_AOVMap;

        List<RTHandle> m_AOVHandles;

        [SerializeField] List<RenderTexture> m_AOVRenderTargets;

        RTHandle m_RTHandle;

        [SerializeField] RenderTexture m_RenderTarget;

        [SerializeField] RTHandle m_AOVTmpRTHandle;

        public bool clearsBackGround
        {
            get => m_ClearsBackGround;
            set => m_ClearsBackGround = value;
        }

        [SerializeField] bool m_ClearsBackGround = false;

        static readonly string[] k_AOVNames = System.Enum.GetNames(typeof(MaterialSharedProperty));

        public bool enabled
        {
            get => m_Show;
            set
            {
                m_Show = value;
            }
        }

        [SerializeField] bool m_Show = true;          // Used to toggle visibility of layers

        public float aspectRatio
        {
            get
            {
                var compositor = CompositionManager.GetInstance();
                if (compositor != null && compositor.outputCamera != null)
                {
                    return (float)compositor.outputCamera.pixelWidth / compositor.outputCamera.pixelHeight;
                }
                return 1.0f;
            }
        }

        public Camera camera => m_LayerCamera;

        [SerializeField] Camera m_LayerCamera;

        // Returns true if this layer is using a camera that was cloned internally for drawing
        internal bool isUsingACameraClone => !m_LayerCamera.Equals(m_Camera);

        // The input alpha will be mapped between the min and max range when blending between the post-processed and plain image regions. This way the user can controls how steep is the transition.
        [SerializeField] float m_AlphaMin = 0.0f;
        [SerializeField] float m_AlphaMax = 1.0f;

        private CompositorLayer()
        {
        }

        public static CompositorLayer CreateStackLayer(LayerType type = CompositorLayer.LayerType.Camera, string layerName = "New Layer")
        {
            var newLayer = new CompositorLayer();
            newLayer.m_LayerName = layerName;
            newLayer.m_Type = type;
            newLayer.m_Camera = CompositionManager.GetSceneCamera();
            newLayer.m_CullingMask = newLayer.m_Camera ? newLayer.m_Camera.cullingMask : 0; //LayerMask.GetMask("None");
            newLayer.m_OutputTarget = CompositorLayer.OutputTarget.CameraStack;
            newLayer.m_ClearDepth = true;

            if (newLayer.m_Type == LayerType.Image || newLayer.m_Type == LayerType.Video)
            {
                if (newLayer.m_Camera == null)
                    newLayer.m_Camera = CompositionManager.CreateCamera(layerName);

                // Image and movie layers do not render any 3D objects
                newLayer.m_OverrideCullingMask = true;
                newLayer.m_CullingMask = 0;

                // By default image and movie layers should not get any post-processing
                newLayer.m_OverrideVolumeMask = true;
                newLayer.m_VolumeMask = 0;
                newLayer.m_ClearAlpha = false;
                newLayer.m_OverrideAntialiasing = true;
                newLayer.m_Antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            }

            return newLayer;
        }

        public static CompositorLayer CreateOutputLayer(string layerName)
        {
            var newLayer = new CompositorLayer();
            newLayer.m_LayerName = layerName;
            newLayer.m_OutputTarget = CompositorLayer.OutputTarget.CompositorLayer;

            return newLayer;
        }

        static float EnumToScale(ResolutionScale scale)
        {
            return 1.0f / (int)scale;
        }

        static T AddComponent<T>(GameObject go, bool allowUndo = false) where T : Component
        {
#if UNITY_EDITOR
            if (allowUndo)
                return UnityEditor.Undo.AddComponent<T>(go);
            else
                return go.AddComponent<T>();

#else
            return go.AddComponent<T>();
#endif
        }

        public int pixelWidth
        {
            get
            {
                var compositor = CompositionManager.GetInstance();
                if (compositor && compositor.outputCamera)
                {
                    float resScale = EnumToScale(m_ResolutionScale);
                    return (int)(resScale * compositor.outputCamera.pixelWidth);
                }
                return 0;
            }
        }
        public int pixelHeight
        {
            get
            {
                var compositor = CompositionManager.GetInstance();
                if (compositor && compositor.outputCamera)
                {
                    float resScale = EnumToScale(m_ResolutionScale);
                    return (int)(resScale * compositor.outputCamera.pixelHeight);
                }
                return 0;
            }
        }
        public void Init(string layerID = "", bool allowUndo = false)
        {
            if (m_LayerName == "")
            {
                m_LayerName = layerID;
            }

            var compositor = CompositionManager.GetInstance();

            // Create a new camera if necessary or use the one specified by the user
            if (m_LayerCamera == null && m_OutputTarget == OutputTarget.CameraStack)
            {
                // We do not clone the camera if :
                // - it has no layer overrides
                // - is not shared between layers
                // - is not used in an mage/video layer (in this case the camera is not exposed at all, so it makes sense to let the compositor manage it)
                // - it does not force-clear the RT (the first layer of a stack, even if disabled by the user), still clears the RT
                bool shouldClear = !enabled && m_LayerPositionInStack == 0 && m_Camera;
                bool isImageOrVideo = (m_Type == LayerType.Image || m_Type == LayerType.Video);
                if (!isImageOrVideo && !hasLayerOverrides && !shouldClear && !compositor.IsThisCameraShared(m_Camera))
                {
                    m_LayerCamera = m_Camera;
                }
                else
                {
                    // Clone the camera that was given by the user. We avoid calling Instantiate because we don't want to clone any other children that might be attachen to the camera
                    var newCameraGameObject = new GameObject("Layer " + layerID)
                    {
                        hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy | HideFlags.HideAndDontSave
                    };
                    m_LayerCamera = newCameraGameObject.AddComponent<Camera>();
                    newCameraGameObject.AddComponent<HDAdditionalCameraData>();
                    CopyInternalCameraData();
                    CompositorCameraRegistry.GetInstance().RegisterInternalCamera(m_LayerCamera);

                    m_LayerCamera.name = "Compositor" + layerID;
                    m_LayerCamera.gameObject.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy | HideFlags.HideAndDontSave;
                    if (m_LayerCamera.tag == "MainCamera")
                    {
                        m_LayerCamera.tag = "Untagged";
                    }
                }
            }
            m_ClearsBackGround = false;
            m_LayerPositionInStack = 0; // will be set in SetupLayerCamera

            // Migrate any formats that we don't support anymore (like R16G16B16A16_UNORM)
            if (m_ColorBufferFormat != UIColorBufferFormat.R11G11B10 &&
                m_ColorBufferFormat != UIColorBufferFormat.R16G16B16A16 &&
                m_ColorBufferFormat != UIColorBufferFormat.R32G32B32A32)
            {
                m_ColorBufferFormat = UIColorBufferFormat.R16G16B16A16;
            }

            if (m_OutputTarget != OutputTarget.CameraStack && m_RenderTarget == null)
            {
                // If we don't have a valid camera (zero width or height) avoid creating the RT
                if (compositor.outputCamera.pixelWidth > 0 && compositor.outputCamera.pixelHeight > 0)
                {
                    float resScale = EnumToScale(m_ResolutionScale);
                    int scaledWidth = (int)(resScale * compositor.outputCamera.pixelWidth);
                    int scaledHeight = (int)(resScale * compositor.outputCamera.pixelHeight);
                    m_RenderTarget = new RenderTexture(scaledWidth, scaledHeight, 24, (GraphicsFormat)m_ColorBufferFormat);
                }
            }

            // check and fix RT handle
            if (m_OutputTarget != OutputTarget.CameraStack && m_RTHandle == null && m_RenderTarget != null)
            {
                m_RTHandle = RTHandles.Alloc(m_RenderTarget);
            }

            if (m_OutputTarget != OutputTarget.CameraStack && m_AOVBitmask != MaterialSharedProperty.None)
            {
                int aovMask = (1 << (int)m_AOVBitmask);
                if (aovMask > 1)
                {
                    m_AOVMap = new Dictionary<string, int>();
                    m_AOVRenderTargets = new List<RenderTexture>();
                    m_AOVHandles = new List<RTHandle>();

                    var aovNames = System.Enum.GetNames(typeof(MaterialSharedProperty));
                    int NUM_AOVs = aovNames.Length;
                    int outputIndex = 0;
                    for (int i = 0; i < NUM_AOVs; ++i)
                    {
                        if ((aovMask & (1 << i)) != 0)
                        {
                            m_AOVMap[aovNames[i]] = outputIndex;
                            m_AOVRenderTargets.Add(new RenderTexture(pixelWidth, pixelHeight, 24, (GraphicsFormat)m_ColorBufferFormat));
                            m_AOVHandles.Add(RTHandles.Alloc(m_AOVRenderTargets[outputIndex]));
                            outputIndex++;
                        }
                    }
                }
            }
            else
            {
                if (m_AOVRenderTargets != null)
                {
                    foreach (var rt in m_AOVRenderTargets)
                    {
                        CoreUtils.Destroy(rt);
                    }
                    m_AOVRenderTargets.Clear();
                }
                if (m_AOVMap != null)
                {
                    m_AOVMap.Clear();
                    m_AOVMap = null;
                }
            }

            if (m_OutputRenderer != null && Application.IsPlaying(compositor.gameObject))
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetTexture("_BaseColorMap", m_RenderTarget);
                m_OutputRenderer.SetPropertyBlock(propertyBlock);
            }

            if (m_LayerCamera)
            {
                m_LayerCamera.enabled = m_Show;
                var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>()
                    ?? AddComponent<HDAdditionalCameraData>(m_LayerCamera.gameObject);

                var layerData = m_LayerCamera.GetComponent<AdditionalCompositorData>();
                {
                    // create the component if it is required and does not exist
                    if (layerData == null)
                    {
                        layerData = AddComponent<AdditionalCompositorData>(m_LayerCamera.gameObject, allowUndo);
                        layerData.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
                    }
                    // Reset the layer params (in case we cloned a camera which already had AdditionalCompositorData)
                    if (layerData != null)
                    {
                        layerData.ResetData();
                    }
                }

                // layer overrides
                SetLayerMaskOverrides();

                if (m_Type == LayerType.Video && m_InputVideo != null)
                {
                    m_InputVideo.targetCamera = m_LayerCamera;
                    m_InputVideo.renderMode = VideoRenderMode.CameraNearPlane;
                }
                else if (m_Type == LayerType.Image && m_InputTexture != null)
                {
                    cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;

                    layerData.clearColorTexture = m_InputTexture;
                    layerData.imageFitMode = m_BackgroundFit;
                }

                // Custom pass to inject an alpha mask
                SetAdditionalLayerData();

                if (m_InputFilters == null)
                {
                    m_InputFilters = new List<CompositionFilter>();
                }
            }
        }

        public bool Validate()
        {
            if ((m_OutputTarget != OutputTarget.CameraStack && m_RenderTarget == null)
                || (m_OutputTarget != OutputTarget.CameraStack && m_RTHandle == null))
            {
                Init();
            }

            if (m_OutputTarget == OutputTarget.CameraStack && m_LayerCamera == null)
            {
                Init();
            }

            return true;
        }

        public void DestroyCameras()
        {
            // We should destroy the layer camera only if it was cloned
            if (m_LayerCamera != null)
            {
                if (isUsingACameraClone)
                {
                    var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
                    if (cameraData)
                    {
                        CoreUtils.Destroy(cameraData);
                    }
                    m_LayerCamera.targetTexture = null;
                    CompositorCameraRegistry.GetInstance().UnregisterInternalCamera(m_LayerCamera);
                    CoreUtils.Destroy(m_LayerCamera);
                    m_LayerCamera = null;
                }
                else
                {
                    m_LayerCamera.targetTexture = null;
                    m_LayerCamera = null;
                }
            }
        }

        public void DestroyRT()
        {
            if (m_RTHandle != null)
            {
                RTHandles.Release(m_RTHandle);
                m_RTHandle = null;
            }

            if (m_RenderTarget != null)
            {
                CoreUtils.Destroy(m_RenderTarget);
                m_RenderTarget = null;
            }

            if (m_AOVHandles != null)
            {
                foreach (var handle in m_AOVHandles)
                {
                    handle.Release();
                }
            }
            if (m_AOVRenderTargets != null)
            {
                foreach (var rt in m_AOVRenderTargets)
                {
                    CoreUtils.Destroy(rt);
                }
            }
            m_AOVMap?.Clear();
            m_AOVMap = null;
        }

        public void Destroy()
        {
            DestroyCameras();
            DestroyRT();
        }

        public void SetLayerMaskOverrides()
        {
            if (m_OverrideCullingMask && m_LayerCamera)
            {
                m_LayerCamera.cullingMask = m_ClearsBackGround ? (LayerMask)0 : m_CullingMask;
            }

            if (m_LayerCamera)
            {
                var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
                if (cameraData)
                {
                    if (m_OverrideVolumeMask && m_LayerCamera)
                    {
                        cameraData.volumeLayerMask = m_VolumeMask;
                    }
                    cameraData.volumeLayerMask |= 1 << 31;

                    if (m_OverrideAntialiasing)
                    {
                        cameraData.antialiasing = m_Antialiasing;
                    }

                    if (m_OverrideClearMode)
                    {
                        cameraData.clearColorMode = m_ClearMode;
                    }
                }
            }
        }

        public void SetAdditionalLayerData()
        {
            if (m_LayerCamera)
            {
                var layerData = m_LayerCamera.GetComponent<AdditionalCompositorData>();
                if (layerData != null)
                {
                    layerData.Init(m_InputFilters, m_ClearAlpha);

                    layerData.alphaMin = m_AlphaMin;
                    layerData.alphaMax = m_AlphaMax;
                }
            }
        }

        internal void CopyInternalCameraData()
        {
            if (!isUsingACameraClone)
            {
                // we are using directly the source camera, so there is no need to copy any properties
                return;
            }

            // Copy/update the camera data (but preserve the camera depth/draw-order [case 1264552])
            var drawOrder = m_LayerCamera.depth;
            if (m_Camera)
            {
                m_LayerCamera.CopyFrom(m_Camera);
                m_LayerCamera.depth = drawOrder;

                var cameraDataOrig = m_Camera.GetComponent<HDAdditionalCameraData>();
                var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
                if (cameraDataOrig)
                {
                    cameraDataOrig.CopyTo(cameraData);
                }
            }
        }

        public void UpdateOutputCamera()
        {
            if (m_LayerCamera == null)
            {
                return;
            }

            var compositor = CompositionManager.GetInstance();
            m_LayerCamera.enabled = (m_Show || m_ClearsBackGround) && compositor.enableOutput;

            if (m_Type == LayerType.Image)
            {
                var compositorData = m_LayerCamera.GetComponent<AdditionalCompositorData>();
                if (compositorData)
                    compositorData.clearColorTexture = (m_Show && m_InputTexture != null) ? m_InputTexture : (m_LayerPositionInStack == 0) ? Texture2D.blackTexture : null;
            }

            if (m_LayerCamera.enabled)
            {
                CopyInternalCameraData();
            }
        }

        public void Update()
        {
            UpdateOutputCamera();
            SetLayerMaskOverrides();
            SetAdditionalLayerData();
        }

        public void SetPriotiry(float priority)
        {
            if (m_LayerCamera)
            {
                m_LayerCamera.depth = priority;
            }
        }

        public RenderTexture GetRenderTarget(bool allowAOV = true, bool alwaysShow = false)
        {
            if (m_Show || alwaysShow)
            {
                if (m_AOVMap != null && allowAOV)
                {
                    foreach (var aov in m_AOVMap)
                    {
                        return m_AOVRenderTargets[aov.Value];
                    }
                }

                return m_RenderTarget;
            }
            return null;
        }

        public bool ValidateRTSize(int referenceWidth, int referenceHeight)
        {
            if (m_RenderTarget == null)
            {
                return true;
            }

            float scale = EnumToScale(m_ResolutionScale);
            return ((m_RenderTarget.width == Mathf.FloorToInt(referenceWidth * scale)) && (m_RenderTarget.height == Mathf.FloorToInt(referenceHeight * scale)));
        }

        public void SetupClearColor()
        {
            if (m_LayerCamera && m_Camera)
            {
                m_LayerCamera.enabled = true;
                m_LayerCamera.cullingMask = 0;
                var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
                var cameraDataOrig = m_Camera.GetComponent<HDAdditionalCameraData>();

                cameraData.clearColorMode = cameraDataOrig.clearColorMode;
                cameraData.clearDepth = true;

                m_ClearsBackGround = true;
            }
        }

        public void AddInputFilter(CompositionFilter filter)
        {
            // avoid duplicate filters
            foreach (var f in m_InputFilters)
            {
                if (f.filterType == filter.filterType)
                {
                    return;
                }
            }
            m_InputFilters.Add(filter);
        }

        public void SetupLayerCamera(CompositorLayer targetLayer, int layerPositionInStack)
        {
            if (!m_LayerCamera || (targetLayer == null))
            {
                return;
            }

            if (targetLayer.GetRenderTarget() == null)
            {
                m_LayerCamera.enabled = false;
                return;
            }

            m_LayerPositionInStack = layerPositionInStack;

            var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
            m_LayerCamera.targetTexture = targetLayer.GetRenderTarget(false);

            // Setup the custom clear pass for camera stacking
            {
                if (layerPositionInStack != 0)
                {
                    // The next layer in the stack should clear with the texture of the previous layer:
                    // this will copy the content of the target RT to the RTHandle and preserve post process
                    // Unless we have an image layer with a valid texture: in this case we use the texture as clear color
                    cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
                    var compositorData = m_LayerCamera.GetComponent<AdditionalCompositorData>();
                    if (!compositorData)
                    {
                        compositorData = m_LayerCamera.gameObject.AddComponent<AdditionalCompositorData>();
                    }
                    if (m_Type != LayerType.Image || (m_Type == LayerType.Image && m_InputTexture == null))
                    {
                        compositorData.clearColorTexture = targetLayer.GetRenderTarget();
                        compositorData.clearDepthTexture = targetLayer.m_RTHandle;
                    }
                    cameraData.volumeLayerMask |= 1 << 31;
                }
                else
                {
                    // First layer on the stack always clears depth
                    m_ClearDepth = true;
                }
            }
            cameraData.clearDepth = m_ClearDepth;

            // The target layer expects AOVs, so this stacked layer should also generate AOVs
            int aovMask = (1 << (int)targetLayer.m_AOVBitmask);
            if (m_Show && aovMask > 1)
            {
                var aovRequestBuilder = new AOVRequestBuilder();

                int outputIndex = 0;
                for (int i = 0; i < k_AOVNames.Length; ++i)
                {
                    if ((aovMask & (1 << i)) != 0)
                    {
                        int aovType = i;

                        var aovRequest = new AOVRequest(AOVRequest.NewDefault());
                        aovRequest.SetFullscreenOutput((MaterialSharedProperty)aovType);

                        int indexLocalCopy = outputIndex; //< required to properly capture the variable in the lambda
                        aovRequestBuilder.Add(
                            aovRequest,
                            bufferId => targetLayer.m_AOVTmpRTHandle ?? (targetLayer.m_AOVTmpRTHandle = RTHandles.Alloc(targetLayer.pixelWidth, targetLayer.pixelHeight)),
                            null,
                            new[] { AOVBuffers.Color },
                            (cmd, textures, properties) =>
                            {
                                // copy result to the output buffer
                                cmd.Blit(textures[0], targetLayer.m_AOVRenderTargets[indexLocalCopy]);
                            }
                        );
                        outputIndex++;
                    }
                }

                cameraData.SetAOVRequests(aovRequestBuilder.Build());
                m_LayerCamera.enabled = true;
            }
            else
            {
                cameraData.SetAOVRequests(null);
            }
        }
    }
}
