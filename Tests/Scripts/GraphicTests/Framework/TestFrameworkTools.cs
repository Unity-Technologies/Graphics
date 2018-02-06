using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEditor;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.Experimental.Rendering
{
    public class TestFrameworkTools
    {
        public static readonly string s_RootPath = Directory.GetParent(Directory.GetFiles(Application.dataPath, "SRPMARKER", SearchOption.AllDirectories).First()).ToString();

        // path where the tests live
        public static string[] s_Path =
        {
            "Tests",
            "GraphicsTests",
            "RenderPipeline"
        };

        public static Dictionary<string, string> renderPipelineAssets = new Dictionary<string, string>()
        {
            { "HDRP", "HDRenderPipeline/CommonAssets/HDRP_GraphicTests_Asset.asset" },
            { "LWRP", "LightweightPipeline/LightweightPipelineAsset.asset" }
        };

        // Renderpipeline assets used for the tests
        public static Dictionary<string, string> renderPipelineScenesFolder = new Dictionary<string, string>()
        {
            { "HDRP", "HDRenderPipeline/Scenes" },
            { "LWRP", "LightweightPipeline/Scenes" }
        };
        
        // info that gets generated for use
        // in a dod way
        public struct TestInfo
        {
            public string name;
            public float threshold;
            public string relativePath;
            public string templatePath;
            public int frameWait;
            public int sceneListIndex;

            public override string ToString()
            {
                return name;
            }

        }

        // collect the scenes that we can use
        public static class CollectScenes
        {
            public static IEnumerable HDRP
            {
                get
                {
                    return GetScenesForPipelineID("HDRP");
                }
            }

            public static IEnumerable HDRP_Params
            {
                get
                {
                    return GetScenesForPipelineID("HDRP", true);
                }
            }

            public static IEnumerable LWRP
            {
                get
                {
                    return GetScenesForPipelineID("LWRP");
                }
            }

            public static IEnumerable GetScenesForPipelineID(string _pipelineID, bool fixtureParam = false)
            {
                return GetScenesForPipeline(renderPipelineScenesFolder[_pipelineID]);
            }

            public static IEnumerable GetScenesForPipeline(string _pipelinePath, bool fixtureParam = false)
            {
                var absoluteScenesPath = s_Path.Aggregate(s_RootPath, Path.Combine);

                var filesPath = Path.Combine(absoluteScenesPath, _pipelinePath);

                // find all the scenes
                var allPaths = Directory.GetFiles(filesPath, "*.unity", SearchOption.AllDirectories);

                // Convert to List for easy sorting in alphabetical order
                List<string> allPaths_List = new List<string>(allPaths);
                allPaths_List.Sort();

                // Get the play mode scenes
                List<string> playModeScenes = new List<string>();
                foreach( TestInfo ti in CollectScenesPlayMode.GetScenesForPipeline( _pipelinePath ) )
                {
                    playModeScenes.Add(ti.templatePath);
                }

                // construct all the needed test infos
                for (int i = 0; i < allPaths_List.Count; ++i)
                {
                    var path = allPaths_List[i];

                    var p = new FileInfo(path);
                    var split = s_Path.Aggregate("", Path.Combine);
                    split = string.Format("{0}{1}", split, Path.DirectorySeparatorChar);
                    var splitPaths = p.FullName.Split(new[] { split }, StringSplitOptions.RemoveEmptyEntries);

                    // Filter out play mode tests from the list
                    if (playModeScenes.Contains(splitPaths.Last()))
                        continue;

                    TestInfo testInfo = new TestInfo()
                    {
                        name = p.Name,
                        relativePath = splitPaths.Last(),
                        templatePath = splitPaths.Last(),
                        threshold = 0.02f,
                        frameWait = 100
                    };

                    if (fixtureParam)
                        yield return new TestFixtureData(testInfo);
                    else
                        yield return testInfo;
                }
            }
        }

        public static class CollectScenesPlayMode
        {
            public static IEnumerable HDRP
            {
                get
                {
                    return GetScenesForPipelineID("HDRP");
                }
            }
            public static IEnumerable HDRP_Param
            {
                get
                {
                    return GetScenesForPipelineID("HDRP", true);
                }
            }

            public static IEnumerable LWRP
            {
                get
                {
                    return GetScenesForPipelineID("LWRP");
                }
            }

            public static IEnumerable GetScenesForPipelineID(string _pipelineID, bool fixtureParam = false)
            {
                return GetScenesForPipeline(renderPipelineScenesFolder[_pipelineID]);
            }

            public static IEnumerable GetScenesForPipeline(string _pipelinePath, bool fixtureParam = false)
            {
#if UNITY_EDITOR
                string absoluteScenesPath = s_Path.Aggregate(s_RootPath, Path.Combine);

                string assetScenesPath = absoluteScenesPath.Replace(Application.dataPath, "");
                assetScenesPath = Path.Combine("Assets", assetScenesPath.Remove(0, 1));

                string filesPath = Path.Combine(assetScenesPath, _pipelinePath);

                string listFilePath = Path.Combine(filesPath, "EditorPlayModeTests.asset");

                EditorPlayModeTests listFile = (EditorPlayModeTests) AssetDatabase.LoadMainAssetAtPath(listFilePath);
                if ( listFile == null)
                {
                    AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EditorPlayModeTests>(), listFilePath);
                    AssetDatabase.Refresh();

                    yield return null;
                }
                else
                {
                    for ( int i=0 ; i<listFile.scenesPath.Length ; ++i)
                    {
                        string path = listFile.scenesPath[i];

                        var p = new FileInfo( Path.Combine(filesPath,  path ) );
                        var split = s_Path.Aggregate("", Path.Combine);
                        split = string.Format("{0}{1}", split, Path.DirectorySeparatorChar);
                        var splitPaths = p.FullName.Split(new[] { split }, StringSplitOptions.RemoveEmptyEntries);

                        TestInfo testInfo = new TestInfo
                        {
                            name = p.Name,
                            relativePath = p.ToString(),
                            templatePath = splitPaths.Last(),
                            threshold = 0.02f,
                            frameWait = 100,
                            sceneListIndex = i
                        };

                        if (fixtureParam)
                            yield return new TestFixtureData(testInfo);
                        else
                            yield return testInfo;
                    }
                }
#else
            yield return "null";
#endif
            }
        }

        // compare textures, use RMS for this
        public static bool CompareTextures(Texture2D fromDisk, Texture2D captured, float threshold)
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
            return rmse < threshold;
        }

        public static Texture2D RenderSetupToTexture( SetupSceneForRenderPipelineTest _testSetup)
        {
            // Setup Render Target
            Camera testCamera = _testSetup.cameraToUse;
            var rtDesc = new RenderTextureDescriptor(
                             _testSetup.width,
                             _testSetup.height,
                             (_testSetup.hdr && testCamera.allowHDR) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32,
                             24);

#if UNITY_EDITOR
            rtDesc.sRGB = PlayerSettings.colorSpace == ColorSpace.Linear;
#endif
            rtDesc.msaaSamples = _testSetup.msaaSamples;

            // render the scene
            var tempTarget = RenderTexture.GetTemporary(rtDesc);
            var oldTarget = _testSetup.cameraToUse.targetTexture;
            _testSetup.cameraToUse.targetTexture = tempTarget;

            _testSetup.cameraToUse.Render();
            _testSetup.cameraToUse.targetTexture = oldTarget;

            // Readback the rendered texture
            var oldActive = RenderTexture.active;
            RenderTexture.active = tempTarget;
            var captured = new Texture2D(tempTarget.width, tempTarget.height, TextureFormat.RGB24, false);
            captured.ReadPixels(new Rect(0, 0, _testSetup.width, _testSetup.height), 0, 0);
            RenderTexture.active = oldActive;

            return captured;
        }

        public static bool FindReferenceImage(TestInfo _testInfo, ref Texture2D _fromDisk, Texture2D _captured, ref string _dumpFileLocation)
        {
            var templatePath = Path.Combine(s_RootPath, "ImageTemplates");

            // find the reference image
            _dumpFileLocation = Path.Combine(templatePath, string.Format("{0}.{1}", _testInfo.templatePath, "png"));
            //Debug.Log("Template file at: " + _dumpFileLocation);
            if (!File.Exists(_dumpFileLocation))
            {
                // no reference exists, create it
                var fileInfo = new FileInfo(_dumpFileLocation);
                fileInfo.Directory.Create();

                var generated = _captured.EncodeToPNG();
                File.WriteAllBytes(_dumpFileLocation, generated);

                return false;
            }

            var template = File.ReadAllBytes(_dumpFileLocation);

            _fromDisk.LoadImage(template, false);

            return true;
        }

        public static Texture2D GetTemplateImage(string _templatePath)
        {
            string templatePath = Path.Combine(s_RootPath, "ImageTemplates");
            templatePath = Path.Combine(templatePath, string.Format("{0}.{1}", _templatePath, "png"));

            if (File.Exists(templatePath))
            {
                byte[] template = File.ReadAllBytes(templatePath);
                Texture2D o = new Texture2D(4, 4);
                return o.LoadImage(template, false) ? o : null;
            }
            else
                return null;
        }

        public static Texture2D GetTemplateImage(TestInfo _testInfo)
        {
            return GetTemplateImage(_testInfo.templatePath);
        }

#if UNITY_EDITOR
        public static Texture2D GetTemplateImage(UnityEngine.Object _sceneAsset, ref string path)
        {
            string _scenePath = AssetDatabase.GetAssetPath(_sceneAsset);

            var p = new FileInfo(_scenePath);

            var split = s_Path.Aggregate("", Path.Combine);
            split = string.Format("{0}{1}", split, Path.DirectorySeparatorChar);
            var splitPaths = p.FullName.Split(new[] { split }, StringSplitOptions.RemoveEmptyEntries);

            path = splitPaths.Last();

            TestInfo testInfo = new TestInfo
            {
                name = p.Name,
                relativePath = p.ToString(),
                templatePath = splitPaths.Last(),
                threshold = 0.02f,
                frameWait = 100,
            };

            return GetTemplateImage(testInfo);
        }
#endif

        public static class AssertFix
        {
            public static void TestWithMessages( bool? _comparison, string _fail = "Test failed", string _pass = null )
            {
                if (_comparison.HasValue)
                {
                    if (_comparison.Value)
                        NUnit.Framework.Assert.IsTrue(true, _pass);
                    else
                        throw new System.Exception(_fail);
                }
                else
                    throw new System.Exception("Test comparison is null.");
            }
        }
    }
}
