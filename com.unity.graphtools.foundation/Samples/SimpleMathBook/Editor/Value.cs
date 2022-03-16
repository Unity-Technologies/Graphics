using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// A value used in the <see cref="MathBook"/> with a dynamic type.
    /// </summary>
    public struct Value : IEquatable<Value>
    {
        TypeHandle m_Type;

        bool m_Bool;
        int m_Int;
        float m_Float;
        Vector2 m_Vector2;
        Vector3 m_Vector3;
        string m_String;

        /// <summary>
        /// The type of the value.
        /// </summary>
        public TypeHandle Type
        {
            get => m_Type;
            set => m_Type = value;
        }

        /// <summary>
        /// Gets the value as a boolean.
        /// </summary>
        public bool Bool
        {
            get
            {
                Assert.AreEqual(Type, TypeHandle.Bool);
                return m_Bool;
            }
            set
            {
                Type = TypeHandle.Bool;
                m_Bool = value;
            }
        }

        /// <summary>
        /// Gets the value as an integer.
        /// </summary>
        public int Int
        {
            get
            {
                if (Type == TypeHandle.Bool)
                    return m_Bool ? 1 : 0;
                if (Type == TypeHandle.Float)
                    return (int)m_Float;
                if (Type == TypeHandle.Int)
                    return m_Int;
                throw new InvalidDataException();
            }
            set { Type = TypeHandle.Int; m_Int = value; }
        }

        /// <summary>
        /// Gets the value as a float.
        /// </summary>
        public float Float
        {
            get
            {
                if (Type == TypeHandle.Float)
                    return m_Float;
                if (Type == TypeHandle.Int)
                    return m_Int;
                throw new InvalidDataException();
            }
            set { Type = TypeHandle.Float; m_Float = value; }
        }

        /// <summary>
        /// Gets the value as a Vector2.
        /// </summary>
        public Vector2 Vector2
        {
            get
            {
                if (Type == TypeHandle.Int)
                    return new Vector2(m_Int, 0);
                if (Type == TypeHandle.Float)
                    return new Vector2(m_Float, 0);
                if (Type == TypeHandle.Vector2)
                    return m_Vector2;
                if (Type == TypeHandle.Vector3)
                    return new Vector2(m_Vector3.x, m_Vector3.y);
                throw new InvalidDataException();
            }
            set
            {
                Type = TypeHandle.Vector2;
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
                if (Type == TypeHandle.Int)
                    return new Vector3(m_Int, 0, 0);
                if (Type == TypeHandle.Float)
                    return new Vector3(m_Float, 0, 0);
                if (Type == TypeHandle.Vector2)
                    return new Vector3(m_Vector2.x, m_Vector2.y, 0);
                if (Type == TypeHandle.Vector3)
                    return m_Vector3;
                throw new InvalidDataException();
            }
            set
            {
                Type = TypeHandle.Vector3;
                m_Vector3 = value;
            }
        }

        /// <summary>
        /// Gets the value as a string.
        /// </summary>
        public string String
        {
            get
            {
                Assert.AreEqual(Type, TypeHandle.String);
                return m_String;
            }
            set
            {
                Type = TypeHandle.String;
                m_String = value;
            }
        }

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
            if (Type == TypeHandle.Unknown)
                return TypeHandle.Unknown.ToString();
            if (Type == TypeHandle.Bool)
                return Bool.ToString(CultureInfo.InvariantCulture);
            if (Type == TypeHandle.Int)
                return Int.ToString(CultureInfo.InvariantCulture);
            if (Type == TypeHandle.Float)
                return Float.ToString(CultureInfo.InvariantCulture);
            if (Type == TypeHandle.Vector2)
                return Vector2.ToString();
            if (Type == TypeHandle.Vector3)
                return Vector3.ToString();
            if (Type == TypeHandle.String)
                return m_String;
            throw new ArgumentOutOfRangeException();
        }

        public bool Equals(Value other)
        {
            if (Type != other.Type)
            {
                if (Type == TypeHandle.Float && other.Type == TypeHandle.Int || Type == TypeHandle.Int && other.Type == TypeHandle.Float)
                    return Float.Equals(other.Float);
            }

            if (Type == TypeHandle.Unknown)
                return false;
            if (Type == TypeHandle.Bool)
                return Bool == other.Bool;
            if (Type == TypeHandle.Int)
                return Int == other.Int;
            if (Type == TypeHandle.Float)
                return Float.Equals(other.Float);
            if (Type == TypeHandle.Vector2)
                return Vector2.Equals(other.Vector2);
            if (Type == TypeHandle.Vector3)
                return Vector3.Equals(other.Vector3);
            if (Type == TypeHandle.String)
                return String.Equals(other.String);
            throw new ArgumentOutOfRangeException();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Value other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return Type.GetHashCode();
        }
    }
}
