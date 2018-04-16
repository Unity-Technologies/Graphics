using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Text;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.IntegrationTests
{
    public class ShaderGenerationTest
    {
        static readonly string s_Path = Path.Combine(Path.Combine(Path.Combine(DefaultShaderIncludes.GetRepositoryPath(), "Testing"), "IntegrationTests"), "Graphs");

        public struct TestInfo
        {
            public string name;
            public FileInfo info;
            public float threshold;

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
                    var filePaths = Directory.GetFiles(s_Path).Select(x => new FileInfo(x))
                        .Where(x => x.Extension == ".ShaderGraph");

                    foreach (var p in filePaths)
                    {
                        yield return new TestInfo
                        {
                            name = p.Name,
                            info = p,
                            threshold = 0.05f
                        };
                    }
                }
            }
        }

        private Shader m_Shader;
        private Material m_PreviewMaterial;
        private Texture2D m_Captured;
        private Texture2D m_FromDisk;

        [TearDown]
        public void CleanUp()
        {
            if (m_Shader != null)
                Object.DestroyImmediate(m_Shader);

            if (m_PreviewMaterial != null)
                Object.DestroyImmediate(m_PreviewMaterial);

            if (m_Captured != null)
                Object.DestroyImmediate(m_Captured);

            if (m_FromDisk != null)
                Object.DestroyImmediate(m_FromDisk);
        }

        [Test, TestCaseSource(typeof(CollectGraphs), "graphs")]
        public void ShaderGeneratorOutput(TestInfo testInfo)
        {
            var file = testInfo.info;
            var filePath = Path.Combine(s_Path, file.Name);

            var textGraph = File.ReadAllText(filePath, Encoding.UTF8);
            var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);

            Assert.IsNotNull(graph.masterNode, "No master node in graph.");

            //

            //Assert.IsNotNull(graphAsset, "Graph asset not found");

            //var materialGraph = graphAsset.graph as UnityEngine.MaterialGraph.MaterialGraph;
            //Assert.IsNotNull(materialGraph);

            // Generate the shader
            List<PropertyCollector.TextureInfo> configuredTextures = new List<PropertyCollector.TextureInfo>();
            var shaderString = ShaderGraphImporter.GetShaderText(filePath, out configuredTextures);

            var rootPath = Path.Combine(Path.Combine(DefaultShaderIncludes.GetRepositoryPath(), "Testing"), "IntegrationTests");
            var shaderTemplatePath = Path.Combine(rootPath, ".ShaderTemplates");
            Directory.CreateDirectory(shaderTemplatePath);

            var textTemplateFilePath = Path.Combine(shaderTemplatePath, string.Format("{0}.{1}", file.Name, "shader"));
            if (!File.Exists(textTemplateFilePath))
            {
                File.WriteAllText(textTemplateFilePath, shaderString);
                Assert.Fail("Text template file not found for {0}, creating it.", file);
            }
            else
            {
                var textTemplate = File.ReadAllText(textTemplateFilePath);
                var textsAreEqual = string.Compare(shaderString, textTemplate, CultureInfo.CurrentCulture, CompareOptions.IgnoreSymbols);

                if (0 != textsAreEqual)
                {
                    var failedPath = Path.Combine(rootPath, ".Failed");
                    Directory.CreateDirectory(failedPath);
                    var misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.{1}", file.Name, "shader"));
                    var misMatchLocationTemplate = Path.Combine(failedPath, string.Format("{0}.template.{1}", file.Name, "shader"));
                    File.WriteAllText(misMatchLocationResult, shaderString);
                    File.WriteAllText(misMatchLocationTemplate, textTemplate);

                    Assert.Fail("Shader text from graph {0}, did not match .template file.", file);
                }
            }

            m_Shader = ShaderUtil.CreateShaderAsset(shaderString);
            m_Shader.hideFlags = HideFlags.HideAndDontSave;
            Assert.IsNotNull(m_Shader, "Shader Generation Failed");

            //Assert.IsFalse(AbstractMaterialNodeUI.ShaderHasError(m_Shader), "Shader has error");

            m_PreviewMaterial = new Material(m_Shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            foreach (var textureInfo in configuredTextures)
            {
                var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                if (texture == null)
                    continue;
                m_PreviewMaterial.SetTexture(textureInfo.name, texture);
            }

            Assert.IsNotNull(m_PreviewMaterial, "preview material could not be created");

            const int res = 256;
            using (var generator = new MaterialGraphPreviewGenerator())
            {
                var renderTexture = new RenderTexture(res, res, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };
                generator.DoRenderPreview(renderTexture, m_PreviewMaterial, null, PreviewMode.Preview3D, true, 10);

                Assert.IsNotNull(renderTexture, "Render failed");

                RenderTexture.active = renderTexture;
                m_Captured = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
                m_Captured.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                RenderTexture.active = null; //can help avoid errors
                Object.DestroyImmediate(renderTexture, true);

                // find the reference image
                var dumpFileLocation = Path.Combine(shaderTemplatePath, string.Format("{0}.{1}", file.Name, "png"));
                if (!File.Exists(dumpFileLocation))
                {
                    // no reference exists, create it
                    var generated = m_Captured.EncodeToPNG();
                    File.WriteAllBytes(dumpFileLocation, generated);
                    Assert.Fail("Image template file not found for {0}, creating it.", file);
                }

                var template = File.ReadAllBytes(dumpFileLocation);
                m_FromDisk = new Texture2D(2, 2);
                m_FromDisk.LoadImage(template, false);

                var rmse = CompareTextures(m_FromDisk, m_Captured);

                if (rmse > testInfo.threshold)
                {
                    var failedPath = Path.Combine(rootPath, ".Failed");
                    Directory.CreateDirectory(failedPath);
                    var misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.{1}", file.Name, "png"));
                    var misMatchLocationTemplate =
                        Path.Combine(failedPath, string.Format("{0}.template.{1}", file.Name, "png"));
                    var generated = m_Captured.EncodeToPNG();
                    File.WriteAllBytes(misMatchLocationResult, generated);
                    File.WriteAllBytes(misMatchLocationTemplate, template);

                    Assert.Fail("Shader image from graph {0}, did not match .template file.", file);
                }
            }
        }

        // compare textures, use RMS for this
        private float CompareTextures(Texture2D fromDisk, Texture2D captured)
        {
            if (fromDisk == null || captured == null)
                return 1f;

            if (fromDisk.width != captured.width
                || fromDisk.height != captured.height)
                return 1f;

            var pixels1 = fromDisk.GetPixels();
            var pixels2 = captured.GetPixels();
            if (pixels1.Length != pixels2.Length)
                return 1f;

            int numberOfPixels = pixels1.Length;
            float sumOfSquaredColorDistances = 0;
            for (int i = 0; i < numberOfPixels; i++)
            {
                Color p1 = pixels1[i];
                Color p2 = pixels2[i];
                Color diff = p1 - p2;
                diff = diff * diff;
                sumOfSquaredColorDistances += (diff.r + diff.g + diff.b) / 3.0f;
            }
            float rmse = Mathf.Sqrt(sumOfSquaredColorDistances / numberOfPixels);
            return rmse;
        }
    }
}
