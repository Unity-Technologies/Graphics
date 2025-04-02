using NUnit.Framework.Interfaces;
using UnityEngine.TestTools;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Internal.Builders;
using NUnit.Framework;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.TestTools.Graphics.TestCases;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using UnityEditor.TestTools.Graphics;
#endif

namespace UnityEngine.VFX.PerformanceTest
{
    public class VfxPerformanceGraphicsTestAttribute : SceneGraphicsTestAttribute
    {
        public VfxPerformanceGraphicsTestAttribute(params string[] scenePaths) : base(typeof(VFXPerformanceGraphicsTestCaseSource), scenePaths) { }
    }

    public class VFXPerformanceGraphicsTestCaseSource : SceneGraphicsTestCaseSource
    {
        public static string GetPrefix()
        {
            //Can't use SRPBinder here, this code is also runtime
            var currentSRP = QualitySettings.renderPipeline ?? GraphicsSettings.currentRenderPipeline;
            if (currentSRP == null)
                return "BRP";
            if (currentSRP.name.Contains("HDRenderPipeline"))
                return "HDRP";
            return currentSRP.name;
        }

        public override IEnumerable<GraphicsTestCase> GetTestCases(IMethodInfo method)
        {
            var testCases = base.GetTestCases(method);

            foreach (var testCase in testCases)
            {
                yield return testCase with { Name = GetPrefix() + "." + testCase.Name };

            }
        }
    }
}
