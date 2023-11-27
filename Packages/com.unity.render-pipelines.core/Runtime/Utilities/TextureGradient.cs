using System;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    // Due to limitations in the builtin Gradient we need this custom wrapper.

    /// <summary>
    /// A wrapper around <c>Gradient</c> to automatically bake it into a texture.
    /// </summary>
    [Serializable]
    public class TextureGradient : IDisposable
    {
        /// <summary>
        /// Texture Size computed.
        /// </summary>
        [field: SerializeField, HideInInspector]
        public int textureSize { get; private set; }

        /// <summary>
        /// Internal Gradient used to generate the Texture
        /// </summary>
        [SerializeField]
        Gradient m_Gradient;

        Texture2D m_Texture = null;

        int m_RequestedTextureSize = -1;

        bool m_IsTextureDirty;
        bool m_Precise;

        /// <summary>All color keys defined in the gradient.</summary>
        [HideInInspector]
        public GradientColorKey[] colorKeys => m_Gradient?.colorKeys;

        /// <summary>All alpha keys defined in the gradient.</summary>
        [HideInInspector]
        public GradientAlphaKey[] alphaKeys => m_Gradient?.alphaKeys;

        /// <summary>Controls how the gradient colors are interpolated.</summary>
        [SerializeField, HideInInspector]
        public GradientMode mode = GradientMode.PerceptualBlend;

        /// <summary>Indicates the color space that the gradient color keys are using.</summary>
        [SerializeField, HideInInspector]
        public ColorSpace colorSpace = ColorSpace.Uninitialized;

        /// <summary>
        /// Creates a new <see cref="TextureGradient"/> from an existing <c>Gradient</c>.
        /// </summary>
        /// <param name="baseCurve">The source <c>Gradient</c>.</param>
        public TextureGradient(Gradient baseCurve)
            : this(baseCurve.colorKeys, baseCurve.alphaKeys)
        {
            mode = baseCurve.mode;
            colorSpace = baseCurve.colorSpace;
            m_Gradient.mode = baseCurve.mode;
            m_Gradient.colorSpace = baseCurve.colorSpace;
        }

        /// <summary>
        /// Creates a new <see cref="TextureCurve"/> from an arbitrary number of keyframes.
        /// </summary>
        /// <param name="colorKeys">An array of keyframes used to define the color of gradient.</param>
        /// <param name="alphaKeys">An array of keyframes used to define the alpha of gradient.</param>
        /// <param name="mode">Indicates the color space that the gradient color keys are using.</param>
        /// <param name="colorSpace">Controls how the gradient colors are interpolated.</param>
        /// <param name="requestedTextureSize">Texture Size used internally, if '-1' using Nyquist-Shannon limits.</param>
        /// <param name="precise">if precise uses 4*Nyquist-Shannon limits, 2* otherwise.</param>
        public TextureGradient(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys, GradientMode mode = GradientMode.PerceptualBlend, ColorSpace colorSpace = ColorSpace.Uninitialized, int requestedTextureSize = -1, bool precise = false)
        {
            Rebuild(colorKeys, alphaKeys, mode, colorSpace, requestedTextureSize, precise);
        }

        void Rebuild(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys, GradientMode mode, ColorSpace colorSpace, int requestedTextureSize, bool precise)
        {
            m_Gradient = new Gradient();
            m_Gradient.mode = mode;
            m_Gradient.colorSpace = colorSpace;
            m_Gradient.SetKeys(colorKeys, alphaKeys);
            m_Precise = precise;
            m_RequestedTextureSize = requestedTextureSize;
            if (requestedTextureSize > 0)
            {
                textureSize = requestedTextureSize;
            }
            else
            {
                float smallestDelta = 1.0f;
                float[] times = new float[colorKeys.Length + alphaKeys.Length];
                for (int i = 0; i < colorKeys.Length; ++i)
                {
                    times[i] = colorKeys[i].time;
                }
                for (int i = 0; i < alphaKeys.Length; ++i)
                {
                    times[colorKeys.Length + i] = alphaKeys[i].time;
                }
                Array.Sort(times);
                // Found the smallest increment between 2 keys
                for (int i = 1; i < times.Length; ++i)
                {
                    int k0 = Math.Max(i - 1, 0);
                    int k1 = Math.Min(i, times.Length - 1);
                    float delta = Mathf.Abs(times[k0] - times[k1]);
                    // Do not compare if time is duplicated
                    if (delta > 0 && delta < smallestDelta)
                        smallestDelta = delta;
                }

                // Nyquist-Shannon
                // smallestDelta: 1.00f => Sampling => 2
                // smallestDelta: 0.50f => Sampling => 3
                // smallestDelta: 0.33f => Sampling => 4
                // smallestDelta: 0.25f => Sampling => 5

                // 2x: Theoretical limits
                // 4x: Preserve original frequency

                // Round to the closest 4 * Nyquist-Shannon limits
                // 4x for Fixed to capture sharp discontinuity
                float scale;
                if (precise || mode == GradientMode.Fixed)
                    scale = 4.0f;
                else
                    scale = 2.0f;
                float sizef = scale * Mathf.Ceil(1.0f / smallestDelta + 1.0f);
                textureSize = Mathf.RoundToInt(sizef);
                // Arbitrary max (1024)
                textureSize = Math.Min(textureSize, 1024);
            }

            SetDirty();
        }

        /// <summary>
        /// Cleans up the internal texture resource.
        /// </summary>
        public void Dispose()
        {
            //Release();
        }

        /// <summary>
        /// Releases the internal texture resource.
        /// </summary>
        public void Release()
        {
            if (m_Texture != null)
                CoreUtils.Destroy(m_Texture);
            m_Texture = null;
        }

        /// <summary>
        /// Marks the curve as dirty to trigger a redraw of the texture the next time <see cref="GetTexture"/>
        /// is called.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirty()
        {
            m_IsTextureDirty = true;
        }

        static GraphicsFormat GetTextureFormat()
        {
            return GraphicsFormat.R8G8B8A8_UNorm;
        }

        /// <summary>
        /// Gets the texture representation of this Gradient.
        /// </summary>
        /// <returns>A texture.</returns>
        public Texture2D GetTexture()
        {
            float step = 1.0f / (float)(textureSize - 1);

            if (m_Texture != null && m_Texture.width != textureSize)
            {
                Object.DestroyImmediate(m_Texture);
                m_Texture = null;
            }

            if (m_Texture == null)
            {
                m_Texture = new Texture2D(textureSize, 1, GetTextureFormat(), TextureCreationFlags.None);
                m_Texture.name = "GradientTexture";
                m_Texture.hideFlags = HideFlags.HideAndDontSave;
                m_Texture.filterMode = FilterMode.Bilinear;
                m_Texture.wrapMode = TextureWrapMode.Clamp;
                m_Texture.anisoLevel = 0;
                m_IsTextureDirty = true;
            }

            if (m_IsTextureDirty)
            {
                var pixels = new Color[textureSize];

                for (int i = 0; i < textureSize; i++)
                    pixels[i] = Evaluate(i * step);

                m_Texture.SetPixels(pixels);
                m_Texture.Apply(false, false);
                m_IsTextureDirty = false;
                m_Texture.IncrementUpdateCount();
            }

            return m_Texture;
        }

        /// <summary>
        /// Evaluate a time value on the Gradient.
        /// </summary>
        /// <param name="time">The time within the Gradient you want to evaluate.</param>
        /// <returns>The value of the Gradient, at the point in time specified.</returns>
        public Color Evaluate(float time)
        {
            if (textureSize <= 0)
                return Color.black;

            return m_Gradient.Evaluate(time);
        }


        /// <summary>
        /// Setup Gradient with an array of color keys and alpha keys.
        /// </summary>
        /// <param name="colorKeys">Color keys of the gradient (maximum 8 color keys).</param>
        /// <param name="alphaKeys">Alpha keys of the gradient (maximum 8 alpha keys).</param>
        /// <param name="mode">Indicates the color space that the gradient color keys are using.</param>
        /// <param name="colorSpace">Controls how the gradient colors are interpolated.</param>
        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys, GradientMode mode, ColorSpace colorSpace)
        {
            m_Gradient.SetKeys(colorKeys, alphaKeys);
            m_Gradient.mode = mode;
            m_Gradient.colorSpace = colorSpace;
            // Rebuild will make the TextureGradient Dirty.
            Rebuild(colorKeys, alphaKeys, mode, colorSpace, m_RequestedTextureSize, m_Precise);
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="TextureGradient"/> value.
    /// </summary>
    [Serializable]
    public class TextureGradientParameter : VolumeParameter<TextureGradient>
    {
        /// <summary>
        /// Creates a new <see cref="TextureGradientParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public TextureGradientParameter(TextureGradient value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Release implementation.
        /// </summary>
        public override void Release() => m_Value.Release();

        // TODO: TextureGradient interpolation
    }
}
