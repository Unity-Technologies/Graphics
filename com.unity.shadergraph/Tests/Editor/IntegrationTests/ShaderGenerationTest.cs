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
using NUnit.Framework.Internal;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.IntegrationTests
{
    class ShaderGenerationTest
    {
        static readonly string s_Path = Path.Combine(Path.Combine(Path.Combine("" /*DefaultShaderIncludes.GetRepositoryPath()*/, "Testing"), "IntegrationTests"), "Graphs");

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
                    var filePaths = Directory.GetFiles(s_Path, string.Format("*.{0}", ShaderGraphImporter.Extension), SearchOption.AllDirectories).Select(x => new FileInfo(x));

                    foreach (var p in filePaths)
                    {
                        var extension = Path.GetExtension(p.FullName);
                        yield return new TestInfo
                        {
                            name = p.FullName.Replace(s_Path + Path.DirectorySeparatorChar, "").Replace(Path.DirectorySeparatorChar, '/').Replace(extension, ""),
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

      // [Test, TestCaseSource(typeof(CollectGraphs), "graphs")]
        [Ignore("Not currently runable")]
        public void Graph(TestInfo testInfo)
        {
            var file = testInfo.info;
            var filePath = file.FullName;

            var textGraph = File.ReadAllText(filePath, Encoding.UTF8);
            var graph = JsonUtility.FromJson<GraphData>(textGraph);

            Assert.IsNotNull(graph.outputNode, "No master node in graph.");

            var rootPath = Path.Combine(Path.Combine("" /*DefaultShaderIncludes.GetRepositoryPath()*/, "Testing"), "IntegrationTests");
            var shaderTemplatePath = Path.Combine(rootPath, ".ShaderTemplates");
            var textTemplateFilePath = Path.Combine(shaderTemplatePath, string.Format("{0}.{1}", testInfo.name, "shader"));


            List<PropertyCollector.TextureInfo> configuredTextures = new List<PropertyCollector.TextureInfo>();

            if (!File.Exists(textTemplateFilePath))
            {
                Directory.CreateDirectory(Directory.GetParent(textTemplateFilePath).FullName);
                File.WriteAllText(textTemplateFilePath, ShaderGraphImporter.GetShaderText(filePath, out configuredTextures));
                configuredTextures.Clear();
            }

            // Generate the shader
            var shaderString = ShaderGraphImporter.GetShaderText(filePath, out configuredTextures);

            Directory.CreateDirectory(shaderTemplatePath);

            var textTemplate = File.ReadAllText(textTemplateFilePath);
            var textsAreEqual = string.Compare(shaderString, textTemplate, CultureInfo.CurrentCulture, CompareOptions.IgnoreSymbols);

            if (0 != textsAreEqual)
            {
                var failedPath = Path.Combine(rootPath, ".Failed");
                var misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.{1}", testInfo.name, "shader"));
                var misMatchLocationTemplate = Path.Combine(failedPath, string.Format("{0}.template.{1}", testInfo.name, "shader"));
                Directory.CreateDirectory(Directory.GetParent(misMatchLocationResult).FullName);
                File.WriteAllText(misMatchLocationResult, shaderString);
                File.WriteAllText(misMatchLocationTemplate, textTemplate);

                Assert.Fail("Shader text from graph {0}, did not match .template file.", file);
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
                var dumpFileLocation = Path.Combine(shaderTemplatePath, string.Format("{0}.{1}", testInfo.name, "png"));
                if (!File.Exists(dumpFileLocation))
                {
                    Directory.CreateDirectory(Directory.GetParent(dumpFileLocation).FullName);
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
                    var misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.{1}", testInfo.name, "png"));
                    var misMatchLocationTemplate =
                        Path.Combine(failedPath, string.Format("{0}.template.{1}", testInfo.name, "png"));
                    var generated = m_Captured.EncodeToPNG();
                    Directory.CreateDirectory(Directory.GetParent(misMatchLocationResult).FullName);
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
