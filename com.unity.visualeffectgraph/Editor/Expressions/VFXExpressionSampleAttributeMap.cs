using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleAttributeMap<T> : VFXExpression
    {
        public VFXExpressionSampleAttributeMap() : this(VFXTexture2DValue.Default, VFXValue<int>.Default, VFXValue<int>.Default)
        {
        }

        public VFXExpressionSampleAttributeMap(VFXExpression texture, VFXExpression x, VFXExpression y)
            : base(Flags.InvalidOnCPU, new VFXExpression[3] { texture, x, y })
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXExpression.GetVFXValueTypeFromType(typeof(T)); } }

        public sealed override string GetCodeString(string[] parents)
        {
            string typeString = VFXExpression.TypeToCode(valueType);
            return string.Format("({3}){0}.Load(int3({1}, {2}, 0))", parents[0], parents[1], parents[2], typeString);
        }
    }
}
