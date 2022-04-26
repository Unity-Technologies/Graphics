using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(Dispatcher dispatcher)
        {
            var template = new GraphTemplate<VerticalStencil>(VerticalStencil.graphName);
            return AddNewGraphButton<VerticalGraphAsset>(template);
        }
    }
}
