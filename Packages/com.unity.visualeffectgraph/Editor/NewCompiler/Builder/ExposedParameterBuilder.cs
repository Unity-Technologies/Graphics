using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class ExposedParameterBuilder : SystemBuilder
    {
        public override void Build()
        {
            foreach (var model in Graph.children)
            {
                if(model is not VFXParameter parameter)
                    continue;
                if (parameter.exposed && !parameter.isOutput)
                    CollectExposed(parameter.exposedName, parameter.GetOutputSlot(0));
            }
        }

        // Mirrors VFXLegacyCompiler.CollectExposedExpression/CollectExposedDesc
        void CollectExposed(string name, VFXSlot slot)
        {
            var expression = slot.valueType != VFXValueType.None ? slot.GetInExpression() : null;
            if (expression != null)
            {
                var task = new ExternalSetTask(name, expression, slot.space);
                var taskId = TaskGraphBuilder.AddTask(task);

                var dataDescription = VfxValueDataFactory.Create(expression.valueType);
                var dataViewId = TaskGraphBuilder.AddData(name, dataDescription);
                TaskGraphBuilder.BindData(taskId, task.Key, dataViewId, BindingUsage.Write);

                RegisterDataView(expression, dataViewId);
            }
            else
            {
                foreach (var child in slot.children)
                    CollectExposed($"{name}_{child.name}", child);
            }
        }
    }
}
