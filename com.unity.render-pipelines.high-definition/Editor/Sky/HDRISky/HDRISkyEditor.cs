using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDRISky))]
    class HDRISkyEditor
        : SkySettingsEditor
    {
        SerializedDataParameter m_hdriSky;
        SerializedDataParameter m_UpperHemisphereLuxValue;
        SerializedDataParameter m_UpperHemisphereLuxColor;
        SerializedDataParameter m_EnableBackplate;
        SerializedDataParameter m_BackplateType;
        SerializedDataParameter m_GroundLevel;
        SerializedDataParameter m_Scale;
        SerializedDataParameter m_ProjectionDistance;
        SerializedDataParameter m_PlateRotation;
        SerializedDataParameter m_PlateTexRotation;
        SerializedDataParameter m_PlateTexOffset;
        SerializedDataParameter m_BlendAmount;
        SerializedDataParameter m_PointLightShadow;
        SerializedDataParameter m_DirLightShadow;
        SerializedDataParameter m_RectLightShadow;
        SerializedDataParameter m_ShadowTint;

        RTHandle m_IntensityTexture;
        Texture2D m_ReadBackTexture;
        Material m_CubeToHemiLatLong;
        Material m_CubeToLatLong;
        Material m_IntegrateMIS;
        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

            m_EnableLuxIntensityMode = true;

            // HDRI sky does not have control over sun display.
            m_CommonUIElementsMask = 0xFFFFFFFF & ~(uint)(SkySettingsUIElement.IncludeSunInBaking);

            var o = new PropertyFetcher<HDRISky>(serializedObject);
            m_hdriSky                   = Unpack(o.Find(x => x.hdriSky));
            m_UpperHemisphereLuxValue   = Unpack(o.Find(x => x.upperHemisphereLuxValue));
            m_UpperHemisphereLuxColor   = Unpack(o.Find(x => x.upperHemisphereLuxColor));

            m_EnableBackplate           = Unpack(o.Find(x => x.enableBackplate));
            m_BackplateType             = Unpack(o.Find(x => x.backplateType));
            m_GroundLevel               = Unpack(o.Find(x => x.groundLevel));
            m_Scale                     = Unpack(o.Find(x => x.scale));
            m_ProjectionDistance        = Unpack(o.Find(x => x.projectionDistance));
            m_PlateRotation             = Unpack(o.Find(x => x.plateRotation));
            m_PlateTexRotation          = Unpack(o.Find(x => x.plateTexRotation));
            m_PlateTexOffset            = Unpack(o.Find(x => x.plateTexOffset));
            m_BlendAmount               = Unpack(o.Find(x => x.blendAmount));
            m_PointLightShadow          = Unpack(o.Find(x => x.pointLightShadow));
            m_DirLightShadow            = Unpack(o.Find(x => x.dirLightShadow));
            m_RectLightShadow           = Unpack(o.Find(x => x.rectLightShadow));
            m_ShadowTint                = Unpack(o.Find(x => x.shadowTint));

            m_IntensityTexture = RTHandles.Alloc(1, 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat);
            var hdrp = HDRenderPipeline.defaultAsset;
            if (hdrp != null)
            {
                m_CubeToHemiLatLong = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToHemiPanoPS);
                m_CubeToLatLong     = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToPanoPS);
                //m_CubeToHemiLatLong = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToPanoPS);
                m_IntegrateMIS      = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.integrateHdriSkyMISPS);
                //m_IntegrateMIS      = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.integrateHdriSkyPS);
            }
            m_ReadBackTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, false);
        }

        public override void OnDisable()
        {
            if (m_IntensityTexture != null)
                RTHandles.Release(m_IntensityTexture);

            m_ReadBackTexture = null;
        }

        // Compute the lux value in the upper hemisphere of the HDRI skybox
        public void GetUpperHemisphereLuxValue()
        {
            Cubemap hdri = m_hdriSky.value.objectReferenceValue as Cubemap;
            if (hdri == null || m_CubeToHemiLatLong == null)
                return;

            // Render LatLong
            RTHandle latLongMap = RTHandles.Alloc(  4*hdri.width, hdri.width,
                                                    colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                                                    enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(latLongMap);
            m_CubeToHemiLatLong.SetTexture  ("_srcCubeTexture",             hdri);
            m_CubeToHemiLatLong.SetInt      ("_cubeMipLvl",                 0);
            m_CubeToHemiLatLong.SetInt      ("_cubeArrayIndex",             0);
            m_CubeToHemiLatLong.SetInt      ("_buildPDF",                   0);
            m_CubeToHemiLatLong.SetInt      ("_preMultiplyByJacobian",      1);
            m_CubeToHemiLatLong.SetInt      ("_preMultiplyByCosTheta",      1);
            m_CubeToHemiLatLong.SetInt      ("_preMultiplyBySolidAngle",    0);
            m_CubeToHemiLatLong.SetVector   (HDShaderIDs._Sizes, new Vector4(      (float)latLongMap.rt.width,        (float)latLongMap.rt.height,
                                                                             1.0f/((float)latLongMap.rt.width), 1.0f/((float)latLongMap.rt.height)));
            Graphics.Blit(Texture2D.whiteTexture, latLongMap.rt, m_CubeToHemiLatLong, 0);

            Texture2D flatten = new Texture2D(latLongMap.rt.width, latLongMap.rt.height, latLongMap.rt.graphicsFormat, TextureCreationFlags.None);

            RenderTexture.active = latLongMap.rt;
            flatten.ReadPixels(new Rect(0.0f, 0.0f, latLongMap.rt.width, latLongMap.rt.height), 0, 0);
            RenderTexture.active = null;
            {
                byte[] bytes0 = ImageConversion.EncodeToEXR(flatten, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\_____Flatten.exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
            }

            flatten = null;

            RTHandle totalRows = GPUScan.ComputeOperation(latLongMap, null, GPUScan.Operation.Total, GPUScan.Direction.Horizontal, latLongMap.rt.graphicsFormat);
            RTHandle totalCols = GPUScan.ComputeOperation(totalRows,  null, GPUScan.Operation.Total, GPUScan.Direction.Vertical,   latLongMap.rt.graphicsFormat);
            RTHandleDeleter.ScheduleRelease(totalRows);
            RTHandleDeleter.ScheduleRelease(totalCols);
            /*
            RTHandle totalCols = RTHandles.Alloc(   1, 1,
                                                    colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                                                    enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(totalCols);

            m_IntegrateMIS.SetTexture("_Cubemap", hdri);
            Graphics.Blit(Texture2D.whiteTexture, totalCols.rt, m_IntegrateMIS, 0);
            */
            RenderTexture.active = totalCols.rt;
            m_ReadBackTexture.ReadPixels(new Rect(0.0f, 0.0f, 1.0f, 1.0f), 0, 0);
            RenderTexture.active = null;

            // And then the value inside this texture
            Color hdriIntensity = m_ReadBackTexture.GetPixel(0, 0);
            //m_UpperHemisphereLuxValue.value.floatValue = hdriIntensity.a;
            //m_UpperHemisphereLuxValue.value.floatValue = Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b);
            m_UpperHemisphereLuxValue.value.floatValue =          Mathf.PI*Mathf.PI*Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b)
                                                        / // ----------------------------------------------------------------------------------------
                                                                            (4.0f*(float)(latLongMap.rt.width*latLongMap.rt.height));
                                                                //(Mathf.PI*Mathf.PI*(1.0f/(float)(latLongMap.rt.width*latLongMap.rt.height))*0.25f);
            float max = Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b);
            if (max == 0.0f)
                max = 1.0f;
            m_UpperHemisphereLuxColor.value.vector3Value = new Vector3(hdriIntensity.r/max, hdriIntensity.g/max, hdriIntensity.b/max);
            m_UpperHemisphereLuxColor.value.vector3Value *= 0.5f; // Arbitrary 50% to not have too dark or too bright shadow
        }

        static bool bUpdate = false;
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                PropertyField(m_hdriSky);
                base.CommonSkySettingsGUI();
            }
            Cubemap hdri = m_hdriSky.value.objectReferenceValue as Cubemap;
            bool updateDefaultShadowTint = false;
            const bool buildHemisphere = true;
            int hdriID = ImportanceSamplers.GetIdentifier(hdri, buildHemisphere);
            if (EditorGUI.EndChangeCheck())
            {
                //ImportanceSamplers.ScheduleMarginalGeneration(hdriID, hdri);
                //ImportanceSamplers.ScheduleMarginalGeneration(hdriID, hdri, buildHemisphere);
                GetUpperHemisphereLuxValue();
                //bUpdate = true;
                updateDefaultShadowTint = true;
            }
            if (false && bUpdate && ImportanceSamplers.ExistAndReady(hdriID))
            {
                var hdrp = HDRenderPipeline.defaultAsset;
                if (hdrp == null)
                    return;
                bUpdate = false;
                ImportanceSamplersSystem.MarginalTextures marginals = ImportanceSamplers.GetMarginals(hdriID);
                RTHandle marginal       = marginals.marginal;
                RTHandle condMarginal   = marginals.conditionalMarginal;
                {
                    Texture2D margTex = new Texture2D(marginal.rt.width, marginal.rt.height, marginal.rt.graphicsFormat, TextureCreationFlags.None);
                    RenderTexture.active = marginal.rt;
                    margTex.ReadPixels(new Rect(0.0f, 0.0f, marginal.rt.width, marginal.rt.height), 0, 0);
                    RenderTexture.active = null;
                    {
                        byte[] bytes0 = ImageConversion.EncodeToEXR(margTex, Texture2D.EXRFlags.CompressZIP);
                        string path = @"C:\UProjects\_____00000Marginals.exr";
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                            System.IO.File.Delete(path);
                        }
                        System.IO.File.WriteAllBytes(path, bytes0);
                    }
                }

                {
                    Texture2D condMargTex = new Texture2D(condMarginal.rt.width, condMarginal.rt.height, condMarginal.rt.graphicsFormat, TextureCreationFlags.None);
                    RenderTexture.active = condMarginal.rt;
                    condMargTex.ReadPixels(new Rect(0.0f, 0.0f, condMarginal.rt.width, condMarginal.rt.height), 0, 0);
                    RenderTexture.active = null;
                    {
                        byte[] bytes0 = ImageConversion.EncodeToEXR(condMargTex, Texture2D.EXRFlags.CompressZIP);
                        string path = @"C:\UProjects\_____00000ConditionalMarginals.exr";
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                            System.IO.File.Delete(path);
                        }
                        System.IO.File.WriteAllBytes(path, bytes0);
                    }
                }

                int samplesCount = 4*4096;

                // Generate Important Sampled Samples
                RTHandle samples = ImportanceSampler2D.GenerateSamples((uint)samplesCount, marginal, condMarginal, GPUOperation.Direction.Horizontal, null, true);
                Texture2D samplesTex = new Texture2D(samples.rt.width, samples.rt.height, samples.rt.graphicsFormat, TextureCreationFlags.None);
                RenderTexture.active = samples.rt;
                samplesTex.ReadPixels(new Rect(0.0f, 0.0f, samples.rt.width, samples.rt.height), 0, 0);
                RenderTexture.active = null;
                {
                    byte[] bytes0 = ImageConversion.EncodeToEXR(samplesTex, Texture2D.EXRFlags.CompressZIP);
                    string path = @"C:\UProjects\_____00000Samples.exr";
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                        System.IO.File.Delete(path);
                    }
                    System.IO.File.WriteAllBytes(path, bytes0);
                }

                {
                    // Cubemap to EquiRectangular
                    RTHandle latLongMap;
                    {
                        latLongMap = RTHandles.Alloc(   4*hdri.width, (buildHemisphere ? 1 : 2)*hdri.width,
                                                        colorFormat: GraphicsFormat.R32_SFloat,
                                                        enableRandomWrite: true);
                        RTHandleDeleter.ScheduleRelease(latLongMap);
                        m_CubeToHemiLatLong.SetTexture  ("_srcCubeTexture",             hdri);
                        m_CubeToHemiLatLong.SetInt      ("_cubeMipLvl",                 0);
                        m_CubeToHemiLatLong.SetInt      ("_cubeArrayIndex",             0);
                        m_CubeToHemiLatLong.SetInt      ("_buildPDF",                   0);
                        m_CubeToHemiLatLong.SetInt      ("_preMultiplyByJacobian",      1);
                        m_CubeToHemiLatLong.SetInt      ("_preMultiplyByCosTheta",      1);
                        m_CubeToHemiLatLong.SetInt      ("_preMultiplyBySolidAngle",    0);
                        m_CubeToHemiLatLong.SetVector   (HDShaderIDs._Sizes, new Vector4(      (float)latLongMap.rt.width,        (float)latLongMap.rt.height,
                                                                                         1.0f/((float)latLongMap.rt.width), 1.0f/((float)latLongMap.rt.height)));
                        Graphics.Blit(Texture2D.whiteTexture, latLongMap.rt, m_CubeToHemiLatLong, 0);
                    }
                    Texture2D latLongMapTex = new Texture2D(latLongMap.rt.width, latLongMap.rt.height, latLongMap.rt.graphicsFormat, TextureCreationFlags.None);

                    RenderTexture.active = latLongMap.rt;
                    latLongMapTex.ReadPixels(new Rect(0.0f, 0.0f, latLongMap.rt.width, latLongMap.rt.height), 0, 0);
                    RenderTexture.active = null;
                    {
                        byte[] bytes0 = ImageConversion.EncodeToEXR(latLongMapTex, Texture2D.EXRFlags.CompressZIP);
                        string path = @"C:\UProjects\_____00000LatLong.exr";
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                            System.IO.File.Delete(path);
                        }
                        System.IO.File.WriteAllBytes(path, bytes0);
                    }
                    // Debug Output Textures
                    RTHandle m_OutDebug = RTHandles.Alloc(latLongMap.rt.width, latLongMap.rt.height, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true);
                    RTHandleDeleter.ScheduleRelease(m_OutDebug);

                    RTHandle black = RTHandles.Alloc(Texture2D.blackTexture);
                    GPUArithmetic.ComputeOperation(m_OutDebug, latLongMap, black, null, GPUArithmetic.Operation.Add);
                    RTHandleDeleter.ScheduleRelease(black);

                    RenderTexture.active = m_OutDebug.rt;
                    latLongMapTex.ReadPixels(new Rect(0.0f, 0.0f, latLongMap.rt.width, latLongMap.rt.height), 0, 0);
                    RenderTexture.active = null;
                    {
                        byte[] bytes0 = ImageConversion.EncodeToEXR(latLongMapTex, Texture2D.EXRFlags.CompressZIP);
                        string path = @"C:\UProjects\_____00000LatLongMAD.exr";
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                            System.IO.File.Delete(path);
                        }
                        System.IO.File.WriteAllBytes(path, bytes0);
                    }

                    ComputeShader outputDebug2D = hdrp.renderPipelineResources.shaders.OutputDebugCS;

                    int kernel = outputDebug2D.FindKernel("CSMain");

                    outputDebug2D.SetTexture(kernel, HDShaderIDs._PDF,      latLongMap);
                    outputDebug2D.SetTexture(kernel, HDShaderIDs._Output,   m_OutDebug);
                    outputDebug2D.SetTexture(kernel, HDShaderIDs._Samples,  samples);
                    outputDebug2D.SetInts   (HDShaderIDs._Sizes,
                                             latLongMap.rt.width, latLongMap.rt.height, samples.rt.width, 1);

                    int numTilesX = (samples.rt.width + (8 - 1))/8;
                    outputDebug2D.Dispatch(kernel, numTilesX, 1, 1);

                    Texture2D debugTex = new Texture2D(m_OutDebug.rt.width, m_OutDebug.rt.height, m_OutDebug.rt.graphicsFormat, TextureCreationFlags.None);

                    RenderTexture.active = m_OutDebug.rt;
                    debugTex.ReadPixels(new Rect(0.0f, 0.0f, m_OutDebug.rt.width, m_OutDebug.rt.height), 0, 0);
                    RenderTexture.active = null;
                    {
                        byte[] bytes0 = ImageConversion.EncodeToEXR(debugTex, Texture2D.EXRFlags.CompressZIP);
                        string path = @"C:\UProjects\_____00000LatLongWithSamples.exr";
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                            System.IO.File.Delete(path);
                        }
                        System.IO.File.WriteAllBytes(path, bytes0);
                    }
                }

                RTHandle integrals = RTHandles.Alloc(samplesCount, 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true);
                RTHandleDeleter.ScheduleRelease(integrals);

                m_IntegrateMIS.SetTexture   ("_Cubemap",             hdri);
                m_IntegrateMIS.SetTexture   ("_Marginal",            marginal);
                m_IntegrateMIS.SetTexture   ("_ConditionalMarginal", condMarginal);
                m_IntegrateMIS.SetTexture   ("_Integral",            marginals.integral);
                m_IntegrateMIS.SetInt       ("_SamplesCount",        samplesCount);
                m_IntegrateMIS.SetVector    ("_Sizes", new Vector4(condMarginal.rt.height, 1.0f/condMarginal.rt.height, 0.5f/condMarginal.rt.height, samplesCount));
                Graphics.Blit(Texture2D.whiteTexture, integrals.rt, m_IntegrateMIS, 0);

                RTHandle integral = GPUScan.ComputeOperation(integrals, null, GPUScan.Operation.Total, GPUScan.Direction.Horizontal, integrals.rt.graphicsFormat);
                RTHandleDeleter.ScheduleRelease(integral);

                //RenderTexture.active = marginals.integral;
                RenderTexture.active = integral;
                m_ReadBackTexture.ReadPixels(new Rect(0.0f, 0.0f, 1.0f, 1.0f), 0, 0);
                RenderTexture.active = null;

                // And then the value inside this texture
                Color hdriIntensity = m_ReadBackTexture.GetPixel(0, 0);
                //Color hdriIntensity = Color.white;
                //m_UpperHemisphereLuxValue.value.floatValue = hdriIntensity.a;
                float coef =                            1.0f
                            / // -------------------------------------------------------------------
                                    (4.0f*(float)(hdri.width*3));

                m_UpperHemisphereLuxValue.value.floatValue = /*4.0f*coef*/coef*Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b);
                //m_UpperHemisphereLuxValue.value.floatValue =        Mathf.PI*Mathf.PI*Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b)
                //                                            / // ----------------------------------------------------------------------------------------
                //                                                            (4.0f*(float)(condMarginal.rt.width*condMarginal.rt.height));
                float max = Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b);
                if (max == 0.0f)
                    max = 1.0f;
                m_UpperHemisphereLuxColor.value.vector3Value = new Vector3(hdriIntensity.r/max, hdriIntensity.g/max, hdriIntensity.b/max);
                m_UpperHemisphereLuxColor.value.vector3Value *= 0.5f; // Arbitrary 50% to not have too dark or too bright shadow
                updateDefaultShadowTint = true;
            }
            else
            {
                ImportanceSamplers.ScheduleMarginalGeneration(hdriID, hdri, true);
            }

            if (isInAdvancedMode)
            {
                PropertyField(m_EnableBackplate, new GUIContent("Backplate", "Enable the projection of the bottom of the CubeMap on a plane with a given shape ('Disc', 'Rectangle', 'Ellispe', 'Infinite')"));
                EditorGUILayout.Space();
                if (m_EnableBackplate.value.boolValue)
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_BackplateType, new GUIContent("Type"));
                    bool constraintAsCircle = false;
                    if (m_BackplateType.value.enumValueIndex == (uint)BackplateType.Disc)
                    {
                        constraintAsCircle = true;
                    }
                    PropertyField(m_GroundLevel);
                    if (m_BackplateType.value.enumValueIndex != (uint)BackplateType.Infinite)
                    {
                        EditorGUI.BeginChangeCheck();
                        PropertyField(m_Scale);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (m_Scale.value.vector2Value.x < 0.0f || m_Scale.value.vector2Value.y < 0.0f)
                            {
                                m_Scale.value.vector2Value = new Vector2(Mathf.Abs(m_Scale.value.vector2Value.x), Mathf.Abs(m_Scale.value.vector2Value.x));
                            }
                        }
                        if (constraintAsCircle)
                        {
                            m_Scale.value.vector2Value = new Vector2(m_Scale.value.vector2Value.x, m_Scale.value.vector2Value.x);
                        }
                        else if (m_BackplateType.value.enumValueIndex == (uint)BackplateType.Ellipse &&
                                 Mathf.Abs(m_Scale.value.vector2Value.x - m_Scale.value.vector2Value.y) < 1e-4f)
                        {
                            m_Scale.value.vector2Value = new Vector2(m_Scale.value.vector2Value.x, m_Scale.value.vector2Value.x + 1e-4f);
                        }
                    }
                    PropertyField(m_ProjectionDistance, new GUIContent("Projection"));
                    PropertyField(m_PlateRotation, new GUIContent("Rotation"));
                    PropertyField(m_PlateTexRotation, new GUIContent("Texture Rotation"));
                    PropertyField(m_PlateTexOffset, new GUIContent("Texture Offset"));
                    if (m_BackplateType.value.enumValueIndex != (uint)BackplateType.Infinite)
                        PropertyField(m_BlendAmount);
                    PropertyField(m_PointLightShadow, new GUIContent("Point/Spot Shadow"));
                    PropertyField(m_DirLightShadow, new GUIContent("Directional Shadow"));
                    PropertyField(m_RectLightShadow, new GUIContent("Area Shadow"));
                    PropertyField(m_ShadowTint);
                    if (updateDefaultShadowTint || GUILayout.Button("Reset Color"))
                    {
                        m_ShadowTint.value.colorValue = new Color(m_UpperHemisphereLuxColor.value.vector3Value.x, m_UpperHemisphereLuxColor.value.vector3Value.y, m_UpperHemisphereLuxColor.value.vector3Value.z);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
