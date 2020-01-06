using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    public class LoadVFXFromAssetBundle : MonoBehaviour
    {
        public static string GetAssetBundleBasePath()
        {
            var basePath = System.IO.Directory.GetCurrentDirectory();

            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-logfile" && i != args.Length - 1)
                {
                    var testResultID = "test-results";
                    var logPath = args[i + 1];
                    if (logPath.Contains(testResultID))
                    {
                        basePath = logPath.Substring(0, logPath.IndexOf(testResultID) + testResultID.Length);
                    }
                    break;
                }
            }
            return System.IO.Path.Combine(basePath, "VFX_Bundle_Test");
        }

        void Start()
        {
            var basePath = GetAssetBundleBasePath();
            var fullPath = System.IO.Path.Combine(basePath, "vfx_in_assetbundle");
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError("Unable to find bundle at : " + fullPath);
            }

            var assetBundle = AssetBundle.LoadFromFile(fullPath);
            var asset = assetBundle.LoadAsset("Packages/com.unity.testing.visualeffectgraph/AssetBundle/VFX_In_AssetBundle.prefab") as GameObject;
            var vfx = Instantiate(asset).GetComponent<VisualEffect>();
            if (vfx == null)
            {
                Debug.LogError("Unable to load VFX_In_AssetBundle");
            }
        }

        void Update()
        {

        }
    }
}
