using System.Text;

namespace UnityEngine.MaterialGraph
{
    public class FloatPropertyChunk : PropertyChunk
    {
		public enum FloatType
		{
			Float,
			Toggle,
			Range,
			PowerSlider
		}

        private readonly float m_DefaultValue;
		private readonly FloatType m_FloatType;
		private readonly Vector3 m_rangeValues = new Vector3(0f, 1f, 2f);

        public FloatPropertyChunk(string propertyName, string propertyDescription, float defaultValue, HideState hideState)
            : base(propertyName, propertyDescription, hideState)
        {
            m_DefaultValue = defaultValue;
        }

		public FloatPropertyChunk(string propertyName, string propertyDescription, float defaultValue, FloatType floatType, HideState hideState)
			: base(propertyName, propertyDescription, hideState)
		{
			m_FloatType = floatType;
			m_DefaultValue = defaultValue;
		}

        public float defaultValue
        {
            get { return m_DefaultValue; }
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
			if (m_FloatType == FloatType.Toggle)
				result.Append ("[Toggle]");
			else if(m_FloatType == FloatType.PowerSlider)
				result.Append ("[PowerSlider(" + m_rangeValues.z + ")]");
            result.Append(propertyName);
            result.Append("(\"");
            result.Append(propertyDescription);

			if (m_FloatType == FloatType.Float || m_FloatType == FloatType.Toggle) {
				result.Append ("\", Float) = ");
			}else if(m_FloatType == FloatType.Range || m_FloatType == FloatType.PowerSlider){
				result.Append ("\", Range(");
				result.Append (m_rangeValues.x + ", " + m_rangeValues.y);
				result.Append (")) = ");
			}
            result.Append(defaultValue);
            return result.ToString();
        }
    }
}
