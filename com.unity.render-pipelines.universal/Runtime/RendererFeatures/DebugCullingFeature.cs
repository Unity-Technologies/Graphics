using System;
using Unity.Mathematics;
using UnityEditor.Graphs;

namespace UnityEngine.Rendering.Universal
{
    internal static class DebugCullingHelpers
    {
        static readonly Vector4[] s_NdcFrustum =
        {
            new Vector4(-1, 1,  -1, 1),
            new Vector4(1, 1,  -1, 1),
            new Vector4(1, -1, -1, 1),
            new Vector4(-1, -1, -1, 1),

            new Vector4(-1, 1,  1, 1),
            new Vector4(1, 1,  1, 1),
            new Vector4(1, -1, 1, 1),
            new Vector4(-1, -1, 1, 1)
        };

        // Cube with edge of length 1
        private static readonly Vector4[] s_UnitCube =
        {
            new Vector4(-0.5f,  0.5f, -0.5f, 1),
            new Vector4(0.5f,  0.5f, -0.5f, 1),
            new Vector4(0.5f, -0.5f, -0.5f, 1),
            new Vector4(-0.5f, -0.5f, -0.5f, 1),

            new Vector4(-0.5f,  0.5f,  0.5f, 1),
            new Vector4(0.5f,  0.5f,  0.5f, 1),
            new Vector4(0.5f, -0.5f,  0.5f, 1),
            new Vector4(-0.5f, -0.5f,  0.5f, 1)
        };

        // Sphere with radius of 1
        private static readonly Vector4[] s_UnitSphere = MakeUnitSphere(16);

        // Square with edge of length 1
        private static readonly Vector4[] s_UnitSquare =
        {
            new Vector4(-0.5f, 0.5f, 0, 1),
            new Vector4(0.5f, 0.5f, 0, 1),
            new Vector4(0.5f, -0.5f, 0, 1),
            new Vector4(-0.5f, -0.5f, 0, 1),
        };

        private static Vector4[] MakeUnitSphere(int len)
        {
            Debug.Assert(len > 2);
            var v = new Vector4[len * 3];
            for (int i = 0; i < len; i++)
            {
                var f = i / (float)len;
                float c = Mathf.Cos(f * (float)(Math.PI * 2.0));
                float s = Mathf.Sin(f * (float)(Math.PI * 2.0));
                v[0 * len + i] = new Vector4(c, s, 0, 1);
                v[1 * len + i] = new Vector4(0, c, s, 1);
                v[2 * len + i] = new Vector4(s, 0, c, 1);
            }
            return v;
        }

        public static void DrawFrustum(Matrix4x4 projMatrix) { DrawFrustum(projMatrix, Color.red, Color.magenta, Color.blue); }
        public static void DrawFrustum(Matrix4x4 projMatrix, Color near, Color edge, Color far)
        {
            Vector4[] v = new Vector4[s_NdcFrustum.Length];
            Matrix4x4 m = projMatrix.inverse;

            for (int i = 0; i < s_NdcFrustum.Length; i++)
            {
                var s = m * s_NdcFrustum[i];
                v[i] = s / s.w;
            }

            // Near
            for (int i = 0; i < 4; i++)
            {
                var s = v[i];
                var e = v[(i + 1) % 4];
                Debug.DrawLine(s, e, near);
            }
            // Far
            for (int i = 0; i < 4; i++)
            {
                var s = v[4 + i];
                var e = v[4 + ((i + 1) % 4)];
                Debug.DrawLine(s, e, far);
            }
            // Middle
            for (int i = 0; i < 4; i++)
            {
                var s = v[i];
                var e = v[i + 4];
                Debug.DrawLine(s, e, edge);
            }
        }

