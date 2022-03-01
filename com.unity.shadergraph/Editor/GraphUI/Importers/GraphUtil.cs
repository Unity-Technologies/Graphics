using System.IO;
using System.Text;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class NewGraphAction : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            ShaderGraphAssetHelper helper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
            GraphHandler output = new GraphHandler();
            var reg = Registry.Default.DefaultRegistry.CreateDefaultRegistry();
            var contextKey = Registry.Registry.ResolveKey< Registry.Default.DefaultContext>();
            output.AddContextNode(contextKey, reg);
            helper.GraphDeltaJSON = output.ToSerializedFormat();
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(helper, true), Encoding.UTF8);
            AssetDatabase.ImportAsset(pathName);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Shader>(pathName);
        }
    }

    public static class GraphUtil
    {

        [MenuItem("Assets/Create/Shader Graph 2/Blank Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateGraph()
        {
            var endEdit = ScriptableObject.CreateInstance<NewGraphAction>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endEdit, $"New Shader Graph.{NewShaderGraphImporter.Extension}", null, null);
        }

        public static GraphHandler OpenGraph(string assetPath)
        {
            string fileText = File.ReadAllText(assetPath, Encoding.UTF8);
            ShaderGraphAssetHelper helper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
            EditorJsonUtility.FromJsonOverwrite(fileText, helper);
            return new GraphHandler(helper.GraphDeltaJSON);
        }

        public static void DestroyGraph(string assetPath)
        {
            throw new System.NotImplementedException();
        }

        public static void SaveGraph(GraphHandler graph, string assetPath, bool overwrite)
        {
            if (!overwrite && File.Exists(assetPath))
            {
                throw new System.Exception();
            }
            ShaderGraphAssetHelper helper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
            string fileText = File.ReadAllText(assetPath, Encoding.UTF8);
            EditorJsonUtility.FromJsonOverwrite(fileText, helper);
            helper.GraphDeltaJSON = graph.ToSerializedFormat();
            File.WriteAllText(assetPath, EditorJsonUtility.ToJson(helper), Encoding.UTF8);
        }

        public static void SaveGraph(GraphHandler graph, ShaderGraphAssetModel assetModel, string assetPath, bool overwrite)
        {
            if (!overwrite && File.Exists(assetPath))
            {
                throw new System.Exception();
            }
            ShaderGraphAssetHelper helper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
            string fileText = File.ReadAllText(assetPath, Encoding.UTF8);
            EditorJsonUtility.FromJsonOverwrite(fileText, helper);
            helper.GraphDeltaJSON = graph.ToSerializedFormat();
            helper.GTFJSON = EditorJsonUtility.ToJson(assetModel, true);

            File.WriteAllText(assetPath, EditorJsonUtility.ToJson(helper), Encoding.UTF8);
        }

        public static void CopyGraph(GraphHandler graph, ShaderGraphAssetModel assetModel, string sourcePath, string destinationPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new System.Exception();
            }
            ShaderGraphAssetHelper helper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
            string fileText = File.ReadAllText(sourcePath, Encoding.UTF8);
            EditorJsonUtility.FromJsonOverwrite(fileText, helper);
            helper.GraphDeltaJSON = graph.ToSerializedFormat();
            helper.GTFJSON = EditorJsonUtility.ToJson(assetModel, true);

            File.WriteAllText(destinationPath, EditorJsonUtility.ToJson(helper), Encoding.UTF8);
        }

        public static bool GraphExists(string assetPath)
        {
            string fileText = File.ReadAllText(assetPath, Encoding.UTF8);
            ShaderGraphAssetHelper helper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
            EditorJsonUtility.FromJsonOverwrite(fileText, helper);
            return !string.IsNullOrEmpty(helper.GraphDeltaJSON);
        }
    }
}
