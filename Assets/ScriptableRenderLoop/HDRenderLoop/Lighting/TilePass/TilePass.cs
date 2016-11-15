using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    namespace TilePass
    {
        //-----------------------------------------------------------------------------
        // structure definition
        //-----------------------------------------------------------------------------

        [GenerateHLSL]
        public enum ShadowType
        {
            Spot,
            Directional,
            Point
        };

        // TODO: we may have to add various parameters here for shadow
        // A point light is 6x PunctualShadowData
        [GenerateHLSL]
        public struct PunctualShadowData
        {
            // World to ShadowMap matrix
            // Include scale and bias for shadow atlas if any
            public Matrix4x4 worldToShadow;

            public ShadowType shadowType;
            public float bias;
            public float quality;
            public Vector2 unused;
        };


        [GenerateHLSL]
        public class LightDefinitions
        {
            public static int MAX_NR_LIGHTS_PER_CAMERA = 1024;
            public static int MAX_NR_BIGTILE_LIGHTS_PLUSONE = 512;      // may be overkill but the footprint is 2 bits per pixel using uint16.
            public static float VIEWPORT_SCALE_Z = 1.0f;

            // enable unity's original left-hand shader camera space (right-hand internally in unity).
            public static int USE_LEFTHAND_CAMERASPACE = 0;

            // types
            public static int MAX_TYPES = 3;

            public static int SPOT_LIGHT = 0;
            public static int SPHERE_LIGHT = 1;
            public static int BOX_LIGHT = 2;
            public static int DIRECTIONAL_LIGHT = 3;

            // direct lights and reflection probes for now
            public static int NR_LIGHT_MODELS = 2;
            public static int DIRECT_LIGHT = 0;
            public static int REFLECTION_LIGHT = 1;
        }

        public class LightLoop
        {
            string GetKeyword()
            {
                return "LIGHTLOOP_SINGLE_PASS";
            }

            private static int s_GenAABBKernel;
            private static int s_GenListPerTileKernel;
            private static int s_GenListPerVoxelKernel;
            private static int s_ClearVoxelAtomicKernel;
            private static ComputeBuffer s_LightDataBuffer;
            private static ComputeBuffer s_ConvexBoundsBuffer;
            private static ComputeBuffer s_AABBBoundsBuffer;
            private static ComputeBuffer s_LightList;
            private static ComputeBuffer s_DirLightList;

            private static ComputeBuffer s_BigTileLightList;        // used for pre-pass coarse culling on 64x64 tiles
            private static int s_GenListPerBigTileKernel;

            // clustered light list specific buffers and data begin
            public bool enableClustered = false;
            public bool disableFptlWhenClustered = false;    // still useful on opaques
            public bool enableBigTilePrepass = true;
            public bool enableDrawLightBoundsDebug = false;
            public bool enableDrawTileDebug = false;
            public bool enableComputeLightEvaluation = false;
            const bool k_UseDepthBuffer = true;//      // only has an impact when EnableClustered is true (requires a depth-prepass)
            const bool k_UseAsyncCompute = true;        // should not use on mobile

            const int k_Log2NumClusters = 6;     // accepted range is from 0 to 6. NumClusters is 1<<g_iLog2NumClusters
            const float k_ClustLogBase = 1.02f;     // each slice 2% bigger than the previous
            float m_ClustScale;
            private static ComputeBuffer s_PerVoxelLightLists;
            private static ComputeBuffer s_PerVoxelOffset;
            private static ComputeBuffer s_PerTileLogBaseTweak;
            private static ComputeBuffer s_GlobalLightListAtomic;
            // clustered light list specific buffers and data end

            const int k_TileSize = 16;

            public bool NeedResize()
            {
                return s_LightList == null || (s_BigTileLightList == null && enableBigTilePrepass) || (s_PerVoxelLightLists == null && enableClustered);
            }

            public void ReleaseResolutionDependentBuffers()
            {
                if (s_LightList != null)
                    s_LightList.Release();

                if (enableClustered)
                {
                    if (s_PerVoxelLightLists != null)
                        s_PerVoxelLightLists.Release();

                    if (s_PerVoxelOffset != null)
                        s_PerVoxelOffset.Release();

                    if (k_UseDepthBuffer && s_PerTileLogBaseTweak != null)
                        s_PerTileLogBaseTweak.Release();
                }

                if (enableBigTilePrepass)
                {
                    if (s_BigTileLightList != null)
                        s_BigTileLightList.Release();
                }
            }

            int NumLightIndicesPerClusteredTile()
            {
                return 8 * (1 << k_Log2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
            }

            public void AllocResolutionDependentBuffers(int width, int height)
            {
                var nrTilesX = (width + k_TileSize - 1) / k_TileSize;
                var nrTilesY = (height + k_TileSize - 1) / k_TileSize;
                var nrTiles = nrTilesX * nrTilesY;
                const int capacityUShortsPerTile = 32;
                const int dwordsPerTile = (capacityUShortsPerTile + 1) >> 1;        // room for 31 lights and a nrLights value.

                s_LightList = new ComputeBuffer(LightDefinitions.NR_LIGHT_MODELS * dwordsPerTile * nrTiles, sizeof(uint));       // enough list memory for a 4k x 4k display

                if (enableClustered)
                {
                    s_PerVoxelOffset = new ComputeBuffer(LightDefinitions.NR_LIGHT_MODELS * (1 << k_Log2NumClusters) * nrTiles, sizeof(uint));
                    s_PerVoxelLightLists = new ComputeBuffer(NumLightIndicesPerClusteredTile() * nrTiles, sizeof(uint));

                    if (k_UseDepthBuffer)
                    {
                        s_PerTileLogBaseTweak = new ComputeBuffer(nrTiles, sizeof(float));
                    }
                }

                if (enableBigTilePrepass)
                {
                    var nrBigTilesX = (width + 63) / 64;
                    var nrBigTilesY = (height + 63) / 64;
                    var nrBigTiles = nrBigTilesX * nrBigTilesY;
                    s_BigTileLightList = new ComputeBuffer(LightDefinitions.MAX_NR_BIGTILE_LIGHTS_PLUSONE * nrBigTiles, sizeof(uint));
                }
            }

            int GenerateSourceLightBuffers(Camera camera, CullResults inputs)
            {
                var probes = inputs.visibleReflectionProbes;
                //ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>();

                var numModels = (int)LightDefinitions.NR_LIGHT_MODELS;
                var numVolTypes = (int)LightDefinitions.MAX_TYPES;
                var numEntries = new int[numModels, numVolTypes];
                var offsets = new int[numModels, numVolTypes];
                var numEntries2nd = new int[numModels, numVolTypes];

                // first pass. Figure out how much we have of each and establish offsets
                foreach (var cl in inputs.visibleLights)
                {
                    var volType = cl.lightType == LightType.Spot ? LightDefinitions.SPOT_LIGHT : (cl.lightType == LightType.Point ? LightDefinitions.SPHERE_LIGHT : -1);
                    if (volType >= 0) ++numEntries[LightDefinitions.DIRECT_LIGHT, volType];
                }

                foreach (var rl in probes)
                {
                    var volType = LightDefinitions.BOX_LIGHT;       // always a box for now
                    if (rl.texture != null) ++numEntries[LightDefinitions.REFLECTION_LIGHT, volType];
                }

                // add decals here too similar to the above

                // establish offsets
                for (var m = 0; m < numModels; m++)
                {
                    offsets[m, 0] = m == 0 ? 0 : (numEntries[m - 1, numVolTypes - 1] + offsets[m - 1, numVolTypes - 1]);
                    for (var v = 1; v < numVolTypes; v++) offsets[m, v] = numEntries[m, v - 1] + offsets[m, v - 1];
                }


                var numLights = inputs.visibleLights.Length;
                var numProbes = probes.Length;
                var numVolumes = numLights + numProbes;


                var lightData = new SFiniteLightData[numVolumes];
                var boundData = new SFiniteLightBound[numVolumes];
                var worldToView = WorldToCamera(camera);
                bool isNegDeterminant = Vector3.Dot(worldToView.GetColumn(0), Vector3.Cross(worldToView.GetColumn(1), worldToView.GetColumn(2))) < 0.0f;      // 3x3 Determinant.

                uint shadowLightIndex = 0;
                foreach (var cl in inputs.visibleLights)
                {
                    var range = cl.range;

                    var lightToWorld = cl.localToWorld;

                    Vector3 lightPos = lightToWorld.GetColumn(3);

                    var bound = new SFiniteLightBound();
                    var light = new SFiniteLightData();

                    bound.boxAxisX.Set(1, 0, 0);
                    bound.boxAxisY.Set(0, 1, 0);
                    bound.boxAxisZ.Set(0, 0, 1);
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = range;

                    light.flags = 0;
                    light.recipRange = 1.0f / range;
                    light.color.Set(cl.finalColor.r, cl.finalColor.g, cl.finalColor.b);
                    light.sliceIndex = 0;
                    light.lightModel = (uint)LightDefinitions.DIRECT_LIGHT;
                    light.shadowLightIndex = shadowLightIndex;
                    shadowLightIndex++;

                    var bHasCookie = cl.light.cookie != null;
                    var bHasShadow = cl.light.shadows != LightShadows.None;

                    var idxOut = 0;

                    if (cl.lightType == LightType.Spot)
                    {
                        var isCircularSpot = !bHasCookie;
                        if (!isCircularSpot)    // square spots always have cookie
                        {
                            light.sliceIndex = m_CookieTexArray.FetchSlice(cl.light.cookie);
                        }

                        Vector3 lightDir = lightToWorld.GetColumn(2);   // Z axis in world space

                        // represents a left hand coordinate system in world space
                        Vector3 vx = lightToWorld.GetColumn(0);     // X axis in world space
                        Vector3 vy = lightToWorld.GetColumn(1);     // Y axis in world space
                        var vz = lightDir;                      // Z axis in world space

                        // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                        vx = worldToView.MultiplyVector(vx);
                        vy = worldToView.MultiplyVector(vy);
                        vz = worldToView.MultiplyVector(vz);


                        const float pi = 3.1415926535897932384626433832795f;
                        const float degToRad = (float)(pi / 180.0);


                        var sa = cl.light.spotAngle;

                        var cs = Mathf.Cos(0.5f * sa * degToRad);
                        var si = Mathf.Sin(0.5f * sa * degToRad);
                        var ta = cs > 0.0f ? (si / cs) : FltMax;

                        var cota = si > 0.0f ? (cs / si) : FltMax;

                        //const float cotasa = l.GetCotanHalfSpotAngle();

                        // apply nonuniform scale to OBB of spot light
                        var squeeze = true;//sa < 0.7f * 90.0f;      // arb heuristic
                        var fS = squeeze ? ta : si;
                        bound.center = worldToView.MultiplyPoint(lightPos + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                        light.lightAxisX = vx;
                        light.lightAxisY = vy;
                        light.lightAxisZ = vz;

                        // scale axis to match box or base of pyramid
                        bound.boxAxisX = (fS * range) * vx;
                        bound.boxAxisY = (fS * range) * vy;
                        bound.boxAxisZ = (0.5f * range) * vz;

                        // generate bounding sphere radius
                        var fAltDx = si;
                        var fAltDy = cs;
                        fAltDy = fAltDy - 0.5f;
                        //if(fAltDy<0) fAltDy=-fAltDy;

                        fAltDx *= range; fAltDy *= range;

                        var altDist = Mathf.Sqrt(fAltDy * fAltDy + (isCircularSpot ? 1.0f : 2.0f) * fAltDx * fAltDx);
                        bound.radius = altDist > (0.5f * range) ? altDist : (0.5f * range);       // will always pick fAltDist
                        bound.scaleXY = squeeze ? new Vector2(0.01f, 0.01f) : new Vector2(1.0f, 1.0f);

                        // fill up ldata
                        light.lightType = (uint)LightDefinitions.SPOT_LIGHT;
                        light.lightPos = worldToView.MultiplyPoint(lightPos);
                        light.radiusSq = range * range;
                        light.penumbra = cs;
                        light.cotan = cota;
                        light.flags |= (isCircularSpot ? LightDefinitions.IS_CIRCULAR_SPOT_SHAPE : 0);

                        light.flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                        light.flags |= (bHasShadow ? LightDefinitions.HAS_SHADOW : 0);

                        int i = LightDefinitions.DIRECT_LIGHT, j = LightDefinitions.SPOT_LIGHT;
                        idxOut = numEntries2nd[i, j] + offsets[i, j]; ++numEntries2nd[i, j];
                    }
                    else if (cl.lightType == LightType.Point)
                    {
                        if (bHasCookie)
                        {
                            light.sliceIndex = m_CubeCookieTexArray.FetchSlice(cl.light.cookie);
                        }

                        bound.center = worldToView.MultiplyPoint(lightPos);
                        bound.boxAxisX.Set(range, 0, 0);
                        bound.boxAxisY.Set(0, range, 0);
                        bound.boxAxisZ.Set(0, 0, isNegDeterminant ? (-range) : range);    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                        bound.scaleXY.Set(1.0f, 1.0f);
                        bound.radius = range;

                        // represents a left hand coordinate system in world space since det(worldToView)<0
                        var lightToView = worldToView * lightToWorld;
                        Vector3 vx = lightToView.GetColumn(0);
                        Vector3 vy = lightToView.GetColumn(1);
                        Vector3 vz = lightToView.GetColumn(2);

                        // fill up ldata
                        light.lightType = (uint)LightDefinitions.SPHERE_LIGHT;
                        light.lightPos = bound.center;
                        light.radiusSq = range * range;

                        light.lightAxisX = vx;
                        light.lightAxisY = vy;
                        light.lightAxisZ = vz;

                        light.flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                        light.flags |= (bHasShadow ? LightDefinitions.HAS_SHADOW : 0);

                        int i = LightDefinitions.DIRECT_LIGHT, j = LightDefinitions.SPHERE_LIGHT;
                        idxOut = numEntries2nd[i, j] + offsets[i, j]; ++numEntries2nd[i, j];
                    }
                    else
                    {
                        //Assert(false);
                    }

                    // next light
                    if (cl.lightType == LightType.Spot || cl.lightType == LightType.Point)
                    {
                        boundData[idxOut] = bound;
                        lightData[idxOut] = light;
                    }
                }
                var numLightsOut = offsets[LightDefinitions.DIRECT_LIGHT, numVolTypes - 1] + numEntries[LightDefinitions.DIRECT_LIGHT, numVolTypes - 1];

                // probe.m_BlendDistance
                // Vector3f extents = 0.5*Abs(probe.m_BoxSize);
                // C center of rendered refl box <-- GetComponent (Transform).GetPosition() + m_BoxOffset;
                // cube map capture point: GetComponent (Transform).GetPosition()
                // shader parameter min and max are C+/-(extents+blendDistance)
                foreach (var rl in probes)
                {
                    var cubemap = rl.texture;

                    // always a box for now
                    if (cubemap == null)
                        continue;

                    var bndData = new SFiniteLightBound();
                    var lgtData = new SFiniteLightData();

                    var idxOut = 0;
                    lgtData.flags = 0;

                    var bnds = rl.bounds;
                    var boxOffset = rl.center;                  // reflection volume offset relative to cube map capture point
                    var blendDistance = rl.blendDistance;
                    float imp = rl.importance;

                    var mat = rl.localToWorld;
                    //Matrix4x4 mat = rl.transform.localToWorldMatrix;
                    Vector3 cubeCapturePos = mat.GetColumn(3);      // cube map capture position in world space


                    // implicit in CalculateHDRDecodeValues() --> float ints = rl.intensity;
                    var boxProj = (rl.boxProjection != 0);
                    var decodeVals = rl.hdr;
                    //Vector4 decodeVals = rl.CalculateHDRDecodeValues();

                    // C is reflection volume center in world space (NOT same as cube map capture point)
                    var e = bnds.extents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);
                    //Vector3 C = bnds.center;        // P + boxOffset;
                    var C = mat.MultiplyPoint(boxOffset);       // same as commented out line above when rot is identity

                    //Vector3 posForShaderParam = bnds.center - boxOffset;    // gives same as rl.GetComponent<Transform>().position;
                    var posForShaderParam = cubeCapturePos;        // same as commented out line above when rot is identity
                    var combinedExtent = e + new Vector3(blendDistance, blendDistance, blendDistance);

                    Vector3 vx = mat.GetColumn(0);
                    Vector3 vy = mat.GetColumn(1);
                    Vector3 vz = mat.GetColumn(2);

                    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    vx = worldToView.MultiplyVector(vx);
                    vy = worldToView.MultiplyVector(vy);
                    vz = worldToView.MultiplyVector(vz);

                    var Cw = worldToView.MultiplyPoint(C);

                    if (boxProj) lgtData.flags |= LightDefinitions.IS_BOX_PROJECTED;

                    lgtData.lightPos = Cw;
                    lgtData.lightAxisX = vx;
                    lgtData.lightAxisY = vy;
                    lgtData.lightAxisZ = vz;
                    lgtData.localCubeCapturePoint = -boxOffset;
                    lgtData.probeBlendDistance = blendDistance;

                    lgtData.lightIntensity = decodeVals.x;
                    lgtData.decodeExp = decodeVals.y;

                    lgtData.sliceIndex = m_CubeReflTexArray.FetchSlice(cubemap);

                    var delta = combinedExtent - e;
                    lgtData.boxInnerDist = e;
                    lgtData.boxInvRange.Set(1.0f / delta.x, 1.0f / delta.y, 1.0f / delta.z);

                    bndData.center = Cw;
                    bndData.boxAxisX = combinedExtent.x * vx;
                    bndData.boxAxisY = combinedExtent.y * vy;
                    bndData.boxAxisZ = combinedExtent.z * vz;
                    bndData.scaleXY.Set(1.0f, 1.0f);
                    bndData.radius = combinedExtent.magnitude;

                    // fill up ldata
                    lgtData.lightType = (uint)LightDefinitions.BOX_LIGHT;
                    lgtData.lightModel = (uint)LightDefinitions.REFLECTION_LIGHT;


                    int i = LightDefinitions.REFLECTION_LIGHT, j = LightDefinitions.BOX_LIGHT;
                    idxOut = numEntries2nd[i, j] + offsets[i, j]; ++numEntries2nd[i, j];
                    boundData[idxOut] = bndData;
                    lightData[idxOut] = lgtData;
                }

                var numProbesOut = offsets[LightDefinitions.REFLECTION_LIGHT, numVolTypes - 1] + numEntries[LightDefinitions.REFLECTION_LIGHT, numVolTypes - 1];
                for (var m = 0; m < numModels; m++)
                {
                    for (var v = 0; v < numVolTypes; v++)
                        Debug.Assert(numEntries[m, v] == numEntries2nd[m, v], "count mismatch on second pass!");
                }

                s_ConvexBoundsBuffer.SetData(boundData);
                s_LightDataBuffer.SetData(lightData);


                return numLightsOut + numProbesOut;
            }



        }
    }
}
