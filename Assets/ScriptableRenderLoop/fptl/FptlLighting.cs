using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

using UnityEditor;

namespace UnityEngine.ScriptableRenderLoop
{
    //[ExecuteInEditMode]
    public class FptlLighting : ScriptableRenderLoop
    {
        [MenuItem("Renderloop/CreateRenderLoopFPTL")]
        static void CreateRenderLoopFPTL()
        {
            var instance = ScriptableObject.CreateInstance<FptlLighting>();
            AssetDatabase.CreateAsset(instance, "Assets/renderloopfptl.asset");
            //AssetDatabase.CreateAsset(instance, "Assets/ScriptableRenderLoop/fptl/renderloopfptl.asset");
        }

        public Shader m_DeferredShader;
        public Shader m_DeferredReflectionShader;

        public ComputeShader m_BuildScreenAABBShader;
        public ComputeShader m_BuildPerTileLightListShader;

        private Material m_DeferredMaterial;
        private Material m_DeferredReflectionMaterial;
        static private int kGBufferAlbedo;
        static private int kGBufferSpecRough;
        static private int kGBufferNormal;
        static private int kGBufferEmission;
        static private int kGBufferZ;
        static private int kCameraDepthTexture;


        static private int kGenAABBKernel;
        static private int kGenListPerTileKernel;
        static private ComputeBuffer m_lightDataBuffer;
        static private ComputeBuffer m_convexBoundsBuffer;
        static private ComputeBuffer m_aabbBoundsBuffer;
        static private ComputeBuffer lightList;

        public const int gMaxNumLights = 1024;
        public const float gFltMax = 3.402823466e+38F;

        private TextureCache2D m_cookieTexArray;
        private TextureCacheCubemap m_cubeCookieTexArray;
        private TextureCacheCubemap m_cubeReflTexArray;

        void OnEnable()
        {
            Rebuild();
        }

        void OnValidate()
        {
            Rebuild();
        }

        void Rebuild()
        {
            kGBufferAlbedo = Shader.PropertyToID("_CameraGBufferTexture0");
            kGBufferSpecRough = Shader.PropertyToID("_CameraGBufferTexture1");
            kGBufferNormal = Shader.PropertyToID("_CameraGBufferTexture2");
            kGBufferEmission = Shader.PropertyToID("_CameraGBufferTexture3");
            kGBufferZ = Shader.PropertyToID("_CameraGBufferZ"); // used while rendering into G-buffer+
            kCameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture"); // copy of that for later sampling in shaders


         //   RenderLoop.renderLoopDelegate += ExecuteRenderLoop;
            //var deferredShader = GraphicsSettings.GetCustomShader (BuiltinShaderType.DeferredShading);
            var deferredShader = m_DeferredShader;
            var deferredReflectionShader = m_DeferredReflectionShader;

            m_DeferredMaterial = new Material(deferredShader);
            m_DeferredReflectionMaterial = new Material(deferredReflectionShader);


            kGenAABBKernel = m_BuildScreenAABBShader.FindKernel("ScreenBoundsAABB");
            kGenListPerTileKernel = m_BuildPerTileLightListShader.FindKernel("TileLightListGen");
            m_aabbBoundsBuffer = new ComputeBuffer(2 * gMaxNumLights, 3 * sizeof(float));
            m_convexBoundsBuffer = new ComputeBuffer(gMaxNumLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
            m_lightDataBuffer = new ComputeBuffer(gMaxNumLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightData)));

            lightList = new ComputeBuffer(LightDefinitions.NR_LIGHT_MODELS * 1024 * 1024, sizeof(uint));       // enough list memory for a 4k x 4k display

            m_BuildScreenAABBShader.SetBuffer(kGenAABBKernel, "g_data", m_convexBoundsBuffer);
            //m_BuildScreenAABBShader.SetBuffer(kGenAABBKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
            m_DeferredMaterial.SetBuffer("g_vLightData", m_lightDataBuffer);
            m_DeferredReflectionMaterial.SetBuffer("g_vLightData", m_lightDataBuffer);

