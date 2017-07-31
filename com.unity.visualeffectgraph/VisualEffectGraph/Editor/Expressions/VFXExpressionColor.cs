using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXExpressionRGBtoHSV : VFXExpression
    {
        public VFXExpressionRGBtoHSV() : this(VFXValue<Vector4>.Default)
        {
        }

        public VFXExpressionRGBtoHSV(VFXExpression parent) : base(VFXExpression.Flags.None, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOp Operation
        {
            get
            {
                return VFXExpressionOp.kVFXRGBtoHSVOp;
            }
        }

        public override VFXValueType ValueType
        {
            get
            {
                return VFXValueType.kFloat3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var rgbReduce = constParents[0];
            var rgb = rgbReduce.Get<Vector4>();

            float h, s, v;
            Color.RGBToHSV(rgb, out h, out s, out v);

            return VFXValue.Constant(new Vector3(h, s, v));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("RGBtoHSV({0})", parents[0]);
        }
    }

    class VFXExpressionHSVtoRGB : VFXExpression
    {
        public VFXExpressionHSVtoRGB() : this(VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionHSVtoRGB(VFXExpression parent) : base(VFXExpression.Flags.None, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOp Operation
        {
            get
            {
                return VFXExpressionOp.kVFXHSVtoRGBOp;
            }
        }

        public override VFXValueType ValueType
        {
            get
            {
                return VFXValueType.kFloat4;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var hsvReduce = constParents[0];
            var hsv = hsvReduce.Get<Vector3>();

            var rgb = Color.HSVToRGB(hsv.x, hsv.y, hsv.z, true);

            return VFXValue.Constant<Vector4>(rgb);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("HSVtoRGB{0}", parents[0]);
        }
    }
}
