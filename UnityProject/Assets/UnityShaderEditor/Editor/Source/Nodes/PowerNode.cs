using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    [Title("Math/Power Node")]
    class PowerNode : Function2Input
    {
        public override void Init()
        {
            name = "PowerNode";
            base.Init();
        }

        protected override string GetFunctionName() { return "pow"; }
    }
}
