using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using Unity.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(ICommandTarget commandTarget)
        {
            var template = new GraphTemplate<ShaderGraphStencil>("ShaderGraph");
            var promptTitle = string.Format(k_PromptToCreateTitle, template.GraphTypeName);
            var buttonText = string.Format(k_ButtonText, template.GraphTypeName);

            // TODO (Sai) : Convert to UXML and instantiate from there
            var container = new VisualElement();
            container.AddToClassList("onboarding-block");

            var label = new Label(promptTitle);
            container.Add(label);

            var button = new Button { text = buttonText };
            button.clicked += () =>
            {
                var graphAsset = CreateBlankShaderGraph();
                Selection.activeObject = graphAsset;
            };
            container.Add(button);

            return container;
        }

        static GraphAsset CreateBlankShaderGraph()
        {
            var template = new GraphTemplate<ShaderGraphStencil>("ShaderGraph");
            var promptTitle = string.Format(k_PromptToCreateTitle, template.GraphTypeName);
            var prompt = string.Format(k_PromptToCreate, template.GraphTypeName);

            var path = EditorUtility.SaveFilePanelInProject(promptTitle, template.DefaultAssetName, ShaderGraphImporter.Extension, prompt);
            if (path.Length != 0)
            {
                ShaderGraphAssetUtils.HandleCreate(path);
                return ShaderGraphAssetUtils.HandleLoad(path);
            }
            return null;
        }
    }
}