        public static void DrawFrustumSplits(Matrix4x4 projMatrix, float splitMaxPct, Vector3 splitPct, int splitStart, int splitCount, Color color)
        {
            Vector4[] v = s_NdcFrustum;
            Matrix4x4 m = projMatrix.inverse;

            // Compute camera frustum
            Vector4[] f = new Vector4[s_NdcFrustum.Length];
            for (int i = 0; i < s_NdcFrustum.Length; i++)
            {
                var s = m * v[i];
                f[i] = s / s.w;
            }

            // Compute shadow far plane/quad
            Vector4[] qMax = new Vector4[4];
            for (int i = 0; i < 4; i++)
            {
                qMax[i] = Vector4.Lerp(f[i], f[4 + i], splitMaxPct);
            }

            // Draw Shadow far/max quad
            for (int i = 0; i < 4; i++)
            {
                var s = qMax[i];
                var e = qMax[(i + 1) % 4];
                Debug.DrawLine(s, e, Color.black);
            }

            // Compute split quad (between near/shadow far)
            Vector4[] q = new Vector4[4];
            for (int j = splitStart; j < splitCount; j++)
            {
                Color color1;
                switch (j)
                {
                    case 0:
                        color1 = Color.cyan;
                        break;
                    case 1:
                        color1 = Color.yellow;
                        break;
                    case 2:
                        color1 = Color.blue;
                        break;
                    default:
                        color1 = Color.red;
                        break;
                }

                float d = splitPct[j];
                for (int i = 0; i < 4; i++)
                {
                    q[i] = Vector4.Lerp(f[i], qMax[i], d);
                }

                // Draw
                for (int i = 0; i < 4; i++)
                {
                    var s = q[i];
                    var e = q[(i + 1) % 4];
                    Debug.DrawLine(s, e, color1);
                }
            }
        }

        public static void DrawBox(Vector4 pos, Vector3 size, Color color)
        {
            Vector4[] v = s_UnitCube;
            Vector4 sz = new Vector4(size.x, size.y, size.z, 1);
            for (int i = 0; i < 4; i++)
            {
                var s = pos + Vector4.Scale(v[i], sz);
                var e = pos + Vector4.Scale(v[(i + 1) % 4], sz);
                Debug.DrawLine(s , e , color);
            }
            for (int i = 0; i < 4; i++)
            {
                var s = pos + Vector4.Scale(v[4 + i], sz);
                var e = pos + Vector4.Scale(v[4 + ((i + 1) % 4)], sz);
                Debug.DrawLine(s , e , color);
            }
            for (int i = 0; i < 4; i++)
            {
                var s = pos + Vector4.Scale(v[i], sz);
                var e = pos + Vector4.Scale(v[i + 4], sz);
                Debug.DrawLine(s , e , color);
            }
        }

        public static void DrawBox(Matrix4x4 transform, Color color)
        {
            Vector4[] v = s_UnitCube;
            Matrix4x4 m = transform;
            for (int i = 0; i < 4; i++)
            {
                var s = m * v[i];
                var e = m * v[(i + 1) % 4];
                Debug.DrawLine(s , e , color);
            }
            for (int i = 0; i < 4; i++)
            {
                var s = m * v[4 + i];
                var e = m * v[4 + ((i + 1) % 4)];
                Debug.DrawLine(s , e , color);
            }
            for (int i = 0; i < 4; i++)
            {
                var s = m * v[i];
                var e = m * v[i + 4];
                Debug.DrawLine(s , e , color);
            }
        }

        public static void DrawSphere(Vector4 pos, float radius, Color color)
        {
            Vector4[] v = s_UnitSphere;
            int len = s_UnitSphere.Length / 3;
            for (int i = 0; i < len; i++)
            {
                var sX = pos + radius * v[0 * len + i];
                var eX = pos + radius * v[0 * len + (i + 1) % len];
                var sY = pos + radius * v[1 * len + i];
                var eY = pos + radius * v[1 * len + (i + 1) % len];
                var sZ = pos + radius * v[2 * len + i];
                var eZ = pos + radius * v[2 * len + (i + 1) % len];
                Debug.DrawLine(sX, eX, color);
                Debug.DrawLine(sY, eY, color);
                Debug.DrawLine(sZ, eZ, color);
            }
        }

        public static void DrawPoint(Vector4 pos, float scale, Color color)
        {
            var sX = pos + new Vector4(+scale, 0, 0);
            var eX = pos + new Vector4(-scale, 0, 0);
            var sY = pos + new Vector4(0, +scale, 0);
            var eY = pos + new Vector4(0, -scale, 0);
            var sZ = pos + new Vector4(0, 0, +scale);
            var eZ = pos + new Vector4(0, 0, -scale);
            Debug.DrawLine(sX , eX , color);
            Debug.DrawLine(sY , eY , color);
            Debug.DrawLine(sZ , eZ , color);
        }

