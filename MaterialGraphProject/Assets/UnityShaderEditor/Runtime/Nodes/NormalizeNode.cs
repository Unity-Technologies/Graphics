using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Normalize Node")]
    class NormalizeNode : Function1Input
    {
        public NormalizeNode(IGraph owner) : base(owner)
        {
            name = "NormalizeNode";
        }
        
        protected override string GetFunctionName() { return "normalize"; }
    }
}
