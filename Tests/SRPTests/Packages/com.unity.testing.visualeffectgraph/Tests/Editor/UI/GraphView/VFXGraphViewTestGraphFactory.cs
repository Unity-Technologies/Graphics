using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    internal static class VFXGraphViewTestGraphFactory
    {
        internal const string TempDirectoryName = "Assets/TmpTests";

        internal static VFXViewWindow CreateSimpleVFXGraph()
        {
            //Create a new vfx based on the usual template
            System.IO.Directory.CreateDirectory(TempDirectoryName);
            var templateString = System.IO.File.ReadAllText(VFXTestCommon.simpleParticleSystemPath);
            var fileName = TempDirectoryName + $"/{GUID.Generate()}.vfx";
            System.IO.File.WriteAllText(fileName, templateString);
            AssetDatabase.ImportAsset(fileName);

            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(fileName);
            VFXViewWindow window = VFXTestCommon.GetWindow(vfx, true);
            window.LoadResource(vfx.GetResource());

            return window;
        }
    }
}
