namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Advanced/Fmod")]
	public class FmodNode : Function2Input
	{
		public FmodNode ()
		{
			name = "Fmod";
		}

		protected override string GetFunctionName ()
		{
			return "fmod";
		}
	}
}

