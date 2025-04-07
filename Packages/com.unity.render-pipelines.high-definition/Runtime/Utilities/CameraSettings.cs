using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A bit flag for each camera settings.
    /// </summary>
    [Flags]
    public enum CameraSettingsFields
    {
        /// <summary>No field</summary>
        none = 0,
        /// <summary>BufferClear.clearColorMode</summary>
        bufferClearColorMode = 1 << 1,
        /// <summary>BufferClear.backgroundColorHDR</summary>
        bufferClearBackgroundColorHDR = 1 << 2,
        /// <summary>BufferClear.clearDepth</summary>
        bufferClearClearDepth = 1 << 3,
        /// <summary>volumes.layerMask</summary>
        volumesLayerMask = 1 << 4,
        /// <summary>volumes.anchorOverride</summary>
        volumesAnchorOverride = 1 << 5,
        /// <summary>frustum.mode</summary>
        frustumMode = 1 << 6,
        /// <summary>frustum.aspect</summary>
        frustumAspect = 1 << 7,
        /// <summary>frustum.farClipPlane</summary>
        frustumFarClipPlane = 1 << 8,
        /// <summary>frustum.nearClipPlane</summary>
        frustumNearClipPlane = 1 << 9,
        /// <summary>frustum.fieldOfView</summary>
        frustumFieldOfView = 1 << 10,
        /// <summary>frustum.projectionMatrix</summary>
        frustumProjectionMatrix = 1 << 11,
        /// <summary>culling.useOcclusionCulling</summary>
        cullingUseOcclusionCulling = 1 << 12,
        /// <summary>culling.cullingMask</summary>
        cullingCullingMask = 1 << 13,
        /// <summary>culling.invertFaceCulling</summary>
        cullingInvertFaceCulling = 1 << 14,
        /// <summary>culling.renderingSettings</summary>
        customRenderingSettings = 1 << 15,
        /// <summary>flipYMode</summary>
        flipYMode = 1 << 16,
        /// <summary>frameSettings</summary>
        frameSettings = 1 << 17,
        /// <summary>probeLayerMask</summary>
        probeLayerMask = 1 << 18
    }

    /// <summary>The overriden fields of a camera settings.</summary>
    [Serializable]
    public struct CameraSettingsOverride
    {
        /// <summary>
        /// Backed value.
        /// </summary>
        public CameraSettingsFields camera;
    }

    /// <summary>Contains all settings required to setup a camera in HDRP.</summary>
    [Serializable]
    public struct CameraSettings
    {
        /// <summary>Defines how color and depth buffers are cleared.</summary>
        [Serializable]
        public struct BufferClearing
        {
            /// <summary>Default value.</summary>
            [Obsolete("Since 2019.3, use BufferClearing.NewDefault() instead.")]
            public static readonly BufferClearing @default = default;
            /// <summary>Default value.</summary>
            /// <returns>The default value.</returns>
            public static BufferClearing NewDefault() => new BufferClearing
            {
                clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky,
                backgroundColorHDR = new Color32(6, 18, 48, 0),
                clearDepth = true
            };

            /// <summary>Define the source for the clear color.</summary>
            public HDAdditionalCameraData.ClearColorMode clearColorMode;
            /// <summary>
            /// The color to use when
            /// <c><see cref="clearColorMode"/> == <see cref="HDAdditionalCameraData.ClearColorMode.Color"/></c>.
            /// </summary>
            [ColorUsage(true, true)]
            public Color backgroundColorHDR;
            /// <summary>True to clear the depth.</summary>
            public bool clearDepth;
        }

        /// <summary>
        /// Defines the settings for querying and evaluating volumes within the framework.
        /// This structure contains options for filtering volumes by <see cref="LayerMask"/> and customizing the location
        /// of volume evaluations through an optional <see cref="Transform"/> anchor override.
        /// These settings control how volumes are processed and how they interact with the scene's camera or other objects.
        /// </summary>
        /// <remarks>
        /// The <see cref="Volumes"/> struct is used for controlling volume behavior, including selecting which volumes
        /// to query using the <see cref="LayerMask"/> and overriding the default evaluation anchor. It is useful when
        /// customizing volume queries and evaluation locations within a scene.
        /// </remarks>
        [Serializable]
        public struct Volumes
        {
            /// <summary>
            /// Default value for volume settings. This is the default configuration used before any customizations are applied.
            /// </summary>
            /// <remarks>
            /// The default value uses a <see cref="LayerMask"/> of -1 (which includes all layers) and a null override
            /// for the anchor (indicating no anchor override).
            /// </remarks>
            [Obsolete("This field is obsolete use Volumes.NewDefault() instead. #from(2019.3)", true)]
            public static readonly Volumes @default = default;

            /// <summary>
            /// Creates a new default <see cref="Volumes"/> instance with predefined settings.
            /// The default configuration includes a <see cref="LayerMask"/> that includes all layers and a null anchor override.
            /// </summary>
            /// <returns>The default <see cref="Volumes"/> configuration.</returns>
            /// <example>
            /// <code>
            /// Volumes defaultVolumes = Volumes.NewDefault();
            /// </code>
            /// </example>
            /// <remarks>
            /// This method returns a fresh instance of <see cref="Volumes"/> with default values. It can be used to initialize
            /// the volume settings before applying specific customizations like setting the <see cref="LayerMask"/> or overriding
            /// the evaluation anchor.
            /// </remarks>
            public static Volumes NewDefault() => new Volumes
            {
                layerMask = -1,
                anchorOverride = null
            };

            /// <summary>
            /// The <see cref="LayerMask"/> used to filter which volumes should be evaluated.
            /// This setting allows you to control which layers are considered during volume evaluation.
            /// </summary>
            /// <remarks>
            /// A <see cref="LayerMask"/> of -1 includes all layers, while setting specific values allows you to limit evaluation
            /// to certain layers.
            /// </remarks>
            public LayerMask layerMask;

            /// <summary>
            /// If not null, specifies a custom location for evaluating the volumes.
            /// This allows for overriding the default anchor point for volume processing.
            /// </summary>
            /// <remarks>
            /// If <see cref="anchorOverride"/> is set to null, the default evaluation location is used. This property provides
            /// additional flexibility in controlling where volumes are processed within the scene.
            /// </remarks>
            public Transform anchorOverride;
        }


        /// <summary>Defines the projection matrix of the camera.</summary>
        [Serializable]
        public struct Frustum
        {
            // Below 1e-5, it causes errors
            // in `ScriptableShadowsUtility::GetPSSMSplitMatricesAndCulling`: "Expanding invalid MinMaxAABB"
            // So we use 1e-5 as the minimum value.
            /// <summary>The near clip plane value will be above this value.</summary>
            public const float MinNearClipPlane = 1e-5f;
            /// <summary> The far clip plane value will be at least above <c><see cref="nearClipPlane"/> + <see cref="MinFarClipPlane"/></c></summary>
            public const float MinFarClipPlane = 1e-4f;

            /// <summary>Default value.</summary>
            [Obsolete("Since 2019.3, use Frustum.NewDefault() instead.")]
            public static readonly Frustum @default = default;
            /// <summary>Default value.</summary>
            /// <returns>The default value.</returns>
            public static Frustum NewDefault() => new Frustum
            {
                mode = Mode.ComputeProjectionMatrix,
                aspect = 1.0f,
                farClipPlaneRaw = 1000.0f,
                nearClipPlaneRaw = 0.1f,
                fieldOfView = 90.0f,
                projectionMatrix = Matrix4x4.identity
            };

            /// <summary>Defines how the projection matrix is computed.</summary>
            public enum Mode
            {
                /// <summary>
                /// For perspective projection, the matrix is computed from <see cref="aspect"/>,
                /// <see cref="farClipPlane"/>, <see cref="nearClipPlane"/> and <see cref="fieldOfView"/> parameters.
                ///
                /// Orthographic projection is not currently supported.
                /// </summary>
                ComputeProjectionMatrix,
                /// <summary>The projection matrix provided is assigned.</summary>
                UseProjectionMatrixField
            }

            /// <summary>Which mode will be used for the projection matrix.</summary>
            public Mode mode;
            /// <summary>Aspect ratio of the frustum (width/height).</summary>
            public float aspect;

            /// <summary>
            /// Far clip plane distance.
            ///
            /// Value that will be stored for the far clip plane distance.
            /// IF you need the effective far clip plane distance, use <see cref="farClipPlane"/>.
            /// </summary>
            [FormerlySerializedAs("farClipPlane")]
            public float farClipPlaneRaw;
            /// <summary>
            /// Near clip plane distance.
            ///
            /// Value that will be stored for the near clip plane distance.
            /// IF you need the effective near clip plane distance, use <see cref="nearClipPlane"/>.
            /// </summary>
            [FormerlySerializedAs("nearClipPlane")]
            public float nearClipPlaneRaw;

            /// <summary>
            /// Effective far clip plane distance.
            ///
            /// Use this value to compute the projection matrix.
            ///
            /// This value is valid to compute a projection matrix.
            /// If you need the raw stored value, see <see cref="farClipPlaneRaw"/> instead.
            /// </summary>
            public float farClipPlane => Mathf.Max(nearClipPlaneRaw + MinFarClipPlane, farClipPlaneRaw);

            /// <summary>
            /// Effective near clip plane distance.
            ///
            /// Use this value to compute the projection matrix.
            ///
            /// This value is valid to compute a projection matrix.
            /// If you need the raw stored value, see <see cref="nearClipPlaneRaw"/> instead.
            /// </summary>
            public float nearClipPlane => Mathf.Max(MinNearClipPlane, nearClipPlaneRaw);

            /// <summary>Field of view for perspective matrix (for y axis, in degree).</summary>
            [Range(1, 179.0f)]
            public float fieldOfView;

            /// <summary>Projection matrix used for <see cref="Mode.UseProjectionMatrixField"/> mode.</summary>
            public Matrix4x4 projectionMatrix;

            /// <summary>Compute the projection matrix based on the mode and settings provided.</summary>
            /// <returns>The projection matrix.</returns>
            public Matrix4x4 ComputeProjectionMatrix()
            {
                return Matrix4x4.Perspective(HDUtils.ClampFOV(fieldOfView), aspect, nearClipPlane, farClipPlane);
            }

            /// <summary>
            /// Get the projection matrix used depending on the projection mode.
            /// </summary>
            /// <returns>The projection matrix</returns>
            public Matrix4x4 GetUsedProjectionMatrix()
            {
                switch (mode)
                {
                    case Mode.ComputeProjectionMatrix: return ComputeProjectionMatrix();
                    case Mode.UseProjectionMatrixField: return projectionMatrix;
                    default: throw new ArgumentException();
                }
            }
        }

        /// <summary>Defines the culling settings of the camera.</summary>
        [Serializable]
        public struct Culling
        {
            /// <summary>Default value.</summary>
            [Obsolete("Since 2019.3, use Culling.NewDefault() instead.")]
            public static readonly Culling @default = default;
            /// <summary>Default value.</summary>
            /// <returns>The default value.</returns>
            public static Culling NewDefault() => new Culling
            {
                cullingMask = -1,
                useOcclusionCulling = true,
                sceneCullingMaskOverride = 0
            };

            /// <summary>True when occlusion culling will be performed during rendering, false otherwise.</summary>
            public bool useOcclusionCulling;
            /// <summary>The mask for visible objects.</summary>
            public LayerMask cullingMask;
            /// <summary>Scene culling mask override.</summary>
            public ulong sceneCullingMaskOverride;
        }

        /// <summary>Default value.</summary>
        [Obsolete("Since 2019.3, use CameraSettings.defaultCameraSettingsNonAlloc instead.")]
        public static readonly CameraSettings @default = default;
        /// <summary>Default value.</summary>
        /// <returns>The default value and allocate ~250B of garbage.</returns>
        public static CameraSettings NewDefault() => new CameraSettings
        {
            bufferClearing = BufferClearing.NewDefault(),
            culling = Culling.NewDefault(),
            renderingPathCustomFrameSettings = FrameSettingsDefaults.Get(FrameSettingsRenderType.Camera),
            frustum = Frustum.NewDefault(),
            customRenderingSettings = false,
            volumes = Volumes.NewDefault(),
            flipYMode = HDAdditionalCameraData.FlipYMode.Automatic,
            invertFaceCulling = false,
            probeLayerMask = ~0,
            probeRangeCompressionFactor = 1.0f
        };

        /// <summary>Default camera settings.</summary>
        public static readonly CameraSettings defaultCameraSettingsNonAlloc = NewDefault();

        /// <summary>
        /// Extract the CameraSettings from an HDCamera
        /// </summary>
        /// <param name="hdCamera">The camera to extract from</param>
        /// <returns>The CameraSettings</returns>
        public static CameraSettings From(HDCamera hdCamera)
        {
            var settings = defaultCameraSettingsNonAlloc;
            settings.culling.cullingMask = hdCamera.camera.cullingMask;
            settings.culling.useOcclusionCulling = hdCamera.camera.useOcclusionCulling;
            settings.culling.sceneCullingMaskOverride = HDUtils.GetSceneCullingMaskFromCamera(hdCamera.camera);
            settings.frustum.aspect = hdCamera.camera.aspect;
            settings.frustum.farClipPlaneRaw = hdCamera.camera.farClipPlane;
            settings.frustum.nearClipPlaneRaw = hdCamera.camera.nearClipPlane;
            settings.frustum.fieldOfView = hdCamera.camera.fieldOfView;
            settings.frustum.mode = Frustum.Mode.UseProjectionMatrixField;
            settings.frustum.projectionMatrix = hdCamera.camera.projectionMatrix;
            settings.invertFaceCulling = false;

            HDAdditionalCameraData add;
            if (hdCamera.camera.TryGetComponent<HDAdditionalCameraData>(out add))
            {
                settings.customRenderingSettings = add.customRenderingSettings;
                settings.bufferClearing.backgroundColorHDR = add.backgroundColorHDR;
                settings.bufferClearing.clearColorMode = add.clearColorMode;
                settings.bufferClearing.clearDepth = add.clearDepth;
                settings.flipYMode = add.flipYMode;
                settings.renderingPathCustomFrameSettings = add.renderingPathCustomFrameSettings;
                settings.renderingPathCustomFrameSettingsOverrideMask = add.renderingPathCustomFrameSettingsOverrideMask;
                settings.volumes = new Volumes
                {
                    anchorOverride = add.volumeAnchorOverride,
                    layerMask = add.volumeLayerMask
                };
                settings.probeLayerMask = add.probeLayerMask;
                settings.invertFaceCulling = add.invertFaceCulling;
            }

            // (case 1131731) Camera.RenderToCubemap inverts faces
            // Unity's API is using LHS standard when rendering cubemaps, so we need to invert the face culling
            //     in that specific case.
            // We try to guess with a lot of constraints when this is the case.
            var isLHSViewMatrix = hdCamera.camera.worldToCameraMatrix.determinant > 0;
            var isPerspectiveMatrix = Mathf.Approximately(hdCamera.camera.projectionMatrix.m32, -1);
            var isFOV45Degrees = Mathf.Approximately(hdCamera.camera.projectionMatrix.m00, 1)
                && Mathf.Approximately(hdCamera.camera.projectionMatrix.m11, 1);

            if (isLHSViewMatrix && isPerspectiveMatrix && isFOV45Degrees)
                settings.invertFaceCulling = true;

            return settings;
        }

        /// <summary>Override rendering settings if true.</summary>
        public bool customRenderingSettings;
        /// <summary>Frame settings to use.</summary>
        public FrameSettings renderingPathCustomFrameSettings;
        /// <summary>Frame settings mask to use.</summary>
        public FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;
        /// <summary>Buffer clearing settings to use.</summary>
        public BufferClearing bufferClearing;
        /// <summary>Volumes settings to use.</summary>
        public Volumes volumes;
        /// <summary>Frustum settings to use.</summary>
        public Frustum frustum;
        /// <summary>Culling settings to use.</summary>
        public Culling culling;
        /// <summary>True to invert face culling, false otherwise.</summary>
        public bool invertFaceCulling;
        /// <summary>The mode to use when we want to flip the Y axis.</summary>
        public HDAdditionalCameraData.FlipYMode flipYMode;
        /// <summary>The layer mask to use to filter probes that can influence this camera.</summary>
        public LayerMask probeLayerMask;
        /// <summary>Which default FrameSettings should be used when rendering with these parameters.</summary>
        public FrameSettingsRenderType defaultFrameSettings;

        // Marked as internal as it is here just for propagation purposes, the correct way to edit this value is through the probe itself.
        internal float probeRangeCompressionFactor;

        [SerializeField]
        [FormerlySerializedAs("renderingPath")]
        [Obsolete("For data migration")]
        internal int m_ObsoleteRenderingPath;
#pragma warning disable 618 // Type or member is obsolete
        [SerializeField]
        [FormerlySerializedAs("frameSettings")]
        [Obsolete("For data migration")]
        internal ObsoleteFrameSettings m_ObsoleteFrameSettings;
#pragma warning restore 618

        internal Hash128 GetHash()
        {
            var h = new Hash128();
            var h2 = new Hash128();

            HashUtilities.ComputeHash128(ref bufferClearing, ref h);
            HashUtilities.ComputeHash128(ref culling, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref customRenderingSettings, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref defaultFrameSettings, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref flipYMode, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref frustum, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref invertFaceCulling, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref probeLayerMask, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref probeRangeCompressionFactor, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref renderingPathCustomFrameSettings, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref renderingPathCustomFrameSettingsOverrideMask, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            int volumeHash = volumes.GetHashCode();
            h2 = new Hash128((ulong)volumeHash, 0);
            HashUtilities.AppendHash(ref h2, ref h);

            return h;
        }
    }
}
