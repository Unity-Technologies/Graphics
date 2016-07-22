using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Lerp Node")]
    public class LerpNode : Function3Input
    {
        public LerpNode()
        {
            name = "LerpNode";
        }

        protected override string GetFunctionName() {return "lerp"; }
    }
}
