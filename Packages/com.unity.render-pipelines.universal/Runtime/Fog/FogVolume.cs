#if VOLUMETRIC_FOG

using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Options for how a <see cref="FogVolume"/> distributes fog density.
    /// </summary>
    public enum FogVolumeDensityMode
    {
        /// <summary>
        /// Applies the same density everywhere inside the fog volume.
        /// </summary>
        Constant,

        /// <summary>
        /// Modulates the density with a 3D texture.
        /// </summary>
        Texture,
    }

    /// <summary>
    /// Options for how a <see cref="FogVolume"/> combines with the global fog and with overlapping fog volumes.
    /// </summary>
    public enum FogVolumeBlendMode
    {
        /// <summary>
        /// Replaces the existing fog with this volume's fog.
        /// </summary>
        Overwrite,

        /// <summary>
        /// Adds this volume's fog to the existing fog.
        /// </summary>
        Add,

        /// <summary>
        /// Multiplies the existing fog by this volume's fog.
        /// </summary>
        Multiply,

        /// <summary>
        /// Keeps the lower of the existing fog and this volume's fog.
        /// </summary>
        Min,

        /// <summary>
        /// Keeps the higher of the existing fog and this volume's fog.
        /// </summary>
        Max,
    }

    /// <summary>
    /// Represents a box-shaped volume that changes the volumetric fog inside it.
    /// </summary>
    /// <remarks>
    /// The GameObject's Transform sets the position, rotation, and size of the box.
    /// </remarks>
    [ExecuteAlways]
    [AddComponentMenu("Rendering/Fog Volume")]
    [Icon("Packages/com.unity.render-pipelines.core/Editor/Icons/Processed/LocalVolumetricFog Icon.asset")]
    public sealed class FogVolume : MonoBehaviour
    {
        [SerializeField]
        FogVolumeDensityMode m_DensityMode;

        [SerializeField, Min(0.0f)]
        float m_Density = 0.1f;

        [SerializeField, ColorUsage(false)]
        Color m_Albedo = Color.white;

        [SerializeField]
        Texture m_DensityTexture;

        [SerializeField]
        Vector3 m_DensityTextureTiling = Vector3.one;

        [SerializeField]
        Vector3 m_DensityTextureScrollSpeed;

        [SerializeField]
        FogVolumeBlendMode m_BlendMode = FogVolumeBlendMode.Add;

        [SerializeField]
        int m_BlendPriority;

        internal static event Action<FogVolume> fogVolumeEnabled;
        internal static event Action<FogVolume> fogVolumeDisabled;
        internal static event Action<FogVolume> fogVolumeChanged;

        /// <summary>
        /// The method the fog volume uses to distribute fog density.
        /// </summary>
        public FogVolumeDensityMode densityMode
        {
            get => m_DensityMode;
            set
            {
                m_DensityMode = value;
                OnValidate();
            }
        }

        /// <summary>
        /// The fog density inside the fog volume.
        /// </summary>
        /// <remarks>
        /// Higher values make the fog thicker. The minimum value is 0.
        /// </remarks>
        public float density
        {
            get => m_Density;
            set
            {
                m_Density = Mathf.Max(0.0f, value);
                OnValidate();
            }
        }

        /// <summary>
        /// The color of the light that the fog scatters.
        /// </summary>
        /// <remarks>
        /// The albedo is an SDR color. Each channel is clamped to the range 0 to 1, and alpha is ignored.
        /// </remarks>
        public Color albedo
        {
            get => m_Albedo;
            set
            {
                m_Albedo = new Color(Mathf.Clamp01(value.r), Mathf.Clamp01(value.g), Mathf.Clamp01(value.b), 1.0f);
                OnValidate();
            }
        }

        /// <summary>
        /// The 3D texture that modulates the fog when <see cref="densityMode"/> is <see cref="FogVolumeDensityMode.Texture"/>.
        /// </summary>
        /// <remarks>
        /// The RGB channels tint the albedo, and the alpha channel scales the density. Alpha-only textures only scale the density.
        /// The texture must be a <see cref="Texture3D"/> or a 3D <see cref="RenderTexture"/>, otherwise the fog volume has no fog.
        /// </remarks>
        public Texture densityTexture
        {
            get => m_DensityTexture;
            set
            {
                m_DensityTexture = value;
                OnValidate();
            }
        }

        /// <summary>
        /// The number of times the density texture repeats across the fog volume along each axis.
        /// </summary>
        public Vector3 densityTextureTiling
        {
            get => m_DensityTextureTiling;
            set
            {
                m_DensityTextureTiling = value;
                OnValidate();
            }
        }

        /// <summary>
        /// The speed at which the density texture moves through the fog volume along each axis, in texture repeats per second.
        /// </summary>
        public Vector3 densityTextureScrollSpeed
        {
            get => m_DensityTextureScrollSpeed;
            set
            {
                m_DensityTextureScrollSpeed = value;
                OnValidate();
            }
        }

        /// <summary>
        /// The method the fog volume uses to combine with the global fog and with overlapping fog volumes.
        /// </summary>
        public FogVolumeBlendMode blendMode
        {
            get => m_BlendMode;
            set
            {
                m_BlendMode = value;
                OnValidate();
            }
        }

        /// <summary>
        /// The order in which overlapping fog volumes blend, from lowest to highest.
        /// </summary>
        public int blendPriority
        {
            get => m_BlendPriority;
            set
            {
                m_BlendPriority = value;
                OnValidate();
            }
        }

        void OnEnable()
        {
            fogVolumeEnabled?.Invoke(this);
        }

        void OnDisable()
        {
            fogVolumeDisabled?.Invoke(this);
        }

        void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            fogVolumeChanged?.Invoke(this);
        }

        void OnDidApplyAnimationProperties()
        {
            OnValidate();
        }
    }
}

#endif
