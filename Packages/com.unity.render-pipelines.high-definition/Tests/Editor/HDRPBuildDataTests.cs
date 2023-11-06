using NUnit.Framework;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Tests
{
    class HDRPBuildDataTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
                Assert.Ignore("This is an HDRP Tests, and the current pipeline is not HDRP.");
        }
        
        static bool CompareObjects<T>(object obj1, object obj2)
        {
            Type type = obj1.GetType();

            // Get all fields of the class
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                object value1 = field.GetValue(obj1);
                object value2 = field.GetValue(obj2);

                if (field.GetValue(obj1) is ICollection enumerable1)
                {
                    var enumerable2 = field.GetValue(obj1) as ICollection;

                    if (enumerable1.Count != enumerable2.Count)
                    {
                        UnityEngine.Debug.LogError($"Field {field.Name} did not rollback to its default state");
                        return false;
                    }

                }
                else if (!object.Equals(value1, value2))
                {
                    UnityEngine.Debug.LogError($"Field {field.Name} did not rollback to its default state");
                    return false;
                }
            }

            return true;
        }

        [Test]
        public void CheckDisposeClearsAllData()
        {
            var instance = new HDRPBuildData(EditorUserBuildSettings.activeBuildTarget, Debug.isDebugBuild);
            instance.Dispose();
            Assert.IsTrue(CompareObjects<HDRPBuildData>(instance, new HDRPBuildData()));
        }
    }
}
