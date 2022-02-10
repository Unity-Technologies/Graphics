namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI
{
    public class MathbookBBVarPropertyView : BlackboardVariablePropertyView
    {
        protected override void BuildRows()
        {
            AddInitializationField();
            AddTooltipField();
        }
    }
}
