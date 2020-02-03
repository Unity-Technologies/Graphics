using System;

namespace UnityEngine.Rendering.HighDefinition
{
    internal enum CameraProjection { Perspective, Orthographic };

    /// <summary>Obsolete</summary>
    [Flags]
    [Obsolete]
    internal enum ObsoleteCaptureSettingsOverrides
    {
        //CubeResolution = 1 << 0,
        //PlanarResolution = 1 << 1,
        /// <summary>Obsolete</summary>
        ClearColorMode = 1 << 2,
        /// <summary>Obsolete</summary>
        BackgroundColorHDR = 1 << 3,
        /// <summary>Obsolete</summary>
        ClearDepth = 1 << 4,
        /// <summary>Obsolete</summary>
        CullingMask = 1 << 5,
        /// <summary>Obsolete</summary>
        UseOcclusionCulling = 1 << 6,
        /// <summary>Obsolete</summary>
        VolumeLayerMask = 1 << 7,
        /// <summary>Obsolete</summary>
        VolumeAnchorOverride = 1 << 8,
        /// <summary>Obsolete</summary>
        Projection = 1 << 9,
        /// <summary>Obsolete</summary>
        NearClip = 1 << 10,
        /// <summary>Obsolete</summary>
        FarClip = 1 << 11,
        /// <summary>Obsolete</summary>
        FieldOfview = 1 << 12,
        /// <summary>Obsolete</summary>
        OrphographicSize = 1 << 13,
        /// <summary>Obsolete</summary>
        RenderingPath = 1 << 14,
        //Aperture = 1 << 15,
        //ShutterSpeed = 1 << 16,
        //Iso = 1 << 17,
        /// <summary>Obsolete</summary>
        ShadowDistance = 1 << 18,
    }

    /// <summary>Obsolete</summary>
    [Serializable]
    [Obsolete]
    internal class ObsoleteCaptureSettings
    {
        /// <summary>Obsolete</summary>
        public static ObsoleteCaptureSettings @default = new ObsoleteCaptureSettings();

        /// <summary>Obsolete</summary>
        public ObsoleteCaptureSettingsOverrides overrides;

        /// <summary>Obsolete</summary>
        public HDAdditionalCameraData.ClearColorMode clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
        /// <summary>Obsolete</summary>
        [ColorUsage(true, true)]
        public Color backgroundColorHDR = new Color32(6, 18, 48, 0);
        /// <summary>Obsolete</summary>
        public bool clearDepth = true;

        /// <summary>Obsolete</summary>
        public LayerMask cullingMask = -1; //= 0xFFFFFFFF which is c++ default
        /// <summary>Obsolete</summary>
        public bool useOcclusionCulling = true;

        /// <summary>Obsolete</summary>
        public LayerMask volumeLayerMask = 1;
        /// <summary>Obsolete</summary>
        public Transform volumeAnchorOverride;

        /// <summary>Obsolete</summary>
        public CameraProjection projection = CameraProjection.Perspective;
        /// <summary>Obsolete</summary>
        public float nearClipPlane = 0.3f;
        /// <summary>Obsolete</summary>
        public float farClipPlane = 1000f;
        /// <summary>Obsolete</summary>
        public float fieldOfView = 90.0f;   //90f for a face of a cubemap
        /// <summary>Obsolete</summary>
        public float orthographicSize = 5f;

        /// <summary>Obsolete</summary>
        public int renderingPath = 0; //0 = former RenderingPath.UseGraphicsSettings

        //public float aperture = 8f;
        //public float shutterSpeed = 1f / 200f;
        //public float iso = 400f;

        /// <summary>Obsolete</summary>
        public float shadowDistance = 100.0f;
    }
}
