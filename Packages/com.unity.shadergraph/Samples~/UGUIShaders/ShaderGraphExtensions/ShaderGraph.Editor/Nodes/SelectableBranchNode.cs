using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "UGUI", "Selectable State Switch")]
    class SelectableBranchNode : CodeFunctionNode
    {
        public SelectableBranchNode()
        {
            name = "Selectable State Switch";
            synonyms = new string[] { "switch", "ui", "selectable" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Switch", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Switch(
            [Slot(0, Binding.None)] Vector1 State,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector Normal,
            [Slot(2, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector Highlighted,
            [Slot(3, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector Pressed,
            [Slot(4, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector Selected,
            [Slot(5, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector Disabled,
            [Slot(6, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    [branch] switch(State)
    {
        case 0:
            Out = Normal; 
            break;
        case 1:
            Out = Highlighted; 
           break;
        case 2:
            Out = Pressed; 
           break;
        case 3:
            Out = Selected; 
           break;
        case 4:
            Out = Disabled; 
           break;
        default:
            Out = Normal; 
           break;
    }
}
";
        }
    }
}
