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
    [Serializable]
    public class TextureCurve : IDisposable
    {
        const int k_Precision = 128; // Edit LutBuilder3D if you change this value
        const float k_Step = 1f / k_Precision;

        // Calling AnimationCurve.length is very slow, let's cache it
        [field: SerializeField]
        public int length { get; private set; }

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

        public Keyframe this[int index] => m_Curve[index];

        public TextureCurve(AnimationCurve baseCurve, float zeroValue, bool loop, in Vector2 bounds)
            : this(baseCurve.keys, zeroValue, loop, bounds) { }

        public TextureCurve(Keyframe[] keys, float zeroValue, bool loop, in Vector2 bounds)
        {
            m_Curve = new AnimationCurve(keys);
            m_ZeroValue = zeroValue;
            m_Loop = loop;
            m_Range = bounds.magnitude;
            length = keys.Length;
            SetDirty();
        }

        ~TextureCurve()
        {
            ReleaseUnityResources();
        }

        public void Dispose()
        {
            ReleaseUnityResources();
            GC.SuppressFinalize(this);
        }

        void ReleaseUnityResources()
        {
            CoreUtils.Destroy(m_Texture);
            m_Texture = null;
        }

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

        public Texture2D GetTexture()
        {
            if (m_IsTextureDirty)
            {
                if (m_Texture == null)
                {
                    m_Texture = new Texture2D(k_Precision, 1, GetTextureFormat(), false, true);
                    m_Texture.name = "CurveTexture";
                    m_Texture.hideFlags = HideFlags.HideAndDontSave;
                    m_Texture.filterMode = FilterMode.Bilinear;
                    m_Texture.wrapMode = TextureWrapMode.Clamp;
                }

                var pixels = new Color[k_Precision];

                for (int i = 0; i < pixels.Length; i++)
                    pixels[i].r = Evaluate(i * k_Step);

                m_Texture.SetPixels(pixels);
                m_Texture.Apply(false, false);
                m_IsTextureDirty = false;
            }

            return m_Texture;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddKey(float time, float value)
        {
            int r = m_Curve.AddKey(time, value);

            if (r > -1)
                SetDirty();

            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MoveKey(int index, in Keyframe key)
        {
            int r = m_Curve.MoveKey(index, key);
            SetDirty();
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveKey(int index)
        {
            m_Curve.RemoveKey(index);
            SetDirty();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SmoothTangents(int index, float weight)
        {
            m_Curve.SmoothTangents(index, weight);
            SetDirty();
        }
    }

    [Serializable]
    public class TextureCurveParameter : VolumeParameter<TextureCurve>
    {
        public TextureCurveParameter(TextureCurve value, bool overrideState = false)
            : base(value, overrideState) { }

        // TODO: TextureCurve interpolation
    }
}
