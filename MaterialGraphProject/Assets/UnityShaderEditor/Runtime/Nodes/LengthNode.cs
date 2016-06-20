using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Length Node")]
    public class LengthNode : Function1Input
    {
        public LengthNode()
        {
            name = "LengthNode";
        }
        
        protected override string GetFunctionName() { return "length"; }
    }
}
