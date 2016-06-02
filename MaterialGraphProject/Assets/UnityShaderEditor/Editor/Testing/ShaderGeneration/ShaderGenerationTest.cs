using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Tests
{

    public class ShaderGenerationTest
    {
        [Test]
        public void RunGenerationTests()
        {
            string[] path =
            {
                "UnityShaderEditor",
                "Editor",
                "Testing",
                "ShaderGeneration",
                "Graphs"
            };
            
            var absoluteGraphsPath = path.Aggregate(Application.dataPath, Path.Combine);
            var prjRelativeGraphsPath = path.Aggregate("Assets", Path.Combine);

            var filePaths = Directory.GetFiles(absoluteGraphsPath).Select(x => new FileInfo(x));

            foreach (var file in filePaths.Where(x => x.Extension == ".ShaderGraph"))
            {
                var filePath = Path.Combine(prjRelativeGraphsPath, file.Name);
                var graph = AssetDatabase.LoadAssetAtPath<MaterialGraphAsset>(filePath);

                if (graph == null)
                    continue;

                // Generate the shader
                List<PropertyGenerator.TextureInfo> buff;
                string shader = ShaderGenerator.GenerateSurfaceShader(graph.graph, graph.name, false, out buff);

                // find the 'reference' shader
                var dumpFileLocation = string.Format("{0}.{1}", file, "dump");

                if (!File.Exists(dumpFileLocation))
                {
                    // no reference exists, create it
                    File.WriteAllText(dumpFileLocation, shader);
                    Assert.Fail("FAILURE: Dump file not found for {0}, creating it.", file);
                }

                string dumpedShader = File.ReadAllText(dumpFileLocation);
                if (string.CompareOrdinal(dumpedShader, shader) != 0)
                    Assert.Fail("FAILURE: Shader from graph {0}, did not match .dump file.", file);
            }
        }
    }
}
