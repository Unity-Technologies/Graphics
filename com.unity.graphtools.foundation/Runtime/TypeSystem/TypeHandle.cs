using System;
using JetBrains.Annotations;
using UnityEngine.Serialization;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The type representing an unknown type.
    /// </summary>
    public class Unknown
    {
        Unknown() {}
    }

    /// <summary>
    /// The placeholder type representing the type of a missing port.
    /// </summary>
    public class MissingPort
    {
        MissingPort() {}
    }

    /// <summary>
    /// The type for execution flow ports.
    /// </summary>
    public class ExecutionFlow
    {
        ExecutionFlow() {}
    }

    /// <summary>
    /// The type for subgraphs.
    /// </summary>
    public class Subgraph
    {
        Subgraph() {}
    }

    /// <summary>
    /// Represents a data type in the graph.
    /// </summary>
    [Serializable]
    [PublicAPI]
    public struct TypeHandle : IEquatable<TypeHandle>, IComparable<TypeHandle>
    {
        /// <summary>
        /// The MissingType type.
        /// </summary>
        public static TypeHandle MissingType { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("__MISSINGTYPE");

        /// <summary>
        /// The UnknownType type.
        /// </summary>
        public static TypeHandle Unknown { get; } = TypeHandleHelpers.GenerateCustomTypeHandle(typeof(Unknown), "__UNKNOWN");

        /// <summary>
        /// The ExecutionFlow type.
        /// </summary>
        public static TypeHandle ExecutionFlow { get; } = TypeHandleHelpers.GenerateCustomTypeHandle(typeof(ExecutionFlow), "__EXECUTIONFLOW");

        /// <summary>
        /// The SubgrapH type.
        /// </summary>
        public static TypeHandle Subgraph { get; } = TypeHandleHelpers.GenerateCustomTypeHandle(typeof(Subgraph), "__SUBGRAPH");

        /// <summary>
        /// The MissingPort type.
        /// </summary>
        public static TypeHandle MissingPort { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(MissingPort));

        /// <summary>
        /// The C# bool type.
        /// </summary>
        public static TypeHandle Bool { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(bool));

        /// <summary>
        /// The C# void type.
        /// </summary>
        public static TypeHandle Void { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(void));

        /// <summary>
        /// The C# char type.
        /// </summary>
        public static TypeHandle Char { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(char));

        /// <summary>
        /// The C# double type.
        /// </summary>
        public static TypeHandle Double { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(double));

        /// <summary>
        /// The C# float type.
        /// </summary>
        public static TypeHandle Float { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(float));

        /// <summary>
        /// The C# int type.
        /// </summary>
        public static TypeHandle Int { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(int));

        /// <summary>
        /// The C# uint type.
        /// </summary>
        public static TypeHandle UInt { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(uint));

        /// <summary>
        /// The C# long type.
        /// </summary>
        public static TypeHandle Long { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(long));

        /// <summary>
        /// The C# object type.
        /// </summary>
        public static TypeHandle Object { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(object));

        /// <summary>
        /// The UnityEngine.GameObject type.
        /// </summary>
        public static TypeHandle GameObject { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(GameObject));

        /// <summary>
        /// The C# string type.
        /// </summary>
        public static TypeHandle String { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(string));

        /// <summary>
        /// The UnityEngine.Vector2 type.
        /// </summary>
        public static TypeHandle Vector2 { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Vector2));

        /// <summary>
        /// The UnityEngine.Vector3 type.
        /// </summary>
        public static TypeHandle Vector3 { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Vector3));

        /// <summary>
        /// The UnityEngine.Vector4 type.
        /// </summary>
        public static TypeHandle Vector4 { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Vector4));

        /// <summary>
        /// The UnityEngine.Quaternion type.
        /// </summary>
        public static TypeHandle Quaternion { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Quaternion));

        /// <summary>
        /// Whether the type handle is valid.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(m_Identification);

        internal bool IsCustomTypeHandle => TypeHandleHelpers.IsCustomTypeHandle(m_Identification);

        /// <summary>
        /// The unique id for the type handle.
        /// </summary>
        [SerializeField, FormerlySerializedAs("Identification")]
        string m_Identification;

        public string Identification => m_Identification;

        internal TypeHandle(string identification)
        {
            m_Identification = identification;
            m_Name = null;
        }

        string m_Name;

        /// <summary>
        /// The name of the type.
        /// </summary>
        public string Name => m_Name ??= IsCustomTypeHandle ? m_Identification : Resolve().Name;
        /// <summary>
        /// Determines whether this TypeHandle is equal to another TypeHandle.
        /// </summary>
        /// <param name="other">The other type handle.</param>
        /// <returns>True if this TypeHandle is equal to the other TypeHandle.</returns>
        public bool Equals(TypeHandle other)
        {
            return string.Equals(m_Identification, other.m_Identification);
        }

        /// <summary>
        /// Determines whether this TypeHandle is equal to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if this TypeHandle is equal to <paramref name="obj"/>.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TypeHandle th && Equals(th);
        }

        /// <summary>
        /// Gets the hash code for this object.
        /// </summary>
        /// <returns>The hash code for this object.</returns>
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return m_Identification?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Gets a string representation of this object.
        /// </summary>
        /// <returns>The string representation of this object.</returns>
        public override string ToString()
        {
            return $"TypeName:{m_Identification}";
        }

        /// <summary>
        /// Determines whether a TypeHandle is equal to another TypeHandle.
        /// </summary>
        /// <param name="left">The first TypeHandle to compare.</param>
        /// <param name="right">The second TypeHandle to compare.</param>
        /// <returns>True if the first TypeHandle is equal to the second one.</returns>
        public static bool operator==(TypeHandle left, TypeHandle right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether a TypeHandle is different from another TypeHandle.
        /// </summary>
        /// <param name="left">The first TypeHandle to compare.</param>
        /// <param name="right">The second TypeHandle to compare.</param>
        /// <returns>True if the first TypeHandle is different from the second one.</returns>
        public static bool operator!=(TypeHandle left, TypeHandle right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Compares this type handle to another type handle.
        /// </summary>
        /// <param name="other">The other type handle to compare.</param>
        /// <returns>-1, 0, or 1 if this instance is smaller, equal or greater than <paramref name="other"/>, respectively.</returns>
        public int CompareTo(TypeHandle other)
        {
            return string.Compare(m_Identification, other.m_Identification, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the <see cref="Type"/> represented by this object.
        /// </summary>
        /// <returns>The Type represented by this object.</returns>
        public Type Resolve()
        {
            return TypeHandleHelpers.ResolveType(this);
        }
    }
}
