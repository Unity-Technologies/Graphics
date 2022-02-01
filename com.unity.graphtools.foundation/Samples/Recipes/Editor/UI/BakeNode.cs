using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    class BakeNode : CollapsibleInOutNode
    {
        public static readonly string paramContainerPartName = "parameter-container";

        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.InsertPartAfter(titleIconContainerPartName, TemperatureAndTimePart.Create(paramContainerPartName, Model, this, ussClassName));
        }
    }
}
