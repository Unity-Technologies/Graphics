namespace UnityEngine.MaterialGraph
{
    [Title("Math/Interpolation/Lerp")]
    public class LerpNode : Function3Input
    {
        public LerpNode()
        {
            name = "Lerp";
        }

		protected override string GetInputSlot1Name()
		{
			return "InputA";
		}

		protected override string GetInputSlot2Name()
		{
			return "InputB";
		}

		protected override string GetInputSlot3Name()
		{
			return "T";
		}

        protected override string GetFunctionName() {return "lerp"; }
    }
}
