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
        kMesh,
        kSpline,
    }

    public abstract partial class VFXExpression
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
            }
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public static Type TypeToType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat: return typeof(float);
                case VFXValueType.kFloat2: return typeof(Vector2);
                case VFXValueType.kFloat3: return typeof(Vector3);
                case VFXValueType.kFloat4: return typeof(Vector4);
            }
            throw new NotImplementedException();
        }


        //Helper using reflection to recreate a concrete type from an abstract class (usefull with reduce behavior)
        static protected VFXExpression CreateNewInstance(Type expressionType)
        {
            var constructor = expressionType.GetConstructors()
                                .OrderBy(o => o.GetParameters().Count()) //promote simplest (or default) constructors
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

        // Reduce the expression within a given context
        protected abstract VFXExpression Reduce(VFXExpression[] reducedParents);
        protected abstract VFXExpression Evaluate(VFXExpression[] constParents);

        // Returns dependencies
        public bool Is(Flags flag) { return (m_Flags & flag) == flag; }
        public abstract VFXValueType ValueType { get; }
        public abstract VFXExpressionOp Operation { get; }
        public virtual VFXExpression[] Parents { get { return new VFXExpression[] { }; } }

        protected virtual string[] ParentsCodeName { get { return new string[] { "a", "b", "c", "d" }; } }

        private string temp_GetUniqueName()
        {
            return "temp_" + GetHashCode().ToString("X");
        }

        private string temp_AggregateWithComa(System.Collections.Generic.IEnumerable<string> input)
        {
            return input.Aggregate((a, b) => a + ", " + b);
        }

        public void temp_GetExpressionCode(out string function, out string call)
        {
            function = call = null;
            if (!Is(Flags.ValidOnGPU))
            {
                throw new ArgumentException(string.Format("GetExpressionCode failed (not valid on GPU) with {0}", GetType().FullName));
            }

            if (Parents.Length == 0)
            {
                throw new ArgumentException(string.Format("GetExpressionCode failed (Parents empty) with {0}", GetType().FullName));
            }

            var fnName = GetType().Name;
            foreach (var additionnalParam in AdditionnalParameters)
            {
                fnName += additionnalParam.ToString();
            }

            var param = Parents.Select((o, i) => string.Format("{0} {1}", TypeToCode(o.ValueType), ParentsCodeName[i]));
            var fnHeader = string.Format("{0} {1}({2})", TypeToCode(ValueType), fnName, temp_AggregateWithComa(param));
            var fnContent = string.Format("return {0};", GetOperationCodeContent());
            function = string.Format("{0}\n{{\n{1}\n}}\n", fnHeader, fnContent);
            call = string.Format("{0} {1} = {2}({3});", TypeToCode(ValueType), temp_GetUniqueName(), fnName, temp_AggregateWithComa(Parents.Select(o => o.temp_GetUniqueName())));
        }

        protected virtual string GetOperationCodeContent()
        {
            throw new ArgumentException(string.Format("Unexpected GetOperationCodeContent call with {0}", GetType().FullName));
        }

        public override int GetHashCode()
        {
            int hash = GetType().GetHashCode();

            var parents = Parents;
            for (int i = 0; i < parents.Length; ++i)
                hash = (hash * 397) ^ parents[i].GetHashCode(); // 397 taken from resharper

            var addionnalParameters = AdditionnalParameters;
            for (int i = 0; i < addionnalParameters.Length; ++i)
                hash = (hash * 397) ^ addionnalParameters[i].GetHashCode();

            hash = (hash * 397) ^ m_Flags.GetHashCode();
            hash = (hash * 397) ^ ValueType.GetHashCode();
            hash = (hash * 397) ^ Operation.GetHashCode();

            return hash;
        }

        public virtual int[] AdditionnalParameters { get { return new int[] { }; } }
        public virtual T GetContent<T>()
        {
            var value = (this as VFXValue<T>);
            if (value == null)
            {
                throw new ArgumentException(string.Format("GetContent isn't available for {0} with {1}", typeof(T).FullName, GetType().FullName));
            }
            return value.GetContent();
        }

        protected Flags m_Flags = Flags.None;

    }

}
