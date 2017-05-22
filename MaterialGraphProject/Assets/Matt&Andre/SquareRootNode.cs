namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Basic/SquareRoot")]
	public class SquareRootNode : Function1Input
	{
		public SquareRootNode ()
		{
			name = "SquareRoot";
		}

		protected override string GetFunctionName ()
		{
			return "sqrt";
		}
	}
}

