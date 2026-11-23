using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal.Tests
{
    enum ClusteringItemKind
    {
        Point,
        Spot,
        Probe,
    }

    /// <summary>
    /// A light or reflection probe in world space, as the clustering jobs receive it.
    /// </summary>
    struct ClusteringItem
    {
        public ClusteringItemKind kind;
        public float3 position;
        public float3 direction;
        public float range;
        public float spotAngle;
        public float3 extents;
        public quaternion rotation;

        public static ClusteringItem Point(float3 position, float range) =>
            new ClusteringItem { kind = ClusteringItemKind.Point, position = position, direction = new float3(0, 0, 1), range = range, spotAngle = 30f, rotation = quaternion.identity };

        public static ClusteringItem Spot(float3 position, float3 direction, float range, float spotAngle) =>
            new ClusteringItem { kind = ClusteringItemKind.Spot, position = position, direction = direction, range = range, spotAngle = spotAngle, rotation = quaternion.identity };

        public static ClusteringItem Probe(float3 center, float3 extents, quaternion rotation) =>
            new ClusteringItem { kind = ClusteringItemKind.Probe, position = center, direction = new float3(0, 0, 1), extents = extents, rotation = rotation };

        public override string ToString() => kind == ClusteringItemKind.Probe
            ? $"Probe(center=({position.x:R},{position.y:R},{position.z:R}) extents=({extents.x:R},{extents.y:R},{extents.z:R}) rotation=({rotation.value.x:R},{rotation.value.y:R},{rotation.value.z:R},{rotation.value.w:R}))"
            : $"{kind}(position=({position.x:R},{position.y:R},{position.z:R}) direction=({direction.x:R},{direction.y:R},{direction.z:R}) range={range:R} spotAngle={spotAngle:R})";
    }

    /// <summary>
    /// One view, or two for XR, and the items to cluster.
    /// </summary>
    class ClusteringScene
    {
        public bool orthographic;
        public int viewCount = 1;
        public float4x4[] worldToViews = { float4x4.identity, float4x4.identity };
        public float4x4[] viewToClips = new float4x4[2];
        public float near;
        public float far;
        public int2 screenSize;
        public List<ClusteringItem> items = new List<ClusteringItem>();

        public static float4x4 Perspective(float p00, float p11, float2 lensShift, float near, float far) => new float4x4(
            new float4(p00, 0, 0, 0),
            new float4(0, p11, 0, 0),
            new float4(lensShift.x, lensShift.y, -(far + near) / (far - near), -1),
            new float4(0, 0, -(2f * far * near) / (far - near), 0));

        public static float4x4 Orthographic(float halfWidth, float halfHeight, float2 lensShift, float near, float far)
        {
            var m = float4x4.identity;
            m.c0.x = 1f / halfWidth;
            m.c1.y = 1f / halfHeight;
            m.c2.z = -2f / (far - near);
            m.c3 = new float4(lensShift.x, lensShift.y, -(far + near) / (far - near), 1);
            return m;
        }

        /// <summary>
        /// A perspective scene with an identity world-to-view matrix and one item given in view space with +z forward.
        /// </summary>
        public static ClusteringScene PerspectiveViewSpace(float p00, float p11, float2 lensShift, float near, float far, int2 screenSize, ClusteringItem itemVS)
        {
            var scene = new ClusteringScene { near = near, far = far, screenSize = screenSize };
            scene.viewToClips[0] = scene.viewToClips[1] = Perspective(p00, p11, lensShift, near, far);
            scene.items.Add(FlipZ(itemVS));
            return scene;
        }

        public static ClusteringScene PerspectiveViewSpace(float fovYDeg, float aspect, float near, float far, int2 screenSize, ClusteringItem itemVS)
        {
            var p11 = 1f / math.tan(math.radians(fovYDeg) * 0.5f);
            return PerspectiveViewSpace(p11 / aspect, p11, float2.zero, near, far, screenSize, itemVS);
        }

        /// <summary>
        /// An orthographic scene with an identity world-to-view matrix and one item given in view space with +z forward.
        /// </summary>
        public static ClusteringScene OrthographicViewSpace(float halfWidth, float halfHeight, float near, float far, int2 screenSize, ClusteringItem itemVS)
        {
            var scene = new ClusteringScene { orthographic = true, near = near, far = far, screenSize = screenSize };
            scene.viewToClips[0] = scene.viewToClips[1] = Orthographic(halfWidth, halfHeight, float2.zero, near, far);
            scene.items.Add(FlipZ(itemVS));
            return scene;
        }

        static ClusteringItem FlipZ(ClusteringItem item)
        {
            item.position.z = -item.position.z;
            item.direction.z = -item.direction.z;
            item.rotation = new quaternion(-item.rotation.value.x, -item.rotation.value.y, item.rotation.value.z, item.rotation.value.w);
            return item;
        }

        public override string ToString()
        {
            var s = $"orthographic={orthographic} views={viewCount} near={near:R} far={far:R} screen={screenSize}\n";
            for (var v = 0; v < viewCount; v++)
                s += $"  view {v}: worldToView={worldToViews[v]} viewToClip={viewToClips[v]}\n";
            foreach (var item in items)
                s += $"  {item}\n";
            return s;
        }
    }

    /// <summary>
    /// Reference for the light clustering jobs. Runs ForwardLights.ScheduleClusteringJobs on a scene and compares its tile
    /// masks and z-bins with ray casts against the exact light and probe volumes.
    ///
    /// Each item is converted to view space in double precision from the same float inputs the jobs receive, so the
    /// reference is the exact geometry of those inputs and any float error in the jobs counts against the jobs. The
    /// volumes are the ones the jobs cover: a sphere for a point light, a cone of half the spot angle intersected with the
    /// range sphere for a spot light, and a box for a reflection probe, all limited to depths between the near and far
    /// planes.
    ///
    /// Two kinds of errors are reported:
    /// - Missing: the GPU looks up lights per pixel center, so an item must be in every tile that contains a pixel center
    ///   whose view ray hits its volume, and in every z-bin between the depths where that ray enters and leaves it.
    ///   This check is exact up to double precision.
    /// - Outside: over-coverage is allowed but costs performance, so this check only reports an item in a tile or z-bin
    ///   it clearly does not reach. It errs towards passing: a tile counts as reached when the volume comes within
    ///   k_TouchMargin pixels of the tile rectangle, which accepts slivers and contact along a tile edge, and a z-bin
    ///   counts as reached when it is at most one bin away from the depth extent of the volume.
    ///
    /// The layout of the outputs is checked as well. Each z-bin has a header with the range of light indices and the
    /// range of probe indices the shader iterates; a range must cover the items of its kind in the bin, stay within the
    /// indices of its kind, and be empty when the bin has none. No bit may be set for an item index that does not exist,
    /// and nothing may be written past the tiles and z-bins in use.
    /// </summary>
    static class LightClusteringOracle
    {
        // Distance in pixels within which a volume counts as touching a tile rectangle. Absorbs float error in the jobs
        // and volumes that touch a tile along an edge or in a sliver too thin to sample.
        const double k_TouchMargin = 0.125;

        public class Result
        {
            public List<string> missing = new List<string>();
            public List<string> outside = new List<string>();
            public bool failed => missing.Count > 0 || outside.Count > 0;

            public override string ToString() =>
                (missing.Count > 0 ? $"Missing ({missing.Count}): {string.Join("; ", missing.GetRange(0, math.min(10, missing.Count)))}\n" : "") +
                (outside.Count > 0 ? $"Outside ({outside.Count}): {string.Join("; ", outside.GetRange(0, math.min(10, outside.Count)))}\n" : "");
        }

        class Output
        {
            public int2 tileResolution;
            public int tileWidth;
            public int wordsPerTile;
            public int binCount;
            public float zBinScale;
            public float zBinOffset;
            public uint[] tileMasks;
            public uint[] zBins;
        }

        static bool reflectionProbeRotation =>
            !GraphicsSettings.TryGetRenderPipelineSettings<URPReflectionProbeSettings>(out var settings) || settings.UseReflectionProbeRotation;

        // The jobs index lights before probes; the reference uses the same order to find each item's bit.
        static List<ClusteringItem> InJobOrder(ClusteringScene scene)
        {
            var ordered = new List<ClusteringItem>();
            ordered.AddRange(scene.items.FindAll(i => i.kind != ClusteringItemKind.Probe));
            ordered.AddRange(scene.items.FindAll(i => i.kind == ClusteringItemKind.Probe));
            return ordered;
        }

        static Output Run(ClusteringScene scene, List<ClusteringItem> ordered)
        {
            var lightCount = ordered.FindAll(i => i.kind != ClusteringItemKind.Probe).Count;
            var lights = new NativeArray<VisibleLight>(lightCount, Allocator.TempJob);
            var probes = new NativeArray<VisibleReflectionProbe>(ordered.Count - lightCount, Allocator.TempJob);
            var zBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.TempJob);
            var tileMasks = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.TempJob);
            try
            {
                for (var i = 0; i < ordered.Count; i++)
                {
                    var item = ordered[i];
                    if (item.kind == ClusteringItemKind.Probe)
                    {
                        probes[i - lightCount] = new VisibleReflectionProbe
                        {
                            bounds = new Bounds(item.position, 2f * (Vector3)item.extents),
                            localToWorldMatrix = Matrix4x4.TRS(item.position, item.rotation, Vector3.one),
                        };
                    }
                    else
                    {
                        lights[i] = new VisibleLight
                        {
                            lightType = item.kind == ClusteringItemKind.Spot ? LightType.Spot : LightType.Point,
                            finalColor = Color.white,
                            screenRect = new Rect(0, 0, 1, 1),
                            localToWorldMatrix = new Matrix4x4(
                                new Vector4(1, 0, 0, 0),
                                new Vector4(0, 1, 0, 0),
                                new Vector4(item.direction.x, item.direction.y, item.direction.z, 0),
                                new Vector4(item.position.x, item.position.y, item.position.z, 1)),
                            range = item.range,
                            spotAngle = item.spotAngle,
                            intersectsNearPlane = true,
                            intersectsFarPlane = true,
                        };
                    }
                }

                var output = new Output();
                var handle = ForwardLights.ScheduleClusteringJobs(
                    false, true, lights, probes, zBins, tileMasks,
                    new Fixed2<float4x4>(scene.worldToViews[0], scene.worldToViews[1]),
                    new Fixed2<float4x4>(scene.viewToClips[0], scene.viewToClips[1]),
                    scene.viewCount, scene.screenSize, scene.near, scene.far, scene.orthographic,
                    out _, out _, out output.binCount, out output.zBinScale, out output.zBinOffset,
                    out output.tileResolution, out output.tileWidth, out output.wordsPerTile);
                JobHandle.ScheduleBatchedJobs();
                handle.Complete();
                output.tileMasks = tileMasks.ToArray();
                output.zBins = zBins.ToArray();
                return output;
            }
            finally
            {
                lights.Dispose();
                probes.Dispose();
                zBins.Dispose();
                tileMasks.Dispose();
            }
        }

        /// <summary>
        /// Runs the clustering jobs on the scene and returns every missing or outside tile and z-bin, per item and view.
        /// </summary>
        public static Result Check(ClusteringScene scene)
        {
            var ordered = InJobOrder(scene);
            var output = Run(scene, ordered);
            var result = new Result();
            var tileResolution = output.tileResolution;
            var tileWidth = output.tileWidth;
            for (var view = 0; view < scene.viewCount; view++)
            {
                var camera = new Camera(scene, view);
                for (var index = 0; index < ordered.Count; index++)
                {
                    var volume = new Volume(scene, view, ordered[index]);
                    var (zMin, zMax) = volume.DepthExtent();

                    // Relative tolerance for depth comparisons, a few hundred times the float precision of the depths.
                    var zTolerance = 1e-5 * (math.abs(zMin) + math.abs(zMax) + math.abs(scene.near) + 1);

                    // A volume that only touches the near or far plane has a visible part of zero area, a point or a line,
                    // which no ray sampling can find. Its tiles are not checked for over-coverage.
                    var degenerate = zMax < scene.near + zTolerance || zMin > scene.far - zTolerance;
                    var word = index / 32;
                    var bit = 1u << (index % 32);
                    var label = $"item {index} {ordered[index].kind} view {view}";
                    var maskBase = view * output.wordsPerTile * tileResolution.x * tileResolution.y;
                    bool InTile(int tx, int ty) => (output.tileMasks[maskBase + (ty * tileResolution.x + tx) * output.wordsPerTile + word] & bit) != 0;
                    bool InBin(int b) => b >= 0 && b < output.binCount &&
                        (output.zBins[(view * output.binCount + b) * (ZBinningJob.headerLength + output.wordsPerTile) + ZBinningJob.headerLength + word] & bit) != 0;
                    // Same z-bin mapping as the shader: linear in depth for orthographic cameras, logarithmic otherwise.
                    int BinOf(double z) => (int)math.floor((scene.orthographic ? z : math.log2(z)) * output.zBinScale + output.zBinOffset);

                    // Cast a ray through each pixel center on screen. A tile is reached when one of its rays hits the volume,
                    // and every z-bin between the depths where that ray enters and leaves the volume must hold the item.
                    // Partial tiles at the screen edges only have rays for their on-screen pixels.
                    for (var ty = 0; ty < tileResolution.y; ty++)
                    {
                        for (var tx = 0; tx < tileResolution.x; tx++)
                        {
                            var hit = false;
                            var binMissing = false;
                            for (var py = ty * tileWidth; py < math.min(scene.screenSize.y, (ty + 1) * tileWidth); py++)
                            {
                                for (var px = tx * tileWidth; px < math.min(scene.screenSize.x, (tx + 1) * tileWidth); px++)
                                {
                                    camera.Ray(px + 0.5, py + 0.5, out var origin, out var direction);
                                    var (lo, hi) = volume.Hit(origin, direction, scene.near, scene.far);
                                    if (lo > hi)
                                        continue;
                                    hit = true;
                                    var lastBin = math.min(BinOf(hi), output.binCount - 1);
                                    for (var b = math.max(BinOf(lo), 0); b <= lastBin && !binMissing; b++)
                                    {
                                        if (!InBin(b))
                                        {
                                            binMissing = true;
                                            result.missing.Add($"{label} z-bin {b} (depths {lo:G6}..{hi:G6} at pixel {px},{py})");
                                        }
                                    }
                                }
                            }

                            if (hit && !InTile(tx, ty))
                                result.missing.Add($"{label} tile ({tx},{ty})");
                            if (!hit && !degenerate && InTile(tx, ty) && !volume.TouchesTile(camera, tx, ty, tileWidth, scene.near, scene.far))
                                result.outside.Add($"{label} tile ({tx},{ty})");
                        }
                    }

                    // A z-bin is over-covered when it lies more than one bin beyond the depth extent of the volume, clipped to
                    // the near and far planes. A volume entirely outside them allows no bin.
                    zMin = math.max(zMin - zTolerance, scene.near);
                    zMax = math.min(zMax + zTolerance, scene.far);
                    var allowedMin = zMin <= zMax ? BinOf(zMin) - 1 : int.MaxValue;
                    var allowedMax = zMin <= zMax ? BinOf(zMax) + 1 : int.MinValue;
                    for (var b = 0; b < output.binCount; b++)
                    {
                        if (InBin(b) && (b < allowedMin || b > allowedMax))
                            result.outside.Add($"{label} z-bin {b} (volume depths {zMin:G6}..{zMax:G6})");
                    }
                }
            }
            CheckLayout(scene, ordered, output, result);
            return result;
        }

        static void CheckLayout(ClusteringScene scene, List<ClusteringItem> ordered, Output output, Result result)
        {
            var lightCount = ordered.FindAll(i => i.kind != ClusteringItemKind.Probe).Count;
            var stride = ZBinningJob.headerLength + output.wordsPerTile;
            // The first header word of a z-bin is the range of light indices the shader iterates in that bin, the second
            // the range of probe indices. A range must cover every item of its kind in the bin, stay within the indices of
            // its kind, and be empty when the bin has no item of its kind; otherwise the shader would visit an item of the
            // other kind.
            var kindBegin = new int2(0, lightCount);
            var kindEnd = new int2(lightCount, ordered.Count);
            for (var bin = 0; bin < scene.viewCount * output.binCount; bin++)
            {
                var present = new bool2(false, false);
                for (var word = 0; word < output.wordsPerTile; word++)
                {
                    var bits = output.zBins[bin * stride + ZBinningJob.headerLength + word];
                    for (var bit = 0; bit < 32; bit++)
                    {
                        if ((bits & (1u << bit)) == 0)
                            continue;
                        var index = word * 32 + bit;
                        if (index >= ordered.Count)
                        {
                            result.outside.Add($"z-bin {bin} has a bit for item {index}, which does not exist");
                            continue;
                        }

                        var kind = index < lightCount ? 0 : 1;
                        present[kind] = true;
                        var header = output.zBins[bin * stride + kind];
                        if (index < (header & 0xFFFF) || index > header >> 16)
                            result.missing.Add($"z-bin {bin} header {kind} is {header & 0xFFFF}..{header >> 16} and leaves out item {index}");
                    }
                }

                for (var kind = 0; kind < 2; kind++)
                {
                    var header = output.zBins[bin * stride + kind];
                    int min = (int)(header & 0xFFFF), max = (int)(header >> 16);
                    if (min > max)
                        continue;
                    if (!present[kind])
                        result.outside.Add($"z-bin {bin} header {kind} is {min}..{max} but the bin has no item of its kind");
                    else if (min < kindBegin[kind] || max >= kindEnd[kind])
                        result.outside.Add($"z-bin {bin} header {kind} is {min}..{max}, outside the item indices {kindBegin[kind]}..{kindEnd[kind] - 1} of its kind");
                }
            }
            for (var i = scene.viewCount * output.binCount * stride; i < output.zBins.Length; i++)
            {
                if (output.zBins[i] != 0)
                    result.outside.Add($"z-bin word {i} past the bins in use is {output.zBins[i]}");
            }

            var tileWords = output.tileResolution.x * output.tileResolution.y * output.wordsPerTile;
            for (var tile = 0; tile < scene.viewCount * output.tileResolution.x * output.tileResolution.y; tile++)
            {
                for (var word = 0; word < output.wordsPerTile; word++)
                {
                    var bits = output.tileMasks[tile * output.wordsPerTile + word];
                    var validBits = math.clamp(ordered.Count - word * 32, 0, 32);
                    if (validBits < 32 && bits >> validBits != 0)
                        result.outside.Add($"tile {tile} word {word} has bits for items that do not exist: {bits:X8}");
                }
            }
            for (var i = scene.viewCount * tileWords; i < output.tileMasks.Length; i++)
            {
                if (output.tileMasks[i] != 0)
                    result.outside.Add($"tile mask word {i} past the tiles in use is {output.tileMasks[i]}");
            }
        }

        // Rays and projections derived from the view-to-clip matrix itself, independently of how the jobs derive their
        // view planes from it. View space has +z forward and the depth along a ray is its parameter.
        readonly struct Camera
        {
            readonly bool m_Orthographic;
            readonly double4x4 m_ViewToClip;

            public bool orthographic => m_Orthographic;
            readonly double2 m_Screen;

            public Camera(ClusteringScene scene, int view)
            {
                m_Orthographic = scene.orthographic;
                m_ViewToClip = scene.viewToClips[view];
                m_Screen = new double2(scene.screenSize.x, scene.screenSize.y);
            }

            // Pixel position of a view-space point.
            public double2 Project(double3 p)
            {
                var ndc = m_Orthographic
                    ? new double2(p.x * m_ViewToClip[0][0] + m_ViewToClip[3][0], p.y * m_ViewToClip[1][1] + m_ViewToClip[3][1])
                    : new double2(p.x / p.z * m_ViewToClip[0][0] - m_ViewToClip[2][0], p.y / p.z * m_ViewToClip[1][1] - m_ViewToClip[2][1]);
                return (ndc * 0.5 + 0.5) * m_Screen;
            }

            // View-space ray through a pixel position, as origin + depth * direction. Perspective rays start at the camera,
            // orthographic rays start in the camera plane and point along +z.
            public void Ray(double px, double py, out double3 origin, out double3 direction)
            {
                var ndc = new double2(px, py) / m_Screen * 2 - 1;
                if (m_Orthographic)
                {
                    origin = new double3((ndc.x - m_ViewToClip[3][0]) / m_ViewToClip[0][0], (ndc.y - m_ViewToClip[3][1]) / m_ViewToClip[1][1], 0);
                    direction = new double3(0, 0, 1);
                }
                else
                {
                    origin = 0;
                    direction = new double3((ndc.x + m_ViewToClip[2][0]) / m_ViewToClip[0][0], (ndc.y + m_ViewToClip[2][1]) / m_ViewToClip[1][1], 1);
                }
            }
        }

        // The volume of one item in the view space of one view, in double precision.
        readonly struct Volume
        {
            readonly ClusteringItemKind m_Kind;
            readonly double3 m_Position;
            readonly double3 m_Axis;
            readonly double m_CosSq;
            readonly double m_Range;
            readonly double3x3 m_Basis;
            readonly double3 m_Extents;

            public Volume(ClusteringScene scene, int view, ClusteringItem item)
            {
                var worldToView = (double4x4)scene.worldToViews[view];
                var flip = new double3(1, 1, -1);
                m_Kind = item.kind;
                m_Position = math.mul(worldToView, new double4((double3)item.position, 1)).xyz * flip;
                m_Axis = math.normalize(math.mul(worldToView, new double4((double3)item.direction, 0)).xyz * flip);
                var cos = math.cos(math.radians((double)item.spotAngle) * 0.5);
                m_CosSq = cos * cos;
                m_Range = item.range;
                var rotation = reflectionProbeRotation ? new double3x3(new float3x3(item.rotation)) : double3x3.identity;
                m_Basis = new double3x3(
                    math.mul(worldToView, new double4(rotation.c0, 0)).xyz * flip,
                    math.mul(worldToView, new double4(rotation.c1, 0)).xyz * flip,
                    math.mul(worldToView, new double4(rotation.c2, 0)).xyz * flip);
                m_Extents = (double3)item.extents;
            }

            // Depth interval [lo, hi] of origin + s * direction inside the volume for s in [s0, s1]; lo > hi if none.
            // Boxes use the slab method. Spheres solve a quadratic. Cones solve the cone quadric and intersect the result
            // with the sphere interval; the volume is convex, so its inside along a ray is one interval.
            public (double, double) Hit(double3 origin, double3 direction, double s0, double s1)
            {
                if (m_Kind == ClusteringItemKind.Probe)
                {
                    double lo = s0, hi = s1;
                    for (var k = 0; k < 3; k++)
                    {
                        var length = math.length(m_Basis[k]);
                        var axis = m_Basis[k] / length;
                        var extent = m_Extents[k] * length;
                        var offset = math.dot(origin - m_Position, axis);
                        var speed = math.dot(direction, axis);
                        if (speed == 0)
                        {
                            if (math.abs(offset) > extent)
                                return (1, 0);
                            continue;
                        }
                        var ta = (-extent - offset) / speed;
                        var tb = (extent - offset) / speed;
                        lo = math.max(lo, math.min(ta, tb));
                        hi = math.min(hi, math.max(ta, tb));
                        if (lo > hi)
                            return (1, 0);
                    }
                    return (lo, hi);
                }

                var toCenter = m_Position - origin;
                double dd = math.dot(direction, direction), da = math.dot(direction, toCenter), aa = math.dot(toCenter, toCenter);
                var disc = da * da - dd * (aa - m_Range * m_Range);
                if (disc < 0)
                    return (1, 0);
                var sq = math.sqrt(disc);
                var a0 = math.max((da - sq) / dd, s0);
                var a1 = math.min((da + sq) / dd, s1);
                if (a0 > a1 || m_Kind == ClusteringItemKind.Point)
                    return (a0, a1);

                // Along the ray, the inside of the cone can only begin or end at a root of the cone quadric
                // ga s^2 + 2 gb s + gc = 0, where the ray meets the double cone, or where it crosses the plane through the
                // vertex, which separates the two nappes. Sorting these candidates with the sphere interval bounds and
                // testing each candidate and each midpoint between neighbors finds the inside interval exactly. The roots are
                // computed as q / ga and gc / q, which keeps the single root when the ray is parallel to a cone line, where
                // ga is 0.
                double du = math.dot(direction, m_Axis), au = math.dot(toCenter, m_Axis);
                double ga = du * du - m_CosSq * dd, gb = -du * au + m_CosSq * da, gc = au * au - m_CosSq * aa;
                Span<double> sorted = stackalloc double[6];
                var n = 0;
                Insert(sorted, ref n, a0, a0, a1);
                Insert(sorted, ref n, a1, a0, a1);
                // A grazing ray can round the discriminant below 0; the extra candidates are harmless, InsideCone decides.
                var root = math.sqrt(math.max(gb * gb - ga * gc, 0));
                var q = -(gb + (gb < 0 ? -root : root));
                if (q != 0)
                {
                    Insert(sorted, ref n, q / ga, a0, a1);
                    Insert(sorted, ref n, gc / q, a0, a1);
                }
                if (du != 0)
                    Insert(sorted, ref n, au / du, a0, a1);

                double inLo = double.MaxValue, inHi = double.MinValue;
                for (var i = 0; i < n; i++)
                {
                    if (InsideCone(origin, direction, sorted[i]))
                    {
                        inLo = math.min(inLo, sorted[i]);
                        inHi = math.max(inHi, sorted[i]);
                    }
                    if (i + 1 < n && InsideCone(origin, direction, 0.5 * (sorted[i] + sorted[i + 1])))
                    {
                        inLo = math.min(inLo, sorted[i]);
                        inHi = math.max(inHi, sorted[i + 1]);
                    }
                }
                return (inLo, inHi);
            }

            static void Insert(Span<double> sorted, ref int count, double value, double min, double max)
            {
                if (double.IsNaN(value) || value < min || value > max)
                    return;
                var k = count++;
                while (k > 0 && sorted[k - 1] > value)
                {
                    sorted[k] = sorted[k - 1];
                    k--;
                }
                sorted[k] = value;
            }

            // Whether the point at depth s lies in the forward nappe of the cone. The small tolerance counts points on the
            // surface as inside, which can only make the missing check stricter.
            bool InsideCone(double3 origin, double3 direction, double s)
            {
                var q = origin + s * direction - m_Position;
                var axial = math.dot(q, m_Axis);
                return axial >= 0 && axial * axial - m_CosSq * math.dot(q, q) >= -1e-12;
            }

            // Exact depth range of the whole volume, ignoring the near and far planes.
            public (double, double) DepthExtent()
            {
                if (m_Kind == ClusteringItemKind.Probe)
                {
                    var e = math.abs(m_Basis.c0.z) * m_Extents.x + math.abs(m_Basis.c1.z) * m_Extents.y + math.abs(m_Basis.c2.z) * m_Extents.z;
                    return (m_Position.z - e, m_Position.z + e);
                }
                if (m_Kind == ClusteringItemKind.Point)
                    return (m_Position.z - m_Range, m_Position.z + m_Range);

                // A spot volume reaches its depth extremes at the vertex or on the base circle, where the cone meets the
                // sphere. The sphere cap goes further only when the view axis lies inside the cone, in which case the extreme
                // is a full range away from the vertex.
                var cos = math.sqrt(m_CosSq);
                var baseCenter = m_Position + m_Axis * m_Range * cos;
                var baseExtent = m_Range * math.sqrt(1 - m_CosSq) * math.sqrt(math.max(0, 1 - m_Axis.z * m_Axis.z));
                var lo = -m_Axis.z >= cos ? m_Position.z - m_Range : math.min(m_Position.z, baseCenter.z - baseExtent);
                var hi = m_Axis.z >= cos ? m_Position.z + m_Range : math.max(m_Position.z, baseCenter.z + baseExtent);
                return (lo, hi);
            }

            // Whether the volume comes within k_TouchMargin pixels of the tile rectangle. Points known to be in the volume
            // are tested first: the center of a point light or probe, or the vertex and two axis points of a spot light.
            // This covers volumes smaller than a sample spacing and a spot light whose vertex touches a tile at a single
            // point. Then the grown rectangle is sampled with rays on grids of 1/16 and 1/64 pixel.
            public bool TouchesTile(Camera camera, int tx, int ty, int tileWidth, double near, double far)
            {
                var depthTolerance = 1e-5 * (math.length(m_Position) + m_Range + math.length(m_Extents) + math.abs(near));
                for (var i = 0; i < 3; i++)
                {
                    if (m_Kind != ClusteringItemKind.Spot && i > 0)
                        break;
                    var p = m_Position + m_Axis * (m_Range * math.sqrt(m_CosSq) * i * 0.5);
                    if (p.z < near - depthTolerance || p.z > far + depthTolerance || (!camera.orthographic && p.z <= 0))
                        continue;
                    var pixel = camera.Project(p);
                    if (math.all(pixel >= new double2(tx, ty) * tileWidth - k_TouchMargin) && math.all(pixel <= new double2(tx + 1, ty + 1) * tileWidth + k_TouchMargin))
                        return true;
                }

                foreach (var samplesPerPixel in new[] { 16, 64 })
                {
                    var size = tileWidth + 2 * k_TouchMargin;
                    var n = (int)math.ceil(size * samplesPerPixel);
                    for (var j = 0; j <= n; j++)
                    {
                        for (var i = 0; i <= n; i++)
                        {
                            camera.Ray(tx * tileWidth - k_TouchMargin + size * i / n, ty * tileWidth - k_TouchMargin + size * j / n, out var origin, out var direction);
                            var (lo, hi) = Hit(origin, direction, near, far);
                            if (lo <= hi)
                                return true;
                        }
                    }
                }
                return false;
            }
        }

        static float3 RandomDirection(ref Unity.Mathematics.Random rng)
        {
            const float a = 0.70710677f;
            var special = new[]
            {
                new float3(0, 0, 1), new float3(0, 0, -1), new float3(1, 0, 0), new float3(-1, 0, 0), new float3(0, 1, 0), new float3(0, -1, 0),
                new float3(a, 0, a), new float3(-a, 0, a), new float3(0, a, a), new float3(0, -a, a), new float3(a, 0, -a), new float3(0, a, -a),
                new float3(a, a, 0), math.normalize(new float3(1, 1, 1)), math.normalize(new float3(-1, 1, 1)),
            };
            return rng.NextInt(0, 2) == 0 ? special[rng.NextInt(0, special.Length)] : math.normalize(rng.NextFloat3(-1f, 1f) + new float3(0, 0, 1e-3f));
        }

        /// <summary>
        /// Seeded random scenes: perspective or orthographic, one or two views, lens shift, and items biased towards the
        /// degenerate configurations that broke the clustering jobs before: axis-aligned and 45 degree spot axes, vertices
        /// on the view axis or near the near plane, cameras on the cone axis, cone edges along the view direction, and items
        /// crossing the near and far planes.
        /// </summary>
        public static ClusteringScene RandomScene(ref Unity.Mathematics.Random rng, int2 screenSize, int maxItems)
        {
            var scene = new ClusteringScene
            {
                orthographic = rng.NextInt(0, 3) == 0,
                viewCount = rng.NextInt(0, 4) == 0 ? 2 : 1,
                screenSize = screenSize,
            };
            var scale = rng.NextFloat(2f, 40f);
            if (scene.orthographic)
            {
                scene.near = rng.NextInt(0, 3) == 0 ? -rng.NextFloat(0.1f, 20f) : rng.NextFloat(0.01f, 2f);
                scene.far = scene.near + rng.NextFloat(5f, 200f);
            }
            else
            {
                scene.near = rng.NextFloat(0.01f, 2f);
                scene.far = rng.NextInt(0, 4) == 0 ? rng.NextFloat(scene.near + 0.5f, 20f) : rng.NextFloat(50f, 5000f);
            }

            var flip = float4x4.Scale(1, 1, -1);
            var viewToWorld = rng.NextBool()
                ? float4x4.identity
                : float4x4.TRS(rng.NextFloat3(-50f, 50f), quaternion.EulerXYZ(rng.NextFloat3(-math.PI, math.PI)), 1f);
            var aspect = (float)screenSize.x / screenSize.y * rng.NextFloat(0.8f, 1.25f);
            var halfHeight = rng.NextFloat(1f, 30f);
            var p11 = 1f / math.tan(math.radians(rng.NextFloat(20f, 120f)) * 0.5f);
            for (var v = 0; v < scene.viewCount; v++)
            {
                var eye = v == 0 ? float3.zero : new float3(rng.NextFloat(0.02f, 0.08f), 0, 0);
                scene.worldToViews[v] = math.mul(flip, math.inverse(math.mul(viewToWorld, float4x4.Translate(eye))));
                var shift = rng.NextInt(0, 3) == 0 || v == 1 ? rng.NextFloat2(-0.4f, 0.4f) : float2.zero;
                scene.viewToClips[v] = scene.orthographic
                    ? ClusteringScene.Orthographic(halfHeight * aspect, halfHeight, shift, scene.near, scene.far)
                    : ClusteringScene.Perspective(p11 / aspect, p11, shift, scene.near, scene.far);
            }
            if (scene.viewCount == 1)
            {
                scene.worldToViews[1] = scene.worldToViews[0];
                scene.viewToClips[1] = scene.viewToClips[0];
            }

            var viewToWorldFlipped = math.mul(viewToWorld, flip);
            var count = rng.NextInt(1, maxItems + 1);
            for (var i = 0; i < count; i++)
            {
                var positionVS = new float3(rng.NextFloat(-1f, 1f) * scale, rng.NextFloat(-1f, 1f) * scale, rng.NextFloat(-0.3f, 1.2f) * scale);
                switch (rng.NextInt(0, 8))
                {
                    case 0: positionVS.z = scene.near; break;
                    case 1: positionVS.z = scene.near * (1f + (rng.NextBool() ? 1f : -1f) * math.pow(10f, rng.NextFloat(-6f, -1f))); break;
                    case 2: positionVS.xy = 0; break;
                    case 3: positionVS = math.round(positionVS); break;
                    case 4: positionVS.z = scene.far * rng.NextFloat(0.9f, 1.1f); break;
                }

                var kind = rng.NextInt(0, 10);
                if (kind < 2)
                {
                    var extents = rng.NextFloat3(0.05f, 1f) * scale * rng.NextFloat(0.1f, 1f);
                    if (rng.NextInt(0, 4) == 0)
                        extents = math.round(extents) + 0.5f;
                    var rotation = rng.NextInt(0, 3) == 0 ? quaternion.identity : quaternion.EulerXYZ(rng.NextFloat3(-math.PI, math.PI));
                    scene.items.Add(ClusteringItem.Probe(math.transform(viewToWorldFlipped, new float3(positionVS.xy, -positionVS.z)), extents, rotation));
                    continue;
                }

                var range = rng.NextFloat(0.2f, 1.5f) * scale;
                if (kind < 4)
                {
                    scene.items.Add(ClusteringItem.Point(math.transform(viewToWorldFlipped, new float3(positionVS.xy, -positionVS.z)), range));
                    continue;
                }

                var directionVS = RandomDirection(ref rng);
                if (rng.NextInt(0, 6) == 0)
                    positionVS = -directionVS * math.round(rng.NextFloat(0.5f, 0.9f) * range);
                float spotAngle;
                switch (rng.NextInt(0, 3))
                {
                    case 0: spotAngle = math.round(rng.NextFloat(1f, 179f) / 5f) * 5f; break;
                    case 1: spotAngle = 2f * math.degrees(math.acos(math.saturate(math.abs(directionVS.z)))); break;
                    default: spotAngle = rng.NextFloat(1f, 179f); break;
                }
                scene.items.Add(ClusteringItem.Spot(
                    math.transform(viewToWorldFlipped, new float3(positionVS.xy, -positionVS.z)),
                    math.rotate(viewToWorldFlipped, new float3(directionVS.xy, -directionVS.z)),
                    range,
                    math.clamp(spotAngle, 1f, 179f)));
            }
            return scene;
        }
    }
}
