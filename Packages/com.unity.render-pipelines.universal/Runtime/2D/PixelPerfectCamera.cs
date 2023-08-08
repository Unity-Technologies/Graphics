using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The Pixel Perfect Camera component ensures your pixel art remains crisp and clear at different resolutions, and stable in motion.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Pixel Perfect Camera")]
    [RequireComponent(typeof(Camera))]
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.Universal")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2d-pixelperfect.html%23properties")]
    public class PixelPerfectCamera : MonoBehaviour, IPixelPerfectCamera, ISerializationCallbackReceiver
    {
        /// <summary>
        /// An enumeration of the types of display cropping.
        /// </summary>
        public enum CropFrame
        {
            /// <summary>
            /// No cropping.
            /// </summary>
            None,
            /// <summary>
            /// Black borders added to the left and right of viewport to match Reference Resolution.
            /// </summary>
            Pillarbox,
            /// <summary>
            /// Black borders added to the top and bottom of viewport to match Reference Resolution.
            /// </summary>
            Letterbox,
            /// <summary>
            /// Black borders added to all sides of viewport to match Reference Resolution.
            /// </summary>
            Windowbox,
            /// <summary>
            /// Expands the viewport to fit the screen resolution while maintaining the viewport's aspect ratio.
            /// </summary>
            StretchFill
        }

        /// <summary>
        /// Determines how pixels are snapped to the grid.
        /// </summary>
        public enum GridSnapping
        {
            /// <summary>
            /// No snapping.
            /// </summary>
            None,
            /// <summary>
            /// Prevent subpixel movement and make Sprites appear to move in pixel-by-pixel increments.
            /// </summary>
            PixelSnapping,
            /// <summary>
            /// The scene is rendered to a temporary texture set as close as possible to the Reference Resolution, while maintaining the full screen aspect ratio. This temporary texture is then upscaled to fit the full screen.
            /// </summary>
            UpscaleRenderTexture
        }

        /// <summary>
        /// Defines the filter mode use to render the final render target.
        /// </summary>
        public enum PixelPerfectFilterMode
        {
            /// <summary>
            /// Uses point filter to upscale to closest multiple of Reference Resolution, followed by bilinear filter to the target resolution.
            /// </summary>
            RetroAA,
            /// <summary>
            /// Uses point filter to upscale to target resolution.
            /// </summary>
            Point,
        }

        private enum ComponentVersions
        {
            Version_Unserialized = 0,
            Version_1 = 1
        }

#if UNITY_EDITOR
        const ComponentVersions k_CurrentComponentVersion = ComponentVersions.Version_1;
        [SerializeField] ComponentVersions m_ComponentVersion = ComponentVersions.Version_Unserialized;
#endif

        /// <summary>
        /// Defines how the output display will be cropped.
        /// </summary>
        public CropFrame cropFrame { get { return m_CropFrame; } set { m_CropFrame = value; } }

        /// <summary>
        /// Defines if pixels will be locked to a grid determined by assetsPPU.
        /// </summary>
        public GridSnapping gridSnapping { get { return m_GridSnapping; } set { m_GridSnapping = value; } }

        /// <summary>
        /// The target orthographic size of the camera.
        /// </summary>
        public float orthographicSize { get { return m_Internal.orthoSize; } }

        /// <summary>
        /// Match this value to to the Pixels Per Unit values of all Sprites within the Scene.
        /// </summary>
        public int assetsPPU { get { return m_AssetsPPU; } set { m_AssetsPPU = value > 0 ? value : 1; } }

        /// <summary>
        /// The original horizontal resolution your Assets are designed for.
        /// </summary>
        public int refResolutionX { get { return m_RefResolutionX; } set { m_RefResolutionX = value > 0 ? value : 1; } }

        /// <summary>
        /// Original vertical resolution your Assets are designed for.
        /// </summary>
        public int refResolutionY { get { return m_RefResolutionY; } set { m_RefResolutionY = value > 0 ? value : 1; } }

        /// <summary>
        /// Set to true to have the Scene rendered to a temporary texture set as close as possible to the Reference Resolution,
        /// while maintaining the full screen aspect ratio. This temporary texture is then upscaled to fit the full screen.
        /// </summary>
        [System.Obsolete("Use gridSnapping instead", false)]
        public bool upscaleRT
        {
            get
            {
                return m_GridSnapping == GridSnapping.UpscaleRenderTexture;
            }
            set
            {
                m_GridSnapping = value ? GridSnapping.UpscaleRenderTexture : GridSnapping.None;
            }
        }

        /// <summary>
        /// Set to true to prevent subpixel movement and make Sprites appear to move in pixel-by-pixel increments.
        /// Only applicable when upscaleRT is false.
        /// </summary>
        [System.Obsolete("Use gridSnapping instead", false)]
        public bool pixelSnapping
        {
            get
            {
                return m_GridSnapping == GridSnapping.PixelSnapping;
            }
            set
            {
                m_GridSnapping = value ? GridSnapping.PixelSnapping : GridSnapping.None;
            }
        }

        /// <summary>
        /// Set to true to crop the viewport with black bars to match refResolutionX in the horizontal direction.
        /// </summary>
        [System.Obsolete("Use cropFrame instead", false)]
        public bool cropFrameX
        {
            get
            {
                return m_CropFrame == CropFrame.StretchFill || m_CropFrame == CropFrame.Windowbox || m_CropFrame == CropFrame.Pillarbox;
            }
            set
            {
                if (value)
                {
                    if (m_CropFrame == CropFrame.None)
                        m_CropFrame = CropFrame.Pillarbox;
                    else if (m_CropFrame == CropFrame.Letterbox)
                        m_CropFrame = CropFrame.Windowbox;
                }
                else
                {
                    if (m_CropFrame == CropFrame.Pillarbox)
                        m_CropFrame = CropFrame.None;
                    else if (m_CropFrame == CropFrame.Windowbox || m_CropFrame == CropFrame.StretchFill)
                        m_CropFrame = CropFrame.Letterbox;
                }
            }
        }

        /// <summary>
        /// Set to true to crop the viewport with black bars to match refResolutionY in the vertical direction.
        /// </summary>
        [System.Obsolete("Use cropFrame instead", false)]
        public bool cropFrameY
        {
            get
            {
                return m_CropFrame == CropFrame.StretchFill || m_CropFrame == CropFrame.Windowbox || m_CropFrame == CropFrame.Letterbox;
            }
            set
            {
                if (value)
                {
                    if (m_CropFrame == CropFrame.None)
                        m_CropFrame = CropFrame.Letterbox;
                    else if (m_CropFrame == CropFrame.Pillarbox)
                        m_CropFrame = CropFrame.Windowbox;
                }
                else
                {
                    if (m_CropFrame == CropFrame.Letterbox)
                        m_CropFrame = CropFrame.None;
                    else if (m_CropFrame == CropFrame.Windowbox || m_CropFrame == CropFrame.StretchFill)
                        m_CropFrame = CropFrame.Pillarbox;
                }
            }
        }

        /// <summary>
        /// Set to true to expand the viewport to fit the screen resolution while maintaining the viewport's aspect ratio.
        /// Only applicable when both cropFrameX and cropFrameY are true.
        /// </summary>
        [System.Obsolete("Use cropFrame instead", false)]
        public bool stretchFill
        {
            get
            {
                return m_CropFrame == CropFrame.StretchFill;
            }
            set
            {
                if (value)
                    m_CropFrame = CropFrame.StretchFill;
                else
                    m_CropFrame = CropFrame.Windowbox;
            }
        }

        /// <summary>
        /// Ratio of the rendered Sprites compared to their original size (readonly).
        /// </summary>
        public int pixelRatio
        {
            get
            {
                if (m_CinemachineCompatibilityMode)
                {
                    if (m_GridSnapping == GridSnapping.UpscaleRenderTexture)
                        return m_Internal.zoom * m_Internal.cinemachineVCamZoom;
                    else
                        return m_Internal.cinemachineVCamZoom;
                }
                else
                {
                    return m_Internal.zoom;
                }
            }
        }

        /// <summary>
        /// Returns if an upscale pass is required.
        /// </summary>
        public bool requiresUpscalePass
        {
            get
            {
                return m_Internal.requiresUpscaling;
            }
        }

        /// <summary>
        /// Round a arbitrary position to an integer pixel position. Works in world space.
        /// </summary>
        /// <param name="position"> The position you want to round.</param>
        /// <returns>
        /// The rounded pixel position.
        /// Depending on the values of upscaleRT and pixelSnapping, it could be a screen pixel position or an art pixel position.
        /// </returns>
        public Vector3 RoundToPixel(Vector3 position)
        {
            float unitsPerPixel = m_Internal.unitsPerPixel;
            if (unitsPerPixel == 0.0f)
                return position;

            Vector3 result;
            result.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
            result.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
            result.z = Mathf.Round(position.z / unitsPerPixel) * unitsPerPixel;

            return result;
        }

        /// <summary>
        /// Find a pixel-perfect orthographic size as close to targetOrthoSize as possible. Used by Cinemachine to solve compatibility issues with Pixel Perfect Camera.
        /// </summary>
        /// <param name="targetOrthoSize">Orthographic size from the live Cinemachine Virtual Camera.</param>
        /// <returns>The corrected orthographic size.</returns>
        public float CorrectCinemachineOrthoSize(float targetOrthoSize)
        {
            m_CinemachineCompatibilityMode = true;

            if (m_Internal == null)
                return targetOrthoSize;
            else
                return m_Internal.CorrectCinemachineOrthoSize(targetOrthoSize);
        }

        [SerializeField] int m_AssetsPPU = 100;
        [SerializeField] int m_RefResolutionX = 320;
        [SerializeField] int m_RefResolutionY = 180;

        [SerializeField] CropFrame m_CropFrame;
        [SerializeField] GridSnapping m_GridSnapping;
        [SerializeField] PixelPerfectFilterMode m_FilterMode = PixelPerfectFilterMode.RetroAA;

        // These are obsolete. They are here only for migration.
#if UNITY_EDITOR
        [SerializeField] bool m_UpscaleRT;
        [SerializeField] bool m_PixelSnapping;
        [SerializeField] bool m_CropFrameX;
        [SerializeField] bool m_CropFrameY;
        [SerializeField] bool m_StretchFill;
#endif

        Camera m_Camera;
        PixelPerfectCameraInternal m_Internal;
        bool m_CinemachineCompatibilityMode;

        internal FilterMode finalBlitFilterMode
        {
            get
            {
                return m_FilterMode == PixelPerfectFilterMode.RetroAA ? FilterMode.Bilinear : FilterMode.Point;
            }
        }

        internal Vector2Int offscreenRTSize
        {
            get
            {
                return new Vector2Int(m_Internal.offscreenRTWidth, m_Internal.offscreenRTHeight);
            }
        }

        Vector2Int cameraRTSize
        {
            get
            {
                var targetTexture = m_Camera.targetTexture;
                return targetTexture == null ? new Vector2Int(Screen.width, Screen.height) : new Vector2Int(targetTexture.width, targetTexture.height);
            }
        }

        // Snap camera position to pixels using Camera.worldToCameraMatrix.
        void PixelSnap()
        {
            Vector3 cameraPosition = m_Camera.transform.position;
            Vector3 roundedCameraPosition = RoundToPixel(cameraPosition);
            Vector3 offset = roundedCameraPosition - cameraPosition;
            offset.z = -offset.z;

            // Get world to local camera matrix without scale
            var invPos = Matrix4x4.TRS(cameraPosition + offset, Quaternion.identity, Vector3.one).inverse;
            var invRot = Matrix4x4.Rotate(m_Camera.transform.rotation).inverse;
            var scaleMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f));

            // Calculate inverse TRS
            m_Camera.worldToCameraMatrix = scaleMatrix * invRot * invPos;
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
            m_Internal = new PixelPerfectCameraInternal(this);

            // Case 1249076: Initialize internals immediately after the scene is loaded,
            // as the Cinemachine extension may need them before OnBeginContextRendering is called.
            UpdateCameraProperties();
        }

        void UpdateCameraProperties()
        {
            var rtSize = cameraRTSize;
            m_Internal.CalculateCameraProperties(rtSize.x, rtSize.y);

            if (m_Internal.useOffscreenRT)
                m_Camera.pixelRect = m_Internal.CalculateFinalBlitPixelRect(rtSize.x, rtSize.y);
            else
                m_Camera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == m_Camera)
            {
                UpdateCameraProperties();
                PixelSnap();

                if (!m_CinemachineCompatibilityMode)
                {
                    m_Camera.orthographicSize = m_Internal.orthoSize;
                }

                UnityEngine.U2D.PixelPerfectRendering.pixelSnapSpacing = m_Internal.unitsPerPixel;
            }
        }

        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == m_Camera)
                UnityEngine.U2D.PixelPerfectRendering.pixelSnapSpacing = 0.0f;
        }

        void OnEnable()
        {
            m_CinemachineCompatibilityMode = false;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        internal void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            m_Camera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            m_Camera.ResetWorldToCameraMatrix();
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // Show on-screen warning about invalid render resolutions.
        void OnGUI()
        {
            Color oldColor = GUI.color;
            GUI.color = Color.red;

            Vector2Int renderResolution = Vector2Int.zero;
            renderResolution.x = m_Internal.useOffscreenRT ? m_Internal.offscreenRTWidth : m_Camera.pixelWidth;
            renderResolution.y = m_Internal.useOffscreenRT ? m_Internal.offscreenRTHeight : m_Camera.pixelHeight;

            if (renderResolution.x % 2 != 0 || renderResolution.y % 2 != 0)
            {
                string warning = string.Format("Rendering at an odd-numbered resolution ({0} * {1}). Pixel Perfect Camera may not work properly in this situation.", renderResolution.x, renderResolution.y);
                GUILayout.Box(warning);
            }

            var targetTexture = m_Camera.targetTexture;
            Vector2Int rtSize = targetTexture == null ? new Vector2Int(Screen.width, Screen.height) : new Vector2Int(targetTexture.width, targetTexture.height);

            if (rtSize.x < refResolutionX || rtSize.y < refResolutionY)
            {
                GUILayout.Box("Target resolution is smaller than the reference resolution. Image may appear stretched or cropped.");
            }

            GUI.color = oldColor;
        }

#endif

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            m_ComponentVersion = k_CurrentComponentVersion;
#endif
        }

        /// <summary>
        /// OnAfterSerialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            // Upgrade from no serialized version
            if (m_ComponentVersion == ComponentVersions.Version_Unserialized)
            {
                if (m_UpscaleRT)
                    m_GridSnapping = GridSnapping.UpscaleRenderTexture;
                else if (m_PixelSnapping)
                    m_GridSnapping = GridSnapping.PixelSnapping;

                if (m_CropFrameX && m_CropFrameY)
                {
                    if (m_StretchFill)
                        m_CropFrame = CropFrame.StretchFill;
                    else
                        m_CropFrame = CropFrame.Windowbox;
                }
                else if (m_CropFrameX)
                {
                    m_CropFrame = CropFrame.Pillarbox;
                }
                else if (m_CropFrameY)
                {
                    m_CropFrame = CropFrame.Letterbox;
                }
                else
                {
                    m_CropFrame = CropFrame.None;
                }

                m_ComponentVersion = ComponentVersions.Version_1;
            }
#endif
        }
    }
}
