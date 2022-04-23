using System;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    static class AllConstantsGraphGenerator
    {
        static void AddConstantNodes(IGraphModel graph)
        {
            var position = new Vector2(0, 0);
            var delta = new Vector2(0, 50);
            ulong i = 0;
            foreach (var data in BBStencil.SupportedConstants)
            {
                graph.CreateConstantNode(data.type, data.name, position, new SerializableGUID(42, i++));
                position += delta;
            }

            var note = graph.CreateStickyNote(new Rect(-200, 0, 150, 400));
            note.Title = "Types";
            note.Contents = string.Join("\n", BBStencil.SupportedConstants.Select(c => c.name));
        }

        static void AddVariableDeclarations(IGraphModel graph)
        {
            ulong i = 0;

            foreach (var data in BBStencil.SupportedConstants)
            {
                graph.CreateGraphVariableDeclaration(data.type, data.name, ModifierFlags.Read, false,
                    null, int.MaxValue, null,
                    new SerializableGUID(84, i++));
            }
        }

        static void CreateAsset(string name, string path)
        {
            AssetDatabase.DeleteAsset(path);

            GraphAssetCreationHelpers.CreateGraphAsset(typeof(BBGraphAsset), typeof(BBStencil), name, path);
            var asset = AssetDatabase.LoadAssetAtPath<BBGraphAsset>(path);

            AddConstantNodes(asset.GraphModel);
            AddVariableDeclarations(asset.GraphModel);
        }

        [MenuItem("internal:GTF/Generate \"All Constants\" Graph Asset")]
        public static void GenerateCompatibilityTestLoadReferenceAsset()
        {
            var assetName = "AllConstantEditors";
            var assetPath = $"Assets/{assetName}.asset";
            CreateAsset(assetName, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ForceReserializeAssets(new[] { assetPath });
        }
    }
}
