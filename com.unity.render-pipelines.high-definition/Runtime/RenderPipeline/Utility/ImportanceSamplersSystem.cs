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
        public class MarginalTextures
        {
            public RTHandle     marginal            = null;
            public RTHandle     conditionalMarginal = null;
        }

        internal class MarginalInfos
        {
            public Texture          input               = null;
            public MarginalTextures marginals           = null;
            public bool             isReady             = false;
            public bool             inProgress          = false;
            public bool             buildHemisphere     = false;
            public int              currentSlice        = 0;
            public int              currentMip          = 0;
        }

        internal static Dictionary<int, MarginalInfos>  m_InternalData = null;
        internal static int _Idx = 0;
        MaterialPropertyBlock m_MaterialBlock = null;

        /// <summary>
        /// RTHandleSystem constructor.
        /// </summary>
        public ImportanceSamplersSystem()
        {
            m_InternalData = new Dictionary<int, MarginalInfos>();
            m_MaterialBlock = new MaterialPropertyBlock();
            _Idx = 0;
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

        internal bool InternalScheduleMarginalGeneration(int identifier, Texture pdfTexture, bool buildHemisphere = false)
        {
            MarginalInfos toGenerate = new MarginalInfos();
            toGenerate.marginals                        = new MarginalTextures();
            toGenerate.input                            = pdfTexture;
            toGenerate.marginals.marginal               = null;
            toGenerate.marginals.conditionalMarginal    = null;
            toGenerate.isReady                          = false;
            toGenerate.inProgress                       = false;
            if (pdfTexture.dimension == TextureDimension.Tex2D || pdfTexture.dimension == TextureDimension.Tex2DArray)
                toGenerate.buildHemisphere  = false;
            else
                toGenerate.buildHemisphere  = buildHemisphere;
            toGenerate.currentSlice                     = 0;
            toGenerate.currentMip                       = 0;
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

        internal bool InternalScheduleRelease(int identifier)
        {
            if (Exist(identifier))
            {
                MarginalInfos current = m_InternalData[identifier];
                RTHandleDeleter.ScheduleRelease(current.marginals.marginal);
                RTHandleDeleter.ScheduleRelease(current.marginals.conditionalMarginal);

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
            foreach(var item in m_InternalData.Where(x => x.Value.input == null).ToList())
            {
                m_InternalData.Remove(item.Key);
            }

            foreach (var cur in m_InternalData)
            {
                if (cur.Value.isReady == false || cur.Value.inProgress)
                //if (cur.Value.input != null && cur.Value.input.isReadable)
                {
                    // Do only one per frame
                    //      One slice & one mip per frame
                    GenerateMarginals(cur, cmd);
                    break;
                }
            }
        }

        static private void DefaultDumper(AsyncGPUReadbackRequest request, string name, Experimental.Rendering.GraphicsFormat gfxFormat)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(
                                                    copy,
                                                    //Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                                                    format: gfxFormat,
                                                    (uint)request.width, (uint)request.height, 0,
                                                    Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\" + name + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
            }
        }

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
            if (HDUtils.GetFormatMaxPrecisionBits(current.Value.input.graphicsFormat) == 32)
            {
                isFullPrecision = true;
            }
            else
            {
                isFullPrecision = false;
            }

            GraphicsFormat format1 = GetFormat(1, isFullPrecision);
            GraphicsFormat format2 = GetFormat(2, isFullPrecision);
            GraphicsFormat format4 = GetFormat(4, isFullPrecision);

            int width       = -1;
            int height      = -1;
            int slicesCount =  1;

            bool dumpFile =
                false;
                //true;
            //value.currentMip == 0 && value.currentSlice == 0;
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
                width   = 4*value.input.width;
                if (value.buildHemisphere)
                    height = value.input.width;
                else
                    height = 2*value.input.width;

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
            RTHandleDeleter.ScheduleRelease(rtInput);
            //if (dumpFile)
            //{
            //    cmd.RequestAsyncReadback(rtInput, delegate (AsyncGPUReadbackRequest request)
            //    {
            //            //DefaultDumper(request, String.Format("___Marginal_{0}_{1}_{2}", _Idx, current.Value.currentSlice, current.Value.currentMip));
            //            DefaultDumper(request, "___InputsMarginal_" + _Idx + "_" + current.Value.currentSlice + "_" + current.Value.currentMip, rtInput.rt.graphicsFormat);
            //    });
            //}

            RTHandle invCDFRows = null;
            RTHandle invCDFFull = null;

            using (new ProfilingScope(cmd, new ProfilingSampler("BuildMarginalsInternal")))
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

                if (value.input.dimension == TextureDimension.Tex2D ||
                    value.input.dimension == TextureDimension.Tex2DArray)
                {
                    //ImportanceSampler2D.GenerateMarginals(out invCDFRows, out invCDFFull, rtInput, current.Value.currentSlice, current.Value.currentMip, cmd, dumpFile, _Idx);
                    int curWidth  = Mathf.RoundToInt((float)width /Mathf.Pow(2.0f, (float)value.currentMip));
                    int curHeight = Mathf.RoundToInt((float)height/Mathf.Pow(2.0f, (float)value.currentMip));

                    //RTHandle texCopy = RTHandles.Alloc( curWidth, curHeight,
                    //                                    colorFormat: rtInput.rt.graphicsFormat,
                    //                                    enableRandomWrite: true);
                    //RTHandleDeleter.ScheduleRelease(texCopy);
                    //cmd.CopyTexture(rtInput, texCopy);
                    ImportanceSampler2D.GenerateMarginals(out invCDFRows, out invCDFFull, /*texCopy*/rtInput, 0, 0, cmd, dumpFile, _Idx);
                }
                else if (value.input.dimension == TextureDimension.Cube ||
                         value.input.dimension == TextureDimension.CubeArray)
                {
                    int curWidth  = Mathf.RoundToInt((float)width /Mathf.Pow(2.0f, (float)value.currentMip));
                    int curHeight = Mathf.RoundToInt((float)height/Mathf.Pow(2.0f, (float)value.currentMip));

                    RTHandle latLongMap = RTHandles.Alloc(  curWidth, curHeight,
                                                            colorFormat: format1,
                                                            enableRandomWrite: true);
                    RTHandleDeleter.ScheduleRelease(latLongMap);

                    var hdrp = HDRenderPipeline.defaultAsset;
                    Material usedMat;
                    if (value.buildHemisphere)
                        usedMat = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToHemiPanoPS);
                            //cubeToHemiLatLong;
                    else
                        usedMat = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToPanoPS);
                            //cubeToLatLong;
                    if (value.input.dimension == TextureDimension.Cube)
                    {
                        usedMat.SetTexture("_srcCubeTexture",       rtInput);
                    }
                    else
                    {
                        usedMat.SetTexture("_srcCubeTextureArray",  rtInput);
                    }
                    usedMat.SetInt      ("_cubeMipLvl",               current.Value.currentMip);
                    usedMat.SetInt      ("_cubeArrayIndex",           current.Value.currentSlice);
                    usedMat.SetInt      ("_buildPDF",                 1);
                    usedMat.SetInt      ("_preMultiplyBySolidAngle",  0);
                    usedMat.SetInt      ("_preMultiplyByJacobian",    1);
                    if (value.buildHemisphere)
                        usedMat.SetInt("_preMultiplyByCosTheta", 1);
                    else
                        usedMat.SetInt("_preMultiplyByCosTheta", 0);
                    usedMat.SetVector   (HDShaderIDs._Sizes, new Vector4(      (float)latLongMap.rt.width,        (float)latLongMap.rt.height,
                                                                         1.0f/((float)latLongMap.rt.width), 1.0f/((float)latLongMap.rt.height)));
                    cmd.Blit(Texture2D.whiteTexture, latLongMap, usedMat, value.input.dimension == TextureDimension.Cube ? 0 : 1);
                    if (dumpFile)
                    {
                        cmd.RequestAsyncReadback(latLongMap, delegate (AsyncGPUReadbackRequest request)
                        {
                            DefaultDumper(
                                    request, "___FirstInput_" + current.Key.ToString() +
                                    "_" + _Idx + "_" + current.Value.currentSlice + "_" + current.Value.currentMip,
                                    latLongMap.rt.graphicsFormat);
                        });
                    }
                    // Begin: Integrate Equirectangular Map
                    RTHandle cdf = GPUScan.ComputeOperation(latLongMap,
                                                            cmd,
                                                            GPUScan.Operation.Add,
                                                            GPUScan.Direction.Horizontal,
                                                            format1);
                    RTHandleDeleter.ScheduleRelease(cdf);
                    RTHandle lastCol = RTHandles.Alloc(1, cdf.rt.height, enableRandomWrite: true, colorFormat: format1);
                    RTHandleDeleter.ScheduleRelease(lastCol);
                    cmd.CopyTexture(cdf, 0, 0, cdf.rt.width - 1, 0, 1, cdf.rt.height, lastCol, 0, 0, 0, 0);
                    RTHandle integral = GPUScan.ComputeOperation(latLongMap,
                                                                 cmd,
                                                                 GPUScan.Operation.Add,
                                                                 GPUScan.Direction.Vertical,
                                                                 format1);
                    RTHandleDeleter.ScheduleRelease(integral);
                    // Normalize the LatLong to have integral over sphere to be == 1
                    //GPUArithmetic.ComputeOperation(latLongMap, latLongMap, integral, cmd, GPUArithmetic.Operation.Div);
                    // End: Integrate Equirectangular Map

                    ImportanceSampler2D.GenerateMarginals(out invCDFRows, out invCDFFull, latLongMap, 0, 0, cmd, dumpFile, _Idx);
                }
                else
                {
                    Debug.LogError("ImportanceSamplersSystem.GenerateMarginals, try to generate marginal texture for a non valid dimension (supported Tex2D, Tex2DArray, Cubemap, CubemapArray).");
                }
            }

            cmd.CopyTexture(invCDFRows, 0, 0, value.marginals.marginal,            current.Value.currentSlice, current.Value.currentMip);
            cmd.CopyTexture(invCDFFull, 0, 0, value.marginals.conditionalMarginal, current.Value.currentSlice, current.Value.currentMip);

            if (current.Value.currentMip + 1 == value.input.mipmapCount)
            {
                if (current.Value.currentSlice + 1 == slicesCount)
                {
                    current.Value.inProgress    = false;
                    current.Value.isReady       = true;
                    current.Value.currentMip    = 0;
                    current.Value.currentSlice  = 0;
                    Debug.Log(String.Format("SKCode - Ready: {0}", current.Key));
                    if (dumpFile)
                    {
                        cmd.RequestAsyncReadback(invCDFRows, delegate (AsyncGPUReadbackRequest request)
                        {
                            //DefaultDumper(request, String.Format("___Marginal_{0}_{1}_{2}", _Idx, current.Value.currentSlice, current.Value.currentMip));
                            DefaultDumper(request, "___Marginal_" + _Idx + "_" + current.Value.currentSlice + "_" + current.Value.currentMip, invCDFRows.rt.graphicsFormat);
                        });
                        cmd.RequestAsyncReadback(invCDFFull, delegate (AsyncGPUReadbackRequest request)
                        {
                            //DefaultDumper(request, String.Format("___ConditionalMarginal_{0}_{1}_{2}", _Idx, current.Value.currentSlice, current.Value.currentMip));
                            DefaultDumper(request, "___ConditionalMarginal_" + _Idx + "_" + current.Value.currentSlice + "_" + current.Value.currentMip, invCDFFull.rt.graphicsFormat);
                        });
                    }
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
            //if (du)
            //{
            //    cmd.RequestAsyncReadback(invCDFRows, delegate (AsyncGPUReadbackRequest request)
            //    {
            //        //DefaultDumper(request, String.Format("___Marginal_{0}_{1}_{2}", _Idx, current.Value.currentSlice, current.Value.currentMip));
            //        DefaultDumper(request, "___Marginal_" + _Idx + "_" + current.Value.currentSlice + "_" + current.Value.currentMip, invCDFRows.rt.graphicsFormat);
            //    });
            //    cmd.RequestAsyncReadback(invCDFFull, delegate (AsyncGPUReadbackRequest request)
            //    {
            //        //DefaultDumper(request, String.Format("___ConditionalMarginal_{0}_{1}_{2}", _Idx, current.Value.currentSlice, current.Value.currentMip));
            //        DefaultDumper(request, "___ConditionalMarginal_" + _Idx + "_" + current.Value.currentSlice + "_" + current.Value.currentMip, invCDFFull.rt.graphicsFormat);
            //    });
            //}
            if (current.Value.currentMip == value.input.mipmapCount)
            {
                current.Value.currentSlice++;
                current.Value.currentMip = 0;
            }
            ++_Idx;
        }
    }
}
