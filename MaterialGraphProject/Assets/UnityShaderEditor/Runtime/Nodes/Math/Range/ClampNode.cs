namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/Clamp")]
    public class ClampNode : Function3Input
    {
        public ClampNode()
        {
            name = "Clamp";
        }

		protected override string GetInputSlot1Name()
		{
			return "Input";
		}

		protected override string GetInputSlot2Name()
		{
			return "Min";
		}

		protected override string GetInputSlot3Name()
		{
			return "Max";
		}

        protected override string GetFunctionName() {return "clamp"; }
    }
}
