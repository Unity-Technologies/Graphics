using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    partial class RenderingDebuggerTests
    {
        class Volumes
        {
            static TestCaseData[] s_TestCaseDatasVolumeParameters =
            {
                new TestCaseData(null).Returns("N/A"), new TestCaseData(new FloatParameter(101.01f)).Returns("101.01"),
                new TestCaseData(new BoolParameter(true)).Returns("True"),
                new TestCaseData(new BoolParameter(false)).Returns("False"),
                new TestCaseData(new IntParameter(1000)).Returns("1000"),
                new TestCaseData(new ColorParameter(Color.red)).Returns("RGBA(1.000, 0.000, 0.000, 1.000)"),
                new TestCaseData(new IntParameter(1000, overrideState: false)).Returns("1000"), // Extract value should give the proper value
            };

            [Test, TestCaseSource(nameof(s_TestCaseDatasVolumeParameters))]
            public string ExtractResult(VolumeParameter parameter)
            {
                return DebugDisplaySettingsVolume.ExtractResult(parameter);
            }

            static TestCaseData[] s_TestCaseDatasCreateParameterWidget =
            {
                new TestCaseData(null).Returns("N/A"),
                new TestCaseData(new FloatParameter(101.01f, overrideState: true)).Returns("101.01"),
                new TestCaseData(new BoolParameter(true, overrideState: true)).Returns("True"),
                new TestCaseData(new BoolParameter(false, overrideState: true)).Returns("False"),
                new TestCaseData(new IntParameter(1000, overrideState: true)).Returns("1000"),
                new TestCaseData(new ColorParameter(Color.red, overrideState: true)).Returns("RGBA(1.000, 0.000, 0.000, 1.000)"),
                new TestCaseData(new IntParameter(1000, overrideState: false)).Returns("1000"),
            };

            [Test, TestCaseSource(nameof(s_TestCaseDatasCreateParameterWidget))]
            public string CreateParameterWidget(VolumeParameter parameter)
            {
                var widget = DebugDisplaySettingsVolume.WidgetFactory.CreateVolumeParameterWidget(string.Empty, false, parameter);
                if (widget is DebugUI.Value value)
                    return value.getter() as string;
                if (widget is DebugUI.ColorField colorField)
                    return colorField.getter().ToString();

                return "Not Implemented";
            }
        }
    }
}
