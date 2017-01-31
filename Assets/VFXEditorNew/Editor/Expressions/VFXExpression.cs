using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    public enum VFXValueType
    {
        kNone,
        kFloat,
        kFloat2,
        kFloat3,
        kFloat4,
        kInt,
        kUint,
        kTexture2D,
        kTexture3D,
        kTransform,
        kCurve,
        kColorGradient,
        kSpline,
    }

    abstract class VFXExpression
    {
        [Flags]
        public enum Flags
        {
            None =          0,
            Value =         1 << 0, // Expression is a value, get/set can be called on it
            Constant =      1 << 1, // Expression is a constant, it can be folded
            ValidOnGPU =    1 << 2, // Expression can be evaluated on GPU
            ValidOnCPU =    1 << 3, // Expression can be evaluated on CPU
        }

        public static int TypeToSize(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat: return 1;
                case VFXValueType.kFloat2: return 2;
                case VFXValueType.kFloat3: return 3;
                case VFXValueType.kFloat4: return 4;
                case VFXValueType.kInt: return 1;
                case VFXValueType.kUint: return 1;
                case VFXValueType.kTransform: return 16;

                case VFXValueType.kCurve: return 4; // float4
                case VFXValueType.kColorGradient: return 1; // float 
                case VFXValueType.kSpline: return 2; // float2
                default:
                    return 0;
            }
        }

        public static string TypeToCode(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat: return "float";
                case VFXValueType.kFloat2: return "float2";
                case VFXValueType.kFloat3: return "float3";
                case VFXValueType.kFloat4: return "float4";
            }
            return "unknownType";
        }

        public bool Is(Flags flag) { return (m_Flags & flag) == flag; }

        public abstract VFXValueType ValueType { get; }
        public abstract VFXExpressionOp Operation { get; }

        // Reduce the expression within a given context
        public abstract VFXExpression Reduce(VFXExpressionContext context);
        // Returns dependencies
        public virtual VFXExpression[] Parents { get { return new VFXExpression[] { }; } }

        protected virtual string[] ParentsCodeName { get { return new string[] { "a", "b", "c", "d" }; } }

        public string temp_GetUniqueName()
        {
            return "temp_" + GetHashCode().ToString("X");
        }

        private string temp_AggregateWithComa(System.Collections.Generic.IEnumerable<string> input)
        {
            return input.Aggregate((a, b) => a + ", " + b);
        }

        public void GetExpressionCode(out string function, out string call)
        {
            function = call = null;
            if (!Is(Flags.ValidOnGPU))
            {
                Debug.LogError("Trying to call GetExpressionCode on invalid expression");
                return;
            }

            if (Parents.Length == 0)
            {
                Debug.LogError("Trying to call GetExpressionCode on incoherent expression");
                return;
            }

            var fnName = GetType().Name;
            foreach (var additionnalParam in AdditionnalParameters)
            {
                fnName += additionnalParam.ToString();
            }

            var param = Parents.Select((o, i) => string.Format("{0} {1}", TypeToCode(o.ValueType), ParentsCodeName[i]));
            var fnHeader = string.Format("{0} {1}({2})", TypeToCode(ValueType), fnName, temp_AggregateWithComa(param));
            var fnContent = GetOperationCodeContent();
            function = string.Format("{0}\n{{\n{1}\n}}\n", fnHeader, fnContent);
            call = string.Format("{0} {1} = {2}({3});", TypeToCode(ValueType), temp_GetUniqueName(), fnName, temp_AggregateWithComa(Parents.Select(o => o.temp_GetUniqueName())));
        }

        protected virtual string GetOperationCodeContent() { return "Unexpected GetOperationCodeContent call"; }

        public virtual int[] AdditionnalParameters { get { return new int[] { }; } }

        public override bool Equals(object obj)
        {
            var other = obj as VFXExpression;
            if (other == null)
                return false;

            if (Operation != other.Operation)
                return false;

            if (GetHashCode() != obj.GetHashCode())
                return false;

            // TODO Not really optimized for an equal function!
            var thisParents = Parents;
            var otherParents = other.Parents;

            if (thisParents.Length != otherParents.Length)
                return false;

            for (int i = 0; i < thisParents.Length; ++i)
                if (!thisParents[i].Equals(otherParents[i]))
                    return false;

            var thisAdditionnalParameters = AdditionnalParameters;
            var otherAdditionnalParamaters = other.AdditionnalParameters;
            for (int i=0; i < thisAdditionnalParameters.Length; ++i)
            {
                if (thisAdditionnalParameters[i] != otherAdditionnalParamaters[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            if (!m_HasCachedHashCode)
            {
                int hash = GetType().GetHashCode();

                var parents = Parents;
                for (int i = 0; i < parents.Length; ++i)
                    hash = (hash * 397) ^ parents[i].GetHashCode(); // 397 taken from resharper

                var addionnalParameters = AdditionnalParameters;
                for (int i = 0; i < addionnalParameters.Length; ++i)
                    hash = (hash * 397) ^ addionnalParameters[i].GetHashCode();

                m_CachedHashCode = hash;
                m_HasCachedHashCode = true;
            }

            return m_CachedHashCode;
        }

        protected Flags m_Flags = Flags.None;

        protected int m_CachedHashCode;
        protected bool m_HasCachedHashCode = false;
    }
}
