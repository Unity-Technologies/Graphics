using System;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Define a value that can be either customized or fetched from a current quality settings' sub level.
    ///
    /// If you intend to serialize this type, use specialized version instead. (<see cref="IntScalableSetting"/>).
    /// </summary>
    /// <typeparam name="T">The type of the scalable setting.</typeparam>
    [Serializable]
    public class ScalableSettingValue<T>
    {
        [SerializeField]
        private T m_Override;
        [SerializeField]
        private bool m_UseOverride;
        [SerializeField]
        private int m_Level;

        /// <summary>The level to use in the associated <see cref="ScalableSetting{T}"/>.</summary>
        public int level
        {
            get => m_Level;
            set => m_Level = value;
        }

        /// <summary>Defines whether the <see cref="@override"/> value is used or not.</summary>
        public bool useOverride
        {
            get => m_UseOverride;
            set => m_UseOverride = value;
        }

        /// <summary>The value to use when <see cref="useOverride"/> is <c>true</c>.</summary>
        public T @override
        {
            get => m_Override;
            set => m_Override = value;
        }

        /// <summary>Resolve the actual value to use.</summary>
        /// <param name="source">The scalable setting to use whne resolving level values. Must not be <c>null</c>.</param>
        /// <returns>
        /// The <see cref="@override"/> value if <see cref="useOverride"/> is <c>true</c> is returned
        /// Otherwise the level value of <paramref name="source"/> for <see cref="level"/> is returned.
        /// </returns>
        public T Value(ScalableSetting<T> source) => m_UseOverride || source == null ? m_Override : source[m_Level];

        /// <summary>Copy the values of this instance to <paramref name="target"/>.</summary>
        /// <param name="target">The target of the copy. Must not be null.</param>
        public void CopyTo(ScalableSettingValue<T> target)
        {
            Assert.IsNotNull(target);

            target.m_Override = m_Override;
            target.m_UseOverride = m_UseOverride;
            target.m_Level = m_Level;
        }
    }

    #region Specialized Scalable Setting Value

    // We define explicitly specialized version of the ScalableSettingValue so it can be serialized with
    // Unity's serialization API.

    /// <summary> An int scalable setting value</summary>
    [Serializable] public class IntScalableSettingValue: ScalableSettingValue<int> {}
    /// <summary> An uint scalable setting value</summary>
    [Serializable] public class UintScalableSettingValue: ScalableSettingValue<uint> {}
    /// <summary> An float scalable setting value</summary>
    [Serializable] public class FloatScalableSettingValue: ScalableSettingValue<float> {}
    /// <summary> An bool scalable setting value</summary>
    [Serializable] public class BoolScalableSettingValue: ScalableSettingValue<bool> {}
    #endregion
}
