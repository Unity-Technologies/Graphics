using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;


namespace UnityEditor.VFX.UIElements
{
    public class VFXFloatField : TextValueField<float>
    {
        protected override string allowedCharacters
        {
            get { return EditorGUI.s_AllowedCharactersForFloat; }
        }

        public VFXFloatField()
            : this(kMaxLengthNone) {}

        public VFXFloatField(int maxLength)
            : base(maxLength)
        {
            formatString = "g7";
        }

        internal override bool AcceptCharacter(char c)
        {
            return c != 0 && allowedCharacters.IndexOf(c) != -1;
        }

        bool m_Indeterminate;
        public bool indeterminate
        {
            get
            {
                return m_Indeterminate;
            }
            set
            {
                if( m_Indeterminate != value)
                {
                    m_Indeterminate = value;
                    this.value = this.value;
                }
                
            }
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, float startValue)
        {
            double sensitivity = NumericFieldDraggerUtility.CalculateFloatDragSensitivity(startValue);
            float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            float v = value;
            v += (float)(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
            v = MathUtils.RoundBasedOnMinimumDifference(v, (float)sensitivity);
            SetValueAndNotify(v);
        }

        protected override string ValueToString(float v)
        {
            if (indeterminate) return VFXControlConstants.indeterminateText;
            return v.ToString(formatString);
        }

        protected override float StringToValue(string str)
        {
            double v;
            EditorGUI.StringToDouble(str, out v);
            return (float)v;
        }
    }
}
