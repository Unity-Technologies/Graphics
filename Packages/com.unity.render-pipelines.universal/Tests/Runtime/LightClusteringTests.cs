// Unit tests for light clustering code used in Forward+ and Deferred+ rendering modes. Each scene is checked against
// LightClusteringOracle: no light or probe may be missing from a tile or z-bin it reaches, and none may be in a tile or
// z-bin its volume stays clear of.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
    class LightClusteringTests
    {
        const float k_Diagonal = 0.57735026f;
        const float k_Half = 0.70710677f;
        static readonly int2 k_SmallScreen = new int2(64, 64);
        static readonly int2 k_Screen = new int2(480, 270);

        static ClusteringScene Perspective(float fovYDeg, float aspect, float near, float far, int2 screenSize, ClusteringItem itemVS) =>
            ClusteringScene.PerspectiveViewSpace(fovYDeg, aspect, near, far, screenSize, itemVS);

        static ClusteringScene Perspective(float p00, float p11, float2 lensShift, float near, float far, int2 screenSize, ClusteringItem itemVS) =>
            ClusteringScene.PerspectiveViewSpace(p00, p11, lensShift, near, far, screenSize, itemVS);

        static ClusteringScene Orthographic(float halfWidth, float halfHeight, float near, float far, int2 screenSize, ClusteringItem itemVS) =>
            ClusteringScene.OrthographicViewSpace(halfWidth, halfHeight, near, far, screenSize, itemVS);

        static TestCaseData Case(string name, string description, ClusteringScene scene) =>
            new TestCaseData(scene).SetName($"LightClustering_IsExact({name})").SetDescription(description);

        static IEnumerable<TestCaseData> ExactScenes()
        {
            {
                var scene = new ClusteringScene { near = 0.3f, far = 1000f, screenSize = new int2(128, 128) };
                scene.worldToViews[0] = scene.worldToViews[1] = new float4x4(
                    new float4(-0.1736481f, 0.8528686f, -0.4924039f, 0f),
                    new float4(0f, 0.5f, 0.8660254f, 0f),
                    new float4(-0.9848078f, -0.1503837f, 0.08682406f, 0f),
                    new float4(1.275023f, 3.212242f, -3.009289f, 1f));
                scene.viewToClips[0] = scene.viewToClips[1] = ClusteringScene.Perspective(0.7968904f, 1.732051f, float2.zero, scene.near, scene.far);
                scene.items.Add(ClusteringItem.Spot(new float3(3f, 3f, 0f), new float3(-0.670667171f, -0.683449626f, 0.288274288f), 10f, 100f));
                yield return Case("SpotRangeOutsideScreenInXZPlane", "A spot light whose intersection with an XZ plane falls outside the screen (UUM-84591).", scene);
            }

            yield return Case("PointLightCrossingNearPlane", "A point light crossing the near plane.",
                Perspective(1f, 1f, float2.zero, 0.5f, 100f, k_SmallScreen, ClusteringItem.Point(new float3(1.604f, -1.6087f, 0.8924f), 1.2312f)));
            yield return Case("PointLightAboveFrustum", "A point light above the frustum reaching the top tile row.",
                Perspective(1f, 1f, float2.zero, 0.5f, 100f, k_SmallScreen, ClusteringItem.Point(new float3(-4.4307f, 7.244f, 5.7363f), 1.9306f)));
            yield return Case("PointLightInFrontOfCamera", "A point light in front of the camera.",
                Perspective(1f, 1f, float2.zero, 0.5f, 100f, k_SmallScreen, ClusteringItem.Point(new float3(0.3f, 0.2f, 5f), 1.3f)));
            yield return Case("PointLightCrossingFarPlane", "A point light crossing the far plane.",
                Perspective(60f, 16f / 9f, 0.3f, 27f, k_Screen, ClusteringItem.Point(new float3(0.5f, 0.3f, 30f), 5f)));
            yield return Case("PointLightCrossingNearPlaneOffCenter", "A point light crossing the near plane whose vertical extent on screen comes from its section with the near plane.",
                Perspective(0.7515261f, 1.1239314f, new float2(-0.35771352f, 0.24809685f), 0.6553464f, 2306.9248f, new int2(250, 141), ClusteringItem.Point(new float3(-5.5001135f, -5.4904704f, 0.96593094f), 6.838029f)));
            yield return Case("PointLightFarOffScreenNearCamera", "A point light so far to the side of a camera with a close near plane that its tile coordinates exceed the 16-bit range of the tile ranges.",
                Perspective(60f, 16f / 9f, 0.01f, 1000f, k_Screen, ClusteringItem.Point(new float3(22f, 0f, 0.3f), 0.35f)));
            yield return Case("PointLightBehindCamera", "A point light entirely behind the camera is in no tile or z-bin.",
                Perspective(60f, 16f / 9f, 0.3f, 1000f, k_Screen, ClusteringItem.Point(new float3(0f, 0f, -5f), 2f)));

            yield return Case("SpotLightInFrontOfNearPlane", "A spot light in front of the near plane.",
                Perspective(72.032f, 1.3333f, 0.412f, 1000f, k_SmallScreen, ClusteringItem.Spot(new float3(1.5852f, -1.4977f, 6.9015f), new float3(-0.1672f, -0.2562f, 0.952f), 2.227f, 75.974f)));
            yield return Case("SpotLightCrossingNearPlane", "A spot light crossing the near plane.",
                Perspective(58.88f, 1.3333f, 0.461f, 1000f, k_SmallScreen, ClusteringItem.Spot(new float3(0.447f, 1.0636f, 1.4462f), new float3(0.011f, -0.121f, 0.9926f), 2.017f, 112.675f)));
            yield return Case("SpotSilhouetteOffScreenEdge", "A spot light whose silhouette leaves the screen.",
                Perspective(86.3651f, 0.9666f, 0.357f, 1000f, k_SmallScreen, ClusteringItem.Spot(new float3(-0.008f, -0.1112f, -2.0052f), new float3(-0.9652f, -0.1627f, 0.2046f), 4.0016f, 66.8309f)));
            yield return Case("SpotVertexInCameraPlane", "A spot light whose vertex lies in the camera plane (UUM-154016).",
                Perspective(60f, 16f / 9f, 0.3f, 1000f, k_Screen, ClusteringItem.Spot(new float3(-4.8e-7f, -0.2f, 0f), new float3(3.7e-9f, 0f, 1f), 50f, 55f)));
            yield return Case("SpotBehindCameraAlongViewAxis", "A spot light behind the camera whose axis is the view direction (UUM-159132).",
                Perspective(60f, 989f / 485f, 0.3f, 1000f, new int2(989, 485), ClusteringItem.Spot(new float3(0.25f, 0f, -2f), new float3(0f, 0f, 1f), 6f, 30f)));
            yield return Case("SpotAxisAt45DegreesInXZ", "A spot light whose axis is at exactly 45 degrees in the XZ plane.",
                Perspective(0.89631605f, 1.0262334f, float2.zero, 0.11539972f, 1000f, k_Screen, ClusteringItem.Spot(new float3(0f, 0f, 12.25f), new float3(k_Half, 0f, k_Half), 40f, 90f)));
            yield return Case("SpotAxisAlongViewDiagonal", "A spot light whose axis is the view-space diagonal (1, 1, 1).",
                Perspective(0.44447416f, 0.85071206f, float2.zero, 0.78974074f, 1000f, k_Screen, ClusteringItem.Spot(new float3(-4.25f, 0.25f, 0f), new float3(k_Diagonal, k_Diagonal, k_Diagonal), 5f, 45f)));
            yield return Case("SpotVertexOnNearPlane", "A wide spot light whose vertex lies on the near plane.",
                Perspective(0.45082957f, 1.0431756f, float2.zero, 0.8158224f, 1000f, k_Screen, ClusteringItem.Spot(new float3(3f, -0.5f, 0.8158224f), new float3(0f, k_Half, k_Half), 6f, 110f)));
            yield return Case("SpotVertexJustBehindNearPlane", "A nearly hemispherical spot light whose vertex lies just behind the near plane.",
                Perspective(0.5678351f, 1.0830803f, float2.zero, 0.3802336f, 1000f, k_Screen, ClusteringItem.Spot(new float3(24.75f, -35.25f, 0.37797466f), new float3(k_Half, k_Half, 0f), 55f, 179f)));
            yield return Case("SpotWithUnboundedNearPlaneSection", "A spot light whose section with the near plane has no finite extremes.",
                Perspective(0.8330596f, 1.219217f, new float2(-0.2787532f, 0.22271621f), 0.50425756f, 1000f, k_Screen, ClusteringItem.Spot(new float3(0f, 0f, 0.25f), new float3(k_Diagonal, k_Diagonal, k_Diagonal), 10f, 109.47122f)));
            yield return Case("SpotLightCrossingFarPlane", "A spot light crossing the far plane.",
                Perspective(60f, 16f / 9f, 0.3f, 25f, k_Screen, ClusteringItem.Spot(new float3(0.5f, 0.3f, 20f), new float3(0f, 0f, 1f), 15f, 60f)));
            {
                var scene = new ClusteringScene { near = 0.54576063f, far = 1000f, screenSize = new int2(160, 90) };
                scene.worldToViews[0] = scene.worldToViews[1] = new float4x4(
                    new float4(-0.1186f, -0.44585413f, 0.8872137f, 0f),
                    new float4(0.22589295f, 0.85797536f, 0.46135753f, 0f),
                    new float4(0.9669056f, -0.25513235f, 0.0010405704f, 0f),
                    new float4(19.057278f, -13.34542f, -38.808697f, 1f));
                scene.viewToClips[0] = scene.viewToClips[1] = ClusteringScene.Perspective(1.2988923f, 2.8259869f, new float2(0.19827574f, -0.04960227f), scene.near, scene.far);
                scene.items.Add(ClusteringItem.Spot(new float3(30.4875f, 25.492296f, -1.2410641f), new float3(-0.11860001f, 0.22589296f, 0.9669056f), 33f, 179f));
                yield return Case("SpotBaseCircleAroundCamera", "A wide spot light whose base circle surrounds the camera in a side view.", scene);
            }

            yield return Case("ReflectionProbeCrossingFarPlane", "A reflection probe crossing the far plane.",
                Perspective(60f, 16f / 9f, 0.3f, 28f, k_Screen, ClusteringItem.Probe(new float3(20f, 0f, 30f), new float3(5f, 5f, 5f), quaternion.identity)));
            yield return Case("ReflectionProbeBehindCamera", "A reflection probe entirely behind the camera is in no tile or z-bin.",
                Perspective(60f, 16f / 9f, 0.3f, 1000f, k_Screen, ClusteringItem.Probe(new float3(0f, 0f, -5f), new float3(1f, 1f, 1f), quaternion.identity)));

            yield return Case("OrthographicPointLightCentered", "An orthographic point light.",
                Orthographic(4f, 4f, 0.5f, 100f, k_SmallScreen, ClusteringItem.Point(new float3(0.3f, 0.2f, 5f), 1.3f)));
            yield return Case("OrthographicPointLightOffLeftEdge", "An orthographic point light partly off the left screen edge.",
                Orthographic(4f, 4f, 0.5f, 100f, k_SmallScreen, ClusteringItem.Point(new float3(-5f, -3.5f, 5f), 1.1f)));
            yield return Case("OrthographicPointLightAtNegativeDepth", "An orthographic point light at negative depth with a negative near plane.",
                Orthographic(6f, 6f, -5f, 10f, k_SmallScreen, ClusteringItem.Point(new float3(0f, 0f, -2f), 1f)));
            yield return Case("OrthographicPointLightCrossingNearPlane", "An orthographic point light crossing the near plane.",
                Orthographic(8f, 4.5f, 1f, 100f, k_Screen, ClusteringItem.Point(new float3(0.5f, 0.3f, 0f), 2f)));
            yield return Case("OrthographicSpotOffRightEdge", "An orthographic spot light off the right screen edge.",
                Orthographic(0.5184f * 11.7982f, 11.7982f, 0.4803f, 1000f, k_SmallScreen, ClusteringItem.Spot(new float3(6.6041f, 6.1272f, 2.8334f), new float3(-0.4904f, 0.868f, -0.0783f), 2.6565f, 56.2812f)));
            yield return Case("OrthographicSpotCrossingNearAndFarPlanes", "An orthographic spot light crossing the near and far planes.",
                Orthographic(8f, 4.5f, 1f, 4f, k_Screen, ClusteringItem.Spot(new float3(-3f, 0.3f, -1f), new float3(k_Half, 0f, k_Half), 9f, 60f)));
            yield return Case("OrthographicReflectionProbeOnTileBoundaries", "An orthographic axis-aligned reflection probe whose edges lie on tile boundaries.",
                Orthographic(4f, 4f, 0.5f, 100f, k_SmallScreen, ClusteringItem.Probe(new float3(0f, 0f, 5f), new float3(1f, 2f, 1f), quaternion.identity)));
        }

        [TestCaseSource(nameof(ExactScenes))]
        public void LightClustering_IsExact(ClusteringScene scene)
        {
            var result = LightClusteringOracle.Check(scene);
            if (result.failed)
                Assert.Fail($"{result}{scene}");
        }

        [Description("The horizon of a projected circle is missing, not a far-away point, when the camera is inside the ellipse.")]
        [Test]
        public void LightClustering_ProjectedCircleHorizonAroundCamera_IsMissing()
        {
            // A cone base circle facing the camera sideways, with the camera inside its projection onto the YZ plane.
            TilingJob.GetProjectedCircleHorizon(new float2(-4.750001f, 2.279692E-09f), 32.998745f, new float2(1f, 0f), new float2(0f, 1f), out var uv1, out var uv2);
            Assert.IsTrue(math.any(math.isnan(uv1)) && math.any(math.isnan(uv2)), $"Expected no horizon, got {uv1} and {uv2}.");
        }

        [Description("Cone section points are accepted on or inside the cone up to its base, and rejected outside its angle, which degenerate sections can produce.")]
        [Test]
        public void LightClustering_ConeSectionPointCheck_RejectsPointsOutsideTheCone()
        {
            var vertex = new float3(1f, 2f, 3f);
            var axis = math.normalize(new float3(0.3f, -0.2f, 1f));
            var halfAngle = math.radians(20f);
            var cosHalfAngle = math.cos(halfAngle);
            var sinHalfAngle = math.sin(halfAngle);
            const float range = 10f;
            var coneHeight = range * cosHalfAngle;
            var tolerance = 1e-5f * (math.length(vertex) + range);
            var side = math.normalize(math.cross(axis, new float3(1f, 0f, 0f)));
            bool IsOnCone(float angle, float distance) =>
                TilingJob.IsOnCone(vertex + (math.cos(angle) * axis + math.sin(angle) * side) * distance, vertex, axis, cosHalfAngle, sinHalfAngle, coneHeight, tolerance);

            Assert.IsTrue(IsOnCone(halfAngle, 5f), "A point on the cone was rejected.");
            Assert.IsTrue(IsOnCone(0f, 5f), "A point on the axis was rejected.");
            Assert.IsFalse(IsOnCone(halfAngle + math.radians(10f), 5f), "A point outside the cone angle was accepted.");
            Assert.IsFalse(IsOnCone(halfAngle, 0.5f * range / cosHalfAngle + range), "A point beyond the cone base was accepted.");
            Assert.IsFalse(IsOnCone(math.PI - halfAngle, 5f), "A point on the opposite nappe was accepted.");
        }

        [Description("Randomized check of light clustering on scenes biased towards degenerate configurations. The scenes depend on floating-point math that can differ between platforms, so this is not deterministic and is run manually. Configure it with URP_LIGHT_CLUSTERING_FUZZ_COUNT, URP_LIGHT_CLUSTERING_FUZZ_SEED and URP_LIGHT_CLUSTERING_FUZZ_MAX_ITEMS.")]
        [Explicit("Randomized and not deterministic across platforms; run manually.")]
        [Test]
        public void LightClustering_RandomScenes_AreExact()
        {
            static int Read(string name, int fallback) => int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
            var seed = (uint)Read("URP_LIGHT_CLUSTERING_FUZZ_SEED", 1);
            var count = Read("URP_LIGHT_CLUSTERING_FUZZ_COUNT", 1000);
            var maxItems = Read("URP_LIGHT_CLUSTERING_FUZZ_MAX_ITEMS", 40);
            var screenSizes = new[] { new int2(160, 90), new int2(250, 141), new int2(97, 203), new int2(64, 64) };
            var rng = new Unity.Mathematics.Random(seed);
            for (var i = 0; i < count; i++)
            {
                var scene = LightClusteringOracle.RandomScene(ref rng, screenSizes[i % screenSizes.Length], maxItems);
                var result = LightClusteringOracle.Check(scene);
                if (result.failed)
                    Assert.Fail($"Scene {i} (seed {seed}, max items {maxItems}):\n{result}{scene}");
            }
        }
    }
}
