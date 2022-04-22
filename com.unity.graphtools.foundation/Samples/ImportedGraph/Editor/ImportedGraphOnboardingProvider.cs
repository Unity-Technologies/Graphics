using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    public class ImportedGraphOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(Dispatcher dispatcher)
        {
            var container = new VisualElement();
            container.AddToClassList("onboarding-block");

            var label = new Label(GraphWrapper.promptToCreate);
            container.Add(label);

            var button = new Button { text = GraphWrapper.promptToCreateTitle };
            button.clicked += () =>
            {
                var graphTemplate = new GraphTemplate<ImportedGraphStencil>(ImportedGraphStencil.graphName, GraphWrapper.assetExtension);
                var graphAsset = GraphAssetCreationHelpers.PromptToCreateGraphAsset(typeof(ImportedGraphAsset), graphTemplate, GraphWrapper.promptToCreateTitle, GraphWrapper.promptToCreate);

                Selection.activeObject = graphAsset as Object;
            };
            container.Add(button);

            return container;
        }
    }
}
