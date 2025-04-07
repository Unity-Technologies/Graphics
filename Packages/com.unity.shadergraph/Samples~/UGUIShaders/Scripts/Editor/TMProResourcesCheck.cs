using UnityEditor;
using UnityEngine;
using TMPro;

namespace Unity.UI.Shaders.Sample.Editor
{
    public static class TMProResourcesCheck
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            var tmProSettings = TMP_Settings.instance; // getting the instance should be enough
            //var tmProStettings = Resources.Load<TMP_Settings>("TMP Settings");
            //if (tmProStettings == null)
            //    TMP_PackageResourceImporterWindow.ShowPackageImporterWindow();
        }
    }
}
