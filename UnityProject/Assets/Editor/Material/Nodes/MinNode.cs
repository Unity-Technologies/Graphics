namespace UnityEditor.Graphs.Material
{
	[Title("Math/Minimum Node")]
	class MinimumNode : Function2Input
	{
		public override void Init()
		{
			name = "MinimumNode";
			base.Init();
		}

		protected override string GetFunctionName() { return "min"; }
	}
}
