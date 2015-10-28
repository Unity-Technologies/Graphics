using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Math/Power Node")]
    class PowerNode : Function2Input
    {
        public override void OnCreate()
        {
            name = "PowerNode";
            base.OnCreate();;
        }

        protected override string GetFunctionName() { return "pow"; }
    }
}
