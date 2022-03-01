using System.IO;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(Dispatcher commandDispatcher)
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

            var path = EditorUtility.SaveFilePanelInProject(promptTitle, template.DefaultAssetName,NewShaderGraphImporter.Extension, prompt);
            if (path.Length != 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                NewGraphAction action = new NewGraphAction();
                action.Action(-1, path, null);
                return AssetDatabase.LoadAssetAtPath<ShaderGraphAssetModel>(path);
            }

            return null;
        }
    }
}
