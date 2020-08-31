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
            R16G16B16A16 = GraphicsFormat.R16G16B16A16_UNorm,
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
                if (m_Camera != null)
                {
                    return (float)m_Camera.pixelWidth / m_Camera.pixelHeight;
                }
                return 1.0f;
            }
        }

        public Camera camera => m_LayerCamera;

        [SerializeField] Camera m_LayerCamera;

        private CompositorLayer()
        {
        }

        public static CompositorLayer CreateStackLayer(LayerType type = CompositorLayer.LayerType.Camera, string layerName = "New Layer")
        {
            var newLayer = new CompositorLayer();
            newLayer.m_LayerName = layerName;
            newLayer.m_Type = type;
            newLayer.m_OverrideCullingMask = true;
            newLayer.m_CullingMask = 0; //LayerMask.GetMask("None");
            newLayer.m_Camera = CompositionManager.GetSceceCamera();
            newLayer.m_OutputTarget = CompositorLayer.OutputTarget.CameraStack;
            newLayer.m_ClearDepth = true;

            if (newLayer.m_Type == LayerType.Image || newLayer.m_Type == LayerType.Video)
            {
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

        public int pixelWidth
        {
            get
            {
                if (m_Camera)
                {
                    float resScale = EnumToScale(m_ResolutionScale);
                    return (int)(resScale * m_Camera.pixelWidth);
                }
                return 0;
            }
        }
        public int pixelHeight
        {
            get
            {
                if (m_Camera)
                {
                    float resScale = EnumToScale(m_ResolutionScale);
                    return (int)(resScale * m_Camera.pixelHeight);
                }
                return 0;
            }
        }
        public void Init(string layerID = "")
        {
            if (m_LayerName == "")
            {
                m_LayerName = layerID;
            }

            // Compositor output layers (that allocate the render targets) also need a reference camera, just to get the reference pixel width/height 
            // Note: Movie & image layers are rendered at the output resolution (and not the movie/image resolution). This is required to have post-processing effects like film grain at full res.
            if (m_Camera == null)
            {
                m_Camera = CompositionManager.GetSceceCamera();
            }

            // Create a new camera if necessary or use the one specified by the user
            if (m_LayerCamera == null && m_OutputTarget == OutputTarget.CameraStack)
            {
                m_LayerCamera = Object.Instantiate(m_Camera);

                // delete any audio listeners from the clone camera
                var listener = m_LayerCamera.GetComponent<AudioListener>();
                if (listener)
                {
                    CoreUtils.Destroy(listener);
                }
                m_LayerCamera.name = "Compositor" + layerID;
                m_LayerCamera.gameObject.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy | HideFlags.HideAndDontSave;
                if(m_LayerCamera.tag == "MainCamera")
                {
                    m_LayerCamera.tag = "Untagged";
                }

                // Remove the compositor copy (if exists) from the cloned camera. This will happen if the compositor script was attached to the camera we are cloning 
                var compositionManager = m_LayerCamera.GetComponent<CompositionManager>();
                if (compositionManager != null)
                {
                    CoreUtils.Destroy(compositionManager);
                }

                var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
                if (cameraData == null)
                {
                    m_LayerCamera.gameObject.AddComponent(typeof(HDAdditionalCameraData));
                }

            }
            m_ClearsBackGround = false;
            m_LayerPositionInStack = 0; // will be set in SetupLayerCamera

            if (m_OutputTarget != OutputTarget.CameraStack && m_RenderTarget == null)
            {
                m_RenderTarget = new RenderTexture(pixelWidth, pixelHeight, 24, (GraphicsFormat)m_ColorBufferFormat);
            }

            // check and fix RT handle
            if (m_OutputTarget != OutputTarget.CameraStack && m_RTHandle == null)
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
                if(m_AOVMap != null)
                {
                    m_AOVMap.Clear();
                    m_AOVMap = null;
                }
            }

            var compositor = CompositionManager.GetInstance();
            if (m_OutputRenderer != null && Application.IsPlaying(compositor.gameObject))
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetTexture("_BaseColorMap", m_RenderTarget);
                m_OutputRenderer.SetPropertyBlock(propertyBlock);
            }

            if (m_LayerCamera)
            {
                m_LayerCamera.enabled = m_Show;
                var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
                var layerData = m_LayerCamera.GetComponent<AdditionalCompositorData>();
                {
                    // create the component if it is required and does not exist
                    if (layerData == null)
                    {
                        layerData = m_LayerCamera.gameObject.AddComponent<AdditionalCompositorData>();
                        layerData.hideFlags = HideFlags.HideAndDontSave;
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

        public void DestroyRT()
        {
            if (m_LayerCamera != null)
            {
                var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
                if (cameraData)
                {
                    CoreUtils.Destroy(cameraData);
                }
                m_LayerCamera.targetTexture = null;
                CoreUtils.Destroy(m_LayerCamera);
                m_LayerCamera = null;
            }

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
                    CoreUtils.Destroy(handle);
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
                if(compositorData)
                    compositorData.clearColorTexture = m_Show ? m_InputTexture : Texture2D.blackTexture;
            }

            if (m_LayerCamera.enabled)
            {
                // Refresh the camera data
                m_LayerCamera.CopyFrom(m_Camera);
                var cameraDataOrig = m_Camera.GetComponent<HDAdditionalCameraData>();
                var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
                if (cameraDataOrig)
                {
                    cameraDataOrig.CopyTo(cameraData);
                }
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
            m_LayerCamera.enabled = true;
            m_LayerCamera.cullingMask = 0;
            var cameraData = m_LayerCamera.GetComponent<HDAdditionalCameraData>();
            var cameraDataOrig = m_Camera.GetComponent<HDAdditionalCameraData>();

            cameraData.clearColorMode = cameraDataOrig.clearColorMode;
            cameraData.clearDepth = true;

            m_ClearsBackGround = true;
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

            if (targetLayer.m_AOVBitmask == 0)
            {
                if (layerPositionInStack != 0)
                {
                    // The next layer in the stack should clear with the texture of the previous layer: this will copy the content of the target RT to the RTHandle and preserve post process
                    cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
                    var compositorData = m_LayerCamera.GetComponent<AdditionalCompositorData>();
                    if (!compositorData)
                    {
                        compositorData = m_LayerCamera.gameObject.AddComponent<AdditionalCompositorData>();
                    }
                    if (m_Type != LayerType.Image)
                    {
                        compositorData.clearColorTexture = targetLayer.GetRenderTarget(false);
                    }
                    cameraData.volumeLayerMask |= 1 << 31;
                }
                else
                {
                    m_ClearDepth = true;
                }
            }
            cameraData.clearDepth = m_ClearDepth;

            // The target layer expects AOVs, so this stacked layer should also generate AOVs
            int aovMask = (1 << (int)targetLayer.m_AOVBitmask);
            if (aovMask > 1)
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
