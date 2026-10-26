using NUnit.Framework.Interfaces;
using UnityEngine.TestTools;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework.Internal.Builders;
using NUnit.Framework;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.TestTools.Graphics.TestCases;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.Graphics;
#endif

namespace UnityEngine.MultipleSRPGraphicsTest
{
    public class MultipleSRPGraphicsTestAttribute : SceneGraphicsTestAttribute
    {
        public MultipleSRPGraphicsTestAttribute(params string[] scenePaths) : base(typeof(MultipleSRPGraphicsTestCaseSource), scenePaths) { }
    }

    public class MultipleSRPGraphicsTestCaseSource : SceneGraphicsTestCaseSource
    {
        private static SRPTestSceneAsset srpTestSceneAsset = Resources.Load<SRPTestSceneAsset>("SRPTestSceneSO");

        public override IEnumerable<GraphicsTestCase> GetTestCases(IMethodInfo methodInfo, ITest suite)
        {
            if (srpTestSceneAsset == null)
                yield break;

            var testCasesList = base.GetTestCases(methodInfo, suite).ToList();
            for (int i = 0; i < testCasesList.Count; i++)
            {
                var testCase = testCasesList[i];
                var srpAssets = srpTestSceneAsset.testDatas[i].srpAssets;

                foreach (var srpAsset in srpAssets)
                {
                    var srpName = srpAsset != null ? srpAsset.name : "BuiltIn";
                    yield return testCase with
                    {
                        Name = testCase.Name + "_" + srpName,
                        FullName = testCase.FullName + "_" + srpName,
                    };
                }
            }
        }
    }
}
