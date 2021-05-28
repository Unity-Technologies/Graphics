using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using System.Runtime.Remoting.Messaging;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling", experimental = true)]
    class SampleAttributeMap : VFXOperatorDynamicType
    {
        override public string name { get { return "Sample Attribute Map"; } }

        public class InputProperties
        {
            [Tooltip("Sets the number of elements in the attribute map.")]
            public uint pointCount = 0u;

            [Tooltip("Sets the attribute map to sample from.")]
            public Texture2D map = null;

            [Tooltip("Sets the index of the point to sample.")]
            public uint index = 0u;
        }

        public enum ValidOutputTypes
        {
            Bool,
            Uint,
            Int,
            Float,
            Vector2,
            Vector3,
            Vector4,
        }


        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the particleId is out of the point cache bounds.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Wrap;


        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetOperandType(), "sample", new TooltipAttribute("Outputs a sample of the point cache field at an index.")));
            }
        }

        public override sealed IEnumerable<int> staticSlotIndex
        {
            get
            {
                yield return 0;
            }
        }

        public override IEnumerable<Type> validTypes => new[]
{
            typeof(bool),
            typeof(float),
            typeof(uint),
            typeof(int),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
        };

        protected override Type defaultValueType => typeof(Vector3);

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression height = new VFXExpressionTextureHeight(inputExpression[1]);
            VFXExpression width = new VFXExpressionTextureWidth(inputExpression[1]);

            VFXExpression u_index = VFXOperatorUtility.ApplyAddressingMode(inputExpression[2], new VFXExpressionMin(height * width, inputExpression[0]), mode);
            VFXExpression y = u_index / width;
            VFXExpression x = u_index - (y * width);

            Type outputType = GetOperandType();
            var type = typeof(VFXExpressionSampleAttributeMap<>).MakeGenericType(outputType);
            var outputExpr = Activator.CreateInstance(type, new object[]{inputExpression[1], x, y });

            return new[] { (VFXExpression)outputExpr};
        }
    }
}
