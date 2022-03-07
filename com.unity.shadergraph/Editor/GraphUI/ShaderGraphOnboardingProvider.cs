using System.IO;
using System.Text;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(Dispatcher commandDispatcher)
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
                Selection.activeObject = graphAsset as Object;
            };
            container.Add(button);

            return container;
        }

        public static IGraphAssetModel CreateBlankShaderGraph()
        {
            var template = new GraphTemplate<ShaderGraphStencil>("ShaderGraph");
            var promptTitle = string.Format(k_PromptToCreateTitle, template.GraphTypeName);
            var prompt = string.Format(k_PromptToCreate, template.GraphTypeName);

            var path = EditorUtility.SaveFilePanelInProject(promptTitle, template.DefaultAssetName,NewShaderGraphImporter.Extension, prompt);
            if (path.Length != 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                NewGraphAction action = new NewGraphAction();
                action.Action(-1, path, null);
                var assetModel = AssetDatabase.LoadAssetAtPath<ShaderGraphAssetModel>(path);

                string fileText = File.ReadAllText(path, Encoding.UTF8);
                ShaderGraphAssetHelper helper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
                EditorJsonUtility.FromJsonOverwrite(fileText, helper);

                GraphHandler graphHandler = new GraphHandler(helper.GraphDeltaJSON);
                if (assetModel.GraphModel is ShaderGraphModel shaderGraphModel)
                    shaderGraphModel.GraphHandler = graphHandler;

                return assetModel;
            }

            return null;
        }
    }
}