            m_BuildPerTileLightListShader.SetBuffer(kGenListPerTileKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
            m_BuildPerTileLightListShader.SetBuffer(kGenListPerTileKernel, "g_vLightData", m_lightDataBuffer);

            m_cookieTexArray = new TextureCache2D();
            m_cubeCookieTexArray = new TextureCacheCubemap();
            m_cubeReflTexArray = new TextureCacheCubemap();
            m_cookieTexArray.AllocTextureArray(8, 128, 128, TextureFormat.Alpha8, true);
            m_cubeCookieTexArray.AllocTextureArray(4, 512, 512, TextureFormat.Alpha8, true);
            m_cubeReflTexArray.AllocTextureArray(64, 128, 128, TextureFormat.DXT5, true);


            m_DeferredMaterial.SetTexture("_spotCookieTextures", m_cookieTexArray.GetTexCache());
            m_DeferredMaterial.SetTexture("_pointCookieTextures", m_cubeCookieTexArray.GetTexCache());
            m_DeferredReflectionMaterial.SetTexture("_reflCubeTextures", m_cubeReflTexArray.GetTexCache());
        }

        void OnDisable()
        {
           // RenderLoop.renderLoopDelegate -= ExecuteRenderLoop;
            if(m_DeferredMaterial) DestroyImmediate(m_DeferredMaterial);
            if(m_DeferredReflectionMaterial) DestroyImmediate(m_DeferredReflectionMaterial);
            m_cookieTexArray.Release();
            m_cubeCookieTexArray.Release();
            m_cubeReflTexArray.Release();

            m_aabbBoundsBuffer.Release();
            m_convexBoundsBuffer.Release();
            m_lightDataBuffer.Release();
            lightList.Release();
        }

