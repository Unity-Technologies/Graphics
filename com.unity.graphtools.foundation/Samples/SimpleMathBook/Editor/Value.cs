using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// A type of value used in the <see cref="MathBook"/>.
    /// </summary>
    [Serializable]
    public enum ValueType : byte
    {
        Unknown = 0,
        Bool = 1,
        Int = 2,
        Float = 3,
        Vector2 = 4,
        Vector3 = 5,
        String = 6,
    }

    /// <summary>
    /// A value used in the <see cref="MathBook"/> with a dynamic type.
    /// </summary>
    [Serializable]
    public struct Value : IEquatable<Value>
    {
        /// <summary>
        /// The type of the value.
        /// </summary>
        public ValueType Type;

        bool m_Bool;
        int m_Int;
        float m_Float;
        Vector2 m_Vector2;
        Vector3 m_Vector3;
        string m_String;

        /// <summary>
        /// Gets the value as a boolean.
        /// </summary>
        public bool Bool { get { Assert.AreEqual(Type, ValueType.Bool); return m_Bool; } set { Type = ValueType.Bool; m_Bool = value; } }

        /// <summary>
        /// Gets the value as an integer.
        /// </summary>
        public int Int
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Bool:
                        return m_Bool ? 1 : 0;
                    case ValueType.Float:
                        return (int)m_Float;
                    case ValueType.Int:
                        return m_Int;
                    default:
                        throw new InvalidDataException();
                }
            }
            set { Type = ValueType.Int; m_Int = value; }
        }

        /// <summary>
        /// Gets the value as a float.
        /// </summary>
        public float Float
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Float:
                        return m_Float;
                    case ValueType.Int:
                        return m_Int;
                    default:
                        throw new InvalidDataException();
                }
            }
            set { Type = ValueType.Float; m_Float = value; }
        }

        /// <summary>
        /// Gets the value as a Vector2.
        /// </summary>
        public Vector2 Vector2
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Int:
                        return new Vector2(m_Int, 0);
                    case ValueType.Float:
                        return new Vector2(m_Float, 0);
                    case ValueType.Vector2:
                        return m_Vector2;
                    case ValueType.Vector3:
                        return new Vector2(m_Vector3.x, m_Vector3.y);
                    default:
                        throw new InvalidDataException();
                }
            }
            set
            {
                Type = ValueType.Vector2;
                m_Vector2 = value;
            }
        }

        /// <summary>
        /// Gets the value as a Vector3.
        /// </summary>
        public Vector3 Vector3
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Int:
                        return new Vector3(m_Int, 0, 0);
                    case ValueType.Float:
                        return new Vector3(m_Float, 0, 0);
                    case ValueType.Vector2:
                        return new Vector3(m_Vector2.x, m_Vector2.y, 0);
                    case ValueType.Vector3:
                        return m_Vector3;
                    default:
                        throw new InvalidDataException();
                }
            }
            set
            {
                Type = ValueType.Vector3;
                m_Vector3 = value;
            }
        }

        /// <summary>
        /// Gets the value as a string.
        /// </summary>
        public string String { get { Assert.AreEqual(Type, ValueType.String); return m_String; } set { Type = ValueType.String; m_String = value; } }

        public static implicit operator Value(bool f)
        {
            return new Value { Bool = f };
        }

        public static implicit operator Value(int f)
        {
            return new Value { Int = f };
        }

        public static implicit operator Value(float f)
        {
            return new Value { Float = f };
        }

        public static implicit operator Value(Vector2 f)
        {
            return new Value { Vector2 = f };
        }

        public static implicit operator Value(Vector3 f)
        {
            return new Value { Vector3 = f };
        }

        public static implicit operator Value(string f)
        {
            return new Value { String = f };
        }

        /// <summary>
        /// Represents of the value as a string.
        /// </summary>
        /// <returns>A string representing the value</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value doesn't have an expected type.</exception>
        public override string ToString()
        {
            switch (Type)
            {
                case ValueType.Unknown:
                    return ValueType.Unknown.ToString();
                case ValueType.Bool:
                    return Bool.ToString(CultureInfo.InvariantCulture);
                case ValueType.Int:
                    return Int.ToString(CultureInfo.InvariantCulture);
                case ValueType.Float:
                    return Float.ToString(CultureInfo.InvariantCulture);
                case ValueType.Vector2:
                    return Vector2.ToString();
                case ValueType.Vector3:
                    return Vector3.ToString();
                case ValueType.String:
                    return m_String;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Represents of the value as a string more user-friendly than <see cref="ToString" />.
        /// </summary>
        /// <returns>A string representing the value</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value doesn't have an expected type.</exception>
        public string ToPrettyString()
        {
            switch (Type)
            {
                case ValueType.Unknown:
                    return ValueType.Unknown.ToString();
                case ValueType.Bool:
                    return Bool.ToString(CultureInfo.InvariantCulture);
                case ValueType.Int:
                    return Int.ToString(CultureInfo.InvariantCulture);
                case ValueType.Float:
                    return Float.ToString("F2");
                case ValueType.Vector2:
                    return Vector2.ToString("F2", CultureInfo.InvariantCulture);
                case ValueType.Vector3:
                    return Vector3.ToString("F2", CultureInfo.InvariantCulture);
                case ValueType.String:
                    return m_String;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Equals(Value other)
        {
            if (Type != other.Type)
            {
                if (Type == ValueType.Float && other.Type == ValueType.Int || Type == ValueType.Int && other.Type == ValueType.Float)
                    return Float.Equals(other.Float);
            }

            switch (Type)
            {
                case ValueType.Unknown:
                    return false;
                case ValueType.Bool:
                    return Bool == other.Bool;
                case ValueType.Int:
                    return Int == other.Int;
                case ValueType.Float:
                    return Float.Equals(other.Float);
                case ValueType.Vector2:
                    return Vector2.Equals(other.Vector2);
                case ValueType.Vector3:
                    return Vector3.Equals(other.Vector3);
                case ValueType.String:
                    return String.Equals(other.String);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Value other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)Type;
        }
    }
}
