using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering
{
    // Due to limitations in the builtin AnimationCurve we need this custom wrapper.
    // Improvements:
    //   - Dirty state handling so we know when a curve has changed or not
    //   - Looping support (infinite curve)
    //   - Zero-value curve
    //   - Cheaper length property

    /// <summary>
    /// A wrapper around <c>AnimationCurve</c> to automatically bake it into a texture.
    /// </summary>
    [Serializable]
    public class TextureCurve : IDisposable
    {
        const int k_Precision = 128; // Edit LutBuilder3D if you change this value
        const float k_Step = 1f / k_Precision;

        /// <summary>
        /// The number of keys in the curve.
        /// </summary>
        [field: SerializeField]
        public int length { get; private set; } // Calling AnimationCurve.length is very slow, let's cache it

        [SerializeField]
        bool m_Loop;

        [SerializeField]
        float m_ZeroValue;

        [SerializeField]
        float m_Range;

        [SerializeField]
        AnimationCurve m_Curve;

        AnimationCurve m_LoopingCurve;
        Texture2D m_Texture;

        bool m_IsCurveDirty;
        bool m_IsTextureDirty;

        /// <summary>
        /// Retrieves the key at index.
        /// </summary>
        /// <param name="index">The index to look for.</param>
        /// <returns>A key.</returns>
        public Keyframe this[int index] => m_Curve[index];

        /// <summary>
        /// Creates a new <see cref="TextureCurve"/> from an existing <c>AnimationCurve</c>.
        /// </summary>
        /// <param name="baseCurve">The source <c>AnimationCurve</c>.</param>
        /// <param name="zeroValue">The default value to use when the curve doesn't have any key.</param>
        /// <param name="loop">Should the curve automatically loop in the given <paramref name="bounds"/>?</param>
        /// <param name="bounds">The boundaries of the curve.</param>
        public TextureCurve(AnimationCurve baseCurve, float zeroValue, bool loop, in Vector2 bounds)
            : this(baseCurve.keys, zeroValue, loop, bounds) {}

        /// <summary>
        /// Creates a new <see cref="TextureCurve"/> from an arbitrary number of keyframes.
        /// </summary>
        /// <param name="keys">An array of Keyframes used to define the curve.</param>
        /// <param name="zeroValue">The default value to use when the curve doesn't have any key.</param>
        /// <param name="loop">Should the curve automatically loop in the given <paramref name="bounds"/>?</param>
        /// <param name="bounds">The boundaries of the curve.</param>
        public TextureCurve(Keyframe[] keys, float zeroValue, bool loop, in Vector2 bounds)
        {
            m_Curve = new AnimationCurve(keys);
            m_ZeroValue = zeroValue;
            m_Loop = loop;
            m_Range = bounds.magnitude;
            length = keys.Length;
            SetDirty();
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~TextureCurve() {}

        /// <summary>
        /// Cleans up the internal texture resource.
        /// </summary>
        [Obsolete("Please use Release() instead.")]
        public void Dispose() {}

        /// <summary>
        /// Releases the internal texture resource.
        /// </summary>
        public void Release()
        {
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
            m_IsCurveDirty = true;
            m_IsTextureDirty = true;
        }

        static TextureFormat GetTextureFormat()
        {
            if (SystemInfo.SupportsTextureFormat(TextureFormat.RHalf))
                return TextureFormat.RHalf;
            if (SystemInfo.SupportsTextureFormat(TextureFormat.R8))
                return TextureFormat.R8;

            return TextureFormat.ARGB32;
        }

        /// <summary>
        /// Gets the texture representation of this curve.
        /// </summary>
        /// <returns>A 128x1 texture.</returns>
        public Texture2D GetTexture()
        {
            if (m_Texture == null)
            {
                m_Texture = new Texture2D(k_Precision, 1, GetTextureFormat(), false, true);
                m_Texture.name = "CurveTexture";
                m_Texture.hideFlags = HideFlags.HideAndDontSave;
                m_Texture.filterMode = FilterMode.Bilinear;
                m_Texture.wrapMode = TextureWrapMode.Clamp;
                m_IsTextureDirty = true;
            }

            if (m_IsTextureDirty)
            {
                var pixels = new Color[k_Precision];

                for (int i = 0; i < pixels.Length; i++)
                    pixels[i].r = Evaluate(i * k_Step);

                m_Texture.SetPixels(pixels);
                m_Texture.Apply(false, false);
                m_IsTextureDirty = false;
            }

            return m_Texture;
        }

        /// <summary>
        /// Evaluate a time value on the curve.
        /// </summary>
        /// <param name="time">The time within the curve you want to evaluate.</param>
        /// <returns>The value of the curve, at the point in time specified.</returns>
        public float Evaluate(float time)
        {
            if (m_IsCurveDirty)
                length = m_Curve.length;

            if (length == 0)
                return m_ZeroValue;

            if (!m_Loop || length == 1)
                return m_Curve.Evaluate(time);

            if (m_IsCurveDirty)
            {
                if (m_LoopingCurve == null)
                    m_LoopingCurve = new AnimationCurve();

                var prev = m_Curve[length - 1];
                prev.time -= m_Range;
                var next = m_Curve[0];
                next.time += m_Range;
                m_LoopingCurve.keys = m_Curve.keys; // GC pressure
                m_LoopingCurve.AddKey(prev);
                m_LoopingCurve.AddKey(next);
                m_IsCurveDirty = false;
            }

            return m_LoopingCurve.Evaluate(time);
        }

        /// <summary>
        /// Adds a new key to the curve.
        /// </summary>
        /// <param name="time">The time at which to add the key.</param>
        /// <param name="value">The value for the key.</param>
        /// <returns>The index of the added key, or -1 if the key could not be added.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddKey(float time, float value)
        {
            int r = m_Curve.AddKey(time, value);

            if (r > -1)
                SetDirty();

            return r;
        }

        /// <summary>
        /// Removes the keyframe at <paramref name="index"/> and inserts <paramref name="key"/>.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="key"></param>
        /// <returns>The index of the keyframe after moving it.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MoveKey(int index, in Keyframe key)
        {
            int r = m_Curve.MoveKey(index, key);
            SetDirty();
            return r;
        }

        /// <summary>
        /// Removes a key.
        /// </summary>
        /// <param name="index">The index of the key to remove.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveKey(int index)
        {
            m_Curve.RemoveKey(index);
            SetDirty();
        }

        /// <summary>
        /// Smoothes the in and out tangents of the keyframe at <paramref name="index"/>. A <paramref name="weight"/> of 0 evens out tangents.
        /// </summary>
        /// <param name="index">The index of the keyframe to be smoothed.</param>
        /// <param name="weight">The smoothing weight to apply to the keyframe's tangents.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SmoothTangents(int index, float weight)
        {
            m_Curve.SmoothTangents(index, weight);
            SetDirty();
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="TextureCurve"/> value.
    /// </summary>
    [Serializable]
    public class TextureCurveParameter : VolumeParameter<TextureCurve>
    {
        /// <summary>
        /// Creates a new <see cref="TextureCurveParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public TextureCurveParameter(TextureCurve value, bool overrideState = false)
            : base(value, overrideState) {}

        /// <summary>
        /// Release implementation.
        /// </summary>
        public override void Release() => m_Value.Release();

        // TODO: TextureCurve interpolation
    }
}
