namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI
{
    public class MathResultUI : CollapsibleInOutNode
    {
        public static readonly string printResultPartName = "print-result";

        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(PrintResultPart.Create(printResultPartName, Model, this, ussClassName));
        }
    }
}
