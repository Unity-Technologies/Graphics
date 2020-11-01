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
    public class VFXSanitizeTests
    {
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
        public void Sanitize_Parameter_With_Previous_Type_Structure()
        {
            //TODOPAUL : Simplify the content of this asset
            string kSourceAsset = "Assets/AllTests/Editor/Tests/VFXSanitize_Test.vfx.rename_me";
            var rand = Guid.NewGuid().ToString();
            var name = "Assets/Temp_" + rand + ".vfx";
            File.Copy(kSourceAsset, name);
            m_ToDeleteAsset.Add(name);
            AssetDatabase.ImportAsset(name);
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(name);

            //TODOPAUL : Check also the actual structure of the graph
        }
    }
}
#endif
