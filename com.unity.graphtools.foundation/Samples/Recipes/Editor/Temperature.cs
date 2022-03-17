using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [Serializable]
    public struct Temperature : IComparable<Temperature>
    {
        [SerializeField]
        int m_Value;

        [SerializeField]
        TemperatureUnit m_Unit;

        public int Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public TemperatureUnit Unit
        {
            get => m_Unit;
            set => m_Unit = value;
        }

        /// <inheritdoc />
        public int CompareTo(Temperature other)
        {
            return ToKelvinValue().CompareTo(other.ToKelvinValue());
        }

        readonly float ToKelvinValue()
        {
            switch (m_Unit)
            {
                case TemperatureUnit.Celsius:
                    return m_Value + 273.15f;
                case TemperatureUnit.Fahrenheit:
                    return (m_Value - 32) * 5f/9f + 273.15f;
                case TemperatureUnit.Kelvin:
                    return m_Value;
            }

            return 0;
        }

        public readonly Temperature As(in TemperatureUnit newUnit)
        {
            if (m_Unit == newUnit)
            {
                return new Temperature {Value = m_Value, Unit = m_Unit};
            }
            float asKelvin = ToKelvinValue();
            float newValue = asKelvin;
            switch (newUnit)
            {
                case TemperatureUnit.Celsius:
                    newValue -= 273.15f;
                    break;
                case TemperatureUnit.Fahrenheit:
                    newValue = ((asKelvin - 273.15f) * 9f / 5f) + 32;
                    break;
            }
            return new Temperature {Value = (int)newValue, Unit = newUnit};
        }

        public static bool operator >  (Temperature a, Temperature b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <  (Temperature a, Temperature b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >=  (Temperature a, Temperature b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator <=  (Temperature a, Temperature b)
        {
            return a.CompareTo(b) <= 0;
        }
    }

    public enum TemperatureUnit
    {
        Celsius,
        Fahrenheit,
        Kelvin
    }
}
