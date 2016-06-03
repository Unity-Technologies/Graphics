using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [Title("Math/Minimum Node")]
    public class MinimumNode : Function2Input
    {
        public MinimumNode(IGraph owner) : base(owner)
        {
            name = "MinimumNode";
        }
        
        protected override string GetFunctionName() { return "min"; }
    }
}
