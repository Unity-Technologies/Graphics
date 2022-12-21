using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace MultipleSRP.EditMode.Analytics
{
    public class BuildTargetAnalyticTests
    {
        const string k_Analytic = "UnityEditor.Rendering.Analytics.{0}, Unity.RenderPipelines.Core.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        const string k_AnalyticData  = "UnityEditor.Rendering.Analytics.{0}+{1}, Unity.RenderPipelines.Core.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        static TestCaseData[] s_TestCaseData =
        {
            new TestCaseData("BuildTargetAnalytic", "BuildTargetAnalyticData")
                .SetName("Analytic for Quality Levels and RenderPipeline is gathered correctly")
                .Returns("{\"build_target\":\"StandaloneWindows64\",\"render_pipeline_asset_type\":\"Built-In Render Pipeline\",\"quality_levels\":1,\"total_quality_levels_on_project\":1}")
        };

        [Test, TestCaseSource(nameof(s_TestCaseData))]
        public string TryGatherData(string analyticTypeString, string analyticDataTypeString)
        {
            var analyticType = Type.GetType(string.Format(k_Analytic, analyticTypeString));
            var analytic = Activator.CreateInstance(analyticType);


            var analyticDataType = Type.GetType(string.Format(k_AnalyticData, analyticTypeString, analyticDataTypeString));
            var analyticData = Activator.CreateInstance(analyticDataType);

            var method = analyticType
                .GetMethod("TryGatherData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.IsNotNull(method, "Unable to find private static method 'TryGatherData'");

            object[] parameters = new object[] { analyticData, string.Empty };
            object result = method.Invoke(analytic, parameters);

            bool booleanResult = (bool)result;
            Assert.IsTrue(booleanResult);

            string json = JsonUtility.ToJson(parameters[0]);

            Assert.IsFalse(string.IsNullOrEmpty(json));
            return json;
        }
    }
}
