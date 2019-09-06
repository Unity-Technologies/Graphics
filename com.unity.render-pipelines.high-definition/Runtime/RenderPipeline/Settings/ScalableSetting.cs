using System;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public static class ScalableSetting
    {
        public static readonly int LevelCount = Enum.GetValues(typeof(Level)).Length;
        public enum Level
        {
            Low,
            Medium,
            High
        }
    }

    [Serializable]
    public class ScalableSetting<T>
    {
        [SerializeField]
        private T m_Low;
        [SerializeField]
        private T m_Medium;
        [SerializeField]
        private T m_High;

        public T this[ScalableSetting.Level index]
        {
            get
            {
                switch (index)
                {
                    case ScalableSetting.Level.Low: return m_Low;
                    case ScalableSetting.Level.Medium: return m_Medium;
                    case ScalableSetting.Level.High: return m_High;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
            set
            {
                switch (index)
                {
                    case ScalableSetting.Level.Low: m_Low = value; break;
                    case ScalableSetting.Level.Medium: m_Medium = value; break;
                    case ScalableSetting.Level.High: m_High = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        public T low
        {
            get => m_Low;
            set => m_Low = value;
        }

        public T medium
        {
            get => m_Medium;
            set => m_Medium = value;
        }

        public T high
        {
            get => m_High;
            set => m_High = value;
        }
    }

    [Serializable] public class IntScalableSetting: ScalableSetting<int> {}
    [Serializable] public class UintScalableSetting: ScalableSetting<uint> {}
    [Serializable] public class FloatScalableSetting: ScalableSetting<float> {}
    [Serializable] public class BoolScalableSetting: ScalableSetting<bool> {}

    [Serializable]
    public class ScalableSettingValue<T>
    {
        [SerializeField] private ScalableSetting.Level m_Level;
        [SerializeField] private bool m_UseOverride;
        [SerializeField] private T m_Override;

        public T @override
        {
            get => m_Override;
            set => m_Override = value;
        }

        public bool useOverride
        {
            get => m_UseOverride;
            set => m_UseOverride = value;
        }

        public ScalableSetting.Level level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public T Value(ScalableSetting<T> source) => m_UseOverride ? m_Override : source[m_Level];

        public void CopyTo(ScalableSettingValue<T> dst)
        {
            Assert.IsNotNull(dst);

            dst.m_Level = m_Level;
            dst.m_UseOverride = m_UseOverride;
            dst.m_Override = m_Override;
        }
    }

    [Serializable] public class IntScalableSettingValue: ScalableSettingValue<int> {}
    [Serializable] public class UintScalableSettingValue: ScalableSettingValue<uint> {}
    [Serializable] public class FloatScalableSettingValue: ScalableSettingValue<float> {}
    [Serializable] public class BoolScalableSettingValue: ScalableSettingValue<bool> {}
}