        public static void DrawAxes(Vector4 pos, float scale = 1.0f)
        {
            Debug.DrawLine(pos, pos + new Vector4(scale, 0, 0), Color.red);
            Debug.DrawLine(pos, pos + new Vector4(0, scale, 0), Color.green);
            Debug.DrawLine(pos, pos + new Vector4(0, 0, scale), Color.blue);
        }

        public static void DrawAxes(Matrix4x4 transform, float scale = 1.0f)
        {
            Vector4 p = transform * new Vector4(0, 0, 0, 1);
            Vector4 x = transform * new Vector4(scale, 0, 0, 1);
            Vector4 y = transform * new Vector4(0, scale, 0, 1);
            Vector4 z = transform * new Vector4(0, 0, scale, 1);

            Debug.DrawLine(p, x, Color.red);
            Debug.DrawLine(p, y, Color.green);
            Debug.DrawLine(p, z, Color.blue);
        }

        public static void DrawQuad(Matrix4x4 transform, Color color)
        {
            Vector4[] v = s_UnitSquare;
            Matrix4x4 m = transform;
            for (int i = 0; i < 4; i++)
            {
                var s = m * v[i];
                var e = m * v[(i + 1) % 4];
                Debug.DrawLine(s , e , color);
            }
        }

        public static void DrawPlane(Plane plane, float scale, Color edgeColor, float normalScale, Color normalColor)
        {
            // Flip plane distance: Unity Plane distance is from plane to origin
            DrawPlane(new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, -plane.distance), scale, edgeColor, normalScale, normalColor);
        }

