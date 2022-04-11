using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using Unity.PerformanceTesting;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static PerformanceTestUtils;
using static PerformanceMetricNames;

public class HDReflectionSystemTests : EditorPerformanceTests
{
    [Version("1"), Test, Performance]
    public void HDProbeSystemRegister()
    {
        using (ListPool<HDProbe>.Get(out var probes))
        {
            // Create a lot of probe
            for (var i = 0; i < 10000; ++i)
            {
                var gameObject = new GameObject(i.ToString("0000"));
                // Deactivate the GameObject to avoid OnEnable calls (which register the probe)
                gameObject.SetActive(false);
                gameObject.AddComponent<ReflectionProbe>();
                var probe = gameObject.AddComponent<HDAdditionalReflectionData>();
                probe.enabled = false;
                probes.Add(probe);
            }

            // Measure registration
            Measure.Method(() =>
            {
                foreach (var probe in probes)
                    HDProbeSystem.RegisterProbe(probe);
            }).Run();

            // Unregister
            foreach (var probe in probes)
                HDProbeSystem.UnregisterProbe(probe);

            // Delete the probes
            foreach (var probe in probes)
                Object.DestroyImmediate(probe.gameObject);
        }
    }
}
