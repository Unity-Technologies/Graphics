using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.UIElements;

namespace GtfPlayground
{
    public class PlaygroundOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(CommandDispatcher commandDispatcher)
        {
            var template = new GraphTemplate<PlaygroundStencil>("Playground Graph");
            return AddNewGraphButton<PlaygroundGraphAssetModel>(template);
        }
    }
}