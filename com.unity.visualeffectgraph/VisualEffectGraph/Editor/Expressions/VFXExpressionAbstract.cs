using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    public static class VFXReflectionHelper
    {
        public static T[] CollectStaticReadOnlyExpression<T>(Type expressionType, System.Reflection.BindingFlags additionnalFlag = System.Reflection.BindingFlags.Public)
        {
            var members = expressionType.GetFields(System.Reflection.BindingFlags.Static | additionnalFlag)
                .Where(m => m.IsInitOnly && m.FieldType == typeof(T))
                .ToArray();
            var expressions = members.Select(m => (T)m.GetValue(null)).ToArray();
            return expressions;
        }
    }

    abstract partial class VFXExpression
    {
        [Flags]
        public enum Flags
        {
            None =          0,
            Value =         1 << 0, // Expression is a value, get/set can be called on it
            Foldable =      1 << 1, // Expression is not a constant but can be folded anyway
            Constant =      1 << 2, // Expression is a constant, it can be folded
            InvalidOnGPU =  1 << 3, // Expression can be evaluated on GPU
            InvalidOnCPU =  1 << 4, // Expression can be evaluated on CPU
            PerElement =    1 << 5, // Expression is per element
        }

        public static bool IsFloatValueType(VFXValueType valueType)
        {
            return valueType == VFXValueType.kFloat
                || valueType == VFXValueType.kFloat2
                || valueType == VFXValueType.kFloat3
                || valueType == VFXValueType.kFloat4;
        }

        public static bool IsUIntValueType(VFXValueType valueType)
        {
            return valueType == VFXValueType.kUint;
        }

        public static int TypeToSize(VFXValueType type)
        {
            return VFXExpressionHelper.GetSizeOfType(type);
        }

        public static string TypeToCode(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat: return "float";
                case VFXValueType.kFloat2: return "float2";
                case VFXValueType.kFloat3: return "float3";
                case VFXValueType.kFloat4: return "float4";
                case VFXValueType.kInt: return "int";
                case VFXValueType.kUint: return "uint";
                case VFXValueType.kTexture2D: return "Texture2D";
                case VFXValueType.kTexture3D: return "Texture3D";
                case VFXValueType.kTransform: return "float4x4";
                case VFXValueType.kBool: return "bool";
            }
            throw new NotImplementedException(type.ToString());
        }

        public static Type TypeToType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat: return typeof(float);
                case VFXValueType.kFloat2: return typeof(Vector2);
                case VFXValueType.kFloat3: return typeof(Vector3);
                case VFXValueType.kFloat4: return typeof(Vector4);
                case VFXValueType.kInt: return typeof(int);
                case VFXValueType.kUint: return typeof(uint);
                case VFXValueType.kTexture2D: return typeof(Texture2D);
                case VFXValueType.kTexture3D: return typeof(Texture3D);
                case VFXValueType.kTransform: return typeof(Matrix4x4);
                case VFXValueType.kMesh: return typeof(Mesh);
                case VFXValueType.kCurve: return typeof(AnimationCurve);
                case VFXValueType.kColorGradient: return typeof(Gradient);
                case VFXValueType.kBool: return typeof(bool);
            }
            throw new NotImplementedException(type.ToString());
        }

        public static bool IsTypeValidOnGPU(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat:
                case VFXValueType.kFloat2:
                case VFXValueType.kFloat3:
                case VFXValueType.kFloat4:
                case VFXValueType.kInt:
                case VFXValueType.kUint:
                case VFXValueType.kTexture2D:
                case VFXValueType.kTexture3D:
                case VFXValueType.kTransform:
                case VFXValueType.kBool:
                    return true;
            }

            return false;
        }

        public static bool IsTexture(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kTexture2D:
                case VFXValueType.kTexture3D:
                    return true;
            }

            return false;
        }

        public static bool IsUniform(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat:
                case VFXValueType.kFloat2:
                case VFXValueType.kFloat3:
                case VFXValueType.kFloat4:
                case VFXValueType.kInt:
                case VFXValueType.kUint:
                case VFXValueType.kTransform:
                case VFXValueType.kBool:
                    return true;
            }
            return false;
        }

        protected VFXExpression(Flags flags, params VFXExpression[] parents)
        {
            m_Parents = parents;
            m_Flags = flags;
            PropagateParentsFlags();
        }

        //Helper using reflection to recreate a concrete type from an abstract class (useful with reduce behavior)
        protected static VFXExpression CreateNewInstance(Type expressionType)
        {
            var allconstructors = expressionType.GetConstructors().ToArray();
            if (allconstructors.Length == 0)
                return null; //Only static readonly expression allowed, constructors are private (attribute or builtIn)

            var constructor =   allconstructors
                .OrderBy(o => o.GetParameters().Count())                 //promote simplest (or default) constructors
                .First();
            var param = constructor.GetParameters().Select(o =>
                {
                    var type = o.GetType();
                    return type.IsValueType ? Activator.CreateInstance(type) : null;
                }).ToArray();
            return (VFXExpression)constructor.Invoke(param);
        }

        protected VFXExpression CreateNewInstance()
        {
            return CreateNewInstance(GetType());
        }

        // Reduce the expression
        protected virtual VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            if (reducedParents.Length == 0)
                return this;

            var reduced = CreateNewInstance();
            reduced.Initialize(m_Flags, reducedParents);
            return reduced;
        }

        // Evaluate the expression
        protected virtual VFXExpression Evaluate(VFXExpression[] constParents)
        {
            throw new NotImplementedException();
        }

        // Get the HLSL code snippet
        public virtual string GetCodeString(string[] parents)
        {
            throw new NotImplementedException(GetType().ToString());
        }

        // Get the operands for the runtime evaluation
        public int[] GetOperands(VFXExpressionGraph graph)
        {
            var parentsIndex = parents.Select(p => graph == null ? -1 : graph.GetFlattenedIndex(p)).ToArray();
            if (parentsIndex.Length + additionnalOperands.Length > 4)
                throw new Exception("Too much parameter for expression : " + this);

            var data = new int[] { -1, -1, -1, -1};
            for (int i = 0; i < parents.Length; ++i)
            {
                data[i] = parentsIndex[i];
            }

            for (int i = 0; i < additionnalOperands.Length; ++i)
            {
                data[data.Length - additionnalOperands.Length + i] = additionnalOperands[i];
            }
            return data;
        }

        public virtual IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            return Enumerable.Empty<VFXAttributeInfo>();
        }

        public bool Is(Flags flag)      { return (m_Flags & flag) == flag; }
        public bool IsAny(Flags flag)   { return (m_Flags & flag) != 0; }

        public virtual VFXValueType valueType
        {
            get
            {
                var data = GetOperands(null);
                return VFXExpressionHelper.GetTypeOfOperation(operation, data[0], data[1], data[2], data[3]);
            }
        }
        public abstract VFXExpressionOp operation { get; }

        public VFXExpression[] parents { get { return m_Parents; } }

        public override bool Equals(object obj)
        {
            var other = obj as VFXExpression;
            if (other == null)
                return false;

            if (operation != other.operation)
                return false;

            if (valueType != other.valueType)
                return false;

            if (m_Flags != other.m_Flags)
                return false;

            if (other.additionnalOperands.Length != additionnalOperands.Length)
                return false;

            for (int i = 0; i < additionnalOperands.Length; ++i)
                if (additionnalOperands[i] != other.additionnalOperands[i])
                    return false;

            var thisParents = parents;
            var otherParents = other.parents;

            if (thisParents == null && otherParents == null)
                return true;
            if (thisParents == null || otherParents == null)
                return false;
            if (thisParents.Length != otherParents.Length)
                return false;

            for (int i = 0; i < thisParents.Length; ++i)
                if (!thisParents[i].Equals(otherParents[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hash = GetType().GetHashCode();

            var parents = this.parents;
            for (int i = 0; i < parents.Length; ++i)
                hash = (hash * 397) ^ parents[i].GetHashCode(); // 397 taken from resharper

            for (int i = 0; i < additionnalOperands.Length; ++i)
                hash = (hash * 397) ^ additionnalOperands[i].GetHashCode();

            hash = (hash * 397) ^ m_Flags.GetHashCode();
            hash = (hash * 397) ^ valueType.GetHashCode();
            hash = (hash * 397) ^ operation.GetHashCode();

            return hash;
        }

        protected virtual int[] additionnalOperands { get { return Enumerable.Empty<int>().ToArray(); } }
        public virtual T Get<T>()
        {
            var value = (this as VFXValue<T>);
            if (value == null)
            {
                throw new ArgumentException(string.Format("Get isn't available for {0} with {1}", typeof(T).FullName, GetType().FullName));
            }
            return value.Get();
        }

        public virtual object GetContent()
        {
            throw new ArgumentException(string.Format("GetContent isn't available for {0}", GetType().FullName));
        }

        // Only do that when constructing an instance if needed
        protected void Initialize(Flags additionalFlags, VFXExpression[] parents)
        {
            m_Parents = parents;
            m_Flags |= additionalFlags;
            PropagateParentsFlags();
        }

        private void PropagateParentsFlags()
        {
            if (m_Parents.Length > 0)
            {
                bool foldable = true;
                foreach (var parent in m_Parents)
                {
                    foldable &= parent.Is(Flags.Foldable);
                    m_Flags |= (parent.m_Flags & (Flags.PerElement | Flags.InvalidOnCPU));
                    if (parent.Is(Flags.PerElement | Flags.InvalidOnGPU))
                        m_Flags |= Flags.InvalidOnGPU; // Only propagate GPU validity for per element expressions
                }
                if (foldable)
                    m_Flags |= Flags.Foldable;
                else
                    m_Flags &= ~Flags.Foldable;
            }
        }

        public static VFXExpression operator*(VFXExpression a, VFXExpression b) { return new VFXExpressionMul(a, b); }
        public static VFXExpression operator/(VFXExpression a, VFXExpression b) { return new VFXExpressionDivide(a, b); }
        public static VFXExpression operator+(VFXExpression a, VFXExpression b) { return new VFXExpressionAdd(a, b); }
        public static VFXExpression operator-(VFXExpression a, VFXExpression b) { return new VFXExpressionSubtract(a, b); }

        public VFXExpression this[int index] { get { return new VFXExpressionExtractComponent(this, index); } }
        public VFXExpression x { get { return new VFXExpressionExtractComponent(this, 0); }  }
        public VFXExpression y { get { return new VFXExpressionExtractComponent(this, 1); }  }
        public VFXExpression z { get { return new VFXExpressionExtractComponent(this, 2); }  }
        public VFXExpression w { get { return new VFXExpressionExtractComponent(this, 3); }  }

        protected Flags m_Flags = Flags.None;
        private VFXExpression[] m_Parents;
    }
}
