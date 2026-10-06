// Unit tests for light clustering code used in Forward+ and Deferred+ rendering
// modes.

using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    class LightClusteringTests
    {
        [Description("The tiling job calculates an intersection between an XZ plane and the light volume to determine the range of tiles that the light affects. In the cases where this range falls completely outside of the screen, this test ensures that the ranges are clamped correctly to screen boundaries. If not tested, this bug affects the cone section of spot lights, the cap section of spot lights, and also point lights. This test was added due to UUM-84591.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_WhenLightVolumeIntersectionWithXZPlaneIsOutsideTheScreen_ZBinsAndTileMasksAreCorrect()
        {
            NativeArray<VisibleLight> lights = new(1, Allocator.TempJob);
            // Light world transform:
            // UnityEditor.TransformWorldPlacementJSON:{"position":{"x":3.0,"y":3.0,"z":0.0},"rotation":{"x":0.511572539806366,"y":0.3068491518497467,"z":-0.7767189145088196,"w":0.20210090279579163},"scale":{"x":1.0,"y":1.0,"z":1.0}}
            lights[0] = new VisibleLight()
            {
                lightType = LightType.Spot,
                finalColor = Color.white,
                screenRect = new Rect(0, 0, 1, 1),
                localToWorldMatrix = new Matrix4x4(
                    new Vector4(-0.394897342f, 0, -0.918725133f, 0),
                    new Vector4(0.627902389f, -0.729997516f, -0.269892514f, 0),
                    new Vector4(-0.670667171f, -0.683449626f, 0.288274288f, 0),
                    new Vector4(3, 3, 0, 1)
                ),
                range = 10,
                spotAngle = 100,
                intersectsNearPlane = true,
                intersectsFarPlane = false,
            };
            NativeArray<VisibleReflectionProbe> probes = new(0, Allocator.TempJob);
            NativeArray<uint> zBins = new(UniversalRenderPipeline.maxZBinWords, Allocator.TempJob);
            NativeArray<uint> tileMasks = new (UniversalRenderPipeline.maxTileWords, Allocator.TempJob);

            // Camera world transform:
            // UnityEditor.TransformWorldPlacementJSON:{"position":{"x":-4.0,"y":1.0,"z":2.0},"rotation":{"x":0.32139381766319277,"y":0.663413941860199,"z":-0.3830222189426422,"w":0.5566704273223877},"scale":{"x":1.0,"y":1.0,"z":1.0}}
            float4x4 worldToView = new float4x4(
                new float4(-0.1736481f, 0.8528686f, -0.4924039f, 0f),
                new float4(0f, 0.5f, 0.8660254f, 0f),
                new float4(-0.9848078f, -0.1503837f, 0.08682406f, 0f),
                new float4(1.275023f, 3.212242f, -3.009289f, 1f)
            );
            var worldToViews = new Fixed2<float4x4>(worldToView, worldToView);
            float4x4 viewToClip = new float4x4(
                new float4(0.7968904f, 0f, 0f, 0f),
                new float4(0f, 1.732051f, 0f, 0f),
                new float4(0f, 0f, -1.0006f, -1f),
                new float4(0f, 0f, -0.60018f, 0f)
            );
            var viewToClips = new Fixed2<float4x4>(viewToClip, viewToClip);
            int2 screenResolution = new(128, 128);
            float nearPlane = 0.3f;
            float farPlane = 1000f;

            JobHandle handle = ForwardLights.ScheduleClusteringJobs(
                false,
                true,
                lights,
                probes,
                zBins,
                tileMasks,
                worldToViews,
                viewToClips,
                1,
                screenResolution,
                nearPlane,
                farPlane,
                false,
                out int localLightCount,
                out int directionalLightCount,
                out int binCount,
                out float zBinScale,
                out float zBinOffset,
                out int2 tileResolution,
                out int actualTileWidth,
                out int wordsPerTile
            );
            JobHandle.ScheduleBatchedJobs();
            handle.Complete();

            // Uncomment temporarily if the reference arrays need to be updated.
            // Debug.Log(string.Join(",", zBins));
            // Debug.Log(string.Join(",", tileMasks));

            // If the following assertions fail, there isn't currently good
            // tooling to visualize the difference. A 3D texture slice viewer
            // can be built that would fix this issue. But the cost-benefit is
            // not worth it at the moment, since the diff of a PR that fails
            // this test can be enough to go on.
            //
            // Also, these assertions can fail if a change is made includes more
            // false positives, i.e. lights contributing to _more_ clusters than
            // expected due to increased conservatism, or lights contributing to
            // _less_ clusters due increased accuracy. In either of these cases,
            // it is up to the author of these code changes to update the
            // expected values.
            try
            {
                CollectionAssert.AreEqual(expectedZBins1, zBins);
                CollectionAssert.AreEqual(expectedTileMasks1, tileMasks);
            }
            finally
            {
                lights.Dispose();
                probes.Dispose();
                zBins.Dispose();
                tileMasks.Dispose();
            }
        }

        // Identity world-to-view, 90-degree FOV; tiling negates view Z, so view (x, y, z) is world (x, y, -z).
        static bool PointLightCoversTile(float3 lightPositionVS, float range, int2 tile)
        {
            var lights = new NativeArray<VisibleLight>(1, Allocator.TempJob);
            lights[0] = new VisibleLight()
            {
                lightType = LightType.Point,
                finalColor = Color.white,
                screenRect = new Rect(0, 0, 1, 1),
                localToWorldMatrix = new Matrix4x4(
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 1, 0, 0),
                    new Vector4(0, 0, 1, 0),
                    new Vector4(lightPositionVS.x, lightPositionVS.y, -lightPositionVS.z, 1)
                ),
                range = range,
                spotAngle = 0,
                intersectsNearPlane = true,
                intersectsFarPlane = false,
            };
            var probes = new NativeArray<VisibleReflectionProbe>(0, Allocator.TempJob);
            var zBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.TempJob);
            var tileMasks = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.TempJob);

            var worldToView = float4x4.identity;
            var worldToViews = new Fixed2<float4x4>(worldToView, worldToView);

            const float near = 0.5f;
            const float far = 100f;
            float p22 = -(far + near) / (far - near);
            float p23 = -(2f * far * near) / (far - near);
            var viewToClip = new float4x4(
                new float4(1, 0, 0, 0),
                new float4(0, 1, 0, 0),
                new float4(0, 0, p22, -1),
                new float4(0, 0, p23, 0)
            );
            var viewToClips = new Fixed2<float4x4>(viewToClip, viewToClip);
            int2 screenResolution = new(64, 64);

            JobHandle handle = ForwardLights.ScheduleClusteringJobs(
                false, true, lights, probes, zBins, tileMasks,
                worldToViews, viewToClips, 1, screenResolution, near, far, false,
                out _, out _, out _, out _, out _, out int2 tileResolution, out _, out int wordsPerTile);
            JobHandle.ScheduleBatchedJobs();
            handle.Complete();

            int maskIndex = (tile.y * tileResolution.x + tile.x) * wordsPerTile;
            bool covered = (tileMasks[maskIndex] & 1u) != 0u;

            lights.Dispose();
            probes.Dispose();
            zBins.Dispose();
            tileMasks.Dispose();
            return covered;
        }

        [Description("A point light crossing the near plane covers the tile at its horizon extreme.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_PointLightCrossingNearPlane_CoversHorizonTileInFrontOfNear()
        {
            Assert.IsTrue(
                PointLightCoversTile(new float3(1.6040f, -1.6087f, 0.8924f), 1.2312f, new int2(7, 2)),
                "Tile (7,2) is covered by the light volume but is missing from the tile mask.");
        }

        [Description("A point light above the frustum covers the edge tile row.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_PointLightOutsideFrustumVertically_CoversEdgeRowTile()
        {
            Assert.IsTrue(
                PointLightCoversTile(new float3(-4.4307f, 7.2440f, 5.7363f), 1.9306f, new int2(0, 7)),
                "Edge-row tile (0,7) is covered by the light volume but is missing from the tile mask.");
        }

        // Identity world-to-view; tiling negates view Z, so view (x, y, z) is world (x, y, -z).
        static bool SpotLightCoversTile(float fovYDeg, float aspect, float near, float3 posVS, float3 dirVS, float range, float spotAngle, int2 tile)
        {
            var lights = new NativeArray<VisibleLight>(1, Allocator.TempJob);
            lights[0] = new VisibleLight()
            {
                lightType = LightType.Spot,
                finalColor = Color.white,
                screenRect = new Rect(0, 0, 1, 1),
                localToWorldMatrix = new Matrix4x4(
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 1, 0, 0),
                    new Vector4(dirVS.x, dirVS.y, -dirVS.z, 0),
                    new Vector4(posVS.x, posVS.y, -posVS.z, 1)
                ),
                range = range,
                spotAngle = spotAngle,
                intersectsNearPlane = true,
                intersectsFarPlane = false,
            };
            var probes = new NativeArray<VisibleReflectionProbe>(0, Allocator.TempJob);
            var zBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.TempJob);
            var tileMasks = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.TempJob);

            var worldToView = float4x4.identity;
            var worldToViews = new Fixed2<float4x4>(worldToView, worldToView);

            const float far = 1000f;
            float p11 = 1f / math.tan(math.radians(fovYDeg) * 0.5f);
            float p00 = p11 / aspect;
            var viewToClip = new float4x4(
                new float4(p00, 0, 0, 0),
                new float4(0, p11, 0, 0),
                new float4(0, 0, -(far + near) / (far - near), -1),
                new float4(0, 0, -(2f * far * near) / (far - near), 0)
            );
            var viewToClips = new Fixed2<float4x4>(viewToClip, viewToClip);
            int2 screenResolution = new(64, 64);

            JobHandle handle = ForwardLights.ScheduleClusteringJobs(
                false, true, lights, probes, zBins, tileMasks,
                worldToViews, viewToClips, 1, screenResolution, near, far, false,
                out _, out _, out _, out _, out _, out int2 tileResolution, out _, out int wordsPerTile);
            JobHandle.ScheduleBatchedJobs();
            handle.Complete();

            int maskIndex = (tile.y * tileResolution.x + tile.x) * wordsPerTile;
            bool covered = (tileMasks[maskIndex] & 1u) != 0u;

            lights.Dispose();
            probes.Dispose();
            zBins.Dispose();
            tileMasks.Dispose();
            return covered;
        }

        [Description("A spot light in front of the near plane covers a tile inside its cone.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_SpotLightInFrontOfNear_CoversInteriorTile()
        {
            Assert.IsTrue(
                SpotLightCoversTile(72.032f, 1.3333f, 0.412f, new float3(1.5852f, -1.4977f, 6.9015f), new float3(-0.1672f, -0.2562f, 0.9520f), 2.227f, 75.974f, new int2(4, 2)),
                "Spot cone covers tile (4,2) but it is missing from the tile mask.");
        }

        [Description("A spot light crossing the near plane covers a tile inside its cone.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_SpotLightCrossingNear_CoversInteriorTile()
        {
            Assert.IsTrue(
                SpotLightCoversTile(58.880f, 1.3333f, 0.461f, new float3(0.4470f, 1.0636f, 1.4462f), new float3(0.0110f, -0.1210f, 0.9926f), 2.017f, 112.675f, new int2(4, 6)),
                "Spot cone covers tile (4,6) but it is missing from the tile mask.");
        }

        [Description("A spot light whose silhouette leaves the screen does not cover the edge column.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_SpotSilhouetteOffScreenEdge_DoesNotOverCoverEdgeColumn()
        {
            Assert.IsFalse(
                SpotLightCoversTile(86.3651f, 0.9666f, 0.3570f, new float3(-0.0080f, -0.1112f, -2.0052f), new float3(-0.9652f, -0.1627f, 0.2046f), 4.0016f, 66.8309f, new int2(0, 1)),
                "Edge-column tile (0,1) is outside the spot cone but was marked (edge over-coverage).");
        }

        // Orthographic camera; half-width = aspect * orthoHalfHeight.
        static bool OrthoSpotLightCoversTile(float orthoHalfHeight, float aspect, float near, float3 posVS, float3 dirVS, float range, float spotAngle, int2 tile)
        {
            var lights = new NativeArray<VisibleLight>(1, Allocator.TempJob);
            lights[0] = new VisibleLight()
            {
                lightType = LightType.Spot,
                finalColor = Color.white,
                screenRect = new Rect(0, 0, 1, 1),
                localToWorldMatrix = new Matrix4x4(
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 1, 0, 0),
                    new Vector4(dirVS.x, dirVS.y, -dirVS.z, 0),
                    new Vector4(posVS.x, posVS.y, -posVS.z, 1)
                ),
                range = range,
                spotAngle = spotAngle,
                intersectsNearPlane = true,
                intersectsFarPlane = false,
            };
            var probes = new NativeArray<VisibleReflectionProbe>(0, Allocator.TempJob);
            var zBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.TempJob);
            var tileMasks = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.TempJob);

            var worldToView = float4x4.identity;
            var worldToViews = new Fixed2<float4x4>(worldToView, worldToView);

            const float far = 1000f;
            float p00 = 1f / (aspect * orthoHalfHeight);
            float p11 = 1f / orthoHalfHeight;
            var viewToClip = new float4x4(
                new float4(p00, 0, 0, 0),
                new float4(0, p11, 0, 0),
                new float4(0, 0, -2f / (far - near), 0),
                new float4(0, 0, -(far + near) / (far - near), 1)
            );
            var viewToClips = new Fixed2<float4x4>(viewToClip, viewToClip);
            int2 screenResolution = new(64, 64);

            JobHandle handle = ForwardLights.ScheduleClusteringJobs(
                false, true, lights, probes, zBins, tileMasks,
                worldToViews, viewToClips, 1, screenResolution, near, far, true,
                out _, out _, out _, out _, out _, out int2 tileResolution, out _, out int wordsPerTile);
            JobHandle.ScheduleBatchedJobs();
            handle.Complete();

            int maskIndex = (tile.y * tileResolution.x + tile.x) * wordsPerTile;
            bool covered = (tileMasks[maskIndex] & 1u) != 0u;

            lights.Dispose();
            probes.Dispose();
            zBins.Dispose();
            tileMasks.Dispose();
            return covered;
        }

        [Description("An orthographic spot light off the right screen edge covers the edge tile.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_OrthographicSpotOffRightEdge_CoversEdgeTile()
        {
            Assert.IsTrue(
                OrthoSpotLightCoversTile(11.7982f, 0.5184f, 0.4803f, new float3(6.6041f, 6.1272f, 2.8334f), new float3(-0.4904f, 0.868f, -0.0783f), 2.6565f, 56.2812f, new int2(7, 6)),
                "Orthographic spot covers tile (7,6) but it is missing from the tile mask.");
        }

        // Orthographic camera; returns whether the z-bin at view depth viewZ contains the light.
        static bool OrthoPointLightCoversZBinAtDepth(float3 lightPositionVS, float range, float near, float far, float viewZ)
        {
            var lights = new NativeArray<VisibleLight>(1, Allocator.TempJob);
            lights[0] = new VisibleLight()
            {
                lightType = LightType.Point,
                finalColor = Color.white,
                screenRect = new Rect(0, 0, 1, 1),
                localToWorldMatrix = new Matrix4x4(
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 1, 0, 0),
                    new Vector4(0, 0, 1, 0),
                    new Vector4(lightPositionVS.x, lightPositionVS.y, -lightPositionVS.z, 1)
                ),
                range = range,
                spotAngle = 0,
                intersectsNearPlane = true,
                intersectsFarPlane = false,
            };
            var probes = new NativeArray<VisibleReflectionProbe>(0, Allocator.TempJob);
            var zBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.TempJob);
            var tileMasks = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.TempJob);

            var worldToView = float4x4.identity;
            var worldToViews = new Fixed2<float4x4>(worldToView, worldToView);

            // Only the half-size and center terms are read by the tiling setup.
            const float halfSize = 6f;
            var viewToClip = new float4x4(
                new float4(1f / halfSize, 0, 0, 0),
                new float4(0, 1f / halfSize, 0, 0),
                new float4(0, 0, -2f / (far - near), 0),
                new float4(0, 0, -(far + near) / (far - near), 1)
            );
            var viewToClips = new Fixed2<float4x4>(viewToClip, viewToClip);
            int2 screenResolution = new(64, 64);

            JobHandle handle = ForwardLights.ScheduleClusteringJobs(
                false, true, lights, probes, zBins, tileMasks,
                worldToViews, viewToClips, 1, screenResolution, near, far, true,
                out _, out _, out _, out float zBinScale, out float zBinOffset, out _, out _, out int wordsPerTile);
            JobHandle.ScheduleBatchedJobs();
            handle.Complete();

            // Orthographic z-bin index is linear in view-space Z (matches the shader).
            int binIndex = (int)(viewZ * zBinScale + zBinOffset);
            int stride = 2 + wordsPerTile; // 2 header words + bitmask words
            bool covered = (zBins[binIndex * stride + 2] & 1u) != 0u;

            lights.Dispose();
            probes.Dispose();
            zBins.Dispose();
            tileMasks.Dispose();
            return covered;
        }

        [Description("With a negative orthographic near plane, a light at negative depth is in its z-bin.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_OrthographicNegativeNearPlane_PointLightCoversZBinAtItsDepth()
        {
            Assert.IsTrue(
                OrthoPointLightCoversZBinAtDepth(new float3(0f, 0f, -2f), 1f, -5f, 10f, -2f),
                "Light at view-space Z -2 is missing from the z-bin at depth -2 (negative near plane).");
        }

        // Identity world-to-view, 64x64 screen (8x8 tiles), square view plane of half-size halfSize (at Z=1 for
        // perspective); tiling negates view Z, so view (x, y, z) is world (x, y, -z). Returns item 0's tile coverage.
        static bool[,] ComputeTileCoverage(NativeArray<VisibleLight> lights, NativeArray<VisibleReflectionProbe> probes, bool orthographic, float halfSize)
        {
            var zBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.TempJob);
            var tileMasks = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.TempJob);
            var worldToViews = new Fixed2<float4x4>(float4x4.identity, float4x4.identity);

            const float near = 0.5f;
            const float far = 100f;
            var viewToClip = orthographic
                ? new float4x4(
                    new float4(1f / halfSize, 0, 0, 0),
                    new float4(0, 1f / halfSize, 0, 0),
                    new float4(0, 0, -2f / (far - near), 0),
                    new float4(0, 0, -(far + near) / (far - near), 1))
                : new float4x4(
                    new float4(1f / halfSize, 0, 0, 0),
                    new float4(0, 1f / halfSize, 0, 0),
                    new float4(0, 0, -(far + near) / (far - near), -1),
                    new float4(0, 0, -(2f * far * near) / (far - near), 0));
            var viewToClips = new Fixed2<float4x4>(viewToClip, viewToClip);

            JobHandle handle = ForwardLights.ScheduleClusteringJobs(
                false, true, lights, probes, zBins, tileMasks,
                worldToViews, viewToClips, 1, new int2(64, 64), near, far, orthographic,
                out _, out _, out _, out _, out _, out int2 tileResolution, out _, out int wordsPerTile);
            JobHandle.ScheduleBatchedJobs();
            handle.Complete();

            var coverage = new bool[tileResolution.x, tileResolution.y];
            for (var y = 0; y < tileResolution.y; y++)
                for (var x = 0; x < tileResolution.x; x++)
                    coverage[x, y] = (tileMasks[(y * tileResolution.x + x) * wordsPerTile] & 1u) != 0u;

            zBins.Dispose();
            tileMasks.Dispose();
            return coverage;
        }

        static bool[,] ComputePointLightTileCoverage(float3 lightPositionVS, float range, bool orthographic, float halfSize)
        {
            var lights = new NativeArray<VisibleLight>(1, Allocator.TempJob);
            lights[0] = new VisibleLight()
            {
                lightType = LightType.Point,
                finalColor = Color.white,
                screenRect = new Rect(0, 0, 1, 1),
                localToWorldMatrix = Matrix4x4.Translate(new Vector3(lightPositionVS.x, lightPositionVS.y, -lightPositionVS.z)),
                range = range,
                spotAngle = 0,
                intersectsNearPlane = false,
                intersectsFarPlane = false,
            };
            var probes = new NativeArray<VisibleReflectionProbe>(0, Allocator.TempJob);
            try
            {
                return ComputeTileCoverage(lights, probes, orthographic, halfSize);
            }
            finally
            {
                lights.Dispose();
                probes.Dispose();
            }
        }

        static string FormatTiles(List<int2> tiles) => string.Join(", ", tiles.ConvertAll(t => $"({t.x},{t.y})"));

        // The orthographic footprint of a point light is a disk, so the exact set of tiles it overlaps is known.
        [Description("An orthographic point light covers exactly the tiles its disk overlaps.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [TestCase(0.3f, 0.2f, 1.3f, TestName = "LightClustering_OrthographicPointLight_CoversExactlyItsDiskTiles(Centered)")]
        [TestCase(-5f, -3.5f, 1.1f, TestName = "LightClustering_OrthographicPointLight_CoversExactlyItsDiskTiles(OffLeftEdge)")]
        public void LightClustering_OrthographicPointLight_CoversExactlyItsDiskTiles(float x, float y, float range)
        {
            const float halfSize = 4f;
            var coverage = ComputePointLightTileCoverage(new float3(x, y, 5f), range, true, halfSize);

            var tileSize = 2f * halfSize / coverage.GetLength(0);
            var missing = new List<int2>();
            var extra = new List<int2>();
            for (var ty = 0; ty < coverage.GetLength(1); ty++)
            {
                for (var tx = 0; tx < coverage.GetLength(0); tx++)
                {
                    var tileMin = -halfSize + tileSize * new float2(tx, ty);
                    var closest = math.clamp(new float2(x, y), tileMin, tileMin + tileSize);
                    var overlaps = math.distancesq(closest, new float2(x, y)) < range * range;
                    if (overlaps && !coverage[tx, ty]) missing.Add(new int2(tx, ty));
                    if (!overlaps && coverage[tx, ty]) extra.Add(new int2(tx, ty));
                }
            }

            Assert.IsEmpty(missing, $"Tiles overlapped by the light are not covered: {FormatTiles(missing)}");
            Assert.IsEmpty(extra, $"Tiles outside the light are covered: {FormatTiles(extra)}");
        }

        [Description("A perspective point light covers no tile outside its projected screen-space bounds.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_PerspectivePointLight_CoversNoTileOutsideItsProjectedBounds()
        {
            const float halfSize = 1f;
            var position = new float3(0.3f, 0.2f, 5f);
            const float range = 1.3f;
            var coverage = ComputePointLightTileCoverage(position, range, false, halfSize);

            // Tangent lines from the eye bound the sphere's projection on each axis.
            float2 ProjectedTileBounds(float lateral, float tileCount)
            {
                var angle = math.atan2(lateral, position.z);
                var halfAngle = math.asin(range / math.length(new float2(lateral, position.z)));
                var ndc = math.tan(new float2(angle - halfAngle, angle + halfAngle)) / halfSize;
                return math.floor((ndc * 0.5f + 0.5f) * tileCount);
            }
            var boundsX = ProjectedTileBounds(position.x, coverage.GetLength(0));
            var boundsY = ProjectedTileBounds(position.y, coverage.GetLength(1));

            var extra = new List<int2>();
            for (var ty = 0; ty < coverage.GetLength(1); ty++)
                for (var tx = 0; tx < coverage.GetLength(0); tx++)
                    if (coverage[tx, ty] && (tx < boundsX.x || tx > boundsX.y || ty < boundsY.x || ty > boundsY.y))
                        extra.Add(new int2(tx, ty));

            Assert.IsTrue(coverage[4, 4], "The tile containing the light center is not covered.");
            Assert.IsEmpty(extra, $"Tiles outside the light's projected bounds are covered: {FormatTiles(extra)}");
        }

        // Horizontal hull edges lying exactly on tile row boundaries hit the 0/0 case of the edge-plane intersection.
        [Description("An orthographic axis-aligned reflection probe with edges on tile boundaries covers no tile outside its bounds.")]
        [UnityPlatform(include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.OSXPlayer })]
        [Test]
        public void LightClustering_OrthographicReflectionProbeOnTileBoundaries_CoversNoTileOutsideItsBounds()
        {
            const float halfSize = 4f;
            var center = new float3(0f, 0f, 5f);
            var extents = new float3(1f, 2f, 1f);

            var lights = new NativeArray<VisibleLight>(0, Allocator.TempJob);
            var probes = new NativeArray<VisibleReflectionProbe>(1, Allocator.TempJob);
            probes[0] = new VisibleReflectionProbe()
            {
                bounds = new Bounds(new Vector3(center.x, center.y, -center.z), 2f * (Vector3)extents),
                localToWorldMatrix = Matrix4x4.Translate(new Vector3(center.x, center.y, -center.z)),
            };
            bool[,] coverage;
            try
            {
                coverage = ComputeTileCoverage(lights, probes, true, halfSize);
            }
            finally
            {
                lights.Dispose();
                probes.Dispose();
            }

            // Closed bounds: a tile that only touches the probe along a tile boundary may be covered.
            var tileCount = new float2(coverage.GetLength(0), coverage.GetLength(1));
            var boundsMin = math.ceil(((center.xy - extents.xy) / halfSize * 0.5f + 0.5f) * tileCount) - 1f;
            var boundsMax = math.floor(((center.xy + extents.xy) / halfSize * 0.5f + 0.5f) * tileCount);

            var extra = new List<int2>();
            for (var ty = 0; ty < coverage.GetLength(1); ty++)
                for (var tx = 0; tx < coverage.GetLength(0); tx++)
                    if (coverage[tx, ty] && (tx < boundsMin.x || tx > boundsMax.x || ty < boundsMin.y || ty > boundsMax.y))
                        extra.Add(new int2(tx, ty));

            Assert.IsTrue(coverage[3, 3], "A tile inside the probe is not covered.");
            Assert.IsEmpty(extra, $"Tiles outside the probe bounds are covered: {FormatTiles(extra)}");
        }

        static readonly uint[] expectedZBins1 =
        {
            0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,0,65535,1,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,65535,65535,0,0
        };

        static readonly uint[] expectedTileMasks1 =
        {
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
        };
        
    }
}
