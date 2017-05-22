namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Floor")]
    public class FloorNode : Function1Input
    {
        public FloorNode()
        {
            name = "Floor";
        }

        protected override string GetFunctionName() { return "floor"; }
    }
}