        static void SetupGBuffer(CommandBuffer cmd)
        {
            var format10 = RenderTextureFormat.ARGB32;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010))
                format10 = RenderTextureFormat.ARGB2101010;
            //@TODO: GetGraphicsCaps().buggyMRTSRGBWriteFlag
            cmd.GetTemporaryRT(kGBufferAlbedo, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            cmd.GetTemporaryRT(kGBufferSpecRough, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            cmd.GetTemporaryRT(kGBufferNormal, -1, -1, 0, FilterMode.Point, format10, RenderTextureReadWrite.Linear);
            cmd.GetTemporaryRT(kGBufferEmission, -1, -1, 0, FilterMode.Point, format10, RenderTextureReadWrite.Linear); //@TODO: HDR
            cmd.GetTemporaryRT(kGBufferZ, -1, -1, 24, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.GetTemporaryRT(kCameraDepthTexture, -1, -1, 24, FilterMode.Point, RenderTextureFormat.Depth);
            var colorMRTs = new RenderTargetIdentifier[4] { kGBufferAlbedo, kGBufferSpecRough, kGBufferNormal, kGBufferEmission };
            cmd.SetRenderTarget(colorMRTs, new RenderTargetIdentifier(kGBufferZ));
            cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

            //@TODO: render VR occlusion mesh
        }

        static void RenderGBuffer(CullResults cull, Camera camera, RenderLoop loop)
        {
            // setup GBuffer for rendering
            var cmd = new CommandBuffer();
            cmd.name = "Create G-Buffer";
            SetupGBuffer(cmd);
            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            // render opaque objects using Deferred pass
            DrawRendererSettings settings = new DrawRendererSettings(cull, camera, new ShaderPassName("Deferred"));
            settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;
            settings.inputCullingOptions.SetQueuesOpaque();
            loop.DrawRenderers(ref settings);

        }

        static void CopyDepthAfterGBuffer(RenderLoop loop)
        {
            var cmd = new CommandBuffer();
            cmd.CopyTexture(new RenderTargetIdentifier(kGBufferZ), new RenderTargetIdentifier(kCameraDepthTexture));
            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        void DoTiledDeferredLighting(RenderLoop loop, Matrix4x4 viewToWorld, Matrix4x4 scrProj, Matrix4x4 incScrProj, ComputeBuffer lightList)
        {
            m_DeferredMaterial.SetBuffer("g_vLightList", lightList);
            m_DeferredReflectionMaterial.SetBuffer("g_vLightList", lightList);

            var cmd = new CommandBuffer();

            //cmd.SetRenderTarget(new RenderTargetIdentifier(kGBufferEmission), new RenderTargetIdentifier(kGBufferZ));

            cmd.SetGlobalMatrix("g_mViewToWorld", viewToWorld);
            cmd.SetGlobalMatrix("g_mWorldToView", viewToWorld.inverse);
            cmd.SetGlobalMatrix("g_mScrProjection", scrProj);
            cmd.SetGlobalMatrix("g_mInvScrProjection", incScrProj);

            //cmd.Blit (kGBufferNormal, (RenderTexture)null); // debug: display normals
            cmd.Blit(kGBufferEmission, (RenderTexture)null, m_DeferredMaterial, 0);
            cmd.Blit(kGBufferEmission, (RenderTexture)null, m_DeferredReflectionMaterial, 0);
            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        void SetMatrixCS(CommandBuffer cmd, ComputeShader shadercs, string name, Matrix4x4 mat)
        {
            float[] data = new float[16];

            for (int c = 0; c < 4; c++)
                for (int r = 0; r < 4; r++)
                    data[4 * c + r] = mat[r, c];

            cmd.SetComputeFloatParams(shadercs, name, data);
        }

        int GenerateSourceLightBuffers(Camera camera, CullResults inputs)
        {
            ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>();

            int numLights = inputs.culledLights.Length;
            int numProbes = probes.Length;
            int numVolumes = numLights + numProbes;


            SFiniteLightData[] lightData = new SFiniteLightData[numVolumes];
            SFiniteLightBound[] boundData = new SFiniteLightBound[numVolumes];
            Matrix4x4 worldToView = camera.worldToCameraMatrix;

            int i = 0;
            foreach (var cl in inputs.culledLights)
            {
                float range = cl.range;

                Matrix4x4 lightToWorld = cl.localToWorld;
                //Matrix4x4 worldToLight = l.worldToLocal;

                Vector3 lightPos = lightToWorld.GetColumn(3);

                boundData[i].vBoxAxisX = new Vec3(1, 0, 0);
                boundData[i].vBoxAxisY = new Vec3(0, 1, 0);
                boundData[i].vBoxAxisZ = new Vec3(0, 0, 1);
                boundData[i].vScaleXY = new Vec2(1.0f, 1.0f);
                boundData[i].fRadius = range;

                lightData[i].flags = 0;
                lightData[i].fRecipRange = 1.0f / range;
                lightData[i].vCol = new Vec3(cl.finalColor.r, cl.finalColor.g, cl.finalColor.b);
                lightData[i].iSliceIndex = 0;
                lightData[i].uLightModel = (uint)LightDefinitions.DIRECT_LIGHT;

                bool bHasCookie = cl.light.cookie != null;

                if (cl.lightType == LightType.Spot)
                {
                    bool bIsCircularSpot = !bHasCookie;
                    if (!bIsCircularSpot)    // square spots always have cookie
                    {
                        lightData[i].iSliceIndex = m_cookieTexArray.FetchSlice(cl.light.cookie);
                    }

                    Vector3 lightDir = lightToWorld.GetColumn(2);	// Z axis in world space

                    // represents a left hand coordinate system in world space
                    Vector3 vx = lightToWorld.GetColumn(0);		// X axis in world space
                    Vector3 vy = lightToWorld.GetColumn(1);		// Y axis in world space
                    Vector3 vz = lightDir;						// Z axis in world space

                    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    vx = worldToView.MultiplyVector(vx);
                    vy = worldToView.MultiplyVector(vy);
                    vz = worldToView.MultiplyVector(vz);


                    const float pi = 3.1415926535897932384626433832795f;
                    const float degToRad = (float)(pi / 180.0);
                    const float radToDeg = (float)(180.0 / pi);


                    //float sa = cl.GetSpotAngle();		// total field of view from left to right side
                    float sa = radToDeg * (2 * Mathf.Acos(1.0f / cl.invCosHalfSpotAngle));       // spot angle doesn't exist in the structure so reversing it for now.


                    float cs = Mathf.Cos(0.5f * sa * degToRad);
                    float si = Mathf.Sin(0.5f * sa * degToRad);
                    float ta = cs > 0.0f ? (si / cs) : gFltMax;

                    float cota = si > 0.0f ? (cs / si) : gFltMax;

                    //const float cotasa = l.GetCotanHalfSpotAngle();

                    // apply nonuniform scale to OBB of spot light
                    bool bSqueeze = sa < 0.7f * 90.0f;		// arb heuristic
                    float fS = bSqueeze ? ta : si;
                    boundData[i].vCen = worldToView.MultiplyPoint(lightPos + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                    lightData[i].vLaxisX = vx;
                    lightData[i].vLaxisY = vy;
                    lightData[i].vLaxisZ = vz;

                    // scale axis to match box or base of pyramid
                    boundData[i].vBoxAxisX = (fS * range) * vx;
                    boundData[i].vBoxAxisY = (fS * range) * vy;
                    boundData[i].vBoxAxisZ = (0.5f * range) * vz;

                    // generate bounding sphere radius
                    float fAltDx = si;
                    float fAltDy = cs;
                    fAltDy = fAltDy - 0.5f;
                    //if(fAltDy<0) fAltDy=-fAltDy;

                    fAltDx *= range; fAltDy *= range;

                    float fAltDist = Mathf.Sqrt(fAltDy * fAltDy + (bIsCircularSpot ? 1.0f : 2.0f) * fAltDx * fAltDx);
                    boundData[i].fRadius = fAltDist > (0.5f * range) ? fAltDist : (0.5f * range);		// will always pick fAltDist
                    boundData[i].vScaleXY = bSqueeze ? new Vec2(0.01f, 0.01f) : new Vec2(1.0f, 1.0f);

                    // fill up ldata
                    lightData[i].uLightType = (uint)LightDefinitions.SPOT_LIGHT;
                    lightData[i].vLpos = worldToView.MultiplyPoint(lightPos);
                    lightData[i].fSphRadiusSq = range * range;
                    lightData[i].fPenumbra = cs;
                    lightData[i].cotan = cota;
                    lightData[i].flags |= (bIsCircularSpot ? LightDefinitions.IS_CIRCULAR_SPOT_SHAPE : 0);

                    lightData[i].flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                }
                else if (cl.lightType == LightType.Point)
                {
                    if (bHasCookie)
                    {
                        lightData[i].iSliceIndex = m_cubeCookieTexArray.FetchSlice(cl.light.cookie);
                    }

                    boundData[i].vCen = worldToView.MultiplyPoint(lightPos);
                    boundData[i].vBoxAxisX = new Vec3(range, 0, 0);
                    boundData[i].vBoxAxisY = new Vec3(0, range, 0);
                    boundData[i].vBoxAxisZ = new Vec3(0, 0, -range);    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    boundData[i].vScaleXY = new Vec2(1.0f, 1.0f);
                    boundData[i].fRadius = range;

                    // fill up ldata
                    lightData[i].uLightType = (uint)LightDefinitions.SPHERE_LIGHT;
                    lightData[i].vLpos = boundData[i].vCen;
                    lightData[i].fSphRadiusSq = range * range;

                    lightData[i].flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                }
                else
                {
                    //Assert(false);
                }

                // next light
                if (cl.lightType == LightType.Spot || cl.lightType == LightType.Point)
                    ++i;
            }


            // probe.m_BlendDistance
            // Vector3f extents = 0.5*Abs(probe.m_BoxSize);
            // C center of rendered refl box <-- GetComponent (Transform).GetPosition() + m_BoxOffset;
            // P parameter position to shader: GetComponent (Transform).GetPosition()
            // shader parameter min and max are C+/-(extents+blendDistance)

            //Vector3[] Ps = new Vector3[3] { new Vector3(6.28f, -1.18f, -5.67f), new Vector3(14.23f, -1.18f, 0.21f), new Vector3(6.28f, -1.18f, 1.91f) };
            //Vector3[] boxSizes = new Vector3[3] { new Vector3(20.0f, 10.0f, 10.0f), new Vector3(10.0f, 10.0f, 10.0f), new Vector3(10.0f, 10.0f, 10.0f) };
            //Vector3 boxOffset = new Vector3(0.0f, 0.0f, 0.0f);
            //float[] bd = new float[3] { 4.0f, 4.0f, 4.0f };


            int numProbesOut = 0;
            foreach (var rl in probes)
            {
                Texture cubemap = rl.mode == ReflectionProbeMode.Custom ? rl.customBakedTexture : rl.bakedTexture;
                if (cubemap != null)        // always a box for now
                {
                    i = numProbesOut + numLights;

                    lightData[i].flags = 0;

                    Bounds bnds = rl.bounds;
                    Vector3 boxOffset = rl.center;
                    float blendDistance = rl.blendDistance;
                    float imp = rl.importance;
                    // implicit in CalculateHDRDecodeValues() --> float ints = rl.intensity;
                    bool boxProj = rl.boxProjection;
                    Vector4 decodeVals = rl.CalculateHDRDecodeValues();

                    Vector3 e = bnds.extents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);
                    Vector3 C = bnds.center;        // P + boxOffset;

                    Vector3 posForShaderParam = bnds.center - boxOffset;    // gives same as rl.GetComponent<Transform>().position;
                    Vector3 combinedExtent = e + new Vector3(blendDistance, blendDistance, blendDistance);

                    Vector3 vx = new Vector3(1, 0, 0);     // always axis aligned in world space for now
                    Vector3 vy = new Vector3(0, 1, 0);
                    Vector3 vz = new Vector3(0, 0, 1);

                    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    vx = worldToView.MultiplyVector(vx);
                    vy = worldToView.MultiplyVector(vy);
                    vz = worldToView.MultiplyVector(vz);

                    Vector3 Cw = worldToView.MultiplyPoint(C);

                    if (boxProj) lightData[i].flags |= LightDefinitions.IS_BOX_PROJECTED;

                    lightData[i].vLpos = Cw;
                    lightData[i].vLaxisX = vx;
                    lightData[i].vLaxisY = vy;
                    lightData[i].vLaxisZ = vz;
                    lightData[i].vProbeBoxOffset = boxOffset;
                    lightData[i].fProbeBlendDistance = blendDistance;

                    lightData[i].fLightIntensity = decodeVals.x;
                    lightData[i].fDecodeExp = decodeVals.y;

                    lightData[i].iSliceIndex = m_cubeReflTexArray.FetchSlice(cubemap);

                    Vector3 delta = combinedExtent - e;
                    lightData[i].vBoxInnerDist = e;
                    lightData[i].vBoxInvRange = new Vec3(1.0f / delta.x, 1.0f / delta.y, 1.0f / delta.z);

                    boundData[i].vCen = Cw;
                    boundData[i].vBoxAxisX = combinedExtent.x * vx;
                    boundData[i].vBoxAxisY = combinedExtent.y * vy;
                    boundData[i].vBoxAxisZ = combinedExtent.z * vz;
                    boundData[i].vScaleXY = new Vec2(1.0f, 1.0f);
                    boundData[i].fRadius = combinedExtent.magnitude;

                    // fill up ldata
                    lightData[i].uLightType = (uint)LightDefinitions.BOX_LIGHT;
                    lightData[i].uLightModel = (uint)LightDefinitions.REFLECTION_LIGHT;

                    ++numProbesOut;
                }
            }


            m_convexBoundsBuffer.SetData(boundData);
            m_lightDataBuffer.SetData(lightData);


            return numLights + numProbesOut;
        }

       /* public override void Render(Camera[] cameras, RenderLoop renderLoop)
        {
            foreach (var camera in cameras)
            {
                CullResults cullResults;
                CullingParameters cullingParams;
                if (!CullResults.GetCullingParameters(camera, out cullingParams))
                    continue;

                m_ShadowPass.UpdateCullingParameters(ref cullingParams);

                cullResults = CullResults.Cull(ref cullingParams, renderLoop);

                ShadowOutput shadows;
                m_ShadowPass.Render(renderLoop, cullResults, out shadows);

                renderLoop.SetupCameraProperties(camera);

                UpdateLightConstants(cullResults.culledLights, ref shadows);

                DrawRendererSettings settings = new DrawRendererSettings(cullResults, camera, new ShaderPassName("ForwardBase"));
                settings.rendererConfiguration = RendererConfiguration.ConfigureOneLightProbePerRenderer | RendererConfiguration.ConfigureReflectionProbesProbePerRenderer;
                settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;

                renderLoop.DrawRenderers(ref settings);
                renderLoop.Submit();
            }

            // Post effects
        }*/

        public override void Render(Camera[] cameras, RenderLoop renderLoop)
        {
            foreach (var camera in cameras)
            {
                CullResults cullResults;
                CullingParameters cullingParams;
                if (!CullResults.GetCullingParameters(camera, out cullingParams))
                    continue;

                if (CullResults.Cull(camera, renderLoop, out cullResults))
                    ExecuteRenderLoop(camera, cullResults, renderLoop);
            }
        }

        void ExecuteRenderLoop(Camera camera, CullResults cullResults, RenderLoop loop)
        {
            // do anything we need to do upon a new frame.
            NewFrame();

            //m_DeferredMaterial.SetInt("_SrcBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.DstColor);
            //m_DeferredMaterial.SetInt("_DstBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.Zero);
            //m_DeferredReflectionMaterial.SetInt("_SrcBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.DstColor);
            //m_DeferredReflectionMaterial.SetInt("_DstBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.Zero);
            loop.SetupCameraProperties(camera);
            RenderGBuffer(cullResults, camera, loop);

            //@TODO: render forward-only objects into depth buffer
            CopyDepthAfterGBuffer(loop);
            //@TODO: render reflection probes

            //RenderLighting(camera, inputs, loop);

            //
            Matrix4x4 proj = camera.projectionMatrix;
            Matrix4x4 temp = new Matrix4x4();
            temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
            temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
            temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
            temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            Matrix4x4 projh = temp * proj;
            Matrix4x4 invProjh = projh.inverse;

            int iW = camera.pixelWidth;
            int iH = camera.pixelHeight;

            temp.SetRow(0, new Vector4(0.5f * iW, 0.0f, 0.0f, 0.5f * iW));
            temp.SetRow(1, new Vector4(0.0f, 0.5f * iH, 0.0f, 0.5f * iH));
            temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
            temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            Matrix4x4 projscr = temp * proj;
            Matrix4x4 invProjscr = projscr.inverse;


            int numLights = GenerateSourceLightBuffers(camera, cullResults);


            int nrTilesX = (iW + 15) / 16;
            int nrTilesY = (iH + 15) / 16;
            //ComputeBuffer lightList = new ComputeBuffer(nrTilesX * nrTilesY * (32 / 2), sizeof(uint));


            var cmd = new CommandBuffer();

            cmd.SetComputeIntParam(m_BuildScreenAABBShader, "g_iNrVisibLights", numLights);
            SetMatrixCS(cmd, m_BuildScreenAABBShader, "g_mProjection", projh);
            SetMatrixCS(cmd, m_BuildScreenAABBShader, "g_mInvProjection", invProjh);
            cmd.SetComputeBufferParam(m_BuildScreenAABBShader, kGenAABBKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
            cmd.ComputeDispatch(m_BuildScreenAABBShader, kGenAABBKernel, (numLights + 7) / 8, 1, 1);

            cmd.SetComputeIntParam(m_BuildPerTileLightListShader, "g_iNrVisibLights", numLights);
            SetMatrixCS(cmd, m_BuildPerTileLightListShader, "g_mScrProjection", projscr);
            SetMatrixCS(cmd, m_BuildPerTileLightListShader, "g_mInvScrProjection", invProjscr);
            cmd.SetComputeTextureParam(m_BuildPerTileLightListShader, kGenListPerTileKernel, "g_depth_tex", new RenderTargetIdentifier(kCameraDepthTexture));
            cmd.SetComputeBufferParam(m_BuildPerTileLightListShader, kGenListPerTileKernel, "g_vLightList", lightList);
            cmd.ComputeDispatch(m_BuildPerTileLightListShader, kGenListPerTileKernel, nrTilesX, nrTilesY, 1);

            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            DoTiledDeferredLighting(loop, camera.cameraToWorldMatrix, projscr, invProjscr, lightList);


            //lightList.Release();

            loop.Submit();
        }

        void NewFrame()
        {
            // update texture caches
            m_cookieTexArray.NewFrame();
            m_cubeCookieTexArray.NewFrame();
            m_cubeReflTexArray.NewFrame();
        }
    }
}