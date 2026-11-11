using System.Linq;
using System.Reflection;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    using CustomHLSL = UnityEditor.VFX.Block.CustomHLSL;
    using CustomHLSLOperator = UnityEditor.VFX.Operator.CustomHLSL;

    internal static class VFXCustomHLSLTestHelper
    {
        internal static HLSLFunction GetFunction(VFXModel hlslOperator)
        {
            var fieldInfo = hlslOperator.GetType().GetField("m_Function", BindingFlags.Instance | BindingFlags.NonPublic);
            return (HLSLFunction)fieldInfo.GetValue(hlslOperator);
        }

        internal static void MakeSimpleGraphWithCustomHLSL(CustomHLSL hlslBlock, out VFXViewWindow view, out VFXGraph graph, bool compilable = true, VFXContextType targetContextType = VFXContextType.Update)
        {
            graph = VFXTestCommon.CreateGraph_And_System();
            view = VFXTestCommon.GetWindow(graph, true, true);
            view.LoadResource(graph.visualEffectResource);

            if (!compilable)
            {
                var outputContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Output);
                outputContext.UnlinkAll();
            }

            var targetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == targetContextType);
            targetContext.AddChild(hlslBlock);

            view.graphView.OnSave();
        }

        internal static CustomHLSL CreateCustomHLSLBlock()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            graph.AddChild(updateContext);
            updateContext.AddChild(hlslBlock);

            return hlslBlock;
        }

        internal static VFXExpression[] CallBuildExpression(CustomHLSLOperator hlslOperator, VFXExpression[] parentExpressions)
        {
            var methodInfo = hlslOperator.GetType().GetMethod("BuildExpression", BindingFlags.Instance | BindingFlags.NonPublic);
            return (VFXExpression[])methodInfo.Invoke(hlslOperator, new object[] { parentExpressions });
        }

        internal static void MakeSimpleGraphWithCustomHLSL(CustomHLSLOperator hlslOperator, out VFXViewWindow view, out VFXGraph graph)
        {
            graph = VFXTestCommon.CreateGraph_And_System();
            view = VFXTestCommon.GetWindow(graph, true, true);
            view.LoadResource(graph.visualEffectResource);

            graph.AddChild(hlslOperator);
            view.graphView.OnSave();
        }

        internal static CustomHLSLOperator CreateCustomHLSLOperator()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSLOperator>();
            graph.AddChild(hlslOperator);

            return hlslOperator;
        }
    }
}
