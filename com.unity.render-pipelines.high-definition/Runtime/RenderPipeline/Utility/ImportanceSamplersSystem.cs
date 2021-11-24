using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// System managing a set of RTHandle textures for
    /// </summary>
    public partial class ImportanceSamplersSystem : IDisposable
    {
        /// <summary>
        /// Information needed to process Importance Sampling
        /// If the input used was Cubemap of CubemapArray the Marginals information was for Equirectangular
        /// The user have to convert LatLong (IR^2) to Direction (IR^3)
        /// HLSL: LatlongToDirectionCoordinate(uv)
        /// </summary>
        public class MarginalTextures
        {
            /// <summary>
            /// Marginal texture of the sum of columns (1 x Height) {R: InvCDF, G:PDF, B:CDF}
            /// PDF on the green channel, is only premultiplied with Jacobian (multiplied by Cosine if hemisphere)
            /// </summary>
            public RTHandle     marginal            = null;
            /// <summary>
            /// Conditional Marginal texture of each rows (Width x Height) {R: InvCDF, G:PDF, B:CDF}
            /// PDF on the green channel, is only premultiplied with Jacobian (multiplied by Cosine if hemisphere)
            /// </summary>
            public RTHandle     conditionalMarginal = null;
            /// <summary>
            /// Integral without the Solid angle, if hemisphere: premultiplied with the Jacobian cosined weighted, otherwise only premultiplied with the Jacobian
            /// </summary>
            public RTHandle     integral            = null;
        }

        /// <summary>
        /// Marginal informations set of informations needed for the time sliced generation of Marginals
        /// </summary>
        internal class MarginalInfos
        {
            /// <summary>
            /// Input texture scheduled for Importance Sampling Generator
            /// Supported Dimension: {Texture2D, Texture2DArray, Cubemap, CubemapArray}, with or without Mips
            /// If the input is a Cubemap/CubemapArray internally the Marginal will be generated for Equirectangular
            /// (or HemiEquirectangular if buildHemisphere is true) projection
            /// </summary>
            public Texture          input               = null;
            /// <summary>
            /// Informations needed to perform Importance Sampling
            /// </summary>
            public MarginalTextures marginals           = null;
            /// <summary>
            /// Current slice progressed (information used only if inProgress is true)
            /// </summary>
            public int              currentSlice        = 0;
            /// <summary>
            /// Current mip progressed (information used only if inProgress is true)
            /// </summary>
            public int              currentMip          = 0;
            /// <summary>
            /// Current age of Marginal Generation scheduled (schedule generation when age is 0)
            /// </summary>
            public int              age                 = -8;
            /// <summary>
            /// Indicator if the Marginals are available for binding
            /// </summary>
            public bool             isReady             = false;
            /// <summary>
            /// Indicator if the Marginals are Work in Progress (Process time sliced)
            /// </summary>
            public bool             inProgress          = false;
            /// <summary>
            /// true to build hemisphere cosine weighted hemisphere
            /// </summary>
            public bool             buildHemisphere     = false;
        }

        internal static List<int>                       m_Keys          = new List<int>(4);
        internal static Dictionary<int, MarginalInfos>  m_InternalData  = new Dictionary<int, MarginalInfos>(4);

        /// <summary>
        /// RTHandleSystem constructor.
        /// </summary>
        public ImportanceSamplersSystem()
        {
        }

        /// <summary>
        /// Disposable pattern implementation.
        /// </summary>
        public void Dispose()
        {
            // TODO
        }

        /// <summary>
        /// Check if an Importance Sampling exist (generated or schedule for generation).
        /// </summary>
        /// <param name="identifier">Unique ID to identify the marginals.</param>
        public bool Exist(int identifier)
        {
            return m_InternalData.ContainsKey(identifier);
        }

        /// <summary>
        /// Check if an Importance Sampling exist & ready (generated or schedule for generation).
        /// </summary>
        /// <param name="identifier">Unique ID to identify the marginals.</param>
        public bool ExistAndReady(int identifier)
        {
            return Exist(identifier) && m_InternalData[identifier].isReady;
        }

        /// <summary>
        /// Getter for marginal textures.
        /// </summary>
        /// <param name="identifier">Unique ID to identify the marginals.</param>
        public MarginalTextures GetMarginals(int identifier)
        {
            if (Exist(identifier) == false)
            {
                return null;
            }

            if (m_InternalData[identifier].isReady)
            {
                return m_InternalData[identifier].marginals;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Schedule generation of the marginal textures. return if the task was scheduled
        /// </summary>
        /// <param name="identifier">Unique ID to identify this scheduling.</param>
        /// <param name="pdfTexture">Texture2D or CubeMap which used for the the generation of the important sampling.</param>
        /// <param name="buildHemisphere">if the pdfTexture is a Cubemap or a CubemapArray, buildHemisphere allow to enforce to build the marginals only for the Upper Hemisphere.</param>
        public bool ScheduleMarginalGeneration(int identifier, Texture pdfTexture, bool buildHemisphere = false)
        {
            if (Exist(identifier) == false)
            {
                return InternalScheduleMarginalGeneration(identifier, pdfTexture, buildHemisphere);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Schedule generation of the marginal textures. Even if the identifier already exist (always return true)
        /// </summary>
        /// <param name="identifier">Unique ID to identify this scheduling.</param>
        /// <param name="pdfTexture">Texture2D or CubeMap which used for the the generation of the important sampling.</param>
        /// <param name="buildHemisphere">if the pdfTexture is a Cubemap or a CubemapArray, buildHemisphere allow to enforce to build the marginals only for the Upper Hemisphere.</param>
        public bool ScheduleMarginalGenerationForce(int identifier, Texture pdfTexture, bool buildHemisphere = false)
        {
            InternalScheduleRelease(identifier);

            return InternalScheduleMarginalGeneration(identifier, pdfTexture, buildHemisphere);
        }

        /// <summary>
        /// Schedule generation of the marginal textures. Even if the identifier already exist (always return true)
        /// Internal use only
        /// </summary>
        /// <param name="identifier">Unique ID to identify this scheduling.</param>
        /// <param name="pdfTexture">Texture2D or CubeMap which used for the the generation of the important sampling.</param>
        internal bool InternalScheduleMarginalGeneration(int identifier, Texture pdfTexture, bool buildHemisphere = false)
        {
            MarginalInfos toGenerate = new MarginalInfos();
            toGenerate.marginals                        = new MarginalTextures();
            toGenerate.input                            = pdfTexture;
            toGenerate.marginals.marginal               = null;
            toGenerate.marginals.conditionalMarginal    = null;
            toGenerate.marginals.integral               = null;
            toGenerate.currentSlice                     = 0;
            toGenerate.currentMip                       = 0;
            toGenerate.age                              = -4;
            toGenerate.isReady                          = false;
            toGenerate.inProgress                       = false;
            if (pdfTexture.dimension == TextureDimension.Tex2D || pdfTexture.dimension == TextureDimension.Tex2DArray)
                toGenerate.buildHemisphere  = false;
            else
                toGenerate.buildHemisphere  = buildHemisphere;
            m_InternalData.Add(identifier, toGenerate);

            return true;
        }

        /// <summary>
        /// Schedule a release of Marginal Textures
        /// </summary>
        /// <param name="identifier">Unique ID to identify this Release.</param>
        public bool ScheduleRelease(int identifier)
        {
            return InternalScheduleRelease(identifier);
        }

        /// <summary>
        /// Schedule a release of Marginal Textures, Internal use only
        /// </summary>
        /// <param name="identifier">Unique ID to identify this Release.</param>
        internal bool InternalScheduleRelease(int identifier)
        {
            if (Exist(identifier))
            {
                MarginalInfos current = m_InternalData[identifier];
                RTHandlesDeleter.ScheduleRelease(current.marginals.marginal);
                RTHandlesDeleter.ScheduleRelease(current.marginals.conditionalMarginal);
                RTHandlesDeleter.ScheduleRelease(current.marginals.integral);

                m_InternalData.Remove(identifier);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Update the logics, done once per frame
        /// </summary>
        /// <param name="cmd">Command buffer provided to setup shader constants.</param>
        public void Update(CommandBuffer cmd)
        {
            m_Keys.Clear();
            m_Keys.InsertRange(0, m_InternalData.Keys);
            foreach (var key in m_Keys)
            {
                if (m_InternalData[key].input == null)
                {
                    m_InternalData.Remove(key);
                }
            }

            foreach (var cur in m_InternalData)
            {
                ++cur.Value.age;
                if (cur.Value.isReady == false || cur.Value.inProgress)
                {
                    // Do only one per frame
                    //      One slice & one mip per frame
                    if (cur.Value.age >= 0)
                    {
                        GenerateMarginals(cur, cmd);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Get the format used for internal process, Internal use only
        /// </summary>
        /// <param name="channelCount">Supported {1, 2, 4}.</param>
        /// <param name="isFullPrecision">isFullPrecision == true => float, half otherwise.</param>
        internal static GraphicsFormat GetFormat(int channelCount, bool isFullPrecision = false)
        {
            if (isFullPrecision)
            {
                if (channelCount == 1)
                    return GraphicsFormat.R32_SFloat;
                else if (channelCount == 2)
                    return GraphicsFormat.R32G32_SFloat;
                else if (channelCount == 4)
                    return GraphicsFormat.R32G32B32A32_SFloat;
                else
                    return GraphicsFormat.None;
            }
            else
            {
                if (channelCount == 1)
                    return GraphicsFormat.R16_SFloat;
                else if (channelCount == 2)
                    return GraphicsFormat.R16G16_SFloat;
                else if (channelCount == 4)
                    return GraphicsFormat.R16G16B16A16_SFloat;
                else
                    return GraphicsFormat.None;
            }
        }

        /// <summary>
        /// Effective generation
        /// </summary>
        /// <param name="current">Informations needed to generate the marginal textures.</param>
        internal void GenerateMarginals(KeyValuePair<int, MarginalInfos> current, CommandBuffer cmd)
        {
            MarginalInfos value = current.Value;

            bool hasMip = value.input.mipmapCount > 1;
            bool isFullPrecision;
            //if (HDUtils.GetFormatMaxPrecisionBits(current.Value.input.graphicsFormat) == 32)
            // Full precision Mandatory
            {
                isFullPrecision = true;
            }
            //else
            //{
            //    isFullPrecision = false;
            //}

            GraphicsFormat format1 = GetFormat(1, isFullPrecision);
            GraphicsFormat format4 = GetFormat(4, isFullPrecision);

            int width       = -1;
            int height      = -1;
            int slicesCount =  1;

            bool buildMarginalArray = false;

            if (value.input.dimension == TextureDimension.Tex2D ||
                value.input.dimension == TextureDimension.Tex2DArray)
            {
                width   = value.input.width;
                height  = value.input.height;

                if (value.input.dimension == TextureDimension.Tex2DArray)
                {
                    slicesCount = (value.input as Texture2DArray).depth;
                    buildMarginalArray = true;
                }
            }
            else if (value.input.dimension == TextureDimension.Cube ||
                     value.input.dimension == TextureDimension.CubeArray)
            {
                // Latlong/equirectangular
                // TODO: Octahedral (Compute: Jacobian), Octahedral_ConstArea vs Octahedral_Isotropic
                width   = 4 * value.input.width;
                if (value.buildHemisphere)
                    height =   value.input.width;
                else
                    height = 2 * value.input.width;

                if (value.input.dimension == TextureDimension.CubeArray)
                {
                    slicesCount = (value.input as CubemapArray).cubemapCount;
                    buildMarginalArray = true;
                }
            }
            else
            {
                Debug.LogError("ImportanceSamplerSystem: Marginal texture generator only avaiable for Texture2D{Array?} or Cubemap{Array?}.");
            }

            RTHandle rtInput = RTHandles.Alloc(value.input);
            RTHandlesDeleter.ScheduleRelease(rtInput);

            RTHandle marginal               = null;
            RTHandle conditionalMarginal    = null;
            RTHandle integral               = null;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.BuildMaginalInternal)))
            {
                // Compute one slice & one mip per frame, we allocate the marginals once
                if (value.marginals.marginal == null)
                {
                    value.marginals.marginal =
                        RTHandles.Alloc(1, height, slices: slicesCount,
                            dimension: buildMarginalArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D,
                            colorFormat: format1, enableRandomWrite: true, useMipMap: hasMip, autoGenerateMips: false);
                }
                if (value.marginals.conditionalMarginal == null)
                {
                    value.marginals.conditionalMarginal =
                        RTHandles.Alloc(width, height, slices: slicesCount,
                            dimension: buildMarginalArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D,
                            colorFormat: format4, enableRandomWrite: true, useMipMap: hasMip, autoGenerateMips: false);
                }
                if (value.marginals.integral == null)
                {
                    value.marginals.integral =
                        RTHandles.Alloc(1, 1, slices: slicesCount,
                            dimension: buildMarginalArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D,
                            colorFormat: format1, enableRandomWrite: true, useMipMap: false, autoGenerateMips: false);
                }

                if (value.input.dimension == TextureDimension.Tex2D ||
                    value.input.dimension == TextureDimension.Tex2DArray)
                {
                    int curWidth  = Mathf.RoundToInt((float)width / Mathf.Pow(2.0f, (float)value.currentMip));
                    int curHeight = Mathf.RoundToInt((float)height / Mathf.Pow(2.0f, (float)value.currentMip));

                    // Begin: Integrate to have the proper PDF
                    if (current.Value.currentMip == 0)
                    {
                        RTHandle rowTotal = GPUScan.ComputeOperation(rtInput,  cmd, GPUScan.Operation.Total, GPUScan.Direction.Horizontal, rtInput.rt.graphicsFormat);
                        integral = GPUScan.ComputeOperation(rowTotal, cmd, GPUScan.Operation.Total, GPUScan.Direction.Vertical,   rtInput.rt.graphicsFormat);
                        RTHandlesDeleter.ScheduleRelease(rowTotal);
                        RTHandlesDeleter.ScheduleRelease(integral);
                    }
                    // End
                    RTHandle texCopy = RTHandles.Alloc(curWidth, curHeight,
                        colorFormat: format1,
                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false);
                    RTHandlesDeleter.ScheduleRelease(texCopy);
                    GPUArithmetic.ComputeOperation(texCopy, rtInput, null, cmd, GPUArithmetic.Operation.Mean);

                    ImportanceSampler2D.GenerateMarginals(out marginal, out conditionalMarginal, texCopy, cmd);
                }
                else if (value.input.dimension == TextureDimension.Cube ||
                         value.input.dimension == TextureDimension.CubeArray)
                {
                    int curWidth  = Mathf.RoundToInt((float)width / Mathf.Pow(2.0f, (float)value.currentMip));
                    int curHeight = Mathf.RoundToInt((float)height / Mathf.Pow(2.0f, (float)value.currentMip));

                    RTHandle latLongMap = RTHandles.Alloc(curWidth, curHeight,
                        colorFormat: format4,
                        enableRandomWrite: true);
                    RTHandlesDeleter.ScheduleRelease(latLongMap);

                    var hdrp = HDRenderPipelineGlobalSettings.instance;
                    Material usedMat;
                    if (value.buildHemisphere)
                        usedMat = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToHemiPanoPS);
                    else
                        usedMat = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToPanoPS);
                    if (value.input.dimension == TextureDimension.Cube)
                    {
                        usedMat.SetTexture(HDShaderIDs._SrcCubeTexture,         rtInput);
                    }
                    else
                    {
                        usedMat.SetTexture(HDShaderIDs._SrcCubeTextureArray,    rtInput);
                    }
                    usedMat.SetInt(HDShaderIDs._CubeMipLvl,               current.Value.currentMip);
                    usedMat.SetInt(HDShaderIDs._CubeArrayIndex,           current.Value.currentSlice);
                    usedMat.SetInt(HDShaderIDs._BuildPDF,                 0);
                    usedMat.SetInt(HDShaderIDs._PreMultiplyBySolidAngle,  0);
                    usedMat.SetInt(HDShaderIDs._PreMultiplyByCosTheta,    0);
                    usedMat.SetInt(HDShaderIDs._PreMultiplyByJacobian,    1);
                    usedMat.SetInt(HDShaderIDs._ManualTex2SRGB,           0);
                    usedMat.SetVector(HDShaderIDs._Sizes, new Vector4((float)latLongMap.rt.width,        (float)latLongMap.rt.height,
                        1.0f / ((float)latLongMap.rt.width), 1.0f / ((float)latLongMap.rt.height)));

                    cmd.Blit(Texture2D.whiteTexture, latLongMap, usedMat, value.input.dimension == TextureDimension.Cube ? 0 : 1);

                    RTHandle latLongMap1 = RTHandles.Alloc(curWidth, curHeight,
                        colorFormat: format1,
                        enableRandomWrite: true);
                    GPUArithmetic.ComputeOperation(latLongMap1, latLongMap, null, cmd, GPUArithmetic.Operation.Mean);

                    // Begin: Integrate to have the proper PDF
                    if (current.Value.currentMip == 0)
                    {
                        RTHandle totalRows = GPUScan.ComputeOperation(latLongMap1, cmd, GPUScan.Operation.Total, GPUScan.Direction.Horizontal, format1);
                        integral  = GPUScan.ComputeOperation(totalRows,   cmd, GPUScan.Operation.Total, GPUScan.Direction.Vertical,   format1);
                        RTHandlesDeleter.ScheduleRelease(totalRows);
                        RTHandlesDeleter.ScheduleRelease(integral);

                        float coef = Mathf.PI * Mathf.PI / ((float)(latLongMap1.rt.width * latLongMap1.rt.height));
                        if (current.Value.buildHemisphere)
                            coef *= 1.0f;
                        else
                            coef *= 2.0f;

                        GPUArithmetic.ComputeOperation(integral, integral, new Vector4(coef, coef, coef, coef), cmd, GPUArithmetic.Operation.Mult);
                    }
                    // End: Integrate Equirectangular Map

                    ImportanceSampler2D.GenerateMarginals(out marginal, out conditionalMarginal, latLongMap1, cmd);
                }
                else
                {
                    Debug.LogError("ImportanceSamplersSystem.GenerateMarginals, try to generate marginal texture for a non valid dimension (supported Tex2D, Tex2DArray, Cubemap, CubemapArray).");
                }
            }

            cmd.CopyTexture(marginal,            0, 0, value.marginals.marginal,            current.Value.currentSlice, current.Value.currentMip);
            cmd.CopyTexture(conditionalMarginal, 0, 0, value.marginals.conditionalMarginal, current.Value.currentSlice, current.Value.currentMip);
            if (current.Value.currentMip == 0)
                cmd.CopyTexture(integral, 0, 0, value.marginals.integral, current.Value.currentSlice, 0);

            if (current.Value.currentMip + 1 == value.input.mipmapCount)
            {
                if (current.Value.currentSlice + 1 == slicesCount)
                {
                    current.Value.inProgress    = false;
                    current.Value.isReady       = true;
                    current.Value.currentMip    = 0;
                    current.Value.currentSlice  = 0;
                }
                else
                {
                    current.Value.inProgress    = true;
                    current.Value.isReady       = false;
                }
            }
            else
            {
                current.Value.inProgress    = true;
                current.Value.isReady       = false;
            }

            current.Value.currentMip++;
            if (current.Value.currentMip == value.input.mipmapCount)
            {
                current.Value.currentSlice++;
                current.Value.currentMip = 0;
            }
        }
    }
}
