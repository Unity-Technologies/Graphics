using System;
using System.IO;
using NUnit.Framework;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    internal static class VFXSerializationRoundTripTestHelper
    {
        internal static void SaveAndReload(string testAssetDirectory, string suffixname, Action<VisualEffectAsset> write, Action<VisualEffectAsset> read)
        {
            var kTempAssetPathA = string.Format("{0}/Temp_{1}_A.vfx", testAssetDirectory, suffixname);
            var kTempAssetPathB = string.Format("{0}/Temp_{1}_B.vfx", testAssetDirectory, suffixname);
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);

            int hashCodeAsset = 0; //check reference are different between load & reload
            {
                var asset = VisualEffectAssetEditorUtility.CreateNewAsset(kTempAssetPathA);

                hashCodeAsset = asset.GetHashCode();

                write(asset);
                asset.GetResource().UpdateSubAssets();

                AssetDatabase.SaveAssets();

                asset = null;
                EditorUtility.UnloadUnusedAssetsImmediate();
                //AssetDatabase.CopyAsset(kTempAssetPathA, kTempAssetPathB); // TODO Deactivated because a regression makes it fail when load the copy
                File.Copy(kTempAssetPathA, kTempAssetPathB);

                if (asset != null)
                    AssetDatabase.RemoveObjectFromAsset(asset);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EditorUtility.UnloadUnusedAssetsImmediate();
            {
                VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(kTempAssetPathB);
                Assert.AreNotEqual(hashCodeAsset, asset.GetHashCode());

                read(asset);
            }
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);
        }
    }
}
