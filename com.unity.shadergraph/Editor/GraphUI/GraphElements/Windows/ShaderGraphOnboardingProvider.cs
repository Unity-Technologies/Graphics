using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows
{
    public class ShaderGraphOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(CommandDispatcher commandDispatcher)
        {
            var template = new GraphTemplate<ShaderGraphStencil>("ShaderGraph");
            return AddNewGraphButton<ShaderGraphAssetModel>(template);
        }

        public static IGraphAssetModel CreateBlankShaderGraph()
        {
            var shaderGraphOnboardingProvider = new ShaderGraphOnboardingProvider();

            var template = new GraphTemplate<ShaderGraphStencil>("ShaderGraph");
            var promptTitle = string.Format(k_PromptToCreateTitle, template.GraphTypeName);
            var prompt = string.Format(k_PromptToCreate, template.GraphTypeName);

            return GraphAssetCreationHelpers<ShaderGraphAssetModel>.PromptToCreate(template, promptTitle, prompt, k_AssetExtension);
        }
    }
}
