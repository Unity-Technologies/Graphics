using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.VFX
{
    public class VFXOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(Dispatcher dispatcher)
        {
            var template = new GraphTemplate<VFXStencil>(VFXStencil.graphName);
            return AddNewGraphButton<VFXGraphAssetModel>(template);
        }
    }
}