        public static void DrawPlane(Vector4 plane, float scale, Color edgeColor, float normalScale, Color normalColor)
        {
            Vector3 n = Vector3.Normalize(plane);
            float   d = plane.w;

            Vector3 u = Vector3.up;
            Vector3 r = Vector3.right;
            if (n == u)
                u = r;

            r = Vector3.Cross(n, u);
            u = Vector3.Cross(n, r);

            for (int i = 0; i < 4; i++)
            {
                var s = scale * s_UnitSquare[i];
                var e = scale * s_UnitSquare[(i + 1) % 4];
                s = s.x * r + s.y * u + n * d;
                e = e.x * r + e.y * u + n * d;
                Debug.DrawLine(s, e, edgeColor);
            }

            // Diagonals
            {
                var s = scale * s_UnitSquare[0];
                var e = scale * s_UnitSquare[2];
                s = s.x * r + s.y * u + n * d;
                e = e.x * r + e.y * u + n * d;
                Debug.DrawLine(s, e, edgeColor);
            }
            {
                var s = scale * s_UnitSquare[1];
                var e = scale * s_UnitSquare[3];
                s = s.x * r + s.y * u + n * d;
                e = e.x * r + e.y * u + n * d;
                Debug.DrawLine(s, e, edgeColor);
            }

            Debug.DrawLine(n * d, n * (d + 1 * normalScale), normalColor);
        }
    }

    [DisallowMultipleRendererFeature]
    public class DebugCullingFeature : ScriptableRendererFeature
    {
        private DebugCullingPass m_Pass;

        // Settings
        public bool drawOrigin               = false;
        public bool drawCullingFrustum       = true;
        public bool drawCullingFrustumSplits = true;
        public bool drawCullingSpheres       = false;

        public bool drawCullingPlanes                 = false;
        public bool drawSingleCullingPlane            = false;
        public int drawSingleCullingPlaneIndex        = 0;    // TODO: better interface, a range? a slider?
        public int drawSingleCullingPlaneCascadeIndex = 0;

        public bool drawVisibleLights      = false;
        public bool drawVisibleLightRadius = false;

        public bool drawShadowCasterBounds = false;

        public bool drawDirectLightFrustum = false;
        public bool drawSpotLightFrustum   = false;
        public bool drawPointLightFrusta   = false;
        public bool noPointLightFrustumBias = false;

        //TODO:
        // - perhaps add a point param to drawPlane, and use it to align/move the plane gizmo along the plane (i.e the gizmo tracks the point)
        // - better UI and controls
        //

        public class DebugCullingPass : ScriptableRenderPass
        {
            private DebugCullingFeature m_Feature;
            public DebugCullingPass(DebugCullingFeature feature)
            {
                base.renderPassEvent = RenderPassEvent.BeforeRendering;
                base.profilingSampler = new ProfilingSampler(nameof(ScriptableRenderPass));

                m_Feature = feature;
            }

            /// <summary>
            /// Execute the pass. This is where custom rendering occurs. Specific details are left to the implementation
            /// </summary>
            /// <param name="context">Use this render context to issue any draw commands during execution</param>
            /// <param name="renderingData">Current rendering state information</param>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.camera.cameraType == CameraType.Game && renderingData.cameraData.camera.name != "Preview Camera")
                {
                    var camPos = renderingData.cameraData.camera.cameraToWorldMatrix * new Vector4(0, 0, 0, 1);
                    var camFront = renderingData.cameraData.camera.cameraToWorldMatrix.GetColumn(2);
                    var shadowCascadesCount = renderingData.shadowData.mainLightShadowCascadesCount;

                    // Test
                    /*DebugCullingHelpers.DrawPlane(new Vector4(1,0,0,2), 4, Color.red, 10,Color.red );
                    DebugCullingHelpers.DrawPlane(new Vector4(0,1,0,2), 4, Color.green, 10,Color.green );
                    DebugCullingHelpers.DrawPlane(new Vector4(0,0,1,2), 4, Color.blue, 10,Color.blue );
                    DebugCullingHelpers.DrawPlane(new Vector4(1,1,1,3.46f), 4, Color.white, 10,Color.white );*/

                    // Test
                    /*{
                        DebugCullingHelpers.DrawPlane(new Vector4(1,0,1,0), 10, Color.green, 5,Color.white );

                        DebugCullingHelpers.DrawBox(new Vector4(0,0,5,1), new Vector3(1, 2, 3 ),  Color.gray );
                        DebugCullingHelpers.DrawPoint(new Vector4(3,1,0,1), 1,  Color.cyan );
                    }*/

                    // Origin
                    if (m_Feature.drawOrigin)
                    {
                        Debug.DrawLine(Vector3.zero, new Vector3(1, 0, 0), Color.red);
                        Debug.DrawLine(Vector3.zero, new Vector3(0, 1, 0), Color.green);
                        Debug.DrawLine(Vector3.zero, new Vector3(0, 0, 1), Color.blue);
                    }

                    // Frustum
                    if (m_Feature.drawCullingFrustum)
                    {
                        DebugCullingHelpers.DrawFrustum(renderingData.cameraData.camera.cullingMatrix);
                        DebugCullingHelpers.DrawAxes(renderingData.cameraData.camera.cameraToWorldMatrix, 0.25f);

                        if (m_Feature.drawCullingFrustumSplits && shadowCascadesCount > 1)
                        {
                            var f = renderingData.cameraData.camera.farClipPlane;
                            var n = renderingData.cameraData.camera.nearClipPlane;
                            var s = renderingData.cameraData.maxShadowDistance;
                            var sMax = (s - n) / f;
                            DebugCullingHelpers.DrawFrustumSplits(renderingData.cameraData.camera.cullingMatrix, sMax, renderingData.shadowData.mainLightShadowCascadesSplit, 0, shadowCascadesCount - 1, Color.gray);

                        }
                    }

                    int mainLightIndex = renderingData.lightData.mainLightIndex;
                    if (mainLightIndex >= 0)
                    {
                        VisibleLight mainLight = renderingData.lightData.visibleLights[mainLightIndex];

                        // Shadow caster bounds
                        Bounds bounds;
                        bool boundsFound = renderingData.cullResults.GetShadowCasterBounds(mainLightIndex, out bounds);
                        if (boundsFound && m_Feature.drawShadowCasterBounds)
                        {
                            DebugCullingHelpers.DrawBox(bounds.center, bounds.size, Color.gray);
                        }

                        Matrix4x4[] view = new Matrix4x4[shadowCascadesCount];
                        Matrix4x4[] proj = new Matrix4x4[shadowCascadesCount];
                        ShadowSplitData[] shadowSplitData = new ShadowSplitData[shadowCascadesCount];

                        {
                            int shadowResolution = ShadowUtils.GetMaxTileResolutionInAtlas(renderingData.shadowData.mainLightShadowmapWidth,
                                renderingData.shadowData.mainLightShadowmapHeight, shadowCascadesCount);
                            for (int cascadeIndex = 0; cascadeIndex < shadowCascadesCount; ++cascadeIndex)
                            {
                                bool success = renderingData.cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(mainLightIndex,
                                    cascadeIndex, renderingData.shadowData.mainLightShadowCascadesCount, renderingData.shadowData.mainLightShadowCascadesSplit, shadowResolution, mainLight.light.shadowNearPlane,
                                    out view[cascadeIndex], out proj[cascadeIndex], out shadowSplitData[cascadeIndex]);
                            }
                        }

                        // Direct light frustum
                        if (m_Feature.drawDirectLightFrustum)
                        {
                            for (int cascadeIndex = 0; cascadeIndex < shadowCascadesCount; ++cascadeIndex)
                            {
                                var shadowTransform = proj[cascadeIndex] * view[cascadeIndex];
                                DebugCullingHelpers.DrawFrustum(shadowTransform, Color.white, Color.yellow, Color.black);
                                DebugCullingHelpers.DrawAxes(shadowTransform.inverse, 0.25f);
                            }
                        }

                        // Culling spheres
                        if (m_Feature.drawCullingSpheres)
                        {
                            for (int cascadeIndex = 0; cascadeIndex < shadowCascadesCount; ++cascadeIndex)
                            {
                                Color color;
                                switch(cascadeIndex)
                                {
                                    case 0:
                                        color = Color.green;
                                        break;
                                    case 1:
                                        color = Color.red;
                                        break;
                                    case 2:
                                        color = Color.cyan;
                                        break;
                                    default:
                                        color = Color.yellow;
                                        break;
                                }

                                Vector4 s = shadowSplitData[cascadeIndex].cullingSphere;
                                Vector3 c = s;
                                float radius = s.w;
                                DebugCullingHelpers.DrawSphere(c, radius, color);
                                DebugCullingHelpers.DrawPoint(c, 0.5f, color);
                            }
                        }

                        // Culling planes
                        if (m_Feature.drawCullingPlanes || m_Feature.drawSingleCullingPlane)
                        {
                            Color planeColor = Color.cyan;
                            Color normalColor = Color.blue;

                            m_Feature.drawSingleCullingPlaneCascadeIndex = math.clamp(m_Feature.drawSingleCullingPlaneCascadeIndex, 0, shadowCascadesCount - 1);
                            m_Feature.drawSingleCullingPlaneIndex = math.clamp(m_Feature.drawSingleCullingPlaneIndex, 0, shadowSplitData[m_Feature.drawSingleCullingPlaneCascadeIndex].cullingPlaneCount - 1);

                            if (m_Feature.drawSingleCullingPlane)
                            {
                                var cascadeIndex = m_Feature.drawSingleCullingPlaneCascadeIndex;

                                var pc = Color.Lerp(planeColor, Color.black, cascadeIndex / (float)shadowCascadesCount);
                                var nc = Color.Lerp(normalColor, Color.black, cascadeIndex / (float)shadowCascadesCount);
                                var ssd = shadowSplitData[cascadeIndex];

                                var pi = m_Feature.drawSingleCullingPlaneIndex;
                                {
                                    var p = ssd.GetCullingPlane(pi);
                                    DebugCullingHelpers.DrawPlane(p, 100.0f, pc, 5.0f, nc);
                                }
                            }
                            else
                            {
                                for (int cascadeIndex = 0; cascadeIndex < shadowCascadesCount; ++cascadeIndex)
                                {
                                    var pc = Color.Lerp(planeColor,  Color.black, cascadeIndex / (float)shadowCascadesCount);
                                    var nc = Color.Lerp(normalColor, Color.black, cascadeIndex / (float)shadowCascadesCount);
                                    var ssd = shadowSplitData[cascadeIndex];

                                    for (int pi = 0; pi < ssd.cullingPlaneCount; pi++)
                                    {
                                        var p = ssd.GetCullingPlane(pi);
                                        DebugCullingHelpers.DrawPlane(p, 100.0f, pc, 5.0f, nc);
                                    }
                                }
                            }
                        }
                    }

                    // Spot light frustum
                    if (m_Feature.drawSpotLightFrustum)
                    {
                        var lightCount = renderingData.lightData.visibleLights.Length;
                        ShadowSplitData[] spotShadowSplitData = new ShadowSplitData[lightCount];
                        Matrix4x4[] spotView = new Matrix4x4[lightCount];
                        Matrix4x4[] spotProj = new Matrix4x4[lightCount];

                        for (int li = 0; li < lightCount; li++)
                        {
                            VisibleLight l = renderingData.lightData.visibleLights[li];
                            if (l.lightType == LightType.Spot)
                            {
                                renderingData.cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(li, out spotView[li], out spotProj[li], out spotShadowSplitData[li]);
                                var shadowTransform = spotProj[li] * spotView[li];
                                DebugCullingHelpers.DrawFrustum(shadowTransform, Color.white, Color.yellow, Color.black);
                                DebugCullingHelpers.DrawAxes(spotView[li].inverse, 0.5f);
                            }
                        }
                    }

                    if (m_Feature.drawPointLightFrusta)
                    {
                        var lightCount = renderingData.lightData.visibleLights.Length;
                        ShadowSplitData[] pointShadowSplitData = new ShadowSplitData[lightCount];
                        Matrix4x4[] pointView = new Matrix4x4[lightCount];
                        Matrix4x4[] pointProj = new Matrix4x4[lightCount];

                        for (int li = 0; li < lightCount; li++)
                        {
                            VisibleLight l = renderingData.lightData.visibleLights[li];
                            if (l.lightType == LightType.Point)
                            {
                                float fovBias = m_Feature.noPointLightFrustumBias
                                    ? 0.0f
                                    : Internal.AdditionalLightsShadowCasterPass.GetPointLightShadowFrustumFovBiasInDegrees(renderingData.shadowData.resolution[li], (l.light.shadows == LightShadows.Soft));
                                for (int f = 0; f < 6; f++)
                                {
                                    CubemapFace face = (CubemapFace)f;
                                    renderingData.cullResults.ComputePointShadowMatricesAndCullingPrimitives(li, face, fovBias, out pointView[li], out pointProj[li], out pointShadowSplitData[li]);
                                    var shadowTransform = pointProj[li] * pointView[li];
                                    DebugCullingHelpers.DrawFrustum(shadowTransform, Color.white, Color.yellow, Color.black);
                                }

                                DebugCullingHelpers.DrawAxes(pointView[li].inverse, 0.5f);
                            }
                        }
                    }

                    // Visible lights
                    if (m_Feature.drawVisibleLights)
                    {
                        foreach (var l in renderingData.lightData.visibleLights)
                        {
                            Debug.DrawLine(camPos, l.localToWorldMatrix.GetColumn(3), Color.yellow);

                            if (m_Feature.drawVisibleLightRadius)
                            {
                                var c = l.localToWorldMatrix.GetColumn(3);
                                DebugCullingHelpers.DrawSphere(c, l.range, Color.yellow);
                                DebugCullingHelpers.DrawPoint(c, 0.25f, Color.yellow);
                            }
                        }
                    }
                }
            }

            static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
            {
                // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
                // apply z reversal to projection matrix. We need to do it manually here.
                /*if (SystemInfo.usesReversedZBuffer)
                {
                    proj.m20 = -proj.m20;
                    proj.m21 = -proj.m21;
                    proj.m22 = -proj.m22;
                    proj.m23 = -proj.m23;
                }*/

                Matrix4x4 worldToShadow = proj * view;

                var textureScaleAndBias = Matrix4x4.identity;
                textureScaleAndBias.m00 = 0.5f;
                textureScaleAndBias.m11 = 0.5f;
                textureScaleAndBias.m22 = 0.5f;
                textureScaleAndBias.m03 = 0.5f;
                textureScaleAndBias.m23 = 0.5f;
                textureScaleAndBias.m13 = 0.5f;

                // Apply texture scale and offset to save a MAD in shader.
                return textureScaleAndBias * worldToShadow;
            }
        }

        /// <summary>
        /// Initializes this feature's resources. This is called every time serialization happens.
        /// </summary>
        public override void Create()
        {
            m_Pass = new DebugCullingPass(this);
        }

        /// <summary>
        /// Injects one or multiple <c>ScriptableRenderPass</c> in the renderer.
        /// </summary>
        /// <param name="renderPasses">List of render passes to add to.</param>
        /// <param name="renderingData">Rendering state. Use this to setup render passes.</param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_Pass);
        }
    }
}
