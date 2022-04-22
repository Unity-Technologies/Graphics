using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public class BBSampleOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(Dispatcher dispatcher)
        {
            var template = new GraphTemplate<BBStencil>(BBStencil.graphName);
            return AddNewGraphButton<BBGraphAsset>(template);
        }
    }
}
