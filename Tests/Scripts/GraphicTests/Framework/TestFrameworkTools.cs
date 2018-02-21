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
        public static int compareTileSize = 32;
        public static float compareThreshold = 0.01f;
        public static int frameWait = 100;

        public enum ComparisonMethod {RMSE, Jzazbz, Lab}
        public static ComparisonMethod comparisonMethod = ComparisonMethod.Lab;

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
            { "HDRP", "HDRenderPipeline/CommonAssets/RP_Assets/HDRP_Test_Def.asset" },
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
            public string comment;
            public float threshold;
            public string relativePath;
            public string templatePath;
            public int frameWait;
            public int sceneListIndex;

            public override string ToString()
            {
                if (string.IsNullOrEmpty(comment))
                    return name;
                else
                    return string.Format("{0}: {1}", name, comment);
            }

        }

        // Get additionalSceneInfo
        public static Dictionary<string, AdditionalTestSceneInfos.AdditionalTestSceneInfo> GetAdditionalInfos ( string path)
        {
            Dictionary<string, AdditionalTestSceneInfos.AdditionalTestSceneInfo> o = new Dictionary<string, AdditionalTestSceneInfos.AdditionalTestSceneInfo>();

            AdditionalTestSceneInfos additionalTestSceneInfos = AssetDatabase.LoadAssetAtPath<AdditionalTestSceneInfos>(path);

            if (additionalTestSceneInfos != null)
            {
                for (int i=0 ; i<additionalTestSceneInfos.additionalInfos.Length ; ++i)
                {
                    o[additionalTestSceneInfos.additionalInfos[i].name] = additionalTestSceneInfos.additionalInfos[i];
                }
            }

            return o;
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

                // Get the additional infos
                var additionalInfos = GetAdditionalInfos( "Assets"+Path.Combine(filesPath.Replace(Application.dataPath, ""), "AdditionalTestSceneInfos.asset") );

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

                    string sceneNum = p.Name.Split("_"[0])[0];

                    TestInfo testInfo = new TestInfo()
                    {
                        name = p.Name,
                        comment = additionalInfos.ContainsKey(sceneNum)? additionalInfos[sceneNum].comment:null,
                        relativePath = splitPaths.Last(),
                        templatePath = splitPaths.Last(),
                        threshold = compareThreshold,
                        frameWait = frameWait
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
                    // Get the additional infos
                    var additionalInfos = GetAdditionalInfos( Path.Combine(filesPath, "AdditionalTestSceneInfos.asset") );

                    for ( int i=0 ; i<listFile.scenesPath.Length ; ++i)
                    {
                        string path = listFile.scenesPath[i];

                        var p = new FileInfo( Path.Combine(filesPath,  path ) );
                        var split = s_Path.Aggregate("", Path.Combine);
                        split = string.Format("{0}{1}", split, Path.DirectorySeparatorChar);
                        var splitPaths = p.FullName.Split(new[] { split }, StringSplitOptions.RemoveEmptyEntries);

                        string sceneNum = p.Name.Split("_"[0])[0];

                        TestInfo testInfo = new TestInfo
                        {
                            name = p.Name,
                            comment = additionalInfos.ContainsKey(sceneNum)? additionalInfos[sceneNum].comment:null,
                            relativePath = p.ToString(),
                            templatePath = splitPaths.Last(),
                            threshold = compareThreshold,
                            frameWait = frameWait,
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

        // compare textures, use by tile Jzazbz perceptual color difference: 5.2 of https://www.osapublishing.org/oe/fulltext.cfm?uri=oe-25-13-15131&id=368272
        public static bool CompareTextures(Texture2D fromDisk, Texture2D captured, float threshold)
        {
            /* Get Min/Max colorspace values for full rgb range

            Vector3 vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 vTmp;
            for (float r = 0f; r<=1f ; r+=0.01f)
            {
                for (float g = 0f; g<=1f ; g+=0.01f)
                {
                    for (float b = 0f; b<=1f ; b+=0.01f)
                    {
                        vTmp = RGB2JzAzBz(new Color(r, g, b, 1f));
                        vMin.x = Mathf.Min(vMin.x, vTmp.x);
                        vMin.y = Mathf.Min(vMin.y, vTmp.y);
                        vMin.z = Mathf.Min(vMin.z, vTmp.z);
                        vMax.x = Mathf.Max(vMax.x, vTmp.x);
                        vMax.y = Mathf.Max(vMax.y, vTmp.y);
                        vMax.z = Mathf.Max(vMax.z, vTmp.z);
                    }
                }
            }

            Debug.Log("ColorRange min/max: "+vMin+" / "+vMax);
            // */


            if (fromDisk == null || captured == null)
                return false;

            if (fromDisk.width != captured.width
                || fromDisk.height != captured.height)
                return false;

            var pixels1 = fromDisk.GetPixels();
            var pixels2 = captured.GetPixels();

            if (pixels1.Length != pixels2.Length)
                return false;

            for (int y = 0 ; y < captured.height ; y+=compareTileSize)
            {
                for (int x = 0 ; x < captured.width ; x+=compareTileSize)
                {
                    int numberOfPixels = 0;

                    float sumOffData = 0f;

                    for (int y2 = y ; y2 < Mathf.Min(captured.height, y+compareTileSize) ; ++y2)
                    {
                        for (int x2 = x ; x2 < Mathf.Min(captured.width, x+compareTileSize) ; ++x2)
                        {
                            Color p1 = pixels1[ y2 * captured.width + x2 ];
                            Color p2 = pixels2[ y2 * captured.width + x2 ];
                            Color diff;

                            switch (comparisonMethod)
                            {
                                case ComparisonMethod.Jzazbz:
                                    sumOffData += JzAzBzDiff(RGB2JzAzBz(p1), RGB2JzAzBz(p2));
                                break;
                                case ComparisonMethod.Lab:
                                    diff = p1 - p2;
                                    Vector3 vDiff = new Vector3(diff.r, diff.g, diff.b);
                                    vDiff.x /= 100f; // L range is [0-100]
                                    sumOffData += vDiff.magnitude;
                                break;
                                default:
                                    diff = p1 - p2;
                                    diff = diff * diff;
                                    sumOffData += (diff.r + diff.g + diff.b) / 3.0f;
                                break;

                            }
                            ++numberOfPixels;
                        }
                    }
                    
                    float result = sumOffData / numberOfPixels;
                    if (result > threshold)
                        return false;
                }
            }

            return true;

            /*
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
            */
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

        // Folowing color conversion code source : https://github.com/nschloe/colorio

        // RGB to XYZ 100 : file:///C:/Users/Remy/Downloads/srgb.pdf
        static Vector3 RGB2XYZ ( Color color )
        {
            return new Vector3(
                color.r * 0.4124564f + color.g * 0.3575761f + color.b * 0.1804375f,
                color.r * 0.2126729f + color.g * 0.7151522f + color.b * 0.0721750f,
                color.r * 0.0193339f + color.g * 0.1191920f + color.b * 0.9503041f
            ) * 100;
        }

        // JzAzBz color conversion : https://www.osapublishing.org/oe/fulltext.cfm?uri=oe-25-13-15131&id=368272

        static Vector3 RGB2JzAzBz (Color color)
        {
            Vector3 xyz = RGB2XYZ( color);

            float b = 1.15f;
            float g = 0.66f;
            float c1 = 0.8359375f;      // 3424f / 2^12
            float c2 = 18.8515625f;     // 2413f / 2^7
            float c3 = 18.6875f;        // 2392f / 2^7
            float n = 0.15930175781f;   // 2610/2^14
            float p= 134.034375f;       // 1.7*2523/2^5
            float d = -0.56f;
            float d0 = 1.6295499532821566E-11f;

            float x2 = b * xyz.x - (b-1) * xyz.z;
            float y2 = g * xyz.y - (g-1) * xyz.x;

            Vector3 lms = new Vector3(
                0.41478372f * x2 + 0.579999f * y2 + 0.0146480f * xyz.z,
                -0.2015100f * x2 + 1.120649f * y2 + 0.0531008f * xyz.z,
                -0.0166008f * x2 + 0.264800f * y2 + 0.6684799f * xyz.z
            );

            Vector3 lmsPowN = Vec3Pow(lms/10000f, n);

            Vector3 tmp = Vec3Divide(
                Vector3.one * c1 + c2 * lmsPowN ,
                Vector3.one + c3 * lmsPowN
                 );

            Vector3 lms2 = Vec3Pow( tmp , p ) ;

            Vector3 jab = new Vector3(
                0.5f * lms2.x + 0.5f * lms2.y,
                3.524000f * lms2.x + -4.066708f * lms2.y + 0.542708f * lms2.z,
                0.199076f * lms2.x + 1.096799f * lms2.y + -1.295875f * lms2.z
            );

            jab.x = (((1f+d)*jab.x)/(1f+d*jab.x))-d0;

            return jab;
        }

        static float JzAzBzDiff( Vector3 v1, Vector3 v2)
        {
            float c1 = Mathf.Sqrt(v1.y*v1.y + v1.z*v1.z);
            float c2 = Mathf.Sqrt(v2.y*v2.y + v2.z*v2.z);

            float h1 = Mathf.Atan(v1.z/v1.y);
            float h2 = Mathf.Atan(v2.z/v2.y);

            float deltaH = 2*Mathf.Sqrt( c1*c2 ) * Mathf.Sin((h1-h2)/2f);

            return Mathf.Sqrt( Mathf.Pow( v1.x-v2.x ,2f) + Mathf.Pow(c1-c2, 2f) + deltaH * deltaH );
        }

        static Vector3 RGB2Lab( Color color )
        {
            Vector3 xyz = RGB2XYZ( color);

            float xn = 95.047f;
            float yn = 100f;
            float zn = 108.883f;

            return new Vector3(
                116f * XYZ2LabFunc( xyz.y / yn ) - 16f,
                500f * ( XYZ2LabFunc(xyz.x / xn) - XYZ2LabFunc(xyz.y/yn) ),
                200f * (XYZ2LabFunc(xyz.y / yn) - XYZ2LabFunc(xyz.z / zn))
            );
        }

        static float XYZ2LabFunc( float f )
        {
            float delta = 6f/29f;

            if ( f > delta )
                return Mathf.Pow(f, 1f/3f);
            else
                return f/(3*delta*delta) + 4f / 29f;
        }

        static Vector3 Vec3Pow (Vector3 v, float p)
        {
            return new Vector3(
                Mathf.Pow(v.x, p),
                Mathf.Pow(v.y, p),
                Mathf.Pow(v.z, p)
            );
        }

        static Vector3 Vec3Divide(Vector3 a, Vector3 b )
        {
            return new Vector3(a.x/b.x, a.y/b.y, a.z/b.z);
        }
    }
}
