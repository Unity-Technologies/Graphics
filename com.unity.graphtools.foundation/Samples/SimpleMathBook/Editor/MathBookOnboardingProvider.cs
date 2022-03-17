using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(Dispatcher dispatcher)
        {
            var graphButtonsContainer = new VisualElement();

            var containerGraphTemplate = new GraphTemplate<MathBookStencil>("Container " + MathBookStencil.GraphName);
            graphButtonsContainer.Add(AddNewGraphButton<ContainerMathBookAsset>(containerGraphTemplate));

            var assetGraphTemplate = new GraphTemplate<MathBookStencil>(MathBookStencil.GraphName);
            graphButtonsContainer.Add(AddNewGraphButton<MathBookAsset>(assetGraphTemplate));

            return graphButtonsContainer;
        }
    }
}
