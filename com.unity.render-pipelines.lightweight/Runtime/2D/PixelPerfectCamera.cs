using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    /// <summary>
    /// The Pixel Perfect Camera component ensures your pixel art remains crisp and clear at different resolutions, and stable in motion.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Pixel Perfect Camera (Experimental)")]
    [RequireComponent(typeof(Camera))]
    public class PixelPerfectCamera : MonoBehaviour, IPixelPerfectCamera
    {
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
        public bool upscaleRT { get { return m_UpscaleRT; } set { m_UpscaleRT = value; } }

        /// <summary>
        /// Set to true to prevent subpixel movement and make Sprites appear to move in pixel-by-pixel increments.
        /// Only applicable when upscaleRT is false.
        /// </summary>
        public bool pixelSnapping { get { return m_PixelSnapping; } set { m_PixelSnapping = value; } }

        /// <summary>
        /// Set to true to crop the viewport with black bars to match refResolutionX in the horizontal direction.
        /// </summary>
        public bool cropFrameX { get { return m_CropFrameX; } set { m_CropFrameX = value; } }

        /// <summary>
        /// Set to true to crop the viewport with black bars to match refResolutionY in the vertical direction.
        /// </summary>
        public bool cropFrameY { get { return m_CropFrameY; } set { m_CropFrameY = value; } }

        /// <summary>
        /// Set to true to expand the viewport to fit the screen resolution while maintaining the viewport's aspect ratio.
        /// Only applicable when both cropFrameX and cropFrameY are true.
        /// </summary>
        public bool stretchFill { get { return m_StretchFill; } set { m_StretchFill = value; } }

        /// <summary>
        /// Ratio of the rendered Sprites compared to their original size (readonly).
        /// </summary>
        public int pixelRatio { get { return m_Internal.zoom; } }

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

        [SerializeField] int    m_AssetsPPU         = 100;
        [SerializeField] int    m_RefResolutionX    = 320;
        [SerializeField] int    m_RefResolutionY    = 180;
        [SerializeField] bool   m_UpscaleRT;
        [SerializeField] bool   m_PixelSnapping;
        [SerializeField] bool   m_CropFrameX;
        [SerializeField] bool   m_CropFrameY;
        [SerializeField] bool   m_StretchFill;

        Camera m_Camera;
        PixelPerfectCameraInternal m_Internal;

        bool isRunning
        {
            get
            {
#if UNITY_EDITOR
                return (Application.isPlaying || runInEditMode) && enabled;
#else
                return enabled;
#endif
            }
        }

        internal FilterMode finalBlitFilterMode
        {
            get
            {
                if (!isRunning)
                    return FilterMode.Bilinear;
                else
                    return m_Internal.useStretchFill ? FilterMode.Bilinear : FilterMode.Point;
            }
        }

        internal Rect finalBlitPixelRect
        {
            get
            {
                if (!isRunning || !m_Internal.useOffscreenRT)
                    return Rect.zero;
                else
                    return m_Internal.CalculateFinalBlitPixelRect(m_Camera.aspect, Screen.width, Screen.height);
            }
        }

        internal bool useOffscreenRT
        {
            get
            {
                if (!isRunning)
                    return false;
                else
                    return m_Internal.useOffscreenRT;
            }
        }

        // Snap camera position to pixels using Camera.worldToCameraMatrix.
        void PixelSnap()
        {
            Vector3 cameraPosition = m_Camera.transform.position;
            Vector3 roundedCameraPosition = RoundToPixel(cameraPosition);
            Vector3 offset = roundedCameraPosition - cameraPosition;
            offset.z = -offset.z;
            Matrix4x4 offsetMatrix = Matrix4x4.TRS(-offset, Quaternion.identity, new Vector3(1.0f, 1.0f, -1.0f));

            m_Camera.worldToCameraMatrix = offsetMatrix * m_Camera.transform.worldToLocalMatrix;
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
            m_Internal = new PixelPerfectCameraInternal(this);

            m_Internal.originalOrthoSize = m_Camera.orthographicSize;

            if (m_Camera.targetTexture != null)
                Debug.LogWarning("Render to texture is not supported by Pixel Perfect Camera.", m_Camera);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != m_Camera)
                return;

            m_Internal.CalculateCameraProperties(Screen.width, Screen.height);

            PixelSnap();

            if (m_Internal.pixelRect != Rect.zero)
                m_Camera.pixelRect = m_Internal.pixelRect;
            else
                m_Camera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

            m_Camera.orthographicSize = m_Internal.orthoSize;

            U2D.PixelPerfectRendering.pixelSnapSpacing = m_Internal.unitsPerPixel;
        }

        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == m_Camera)
                U2D.PixelPerfectRendering.pixelSnapSpacing = 0.0f;
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

        internal void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            m_Camera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            m_Camera.orthographicSize = m_Internal.originalOrthoSize;
            m_Camera.ResetAspect();
            m_Camera.ResetWorldToCameraMatrix();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
        }

        // Show on-screen warning about invalid render resolutions.
        void OnGUI()
        {
            if (!Debug.isDebugBuild && !Application.isEditor)
                return;

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && !runInEditMode)
                return;
#endif

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

            if (Screen.width < refResolutionX || Screen.height < refResolutionY)
            {
                GUILayout.Box("Screen resolution is smaller than the reference resolution. Image may appear stretched or cropped.");
            }

            GUI.color = oldColor;
        }

#if UNITY_EDITOR
        void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            // Stop running in edit mode when entering play mode.
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
            {
                runInEditMode = false;
                OnDisable();
            }
        }
#endif
    }
}
