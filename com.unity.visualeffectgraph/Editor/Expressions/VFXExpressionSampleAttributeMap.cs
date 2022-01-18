using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
            : base(Flags.InvalidOnCPU, new VFXExpression[3] {texture, x, y})
        {
        }

        public sealed override VFXExpressionOperation operation => VFXExpressionOperation.None;
        public sealed override VFXValueType valueType => VFXExpression.GetVFXValueTypeFromType(typeof(T));

        public sealed override string GetCodeString(string[] parents)
        {
            var readValue = $"{parents[0]}.Load(int3({parents[1]}, {parents[2]}, 0))";

            //Int & UInt are actually stored in float
            if (valueType == VFXValueType.Int32)
                return $"asint({readValue}.r)";

            if (valueType == VFXValueType.Uint32)
                return $"asuint({readValue}.r)";

            var typeString = VFXExpression.TypeToCode(valueType);
            return $"({typeString}){readValue}";
        }
    }
}
