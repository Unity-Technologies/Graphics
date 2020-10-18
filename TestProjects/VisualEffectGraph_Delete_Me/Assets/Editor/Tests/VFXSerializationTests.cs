#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.VFX;
using UnityEditor.VFX;


using Object = UnityEngine.Object;
using System.IO;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSerializationTests
    {
        private readonly static string kSourceAsset = "Assets/_ProblematicVFX.vfx.rename_me";

        private static List<string> m_ToDeleteAsset = new List<string>();
        [OneTimeTearDown]
        public void CleanUp()
        {
            foreach (var asset in m_ToDeleteAsset)
            {
                File.Delete(asset);
            }
        }

        [Test]
        public void QuickTest_Sanitize_Parameter()
        {
            var rand = Guid.NewGuid().ToString();
            var name = "Assets/Temp_" + rand + ".vfx";
            File.Copy(kSourceAsset, name);
            m_ToDeleteAsset.Add(name);
            AssetDatabase.ImportAsset(name);
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(name);
        }
    }
}
#endif
