namespace UnityEngine.MaterialGraph
{ 
    [Title("Math/Absolute Node")]
    class AbsoluteNode : Function1Input
    {
        public AbsoluteNode()
        {
            name = "AbsoluteNode";
        }
        
        protected override string GetFunctionName() {return "abs"; }
    }
}
