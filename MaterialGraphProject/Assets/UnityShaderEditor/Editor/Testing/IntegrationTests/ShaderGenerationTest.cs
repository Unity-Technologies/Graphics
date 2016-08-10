using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;


namespace UnityEditor.MaterialGraph.IntegrationTests
{
    public class ShaderGenerationTest
    {
        private static readonly string[] s_Path =
        {
            "UnityShaderEditor",
            "Editor",
            "Testing",
            "IntegrationTests",
            "Graphs"
        };

        public struct TestInfo
        {
            public string name;
            public FileInfo info;

            public override string ToString()
            {
                return name;
            }
        }

        public static class CollectGraphs
        {
            public static IEnumerable graphs
            {
                get
                {
                    var absoluteGraphsPath = s_Path.Aggregate(Application.dataPath, Path.Combine);
                    var filePaths = Directory.GetFiles(absoluteGraphsPath).Select(x => new FileInfo(x)).Where(x => x.Extension == ".ShaderGraph");

                    foreach (var p in filePaths)
                    {
                        yield return new TestInfo
                        {
                            name = p.Name,
                            info = p
                        };
                    }
                }
            }
        }
        
        private Shader m_Shader;
        private Material m_PreviewMaterial;

        [TearDown]
        public void CleanUp()
        {
            if (m_Shader != null)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Shader));

            if (m_PreviewMaterial != null)
                Object.DestroyImmediate(m_PreviewMaterial);
        }

        [Test, TestCaseSource(typeof(CollectGraphs), "graphs")]
        public void ShaderGeneratorOutput(TestInfo testInfo)
        {
            var file = testInfo.info;
            var prjRelativeGraphsPath = s_Path.Aggregate("Assets", Path.Combine);
            var filePath = Path.Combine(prjRelativeGraphsPath, file.Name);

            var graph = AssetDatabase.LoadAssetAtPath<MaterialGraphAsset>(filePath);

            Assert.IsNotNull(graph, "Graph asset not found");

            // Generate the shader
            var shaderOutputLocation = string.Format("{0}.{1}", filePath, "shader");
            m_Shader = GraphEditWindow.ExportShader(graph, shaderOutputLocation);

            Assert.IsNotNull(m_Shader, "Shader Generation Failed");
            Assert.IsFalse(AbstractMaterialNodeUI.ShaderHasError(m_Shader), "Shader has error");

            m_PreviewMaterial = new Material(m_Shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            Assert.IsNotNull(m_PreviewMaterial, "preview material could not be created");

            const int res = 256;
            var generator = new MaterialGraphPreviewGenerator();
            var rendered = generator.DoRenderPreview(m_PreviewMaterial, PreviewMode.Preview3D, new Rect(0, 0, res, res), 10) as RenderTexture;
            
            Assert.IsNotNull(rendered, "Render failed");

            RenderTexture.active = rendered;
            Texture2D captured = new Texture2D(rendered.width, rendered.height, TextureFormat.ARGB32, false);
            captured.ReadPixels(new Rect(0, 0, rendered.width, rendered.height), 0, 0);
            RenderTexture.active = null; //can help avoid errors 

            var generated = captured.EncodeToPNG();
           
            // find the reference image
            var dumpFileLocation = string.Format("{0}.template.{1}", file, "png");

            if (!File.Exists(dumpFileLocation))
            {
                // no reference exists, create it
                File.WriteAllBytes(dumpFileLocation, generated);
                Assert.Fail("Template file not found for {0}, creating it.", file);
            }

            var saved = File.ReadAllBytes(dumpFileLocation);

            var areEqual = Enumerable.SequenceEqual(saved, generated);
            if (!areEqual)
            {
                var misMatchLocation = string.Format("{0}.{1}", file, "png");
                File.WriteAllBytes(misMatchLocation, generated);
            }

            Assert.IsTrue(areEqual, "Shader from graph {0}, did not match .template file.", file);
        }
    }
}
