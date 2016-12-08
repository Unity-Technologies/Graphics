using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.MaterialGraph.Drawing;
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
                    var absoluteGraphsPath = s_Path.Aggregate(Application.dataPath, Path.Combine);
                    var filePaths = Directory.GetFiles(absoluteGraphsPath).Select(x => new FileInfo(x)).Where(x => x.Extension == ".ShaderGraph");

                    foreach (var p in filePaths)
                    {
                        yield return new TestInfo
                               {
                                   name = p.Name,
                                   info = p,
                                   threshold = 0.02f
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
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Shader));

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
            var prjRelativeGraphsPath = s_Path.Aggregate("Assets", Path.Combine);
            var filePath = Path.Combine(prjRelativeGraphsPath, file.Name);

            var graphAsset = AssetDatabase.LoadAssetAtPath<MaterialGraphAsset>(filePath);

            Assert.IsNotNull(graphAsset, "Graph asset not found");

            var materialGraph = graphAsset.graph as UnityEngine.MaterialGraph.MaterialGraph;
            Assert.IsNotNull(materialGraph);

            // Generate the shader
            List<PropertyGenerator.TextureInfo> configuredTextures;
            var shaderString = materialGraph.masterNode.GetFullShader(GenerationMode.ForReals, out configuredTextures);
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
            var generator = new MaterialGraphPreviewGenerator();
            var rendered = generator.DoRenderPreview(m_PreviewMaterial, PreviewMode.Preview3D, new Rect(0, 0, res, res), 10) as RenderTexture;

            Assert.IsNotNull(rendered, "Render failed");

            RenderTexture.active = rendered;
            m_Captured = new Texture2D(rendered.width, rendered.height, TextureFormat.ARGB32, false);
            m_Captured.ReadPixels(new Rect(0, 0, rendered.width, rendered.height), 0, 0);
            RenderTexture.active = null; //can help avoid errors

            var rootPath = Directory.GetParent(Directory.GetParent(Application.dataPath).ToString());
            var templatePath = Path.Combine(rootPath.ToString(), "ImageTemplates");

            // find the reference image
            var dumpFileLocation = Path.Combine(templatePath, string.Format("{0}.{1}", file.Name, "png"));
            if (!File.Exists(dumpFileLocation))
            {
                // no reference exists, create it
                Directory.CreateDirectory(templatePath);
                var generated = m_Captured.EncodeToPNG();
                File.WriteAllBytes(dumpFileLocation, generated);
                Assert.Fail("Template file not found for {0}, creating it.", file);
            }

            var template = File.ReadAllBytes(dumpFileLocation);
            m_FromDisk = new Texture2D(2, 2);
            m_FromDisk.LoadImage(template, false);

            var areEqual = CompareTextures(m_FromDisk, m_Captured, testInfo.threshold);

            if (!areEqual)
            {
                var failedPath = Path.Combine(rootPath.ToString(), "Failed");
                Directory.CreateDirectory(failedPath);
                var misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.{1}", file.Name, "png"));
                var misMatchLocationTemplate = Path.Combine(failedPath, string.Format("{0}.template.{1}", file.Name, "png"));
                var generated = m_Captured.EncodeToPNG();
                File.WriteAllBytes(misMatchLocationResult, generated);
                File.WriteAllBytes(misMatchLocationTemplate, template);
            }

            Assert.IsTrue(areEqual, "Shader from graph {0}, did not match .template file.", file);
        }

        private bool CompareTextures(Texture2D fromDisk, Texture2D captured, float threshold)
        {
            if (fromDisk == null || captured == null)
                return false;

            if (fromDisk.width != captured.width
                || fromDisk.height != captured.height)
                return false;

            var pixels1 = fromDisk.GetPixels();
            var pixels2 = captured.GetPixels();

            if (pixels1.Length != pixels2.Length)
                return false;

            for (int i = 0; i < pixels1.Length; i++)
            {
                if (!CompareColor(pixels1[i], pixels2[i], threshold))
                    return false;
            }
            return true;
        }

        private bool CompareColor(Vector4 left, Vector4 right, float threshold)
        {
            Vector4 diff = left - right;

            if (Mathf.Abs(diff.x) > threshold)
                return false;
            if (Mathf.Abs(diff.y) > threshold)
                return false;
            if (Mathf.Abs(diff.z) > threshold)
                return false;
            if (Mathf.Abs(diff.w) > threshold)
                return false;

            return true;
        }
    }
}
