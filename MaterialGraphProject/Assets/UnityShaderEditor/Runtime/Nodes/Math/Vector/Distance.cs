namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/Distance")]
    public class DistanceNode : Function2Input
    {
        public DistanceNode()
        {
            name = "Distance";
        }

        protected override string GetFunctionName() { return "distance"; }
    }    
}


