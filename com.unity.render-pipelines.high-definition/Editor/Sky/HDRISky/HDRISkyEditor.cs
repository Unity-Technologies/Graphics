using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

using System;
using System.Collections.Generic;

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
        Material m_IntegrateSphereHDRISky;
        ComputeShader m_ImportanceSamplingFromSamples;

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
                m_CubeToHemiLatLong         = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToHemiPanoPS);
                m_CubeToLatLong             = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToPanoPS);
                m_IntegrateSphereHDRISky    = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.integrateHdriSkyPS);
                m_ImportanceSamplingFromSamples = hdrp.renderPipelineResources.shaders.ImportanceSamplingFromSamplesCS;
            }
            m_ReadBackTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, false);
        }

        public override void OnDisable()
        {
            if (m_IntensityTexture != null)
                RTHandles.Release(m_IntensityTexture);

            m_ReadBackTexture = null;
        }

        void WriteEXR(string name, Texture2D img)
        {
            byte[] bytes0 = ImageConversion.EncodeToEXR(img, Texture2D.EXRFlags.CompressZIP);
            string path = @"C:\UProjects\" + name + ".exr";
            if (System.IO.File.Exists(path))
            {
                System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                System.IO.File.Delete(path);
            }
            System.IO.File.WriteAllBytes(path, bytes0);
        }

        void WriteEXR(string name, RTHandle img)
        {
            Texture2D tex = new Texture2D(img.rt.width, img.rt.height, img.rt.graphicsFormat, TextureCreationFlags.None);
            RenderTexture.active = img.rt;
            tex.ReadPixels(new Rect(0.0f, 0.0f, img.rt.width, img.rt.height), 0, 0);
            RenderTexture.active = null;

            if (name == "usedIntegral")
            {
                Color integral = tex.GetPixel(0, 0);

                Debug.Log(String.Format("SKCode: Computed Integral: {0}", integral.ToString()));
            }

            WriteEXR(name, tex);
            CoreUtils.Destroy(tex);
        }

        // Compute the lux value in the upper hemisphere of the HDRI skybox
        public void GetUpperHemisphereLuxValue()
        {
            Cubemap hdri = m_hdriSky.value.objectReferenceValue as Cubemap;
            if (hdri == null || m_CubeToHemiLatLong == null)
                return;

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

            //WriteEXR("CubeToPano", flatten);
            //WriteEXR("CubeToHemiPano", flatten);
            CoreUtils.Destroy(flatten);

            RTHandle totalRows = GPUScan.ComputeOperation(latLongMap, null, GPUScan.Operation.Total, GPUScan.Direction.Horizontal, latLongMap.rt.graphicsFormat);
            RTHandle totalCols = GPUScan.ComputeOperation(totalRows,  null, GPUScan.Operation.Total, GPUScan.Direction.Vertical,   latLongMap.rt.graphicsFormat);
            RTHandleDeleter.ScheduleRelease(totalRows);
            RTHandleDeleter.ScheduleRelease(totalCols);

            RenderTexture.active = totalCols.rt;
            m_ReadBackTexture.ReadPixels(new Rect(0.0f, 0.0f, 1.0f, 1.0f), 0, 0);
            RenderTexture.active = null;

            Color hdriIntensity = m_ReadBackTexture.GetPixel(0, 0);

            float coef   = (/*2.0f**/Mathf.PI*Mathf.PI)/((float)(latLongMap.rt.width*latLongMap.rt.height));
            float ref3   = (hdriIntensity.r + hdriIntensity.g + hdriIntensity.b)/3.0f;
            float maxRef = Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b);

            m_UpperHemisphereLuxValue.value.floatValue = coef*ref3;

            float max = Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b);
            if (max == 0.0f)
                max = 1.0f;

            m_UpperHemisphereLuxColor.value.vector3Value = new Vector3(hdriIntensity.r/max, hdriIntensity.g/max, hdriIntensity.b/max);
            m_UpperHemisphereLuxColor.value.vector3Value *= 0.5f; // Arbitrary 50% to not have too dark or too bright shadow
        }

        public void DoIt()
        {
            Cubemap hdri = m_hdriSky.value.objectReferenceValue as Cubemap;
            int hdriSkyID = ImportanceSamplers.GetIdentifier(hdri);
            if (ImportanceSamplers.ExistAndReady(hdriSkyID))
            {
                ImportanceSamplersSystem.MarginalTextures marginals = ImportanceSamplers.GetMarginals(hdriSkyID);

                int   samplesCount  = 256;
                float fSamplesCount = (float)samplesCount;

                WriteEXR("usedMarginal",            marginals.marginal);
                WriteEXR("usedConditionalMarginal", marginals.conditionalMarginal);
                WriteEXR("usedIntegral",            marginals.integral);

                RTHandle samples = ImportanceSampler2D.GenerateSamples((uint)samplesCount, marginals.marginal, marginals.conditionalMarginal, GPUScan.Direction.Horizontal, null);
                RTHandle output  = RTHandles.Alloc(samples.rt.width, 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true);
                RTHandleDeleter.ScheduleRelease(output);
                RTHandleDeleter.ScheduleRelease(samples);

                m_ImportanceSamplingFromSamples.SetTexture(0, HDShaderIDs._Samples,     samples);
                m_ImportanceSamplingFromSamples.SetTexture(0, HDShaderIDs._Cubemap,     hdri);
                m_ImportanceSamplingFromSamples.SetTexture(0, HDShaderIDs._Output,      output);
                m_ImportanceSamplingFromSamples.SetTexture(0, HDShaderIDs._Integral,    marginals.integral);
                m_ImportanceSamplingFromSamples.SetFloats (   HDShaderIDs._Sizes,       output.rt.width, output.rt.height, samples.rt.width, 1.0f);

                int numTilesX = (samplesCount + (8 - 1))/8;
                m_ImportanceSamplingFromSamples.Dispatch(0, numTilesX, 1, 1);

                RTHandle sum = GPUScan.ComputeOperation(output, null, GPUScan.Operation.Total, GPUScan.Direction.Horizontal, output.rt.graphicsFormat);
                RTHandleDeleter.ScheduleRelease(sum);

                RTHandle add = GPUScan.ComputeOperation(output, null, GPUScan.Operation.Add, GPUScan.Direction.Horizontal, output.rt.graphicsFormat);
                RTHandleDeleter.ScheduleRelease(add);

                Texture2D flatten = new Texture2D(1, 1, sum.rt.graphicsFormat, TextureCreationFlags.None);
                RenderTexture.active = sum.rt;
                flatten.ReadPixels(new Rect(0.0f, 0.0f, 1.0f, 1.0f), 0, 0);
                RenderTexture.active = null;

                Color hdriIntensity = flatten.GetPixel(0, 0);
                CoreUtils.Destroy(flatten);

                float coef   = 1.0f/fSamplesCount;
                float ref3   = (hdriIntensity.r + hdriIntensity.g + hdriIntensity.b)/3.0f;
                float maxRef = Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b);

                Debug.Log(String.Format("SKCode - Sphere - Integral - IS: {0} --- {1}", coef*ref3, coef*maxRef));

                //////////////////////////////////////////////////////////////////
                ///
                {
                    // 12. Generate Output
                    RTHandle latLongMap = RTHandles.Alloc(  4*hdri.width, 2*hdri.width,
                                                            colorFormat: GraphicsFormat.R32_SFloat,
                                                            enableRandomWrite: true);
                    RTHandleDeleter.ScheduleRelease(latLongMap);
                    m_CubeToLatLong.SetTexture  ("_srcCubeTexture",             hdri);
                    m_CubeToLatLong.SetInt      ("_cubeMipLvl",                 0);
                    m_CubeToLatLong.SetInt      ("_cubeArrayIndex",             0);
                    m_CubeToLatLong.SetInt      ("_buildPDF",                   1);
                    m_CubeToLatLong.SetInt      ("_preMultiplyByJacobian",      1);
                    m_CubeToLatLong.SetInt      ("_preMultiplyByCosTheta",      0);
                    m_CubeToLatLong.SetInt      ("_preMultiplyBySolidAngle",    0);
                    m_CubeToLatLong.SetVector   (HDShaderIDs._Sizes, new Vector4(      (float)latLongMap.rt.width,        (float)latLongMap.rt.height,
                                                                                     1.0f/((float)latLongMap.rt.width), 1.0f/((float)latLongMap.rt.height)));
                    Graphics.Blit(Texture2D.whiteTexture, latLongMap.rt, m_CubeToLatLong, 0);

                    RTHandle pdfCopyRGBA = RTHandles.Alloc(latLongMap.rt.width, latLongMap.rt.height, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true, useMipMap: false, autoGenerateMips: false);
                    RTHandle blackRT = RTHandles.Alloc(Texture2D.blackTexture);
                    RTHandleDeleter.ScheduleRelease(pdfCopyRGBA);
                    RTHandleDeleter.ScheduleRelease(blackRT);
                    GPUArithmetic.ComputeOperation(pdfCopyRGBA, latLongMap, blackRT, null, GPUArithmetic.Operation.Add);

                    var hdrp = HDRenderPipeline.defaultAsset;
                    ComputeShader outputDebug2D = hdrp.renderPipelineResources.shaders.outputDebugCS;
                    int kernel = outputDebug2D.FindKernel("CSMain");
                    outputDebug2D.SetTexture(kernel, HDShaderIDs._PDF,     pdfCopyRGBA);
                    outputDebug2D.SetTexture(kernel, HDShaderIDs._Output,  pdfCopyRGBA);
                    outputDebug2D.SetTexture(kernel, HDShaderIDs._Samples, samples);
                    outputDebug2D.SetInts   (HDShaderIDs._Sizes,
                                               latLongMap.rt.width, latLongMap.rt.height, samples.rt.width, 1);
                    outputDebug2D.Dispatch(kernel, (samplesCount + (8 - 1))/8, 1, 1);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                PropertyField(m_hdriSky);
                base.CommonSkySettingsGUI();
            }
            bool updateDefaultShadowTint = false;
            if (EditorGUI.EndChangeCheck())
            {
                GetUpperHemisphereLuxValue();
                updateDefaultShadowTint = true;
                //Cubemap hdri = m_hdriSky.value.objectReferenceValue as Cubemap;
                //int hdriSkyID = ImportanceSamplers.GetIdentifier(hdri);
                //ImportanceSamplers.ScheduleMarginalGeneration(hdriSkyID, hdri);
                //
                //DoIt();
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
