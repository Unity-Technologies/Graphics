namespace UnityEngine.MaterialGraph
{
    [Title("Math/Interpolation/SmoothStep")]
    class SmoothStepNode : Function3Input
    {
        public SmoothStepNode()
        {
            name = "SmoothStep";
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

        protected override string GetFunctionName() {return "smoothstep"; }
    }
}
