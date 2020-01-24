using System;
using System.Collections.Generic;
using System.Linq;

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
            public int              currentSlice        = 0;
            public int              currentMip          = 0;
        }

        internal static Dictionary<int, MarginalInfos>  m_InternalData;
        internal static int _Idx = 0;

        /// <summary>
        /// RTHandleSystem constructor.
        /// </summary>
        public ImportanceSamplersSystem()
        {
            m_InternalData  = new Dictionary<int, MarginalInfos>();
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
        public bool ScheduleMarginalGeneration(int identifier, Texture pdfTexture)
        {
            if (Exist(identifier) == false)
            {
                return InternalScheduleMarginalGeneration(identifier, pdfTexture);
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
        public bool ScheduleMarginalGenerationForce(int identifier, Texture pdfTexture)
        {
            if (Exist(identifier))
            {
                MarginalInfos current = m_InternalData[identifier];
                RTHandleDeleter.ScheduleRelease(current.marginals.marginal);
                RTHandleDeleter.ScheduleRelease(current.marginals.conditionalMarginal);

                m_InternalData.Remove(identifier);
            }

            return InternalScheduleMarginalGeneration(identifier, pdfTexture);
        }

        internal bool InternalScheduleMarginalGeneration(int identifier, Texture pdfTexture)
        {
            MarginalInfos toGenerate = new MarginalInfos();
            toGenerate.marginals                        = new MarginalTextures();
            toGenerate.input                            = pdfTexture;
            toGenerate.marginals.marginal               = null;
            toGenerate.marginals.conditionalMarginal    = null;
            toGenerate.isReady                          = false;
            toGenerate.inProgress                       = false;
            toGenerate.currentSlice                     = 0;
            toGenerate.currentMip                       = 0;
            m_InternalData.Add(identifier, toGenerate);

            return true;
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
                {
                    // Do only one per frame
                    //      One slice & one mip per frame
                    GenerateMarginals(cur, cmd);
                    break;
                }
            }
        }

        static private void DefaultDumper(AsyncGPUReadbackRequest request, string name)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(
                                                    copy,
                                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
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

        /// <summary>
        /// Effective generation
        /// </summary>
        /// <param name="current">Informations needed to generate the marginal textures.</param>
        internal void GenerateMarginals(KeyValuePair<int, MarginalInfos> current, CommandBuffer cmd)
        {
            MarginalInfos value = current.Value;

            bool hasMip = value.input.mipmapCount > 1;
            UnityEngine.Experimental.Rendering.GraphicsFormat internalFormat =
                Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
                //value.input.graphicsFormat;

            int width       = -1;
            int height      = -1;
            int slicesCount =  1;

            bool dumpFile = true;
                //value.currentMip == 0 && value.currentSlice == 0;

            if (value.input.dimension == TextureDimension.Tex2D ||
                value.input.dimension == TextureDimension.Tex2DArray)
            {
                width   = value.input.width;
                height  = value.input.height;

                if (value.input.dimension == TextureDimension.Tex2DArray)
                {
                    slicesCount = (value.input as Texture2DArray).depth;
                }
            }
            else if (value.input.dimension == TextureDimension.Cube ||
                     value.input.dimension == TextureDimension.CubeArray)
            {
                // Latlong/equirectangular
                // TODO: Octahedral (Compute: Jacobian), Octahedral_ConstArea vs Octahedral_Isotropic
                width   = value.input.width*4;
                height  = value.input.width*2;

                if (value.input.dimension == TextureDimension.CubeArray)
                {
                    slicesCount = (value.input as CubemapArray).cubemapCount;
                }
            }
            else
            {
                Debug.LogError("ImportanceSamplerSystem: Marginal texture generator only avaiable for Texture2D{Array?} or Cubemap{Array?}.");
            }

            // Compute one slice & one mip per frame, we allocate the marginals once
            if (value.marginals.marginal == null)
            {
                value.marginals.marginal =
                    RTHandles.Alloc(1, height, slices: slicesCount, colorFormat: internalFormat, enableRandomWrite: true, useMipMap: hasMip, autoGenerateMips: false);
            }
            if (value.marginals.conditionalMarginal == null)
            {
                value.marginals.conditionalMarginal =
                    RTHandles.Alloc(width, height, slices: slicesCount, colorFormat: internalFormat, enableRandomWrite: true, useMipMap: hasMip, autoGenerateMips: false);
            }

            ImportanceSampler2D generator = new ImportanceSampler2D();

            if (value.input.dimension == TextureDimension.Tex2D ||
                value.input.dimension == TextureDimension.Tex2DArray)
            {
                generator.Init(value.input, current.Value.currentSlice, current.Value.currentMip, cmd, dumpFile, _Idx);
            }
            else if (value.input.dimension == TextureDimension.Cube ||
                     value.input.dimension == TextureDimension.CubeArray)
            {
                int curWidth    = Mathf.RoundToInt((float)width /Mathf.Pow(2.0f, (float)value.currentMip));
                int curHeight   = Mathf.RoundToInt((float)height/Mathf.Pow(2.0f, (float)value.currentMip));

                RTHandle latLongMap = RTHandles.Alloc(  curWidth, curHeight,
                                                        colorFormat: internalFormat,
                                                        enableRandomWrite: true);
                RTHandleDeleter.ScheduleRelease(latLongMap);
                RTHandle cubemap = RTHandles.Alloc( current.Value.input.width, current.Value.input.height,
                                                    useMipMap: true,
                                                    autoGenerateMips: false,
                                                    dimension: TextureDimension.Cube,
                                                    colorFormat: value.input.graphicsFormat,
                                                    enableRandomWrite: true);
                RTHandleDeleter.ScheduleRelease(cubemap);
                for (int i = 0; i < 6; ++i)
                {
                    cmd.CopyTexture(value.input, 6*current.Value.currentSlice + i, cubemap, i);
                }

                var hdrp = HDRenderPipeline.defaultAsset;
                Material cubeToLatLong = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToPanoPS);
                cubeToLatLong.SetTexture("_srcCubeTexture", cubemap);
                cubeToLatLong.SetInt("_cubeMipLvl", current.Value.currentMip);
                cmd.RequestAsyncReadback(latLongMap, delegate (AsyncGPUReadbackRequest request)
                {
                    //DefaultDumper(request, String.Format("___FirstInput_{0}_{1}_{2}", _Idx, current.Value.currentSlice, current.Value.currentMip));
                    DefaultDumper(request, "___FirstInput_" + _Idx + "_" + current.Value.currentSlice + "_" + current.Value.currentMip);
                });
                //Debug.Log(String.Format("SKCode: {0}", _Idx));

                cmd.Blit(Texture2D.whiteTexture, latLongMap, cubeToLatLong, 0);

                generator.Init(latLongMap, 0, 0, cmd, dumpFile, _Idx);
            }

            cmd.CopyTexture(generator.invCDFRows, 0, 0, value.marginals.marginal,            current.Value.currentSlice, current.Value.currentMip);
            cmd.CopyTexture(generator.invCDFFull, 0, 0, value.marginals.conditionalMarginal, current.Value.currentSlice, current.Value.currentMip);

            if (current.Value.currentMip + 1 == value.input.mipmapCount)
            {
                if (current.Value.currentSlice + 1 == slicesCount)
                {
                    current.Value.inProgress    = false;
                    current.Value.isReady       = true;
                    cmd.RequestAsyncReadback(generator.invCDFRows, delegate (AsyncGPUReadbackRequest request)
                    {
                        //DefaultDumper(request, String.Format("___Marginal_{0}_{1}_{2}", _Idx, current.Value.currentSlice, current.Value.currentMip));
                        DefaultDumper(request, "___Marginal_" + _Idx + "_" + current.Value.currentSlice + "_" + current.Value.currentMip);
                    });
                    cmd.RequestAsyncReadback(generator.invCDFFull, delegate (AsyncGPUReadbackRequest request)
                    {
                        //DefaultDumper(request, String.Format("___ConditionalMarginal_{0}_{1}_{2}", _Idx, current.Value.currentSlice, current.Value.currentMip));
                        DefaultDumper(request, "___ConditionalMarginal_" + _Idx + "_" + current.Value.currentSlice + "_" + current.Value.currentMip);
                    });
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
            ++_Idx;
        }
    }
}
