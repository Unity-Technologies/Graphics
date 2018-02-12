using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Random")]
    class VFXOperatorRandom : VFXOperator
    {
        public enum SeedMode
        {
            PerParticle,
            PerSystem
        }

        public class InputProperties
        {
            [Tooltip("The minimum value to be generated.")]
            public FloatN min = new FloatN(0.0f);
            [Tooltip("The maximum value to be generated.")]
            public FloatN max = new FloatN(1.0f);
            [Tooltip("An optional additional custom seed.")]
            public uint customSeed = 0; // TODO - hide this from UI when constant==false
        }

        [VFXSetting, Tooltip("Generate a random number for each particle, or one that is shared by the whole system.")]
        public SeedMode seed = SeedMode.PerParticle;
        [VFXSetting, Tooltip("The random number may either remain constant, or change every time it is evaluated.")]
        public bool constant = true;

        override public string name { get { return "Random Number"; } }

        private float m_FallbackValue = 0.0f;

        public override void OnEnable()
        {
            base.OnEnable();

            var propertyType = GetType().GetNestedType(GetInputPropertiesTypeName());
            if (propertyType != null)
            {
                var fields = propertyType.GetFields().Where(o => o.IsStatic && o.Name == "FallbackValue");
                var field = fields.FirstOrDefault(o => o.FieldType == typeof(float));
                if (field != null)
                {
                    m_FallbackValue = (float)field.GetValue(null);
                }
            }
        }

        //Convert automatically input expression with diverging floatN size to floatMax
        sealed override protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            var inputExpression = base.GetInputExpressions();
            return VFXOperatorUtility.UpcastAllFloatN(inputExpression, m_FallbackValue);
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression rand;

            VFXValueType maxValueType = VFXOperatorUtility.FindMaxFloatNValueType(inputExpression);
            if (constant)
            {
                switch (seed)
                {
                    default:
                    case SeedMode.PerParticle:
                        rand = VFXOperatorUtility.RandomFloatN(VFXExpressionRandom.RandomFlags.Fixed | VFXExpressionRandom.RandomFlags.PerElement, maxValueType, inputExpression[2]);
                        break;
                    case SeedMode.PerSystem:
                        rand = VFXOperatorUtility.RandomFloatN(VFXExpressionRandom.RandomFlags.Fixed, maxValueType, VFXBuiltInExpression.SystemSeed, inputExpression[2]);
                        break;
                }
            }
            else
            {
                switch (seed)
                {
                    default:
                    case SeedMode.PerParticle:
                        rand = VFXOperatorUtility.RandomFloatN(VFXExpressionRandom.RandomFlags.PerElement, maxValueType, null);
                        break;
                    case SeedMode.PerSystem:
                        rand = VFXOperatorUtility.RandomFloatN(VFXExpressionRandom.RandomFlags.None, maxValueType, null);
                        break;
                }
            }

            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], rand) };
        }
    }
}
