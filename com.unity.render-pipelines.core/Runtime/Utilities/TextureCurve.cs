using System;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;

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

        static AnimationCurve s_TempCurve;

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

        bool m_IsLengthDirty;
        bool m_IsLoopingCurveDirty;
        bool m_IsTextureDirty;
        int m_PreviousCurveHash;

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
            : this(baseCurve.keys, zeroValue, loop, bounds) { }

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
            m_IsLengthDirty = true;
            SetValuesDirty();
        }

        /// <summary>
        /// Marks the curve key values as dirty to trigger a redraw of the texture the next time
        /// <see cref="GetTexture"/> is called but without forcing the update of <see cref="length"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetValuesDirty()
        {
            m_IsLoopingCurveDirty = true;
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
                m_PreviousCurveHash = 0;
            }

            if (m_IsTextureDirty)
            {
                AnimationCurve curve;
                int curveHash;

                UpdateLength();
                if (length != 0)
                {
                    curve = GetEffectiveCurve();
                    curveHash = 0;
                    var curveLength = curve.length;
                    for (int i = 0; i < curveLength; i++)
                    {
                        var key = curve[i];
                        var keyHash = key.time.GetHashCode();
                        keyHash = keyHash * 23 + key.value.GetHashCode();
                        keyHash = keyHash * 23 + key.inTangent.GetHashCode();
                        keyHash = keyHash * 23 + key.outTangent.GetHashCode();
                        // TODO: Should we handle weights? It seems they are not used in Volumes.

                        curveHash = curveHash * 23 + keyHash;
                    }
                }
                else
                {
                    curve = null;
                    curveHash = m_ZeroValue.GetHashCode();
                }

                if (curveHash != m_PreviousCurveHash)
                {
                    var format = m_Texture.format;
                    if (format == TextureFormat.RHalf)
                    {
                        var data = m_Texture.GetPixelData<ushort>(0);
                        if (length > 1)
                        {
                            for (int i = 0; i < k_Precision; i++)
                                data[i] = Mathf.FloatToHalf(curve.Evaluate(i * k_Step));
                        }
                        else // Constant value
                        {
                            var value = Mathf.FloatToHalf(length == 0 ? m_ZeroValue : m_Curve[0].value);
                            for (int i = 0; i < k_Precision; i++)
                                data[i] = value;
                        }
                    }
                    else
                    {
                        var data = m_Texture.GetPixelData<byte>(0);
                        if (length > 1)
                        {
                            if (format == TextureFormat.R8)
                            {
                                for (int i = 0; i < k_Precision; i++)
                                    data[i] = (byte)(Mathf.Clamp01(curve.Evaluate(i * k_Step)) * byte.MaxValue);
                            }
                            else
                            {
                                for (int i = 0; i < k_Precision; i++)
                                    data[i * 4 + 1] = (byte)(Mathf.Clamp01(curve.Evaluate(i * k_Step)) * byte.MaxValue);
                            }
                        }
                        else // Constant value
                        {
                            var value = (byte)(Mathf.Clamp01(length == 0 ? m_ZeroValue : m_Curve[0].value) * byte.MaxValue);
                            if (format == TextureFormat.R8)
                            {
                                for (int i = 0; i < k_Precision; i++)
                                    data[i] = value;
                            }
                            else
                            {
                                for (int i = 0; i < k_Precision; i++)
                                    data[i * 4 + 1] = value;
                            }
                        }
                    }

                    m_Texture.Apply(false, false);

                    m_PreviousCurveHash = curveHash;
                }

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
            UpdateLength();

            if (length == 0)
                return m_ZeroValue;

            return GetEffectiveCurve().Evaluate(time);
        }

        /// <summary>
        /// Update <see cref="length"/> if the curve was changed since the last call.
        /// </summary>
        void UpdateLength()
        {
            if (m_IsLengthDirty)
            {
                length = m_Curve.length;
                m_IsLengthDirty = false;
            }
        }

        /// <summary>
        /// Get the updated curve that has looping applied to it if necessary. It may not equal <see cref="m_Curve"/>.
        /// </summary>
        /// <returns>The effective curve to evaluate.</returns>
        AnimationCurve GetEffectiveCurve()
        {
            // If length is 0 it should never be called as m_ZeroValue is used instead.
            Assert.AreNotEqual(0, length);

            if (!m_Loop || length == 1)
                return m_Curve;

            if (m_IsLoopingCurveDirty)
            {
                if (m_LoopingCurve == null)
                {
                    m_LoopingCurve = new AnimationCurve();
                }
                else
                {
                    for (int i = m_LoopingCurve.length - 1; i >= 0; i--)
                        m_LoopingCurve.RemoveKey(i);
                }

                var prev = m_Curve[length - 1];
                prev.time -= m_Range;
                var next = m_Curve[0];
                next.time += m_Range;

                m_LoopingCurve.AddKey(prev);
                for (int i = 0; i < length; i++)
                    m_LoopingCurve.AddKey(m_Curve[i]);
                m_LoopingCurve.AddKey(next);

                m_IsLoopingCurveDirty = false;
            }

            return m_LoopingCurve;
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
            SetValuesDirty();
        }

        /// <summary>
        /// Interpolate values of this curve toward the specified <see cref="TextureCurve"/>.
        /// </summary>
        /// <param name="to"><see cref="TextureCurve"/> with values to be used at t = 1.</param>
        /// <param name="t">The interpolation value.</param>
        public void Interp(TextureCurve to, float t)
        {
            if (t == 0f)
                return;

            if (t == 1f)
            {
                SetValue(to);
                return;
            }

            if (s_TempCurve == null)
                s_TempCurve = new AnimationCurve();

            to.UpdateLength();
            if (to.length == 0)
            {
                s_TempCurve.AddKey(0f, to.m_ZeroValue);
            }
            else if (to.length == 1)
            {
                s_TempCurve.AddKey(0f, to[0].value);
            }
            else
            {
                var curve = to.GetEffectiveCurve();
                var curveLength = curve.length;
                for (int i = 0; i < curveLength; i++)
                    s_TempCurve.AddKey(curve[i]);
            }

            UpdateLength();
            if (length == 0)
            {
                m_Curve.AddKey(0f, m_ZeroValue);
                length = 1;
            }
            else if (length == 1)
            {
                var key = m_Curve[0];
                m_Curve.MoveKey(0, new Keyframe(0f, key.value));
            }
            else if (m_Loop)
            {
                // Bake looped values
                m_Curve = GetEffectiveCurve();
                m_LoopingCurve = null;
                m_Loop = false;
                length = m_Curve.length;
            }

            for (int i = 0; i < length; i++)
            {
                var time = m_Curve[i].time;
                s_TempCurve.AddKey(time, s_TempCurve.Evaluate(time));
            }

            length = s_TempCurve.length;
            for (int i = 0; i < length; i++)
            {
                var time = s_TempCurve[i].time;
                m_Curve.AddKey(time, m_Curve.Evaluate(time));
            }

            for (int i = 0; i < length; i++)
            {
                var key = m_Curve[i];

                var toKey = s_TempCurve[i];
                key.value = Mathf.Lerp(key.value, toKey.value, t);
                key.inTangent = Mathf.Lerp(key.inTangent, toKey.inTangent, t);
                key.outTangent = Mathf.Lerp(key.outTangent, toKey.outTangent, t);
                // TODO: Should we handle weights? It seems they are not used in Volumes.

                m_Curve.MoveKey(i, key);
            }

            // Clear the temp curve for future use.
            for (int i = length - 1; i >= 0f; i--)
                s_TempCurve.RemoveKey(i);

            // Don't call SetDirty() here because we've already set the correct length.
            SetValuesDirty();
        }

        /// <summary>
        /// Deep-copy the data of the specified <see cref="TextureCurve"/> to this one.
        /// </summary>
        /// <param name="value"><see cref="TextureCurve"/> to copy the data from.</param>
        public void SetValue(TextureCurve value)
        {
            if (value == this)
                return;

            UpdateLength();
            for (int i = length - 1; i >= 0; i--)
                m_Curve.RemoveKey(i);

            value.UpdateLength();
            length = value.length;
            for (int i = 0; i < length; i++)
                m_Curve.AddKey(value[i]);

            m_ZeroValue = value.m_ZeroValue;
            m_Loop = value.m_Loop;
            m_Range = value.m_Range;

            // Don't call SetDirty() here because we've already set the correct length.
            SetValuesDirty();
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
            : base(value, overrideState) { }

        /// <summary>
        /// Release implementation.
        /// </summary>
        public override void Release() => m_Value.Release();

        /// <summary>
        /// Deep-copy the data of the specified <see cref="VolumeParameter"/> to this one.
        /// </summary>
        /// <param name="value"><see cref="VolumeParameter"/> to copy the data from.</param>
        public override void SetValue(VolumeParameter parameter)
        {
            m_Value.SetValue(((TextureCurveParameter)parameter).m_Value);
        }

        /// <summary>
        /// Interpolate values of this <see cref="TextureCurve"/> toward the specified <see cref="TextureCurve"/>.
        /// The "from" curve is unused in this implementation and expected to be the same as this.
        /// </summary>
        /// <param name="from">Unused. Expected to be the same <see cref="TextureCurve"/> as this.</param>
        /// <param name="to"><see cref="TextureCurve"/> with values to be used at t = 1.</param>
        /// <param name="t">The interpolation value.</param>
        public override void Interp(TextureCurve from, TextureCurve to, float t)
        {
            // Volume system doesn't seem to ever interpolate 2 sources into a 3rd target.
            // Instead it applies "to" on top of "this", which is also passed as "from".
            // We cut some corners here and really support only the second case for simplicity.
            Assert.AreEqual(m_Value, from);

            m_Value.Interp(to, t);
        }
    }
}
