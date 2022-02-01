using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(Dispatcher dispatcher)
        {
            var template = new GraphTemplate<MathBookStencil>(MathBookStencil.GraphName);
            return AddNewGraphButton<MathBookAsset>(template);
        }
    }
}
