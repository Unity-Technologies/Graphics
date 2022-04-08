using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Holds information about whether to override certain camera rendering options from the render pipeline asset.
    /// When set to <c>Off</c> option will be disabled regardless of what is set on the pipeline asset.
    /// When set to <c>On</c> option will be enabled regardless of what is set on the pipeline asset.
    /// When set to <c>UsePipelineSetting</c> value set in the <see cref="UniversalRenderPipelineAsset"/>.
    /// </summary>
    public enum CameraOverrideOption
    {
        Off,
        On,
        UsePipelineSettings,
    }

    //[Obsolete("Renderer override is no longer used, renderers are referenced by index on the pipeline asset.")]
    public enum RendererOverrideOption
    {
        Custom,
        UsePipelineSettings,
    }

    /// <summary>
    /// Holds information about the post-processing anti-aliasing mode.
    /// When set to <c>None</c> no post-processing anti-aliasing pass will be performed.
    /// When set to <c>Fast</c> a fast approximated anti-aliasing pass will render when resolving the camera to screen.
    /// When set to <c>SubpixelMorphologicalAntiAliasing</c> SMAA pass will render when resolving the camera to screen. You can choose the SMAA quality by setting <seealso cref="AntialiasingQuality"/>
    /// </summary>
    public enum AntialiasingMode
    {
        [InspectorName("No Anti-aliasing")]
        None,
        [InspectorName("Fast Approximate Anti-aliasing (FXAA)")]
        FastApproximateAntialiasing,
        [InspectorName("Subpixel Morphological Anti-aliasing (SMAA)")]
        SubpixelMorphologicalAntiAliasing,
        //TemporalAntialiasing
    }

    /// <summary>
    /// Holds information about the render type of a camera. Options are Base or Overlay.
    /// Base rendering type allows the camera to render to either the screen or to a texture.
    /// Overlay rendering type allows the camera to render on top of a previous camera output, thus compositing camera results.
    /// </summary>
    public enum CameraRenderType
    {
        Base,
        Overlay,
    }

    /// <summary>
    /// Controls SMAA anti-aliasing quality.
    /// </summary>
    public enum AntialiasingQuality
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Contains extension methods for Camera class.
    /// </summary>
    public static class CameraExtensions
    {
        /// <summary>
        /// Universal Render Pipeline exposes additional rendering data in a separate component.
        /// This method returns the additional data component for the given camera or create one if it doesn't exist yet.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns>The <c>UniversalAdditionalCameraData</c> for this camera.</returns>
        /// <see cref="UniversalAdditionalCameraData"/>
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera)
        {
            var gameObject = camera.gameObject;
            bool componentExists = gameObject.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData);
            if (!componentExists)
                cameraData = gameObject.AddComponent<UniversalAdditionalCameraData>();

            return cameraData;
        }

        /// <summary>
        /// Returns the VolumeFrameworkUpdateMode set on the camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static VolumeFrameworkUpdateMode GetVolumeFrameworkUpdateMode(this Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            return cameraData.volumeFrameworkUpdateMode;
        }

        /// <summary>
        /// Sets the VolumeFrameworkUpdateMode for the camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="mode"></param>
        public static void SetVolumeFrameworkUpdateMode(this Camera camera, VolumeFrameworkUpdateMode mode)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            if (cameraData.volumeFrameworkUpdateMode == mode)
            {
                return;
            }

            bool requiredUpdatePreviously = cameraData.requiresVolumeFrameworkUpdate;
            cameraData.volumeFrameworkUpdateMode = mode;

            // We only update the local volume stacks for cameras set to ViaScripting.
            // Otherwise it will be updated in every frame.
            // We also check the previous value to make sure we're not updating when
            // switching between Camera ViaScripting and the URP Asset set to ViaScripting
            if (requiredUpdatePreviously && !cameraData.requiresVolumeFrameworkUpdate)
            {
                camera.UpdateVolumeStack(cameraData);
            }
        }

        /// <summary>
        /// Updates the volume stack for this camera.
        /// This function will only update the stack when the camera has VolumeFrameworkUpdateMode set to ViaScripting
        /// or when it set to UsePipelineSettings and the update mode on the Render Pipeline Asset is set to ViaScripting.
        /// </summary>
        /// <param name="camera"></param>
        public static void UpdateVolumeStack(this Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            camera.UpdateVolumeStack(cameraData);
        }

        /// <summary>
        /// Updates the volume stack for this camera.
        /// This function will only update the stack when the camera has ViaScripting selected or if
        /// the camera is set to UsePipelineSettings and the Render Pipeline Asset is set to ViaScripting.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="cameraData"></param>
        public static void UpdateVolumeStack(this Camera camera, UniversalAdditionalCameraData cameraData)
        {
            Assert.IsNotNull(cameraData, "cameraData can not be null when updating the volume stack.");

            // We only update the local volume stacks for cameras set to ViaScripting.
            // Otherwise it will be updated in the frame.
            if (cameraData.requiresVolumeFrameworkUpdate)
            {
                return;
            }

            // Create stack for camera
            if (cameraData.volumeStack == null)
            {
                cameraData.volumeStack = VolumeManager.instance.CreateStack();
            }

            camera.GetVolumeLayerMaskAndTrigger(cameraData, out LayerMask layerMask, out Transform trigger);
            VolumeManager.instance.Update(cameraData.volumeStack, trigger, layerMask);
        }

        /// <summary>
        /// Destroys the volume stack for this camera.
        /// </summary>
        /// <param name="camera"></param>
        public static void DestroyVolumeStack(this Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            camera.DestroyVolumeStack(cameraData);
        }

        /// <summary>
        /// Destroys the volume stack for this camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="cameraData"></param>
        public static void DestroyVolumeStack(this Camera camera, UniversalAdditionalCameraData cameraData)
        {
            cameraData.volumeStack.Dispose();
            cameraData.volumeStack = null;
        }

        /// <summary>
        /// Returns the mask and trigger assigned for volumes on the camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="cameraData"></param>
        /// <param name="layerMask"></param>
        /// <param name="trigger"></param>
        internal static void GetVolumeLayerMaskAndTrigger(this Camera camera, UniversalAdditionalCameraData cameraData, out LayerMask layerMask, out Transform trigger)
        {
            // Default values when there's no additional camera data available
            layerMask = 1; // "Default"
            trigger = camera.transform;

            if (cameraData != null)
            {
                layerMask = cameraData.volumeLayerMask;
                trigger = (cameraData.volumeTrigger != null) ? cameraData.volumeTrigger : trigger;
            }
            else if (camera.cameraType == CameraType.SceneView)
            {
                // Try to mirror the MainCamera volume layer mask for the scene view - do not mirror the target
                var mainCamera = Camera.main;
                UniversalAdditionalCameraData mainAdditionalCameraData = null;

                if (mainCamera != null && mainCamera.TryGetComponent(out mainAdditionalCameraData))
                {
                    layerMask = mainAdditionalCameraData.volumeLayerMask;
                }

                trigger = (mainAdditionalCameraData != null && mainAdditionalCameraData.volumeTrigger != null) ? mainAdditionalCameraData.volumeTrigger : trigger;
            }
        }
    }

    static class CameraTypeUtility
    {
        static string[] s_CameraTypeNames = Enum.GetNames(typeof(CameraRenderType)).ToArray();

        public static string GetName(this CameraRenderType type)
        {
            int typeInt = (int)type;
            if (typeInt < 0 || typeInt >= s_CameraTypeNames.Length)
                typeInt = (int)CameraRenderType.Base;
            return s_CameraTypeNames[typeInt];
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    [URPHelpURL("universal-additional-camera-data")]
    public class UniversalAdditionalCameraData : MonoBehaviour, ISerializationCallbackReceiver, IAdditionalData
    {
        const string k_GizmoPath = "Packages/com.unity.render-pipelines.universal/Editor/Gizmos/";
        const string k_BaseCameraGizmoPath = k_GizmoPath + "Camera_Base.png";
        const string k_OverlayCameraGizmoPath = k_GizmoPath + "Camera_Base.png";
        const string k_PostProcessingGizmoPath = k_GizmoPath + "Camera_PostProcessing.png";

        [FormerlySerializedAs("renderShadows"), SerializeField]
        bool m_RenderShadows = true;

        [SerializeField]
        CameraOverrideOption m_RequiresDepthTextureOption = CameraOverrideOption.UsePipelineSettings;

        [SerializeField]
        CameraOverrideOption m_RequiresOpaqueTextureOption = CameraOverrideOption.UsePipelineSettings;

        [SerializeField] CameraRenderType m_CameraType = CameraRenderType.Base;
        [SerializeField] List<Camera> m_Cameras = new List<Camera>();
        [SerializeField] int m_RendererIndex = -1;

        [SerializeField] LayerMask m_VolumeLayerMask = 1; // "Default"
        [SerializeField] Transform m_VolumeTrigger = null;
        [SerializeField] VolumeFrameworkUpdateMode m_VolumeFrameworkUpdateModeOption = VolumeFrameworkUpdateMode.UsePipelineSettings;

        [SerializeField] bool m_RenderPostProcessing = false;
        [SerializeField] AntialiasingMode m_Antialiasing = AntialiasingMode.None;
        [SerializeField] AntialiasingQuality m_AntialiasingQuality = AntialiasingQuality.High;
        [SerializeField] bool m_StopNaN = false;
        [SerializeField] bool m_Dithering = false;
        [SerializeField] bool m_ClearDepth = true;
        [SerializeField] bool m_AllowXRRendering = true;

        [NonSerialized] Camera m_Camera;
        // Deprecated:
        [FormerlySerializedAs("requiresDepthTexture"), SerializeField]
        bool m_RequiresDepthTexture = false;

        [FormerlySerializedAs("requiresColorTexture"), SerializeField]
        bool m_RequiresColorTexture = false;

        [HideInInspector] [SerializeField] float m_Version = 2;

        public float version => m_Version;

        static UniversalAdditionalCameraData s_DefaultAdditionalCameraData = null;
        internal static UniversalAdditionalCameraData defaultAdditionalCameraData
        {
            get
            {
                if (s_DefaultAdditionalCameraData == null)
                    s_DefaultAdditionalCameraData = new UniversalAdditionalCameraData();

                return s_DefaultAdditionalCameraData;
            }
        }

#if UNITY_EDITOR
        internal new Camera camera
#else
        internal Camera camera
#endif
        {
            get
            {
                if (!m_Camera)
                {
                    gameObject.TryGetComponent<Camera>(out m_Camera);
                }
                return m_Camera;
            }
        }


        /// <summary>
        /// Controls if this camera should render shadows.
        /// </summary>
        public bool renderShadows
        {
            get => m_RenderShadows;
            set => m_RenderShadows = value;
        }

        /// <summary>
        /// Controls if a camera should render depth.
        /// The depth is available to be bound in shaders as _CameraDepthTexture.
        /// <seealso cref="CameraOverrideOption"/>
        /// </summary>
        public CameraOverrideOption requiresDepthOption
        {
            get => m_RequiresDepthTextureOption;
            set => m_RequiresDepthTextureOption = value;
        }

        /// <summary>
        /// Controls if a camera should copy the color contents of a camera after rendering opaques.
        /// The color texture is available to be bound in shaders as _CameraOpaqueTexture.
        /// </summary>
        public CameraOverrideOption requiresColorOption
        {
            get => m_RequiresOpaqueTextureOption;
            set => m_RequiresOpaqueTextureOption = value;
        }

        /// <summary>
        /// Returns the camera renderType.
        /// <see cref="CameraRenderType"/>.
        /// </summary>
        public CameraRenderType renderType
        {
            get => m_CameraType;
            set => m_CameraType = value;
        }

        /// <summary>
        /// Returns the camera stack. Only valid for Base cameras.
        /// Will return null if it is not a Base camera.
        /// <seealso cref="CameraRenderType"/>.
        /// </summary>
        public List<Camera> cameraStack
        {
            get
            {
                if (renderType != CameraRenderType.Base)
                {
                    var camera = gameObject.GetComponent<Camera>();
                    Debug.LogWarning(string.Format("{0}: This camera is of {1} type. Only Base cameras can have a camera stack.", camera.name, renderType));
                    return null;
                }

                if (!scriptableRenderer.SupportsCameraStackingType(CameraRenderType.Base))
                {
                    var camera = gameObject.GetComponent<Camera>();
                    Debug.LogWarning(string.Format("{0}: This camera has a ScriptableRenderer that doesn't support camera stacking. Camera stack is null.", camera.name));
                    return null;
                }
                return m_Cameras;
            }
        }

        internal void UpdateCameraStack()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Update camera stack");
#endif
            int prev = m_Cameras.Count;
            m_Cameras.RemoveAll(cam => cam == null);
            int curr = m_Cameras.Count;
            int removedCamsCount = prev - curr;
            if (removedCamsCount != 0)
            {
                Debug.LogWarning(name + ": " + removedCamsCount + " camera overlay" + (removedCamsCount > 1 ? "s" : "") + " no longer exists and will be removed from the camera stack.");
            }
        }

        /// <summary>
        /// If true, this camera will clear depth value before rendering. Only valid for Overlay cameras.
        /// </summary>
        public bool clearDepth
        {
            get => m_ClearDepth;
        }

        /// <summary>
        /// Returns true if this camera needs to render depth information in a texture.
        /// If enabled, depth texture is available to be bound and read from shaders as _CameraDepthTexture after rendering skybox.
        /// </summary>
        public bool requiresDepthTexture
        {
            get
            {
                if (m_RequiresDepthTextureOption == CameraOverrideOption.UsePipelineSettings)
                {
                    return UniversalRenderPipeline.asset.supportsCameraDepthTexture;
                }
                else
                {
                    return m_RequiresDepthTextureOption == CameraOverrideOption.On;
                }
            }
            set { m_RequiresDepthTextureOption = (value) ? CameraOverrideOption.On : CameraOverrideOption.Off; }
        }

        /// <summary>
        /// Returns true if this camera requires to color information in a texture.
        /// If enabled, color texture is available to be bound and read from shaders as _CameraOpaqueTexture after rendering skybox.
        /// </summary>
        public bool requiresColorTexture
        {
            get
            {
                if (m_RequiresOpaqueTextureOption == CameraOverrideOption.UsePipelineSettings)
                {
                    return UniversalRenderPipeline.asset.supportsCameraOpaqueTexture;
                }
                else
                {
                    return m_RequiresOpaqueTextureOption == CameraOverrideOption.On;
                }
            }
            set { m_RequiresOpaqueTextureOption = (value) ? CameraOverrideOption.On : CameraOverrideOption.Off; }
        }

        /// <summary>
        /// Returns the <see cref="ScriptableRenderer"/> that is used to render this camera.
        /// </summary>
        public ScriptableRenderer scriptableRenderer
        {
            get
            {
                if (UniversalRenderPipeline.asset is null)
                    return null;
                if (!UniversalRenderPipeline.asset.ValidateRendererData(m_RendererIndex))
                {
                    int defaultIndex = UniversalRenderPipeline.asset.m_DefaultRendererIndex;
                    Debug.LogWarning(
                        $"Renderer at <b>index {m_RendererIndex.ToString()}</b> is missing for camera <b>{camera.name}</b>, falling back to Default Renderer. <b>{UniversalRenderPipeline.asset.m_RendererDataList[defaultIndex].name}</b>",
                        UniversalRenderPipeline.asset);
                    return UniversalRenderPipeline.asset.GetRenderer(defaultIndex);
                }
                return UniversalRenderPipeline.asset.GetRenderer(m_RendererIndex);
            }
        }

        /// <summary>
        /// Use this to set this Camera's current <see cref="ScriptableRenderer"/> to one listed on the Render Pipeline Asset. Takes an index that maps to the list on the Render Pipeline Asset.
        /// </summary>
        /// <param name="index">The index that maps to the RendererData list on the currently assigned Render Pipeline Asset</param>
        public void SetRenderer(int index)
        {
            m_RendererIndex = index;
        }

        /// <summary>
        /// Returns the selected scene-layers affecting this camera.
        /// </summary>
        public LayerMask volumeLayerMask
        {
            get => m_VolumeLayerMask;
            set => m_VolumeLayerMask = value;
        }

        /// <summary>
        /// Returns the Transform that acts as a trigger for Volume blending.
        /// </summary>
        public Transform volumeTrigger
        {
            get => m_VolumeTrigger;
            set => m_VolumeTrigger = value;
        }

        /// <summary>
        /// Returns the selected mode for Volume Frame Updates.
        /// </summary>
        internal VolumeFrameworkUpdateMode volumeFrameworkUpdateMode
        {
            get => m_VolumeFrameworkUpdateModeOption;
            set => m_VolumeFrameworkUpdateModeOption = value;
        }

        /// <summary>
        /// Returns true if this camera requires the volume framework to be updated every frame.
        /// </summary>
        public bool requiresVolumeFrameworkUpdate
        {
            get
            {
                if (m_VolumeFrameworkUpdateModeOption == VolumeFrameworkUpdateMode.UsePipelineSettings)
                {
                    return UniversalRenderPipeline.asset.volumeFrameworkUpdateMode != VolumeFrameworkUpdateMode.ViaScripting;
                }

                return m_VolumeFrameworkUpdateModeOption == VolumeFrameworkUpdateMode.EveryFrame;
            }
        }

        /// <summary>
        /// Returns the current volume stack used by this camera.
        /// </summary>
        VolumeStack m_VolumeStack = null;
        public VolumeStack volumeStack
        {
            get => m_VolumeStack;
            set => m_VolumeStack = value;
        }

        /// <summary>
        /// Returns true if this camera should render post-processing.
        /// </summary>
        public bool renderPostProcessing
        {
            get => m_RenderPostProcessing;
            set => m_RenderPostProcessing = value;
        }

        /// <summary>
        /// Returns the current anti-aliasing mode used by this camera.
        /// <see cref="AntialiasingMode"/>.
        /// </summary>
        public AntialiasingMode antialiasing
        {
            get => m_Antialiasing;
            set => m_Antialiasing = value;
        }

        /// <summary>
        /// Returns the current anti-aliasing quality used by this camera.
        /// <seealso cref="antialiasingQuality"/>.
        /// </summary>
        public AntialiasingQuality antialiasingQuality
        {
            get => m_AntialiasingQuality;
            set => m_AntialiasingQuality = value;
        }

        /// <summary>
        /// Returns true if this camera should automatically replace NaN/Inf in shaders by a black pixel to avoid breaking some effects.
        /// </summary>
        public bool stopNaN
        {
            get => m_StopNaN;
            set => m_StopNaN = value;
        }

        /// <summary>
        /// Returns true if this camera applies 8-bit dithering to the final render to reduce color banding
        /// </summary>
        public bool dithering
        {
            get => m_Dithering;
            set => m_Dithering = value;
        }

        /// <summary>
        /// Returns true if this camera allows render in XR.
        /// </summary>
        public bool allowXRRendering
        {
            get => m_AllowXRRendering;
            set => m_AllowXRRendering = value;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (version <= 1)
            {
                m_RequiresDepthTextureOption = (m_RequiresDepthTexture) ? CameraOverrideOption.On : CameraOverrideOption.Off;
                m_RequiresOpaqueTextureOption = (m_RequiresColorTexture) ? CameraOverrideOption.On : CameraOverrideOption.Off;
            }
        }

        public void OnDrawGizmos()
        {
            string gizmoName = "";
            Color tint = Color.white;

            if (m_CameraType == CameraRenderType.Base)
            {
                gizmoName = k_BaseCameraGizmoPath;
            }
            else if (m_CameraType == CameraRenderType.Overlay)
            {
                gizmoName = k_OverlayCameraGizmoPath;
            }

#if UNITY_2019_2_OR_NEWER
#if UNITY_EDITOR
            if (Selection.activeObject == gameObject)
            {
                // Get the preferences selection color
                tint = SceneView.selectedOutlineColor;
            }
#endif
            if (!string.IsNullOrEmpty(gizmoName))
            {
                Gizmos.DrawIcon(transform.position, gizmoName, true, tint);
            }

            if (renderPostProcessing)
            {
                Gizmos.DrawIcon(transform.position, k_PostProcessingGizmoPath, true, tint);
            }
#else
            if (renderPostProcessing)
            {
                Gizmos.DrawIcon(transform.position, k_PostProcessingGizmoPath);
            }
            Gizmos.DrawIcon(transform.position, gizmoName);
#endif
        }
    }
}
