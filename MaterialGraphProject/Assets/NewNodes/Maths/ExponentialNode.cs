namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Basic/Exponential")]
	public class ExponentialNode : Function1Input
	{
		public ExponentialNode ()
		{
			name = "Exponential";
		}

		protected override string GetFunctionName ()
		{
			return "exp";
		}
	}
}

