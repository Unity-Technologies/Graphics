using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    // PF FIXME if we can do without CanAcceptSelectionDrop, there is no need for a DebuggingPort class.

    class DebuggingPort : Port
    {
        public static readonly string portExecutionActiveModifierUssClassName = ussClassName.WithUssModifier("execution-active");

        /// <summary>
        /// Used to highlight the port when it is triggered during tracing
        /// </summary>
        public bool ExecutionPortActive
        {
            set => EnableInClassList(portExecutionActiveModifierUssClassName, value);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetHelper.AssetPath + "Plugins/Debugging/Elements/Templates/Port.uss"));
        }

        public override bool CanAcceptDrop(IReadOnlyList<IGraphElementModel> droppedElements)
        {
            // PF: Why is this logic in DebuggingPort ?
            return base.CanAcceptDrop(droppedElements)
                || (droppedElements.Count == 1 && PortModel.PortType != PortType.Execution
                    && (droppedElements.FirstOrDefault() is IVariableDeclarationModel));
        }
    }
}
