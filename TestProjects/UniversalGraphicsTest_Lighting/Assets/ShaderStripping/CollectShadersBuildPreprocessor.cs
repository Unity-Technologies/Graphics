using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ShaderStripping
{
    [InitializeOnLoad]
    class CollectShadersCallbacks : ICallbacks
    {
        static CollectShadersCallbacks()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new CollectShadersCallbacks());
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            ClearCurrentShaderVariantCollection();
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"{GetCurrentShaderVariantCollectionShaderCount()} shaders, {GetCurrentShaderVariantCollectionVariantCount()} variants");
            if (!File.Exists("Assets/UniversalGraphicsTests.shadervariants"))
            {
                SaveCurrentShaderVariantCollection("Assets/UniversalGraphicsTests.shadervariants");
                AssetDatabase.ImportAsset("Assets/UniversalGraphicsTests.shadervariants");
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }

        static Type s_ShaderUtil = typeof(ShaderUtil);

        static MethodInfo s_ClearCurrentShaderVariantCollection = s_ShaderUtil.GetMethod("ClearCurrentShaderVariantCollection", BindingFlags.NonPublic | BindingFlags.Static);

        static MethodInfo s_GetCurrentShaderVariantCollectionShaderCount = s_ShaderUtil.GetMethod("GetCurrentShaderVariantCollectionShaderCount", BindingFlags.NonPublic | BindingFlags.Static);

        static MethodInfo s_GetCurrentShaderVariantCollectionVariantCount = s_ShaderUtil.GetMethod("GetCurrentShaderVariantCollectionVariantCount", BindingFlags.NonPublic | BindingFlags.Static);

        static MethodInfo s_SaveCurrentShaderVariantCollection = s_ShaderUtil.GetMethod("SaveCurrentShaderVariantCollection", BindingFlags.NonPublic | BindingFlags.Static);

        static void ClearCurrentShaderVariantCollection()
        {
            s_ClearCurrentShaderVariantCollection.Invoke(null, new object[] {});
        }

        static int GetCurrentShaderVariantCollectionShaderCount()
        {
            return (int)s_GetCurrentShaderVariantCollectionShaderCount.Invoke(null, new object[] {});
        }

        static int GetCurrentShaderVariantCollectionVariantCount()
        {
            return (int)s_GetCurrentShaderVariantCollectionVariantCount.Invoke(null, new object[] {});
        }

        static void SaveCurrentShaderVariantCollection(string path)
        {
            s_SaveCurrentShaderVariantCollection.Invoke(null, new object[] { path });
        }
    }
}
