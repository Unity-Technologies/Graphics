using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// UUM-153123: dragging a Freeform light's points until its edges crossed threw IndexOutOfRangeException
// from the tessellator, because the outline reached it without the crossing edges being split first.
class Light2DFreeformShapeMeshTests
{
    // The Freeform light from the bug's project.
    static readonly Vector3[] k_Kite = { new Vector3(-0.5f, 0, 0), new Vector3(0, -0.25f, 0), new Vector3(0.8754394f, 0.037077308f, 0), new Vector3(0, 0.25f, 0) };

    readonly List<GameObject> m_Created = new List<GameObject>();

    [TearDown]
    public void DestroyCreatedGameObjects()
    {
        foreach (var go in m_Created)
            if (go != null)
                Object.DestroyImmediate(go);
        m_Created.Clear();
    }

    Light2D NewFreeformLight()
    {
        var go = new GameObject("Freeform Light 2D");
        m_Created.Add(go);
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Freeform;
        return light;
    }

    // Whether a crossing outline throws depends on exactly where the points land, so a single hand-picked
    // shape can miss it. Drag each point across a grid instead, as a user dragging in the Scene view would.
    // The grid is built by repeated addition on purpose: before the fix, 244 of these positions threw.
    [Test]
    public void DraggingPointsAcrossEdges_BuildsMeshWithoutThrowing()
    {
        var light = NewFreeformLight();
        var failures = new List<string>();

        for (int point = 0; point < k_Kite.Length; ++point)
        for (float x = -1.5f; x <= 1.5001f; x += 0.1f)
        for (float y = -1.5f; y <= 1.5001f; y += 0.1f)
        {
            var path = (Vector3[])k_Kite.Clone();
            path[point] = new Vector3(x, y, 0);
            light.SetShapePath(path);
            try
            {
                light.UpdateMesh(true);
            }
            catch (Exception e)
            {
                failures.Add($"point {point} at ({x:R}, {y:R}): {e.GetType().Name}");
            }
        }

        Assert.IsEmpty(failures, $"{failures.Count} shapes failed to build, first: {(failures.Count > 0 ? failures[0] : "")}");
    }

    // Many crossings add many points when the outline is split, which must still fit the mesh buffers.
    [Test]
    public void StarWithManyCrossings_BuildsMesh()
    {
        const int points = 24, step = 11;
        var path = new Vector3[points];
        for (int i = 0; i < points; ++i)
        {
            float angle = 2.0f * Mathf.PI * ((i * step) % points) / points;
            path[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        }

        var light = NewFreeformLight();
        light.SetShapePath(path);

        Assert.DoesNotThrow(() => light.UpdateMesh(true));
        Assert.Greater(light.lightMesh.GetIndexCount(0), 0, "the light has no triangles");
    }

    [Test]
    public void SimpleShape_BuildsMesh()
    {
        var light = NewFreeformLight();
        light.SetShapePath(k_Kite);

        Assert.DoesNotThrow(() => light.UpdateMesh(true));
        Assert.Greater(light.lightMesh.GetIndexCount(0), 0, "the light has no triangles");
    }
}
