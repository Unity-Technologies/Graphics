namespace UnityEditor.Graphs.Material
{
	[Title("Math/Length Node")]
	class LengthNode : Function1Input
	{
		public override void Init()
		{
			name = "LengthNode";
			base.Init();
		}

		protected override string GetFunctionName() { return "length"; }
	}
}
