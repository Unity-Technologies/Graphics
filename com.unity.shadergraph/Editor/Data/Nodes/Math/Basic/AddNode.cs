using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Add")]
    class AddNode : CodeFunctionNode, IDifferentiable
    {
        public AddNode()
        {
            name = "Add";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Add", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Add(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A + B;
}
";
        }

        public Derivative GetDerivative(int outputSlotId)
        {
            if (outputSlotId != 3)
                throw new System.ArgumentException("outputSlotId");

            return new Derivative()
            {
                FuncVariableInputSlotIds = new[] { 0, 1 },
                Function = genMode => "{0} + {1}"
            };
        }
    }
}
