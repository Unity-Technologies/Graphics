namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-AgeOverLifetime")]
    [VFXInfo(name = "Get Age Over Lifetime [0..1]", category = "Attribute")]
    class AgeOverLifetime : VFXOperator
    {
        public class OutputProperties
        {
            public float t = 0;
        }

        public override string name
        {
            get
            {
                return "Get Age Over Lifetime [0..1]";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[] { new VFXAttributeExpression(VFXAttribute.Age) / new VFXAttributeExpression(VFXAttribute.Lifetime) };
            return output;
        }
    }
}
