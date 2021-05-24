using System;
using UnityEngine.Rendering;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.VFX.SDF
{
    /// <summary>
    /// This class allows to bake Signed Distance Fields from Meshes, and holds the necessary data to perform this operation.
    /// </summary>
    public class MeshToSDFBaker : IDisposable
    {
        private RenderTexture m_RayMap, m_SignMap, m_SignMapBis;
        private RenderTexture[] m_RenderTextureViews;
        private GraphicsBuffer m_CounterBuffer, m_AccumCounterBuffer, m_TrianglesInVoxels, m_TrianglesUV;
        private GraphicsBuffer
            m_TmpBuffer,
            m_AccumSumBlocks,
            m_SumBlocksBuffer,
            m_InSumBlocksBuffer,
            m_SumBlocksAdditional;

        private GraphicsBuffer
            m_IndicesBuffer,
            m_VerticesBuffer,
            m_VerticesOutBuffer,
            m_CoordFlipBuffer,
            m_AabbBuffer;
        private int m_ThreadGroupSize = 512;
        private int m_SignPassesCount;
        private float m_InOutThreshold;
        private Material[] m_Material;
        private Matrix4x4[] m_WorldToClip, m_ProjMat, m_ViewMat;
        private int m_nStepsJFA;
        private Kernels m_Kernels;
        private Mesh m_Mesh;
        private RenderTexture m_textureVoxel, m_textureVoxelBis, m_DistanceTexture;
        private GraphicsBuffer m_bufferVoxel;
        private ComputeShader m_computeShader;
        private int m_maxResolution;
        private int m_dimX, m_dimY, m_dimZ;
        private float m_MaxExtent;
        private float m_SdfOffset;
        private int nTriangles;
        private Vector3 m_SizeBox, m_Center;
        private CommandBuffer m_Cmd;
        private bool m_OwnsCommandBuffer = true;
        private bool m_IsDisposed = false;

        internal static uint kMaxGridSize = 1 << 24;

        internal VFXRuntimeResources m_RuntimeResources;

        private struct Triangle
        {
            Vector3 a, b, c;
        }

        /// <summary>
        /// Returns the texture containing the baked Signed Distance Field
        /// </summary>
        public RenderTexture SdfTexture => m_DistanceTexture;
        private void InitMeshFromList(List<Mesh> meshes, List<Matrix4x4> transforms)
        {
            int nMeshes = meshes.Count;
            if (nMeshes != transforms.Count)
                throw new ArgumentException("The number of meshes must be the same as the number of transforms");
            List<CombineInstance> combine = new List<CombineInstance>();
            for (var i = 0; i < nMeshes; i++)
            {
                Mesh mesh = meshes[i];
                for (int j = 0; j < mesh.subMeshCount; j++)
                {
                    CombineInstance comb = new CombineInstance();
                    comb.mesh = meshes[i];
                    comb.subMeshIndex = j;
                    comb.transform = transforms[i];
                    combine.Add(comb);
                }
            }
            m_Mesh = new Mesh();
            m_Mesh.indexFormat = IndexFormat.UInt32;
            m_Mesh.CombineMeshes(combine.ToArray());
        }

        private void InitCommandBuffer()
        {
            if (m_Cmd == null)
            {
                m_Cmd = new CommandBuffer
                {
                    name = "SDFBakingCommand"
                };
            }
        }

        private void InitSizeBox()
        {
            m_MaxExtent = Mathf.Max(m_SizeBox.x, Mathf.Max(m_SizeBox.y, m_SizeBox.z));

            if (m_MaxExtent == m_SizeBox.x)
            {
                m_dimX = Mathf.Max(Mathf.RoundToInt(m_maxResolution * m_SizeBox.x / m_MaxExtent), 1);
                m_dimY = Mathf.Max(Mathf.CeilToInt(m_maxResolution * m_SizeBox.y / m_MaxExtent), 1);
                m_dimZ = Mathf.Max(Mathf.CeilToInt(m_maxResolution * m_SizeBox.z / m_MaxExtent), 1);
                float voxelSize = m_MaxExtent / m_dimX;
                m_SizeBox.x = m_dimX * voxelSize;
                m_SizeBox.y = m_dimY * voxelSize;
                m_SizeBox.z = m_dimZ * voxelSize;
                m_MaxExtent = m_SizeBox.x;
            }
            else if (m_MaxExtent == m_SizeBox.y)
            {
                m_dimY = Mathf.Max(Mathf.RoundToInt(m_maxResolution * m_SizeBox.y / m_MaxExtent), 1);
                m_dimX = Mathf.Max(Mathf.CeilToInt(m_maxResolution * m_SizeBox.x / m_MaxExtent), 1);
                m_dimZ = Mathf.Max(Mathf.CeilToInt(m_maxResolution * m_SizeBox.z / m_MaxExtent), 1);
                float voxelSize = m_MaxExtent / m_dimY;
                m_SizeBox.x = m_dimX * voxelSize;
                m_SizeBox.y = m_dimY * voxelSize;
                m_SizeBox.z = m_dimZ * voxelSize;
                m_MaxExtent = m_SizeBox.y;
            }
            else if (m_MaxExtent == m_SizeBox.z)
            {
                m_dimZ = Mathf.Max(Mathf.RoundToInt(m_maxResolution * m_SizeBox.z / m_MaxExtent), 1);
                m_dimY = Mathf.Max(Mathf.CeilToInt(m_maxResolution * m_SizeBox.y / m_MaxExtent), 1);
                m_dimX = Mathf.Max(Mathf.CeilToInt(m_maxResolution * m_SizeBox.x / m_MaxExtent), 1);
                float voxelSize = m_MaxExtent / m_dimZ;

                m_SizeBox.x = m_dimX * voxelSize;
                m_SizeBox.y = m_dimY * voxelSize;
                m_SizeBox.z = m_dimZ * voxelSize;
                m_MaxExtent = m_SizeBox.z;
            }

            if (m_dimX * m_dimY * m_dimZ > kMaxGridSize)
            {
                throw new ArgumentException(
                    $"The size of the voxel grid is too big (>2^{Mathf.Log(kMaxGridSize, 2)}), reduce the resolution, or provide a thinner bounding box.");
            }
        }

        /// <summary>
        /// Gets the dimensions of the baked texture.
        /// </summary>
        /// <returns>
        /// A Vector3Int containing the height, width and depth of the texture.
        /// </returns>
        public Vector3Int GetGridSize()
        {
            return new Vector3Int(m_dimX, m_dimY, m_dimZ);
        }

        /// <summary>
        /// Gets the size of the baking box used.
        /// </summary>
        /// <returns>
        /// A Vector3 containing the size of the box along the three directions of space
        /// </returns>
        public Vector3 GetActualBoxSize()
        {
            return m_SizeBox;
        }

        /// <summary>
        /// Constructor of the class MeshToSDFBaker.
        /// </summary>
        /// <param name="sizeBox">The desired size of the baking box.</param>
        /// <param name="center">The center of the baking box.</param>
        /// <param name="maxRes">The resolution along the largest dimension.</param>
        /// <param name="mesh">The Mesh to be baked into an SDF.</param>
        /// <param name="signPassesCount">The number of refinement passes on the sign of the SDF. This should stay below 20.</param>
        /// <param name="threshold">The threshold controlling which voxels will be considered inside or outside of the surface.</param>
        /// <param name="sdfOffset">The Offset to add to the SDF. It can be used to make the SDF more bulky or skinny.</param>
        /// <param name="cmd">The CommandBuffer on which the baking process will be added.</param>
        public MeshToSDFBaker(Vector3 sizeBox, Vector3 center, int maxRes, Mesh mesh, int signPassesCount = 1, float threshold = 0.5f, float sdfOffset = 0.0f, CommandBuffer cmd = null)
        {
            m_SignPassesCount = signPassesCount;
            if (m_SignPassesCount >= 20)
            {
                throw new ArgumentException("The signPassCount argument should be smaller than 20.");
            }
            m_InOutThreshold = threshold;
            m_RuntimeResources = VFXRuntimeResources.runtimeResources;
            if (m_RuntimeResources == null)
            {
                throw new InvalidOperationException("VFX Runtime Resources could not be loaded.");
            }

            m_SdfOffset = sdfOffset;
            m_Center = center;
            m_SizeBox = sizeBox;
            m_Mesh = mesh;
            m_maxResolution = maxRes;
            if (cmd != null)
            {
                m_Cmd = cmd;
                m_OwnsCommandBuffer = false;
            }
            Init();
        }

        /// <summary>
        /// Constructor of the class MeshToSDFBaker.
        /// </summary>
        /// <param name="sizeBox">The desired size of the baking box</param>
        /// <param name="center">The center of the baking box.</param>
        /// <param name="maxRes">The resolution along the largest dimension.</param>
        /// <param name="meshes">The list of meshes to be baked into an SDF.</param>
        /// <param name="transforms">The list of transforms of the meshes.</param>
        /// <param name="signPassesCount">The number of refinement passes on the sign of the SDF. This should stay below 20.</param>
        /// <param name="threshold">The threshold controlling which voxels will be considered inside or outside of the surface.</param>
        /// <param name="sdfOffset">The Offset to add to the SDF. It can be used to make the SDF more bulky or skinny.</param>
        /// <param name="cmd">The CommandBuffer on which the baking process will be added.</param>
        public MeshToSDFBaker(Vector3 sizeBox, Vector3 center, int maxRes, List<Mesh> meshes, List<Matrix4x4> transforms, int signPassesCount = 1, float threshold = 0.5f, float sdfOffset = 0.0f, CommandBuffer cmd = null)
        {
            m_RuntimeResources = VFXRuntimeResources.runtimeResources;
            if (m_RuntimeResources == null)
            {
                throw new InvalidOperationException("VFX Runtime Resources could not be loaded.");
            }
            InitMeshFromList(meshes, transforms);
            m_SdfOffset = sdfOffset;
            m_Center = center;
            m_SizeBox = sizeBox;
            m_maxResolution = maxRes;
            if (cmd != null)
            {
                m_Cmd = cmd;
                m_OwnsCommandBuffer = false;
            }
            m_SignPassesCount = signPassesCount;
            m_InOutThreshold = threshold;
            Init();
        }

        /// <summary>
        /// This finalizer should never be called. Dispose() should be called explicitly when an MeshToSDFBaker instance is finished being used.
        /// </summary>
        ~MeshToSDFBaker()
        {
            if (!m_IsDisposed)
            {
                Debug.LogWarning("Dispose() should be called explicitly when an MeshToSDFBaker instance is finished being used.");
            }
        }

        /// <summary>
        /// Reinitialize the baker with the new mesh and provided parameters.
        /// </summary>
        /// <param name="sizeBox">The desired size of the baking box.</param>
        /// <param name="center">The center of the baking box.</param>
        /// <param name="maxRes">The resolution along the largest dimension.</param>
        /// <param name="mesh">The Mesh to be baked into an SDF.</param>
        /// <param name="signPassesCount">The number of refinement passes on the sign of the SDF. This should stay below 20.</param>
        /// <param name="threshold">The threshold controlling which voxels will be considered inside or outside of the surface.</param>
        /// <param name="sdfOffset">The Offset to add to the SDF. It can be used to make the SDF more bulky or skinny.</param>
        public void Reinit(Vector3 sizeBox, Vector3 center, int maxRes, Mesh mesh, int signPassesCount = 1, float threshold = 0.5f, float sdfOffset = 0.0f)
        {
            m_Mesh = mesh;
            m_Center = center;
            m_SizeBox = sizeBox;
            m_maxResolution = maxRes;
            m_SignPassesCount = signPassesCount;
            m_InOutThreshold = threshold;
            m_SdfOffset = sdfOffset;
            Init();
        }

        /// <summary>
        /// Reinitialize the baker with the new lists of meshes and transforms, and provided parameters.
        /// </summary>
        /// <param name="sizeBox">The desired size of the baking box</param>
        /// <param name="center">The center of the baking box.</param>
        /// <param name="maxRes">The resolution along the largest dimension.</param>
        /// <param name="meshes">The list of meshes to be baked into an SDF.</param>
        /// <param name="transforms">The list of transforms of the meshes.</param>
        /// <param name="signPassesCount">The number of refinement passes on the sign of the SDF. This should stay below 20.</param>
        /// <param name="threshold">The threshold controlling which voxels will be considered inside or outside of the surface.</param>
        /// <param name="sdfOffset">The Offset to add to the SDF. It can be used to make the SDF more bulky or skinny.</param>
        public void Reinit(Vector3 sizeBox, Vector3 center, int maxRes, List<Mesh> meshes, List<Matrix4x4> transforms, int signPassesCount = 1, float threshold = 0.5f, float sdfOffset = 0.0f)
        {
            InitMeshFromList(meshes, transforms);
            m_Center = center;
            m_SizeBox = sizeBox;
            m_maxResolution = maxRes;
            m_SignPassesCount = signPassesCount;
            m_InOutThreshold = threshold;
            m_SdfOffset = sdfOffset;
            Init();
        }

        void InitTextures()
        {
            RenderTextureDescriptor rtDesc4Channels = new RenderTextureDescriptor
            {
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                dimension = TextureDimension.Tex3D,
                enableRandomWrite = true,
                width = m_dimX,
                height = m_dimY,
                volumeDepth = m_dimZ,
                msaaSamples = 1,
            };
            RenderTextureDescriptor rtDesc1Channel = new RenderTextureDescriptor
            {
                graphicsFormat = GraphicsFormat.R16_SFloat,
                dimension = TextureDimension.Tex3D,
                enableRandomWrite = true,
                width = m_dimX,
                height = m_dimY,
                volumeDepth = m_dimZ,
                msaaSamples = 1,
            };

            RenderTextureDescriptor rtDescSignMap = new RenderTextureDescriptor
            {
                graphicsFormat = GraphicsFormat.R32_SFloat,
                dimension = TextureDimension.Tex3D,
                enableRandomWrite = true,
                width = m_dimX,
                height = m_dimY,
                volumeDepth = m_dimZ,
                msaaSamples = 1,
            };

            CreateRenderTextureIfNeeded(ref m_textureVoxel, rtDesc4Channels);
            CreateRenderTextureIfNeeded(ref m_textureVoxelBis, rtDesc4Channels);
            CreateRenderTextureIfNeeded(ref m_RayMap, rtDesc4Channels);
            CreateRenderTextureIfNeeded(ref m_SignMap, rtDescSignMap);
            CreateRenderTextureIfNeeded(ref m_SignMapBis, rtDescSignMap);

            CreateRenderTextureIfNeeded(ref m_DistanceTexture, rtDesc1Channel);

            CreateGraphicsBufferIfNeeded(ref m_bufferVoxel, m_dimX * m_dimY * m_dimZ,
                4 * sizeof(float));

            InitPrefixSumBuffers();
        }

        void Init()
        {
            InitSizeBox();
            InitCommandBuffer();

            m_ThreadGroupSize = 512; //TODO call the proper system function

            m_computeShader = m_RuntimeResources.sdfRayMapCS;

            if (m_computeShader == null)
            {
                throw new InvalidOperationException("VFX Runtime Resources could not be loaded correctly.");
            }
            if (m_Kernels == null)
                m_Kernels = new Kernels(m_computeShader);
            InitTextures();

            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor();
            rtDesc.width = m_dimX;
            rtDesc.height = m_dimY;
            rtDesc.graphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
            rtDesc.volumeDepth = 1;
            rtDesc.msaaSamples = 1;
            rtDesc.dimension = TextureDimension.Tex2D;
            if (m_RenderTextureViews == null)
                m_RenderTextureViews = new RenderTexture[3];
            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        rtDesc.width = m_dimX;
                        rtDesc.height = m_dimY;
                        CreateRenderTextureIfNeeded(ref m_RenderTextureViews[i], rtDesc);
                        break;
                    case 1:
                        rtDesc.width = m_dimZ;
                        rtDesc.height = m_dimX;
                        CreateRenderTextureIfNeeded(ref m_RenderTextureViews[i], rtDesc);
                        break;
                    case 2:
                        rtDesc.width = m_dimY;
                        rtDesc.height = m_dimZ;
                        CreateRenderTextureIfNeeded(ref m_RenderTextureViews[i], rtDesc);
                        break;
                }
            }

            if (m_Material == null || m_Material[0] == null || m_Material[1] == null || m_Material[2] == null)
            {
                m_Material = new Material[3];
                Shader rayMapShader = m_RuntimeResources.sdfRayMapShader;
                if (rayMapShader == null)
                {
                    throw new InvalidOperationException("VFX Runtime Resources could not be loaded correctly.");
                }
                for (var i = 0; i < 3; i++)
                {
                    m_Material[i] = new Material(rayMapShader);
                }
            }

            if (m_WorldToClip == null)
            {
                m_WorldToClip = new Matrix4x4[3];
            }
            if (m_ProjMat == null)
            {
                m_ProjMat = new Matrix4x4[3];
            }
            if (m_ViewMat == null)
            {
                m_ViewMat = new Matrix4x4[3];
            }
            UpdateCameras();
        }

        void UpdateCameras()
        {
            Vector3 pos =  m_Center + Vector3.back * (m_SizeBox.z * 0.5f + 1f);
            Quaternion rot = Quaternion.identity;
            float near = 1.0f;
            float far = near + m_SizeBox.z;
            m_WorldToClip[0] = ComputeOrthographicWorldToClip(pos, rot, m_SizeBox.x, m_SizeBox.y, near, far, out m_ProjMat[0], out m_ViewMat[0]);

            pos = m_Center + Vector3.down * (m_SizeBox.y * 0.5f + 1f);
            rot = Quaternion.Euler(-90, -90, 0);
            far = near + m_SizeBox.y;
            m_WorldToClip[1] = ComputeOrthographicWorldToClip(pos, rot, m_SizeBox.z, m_SizeBox.x, near, far, out m_ProjMat[1], out m_ViewMat[1]);

            pos = m_Center + Vector3.left * (m_SizeBox.x * 0.5f + 1f);
            rot = Quaternion.Euler(0, 90, 90);
            far =  near + m_SizeBox.x;
            m_WorldToClip[2] = ComputeOrthographicWorldToClip(pos, rot, m_SizeBox.y, m_SizeBox.z, near, far, out m_ProjMat[2], out m_ViewMat[2]);
        }

        Matrix4x4 ComputeOrthographicWorldToClip(Vector3 pos, Quaternion rot, float width, float height, float near, float far, out Matrix4x4 proj, out Matrix4x4 view)
        {
            proj = Matrix4x4.Ortho(-width / 2, width / 2, -height / 2, height / 2, near, far);
            proj = GL.GetGPUProjectionMatrix(proj, false);
            view = Matrix4x4.TRS(pos, rot, new Vector3(1, 1, -1)).inverse;
            return proj * view;
        }

        int iDivUp(int a, int b)
        {
            return (a % b != 0) ? (a / b + 1) : (a / b);
        }

        void PrefixSumCount()
        {
            int nVoxels = m_dimX * m_dimY * m_dimZ;

            m_Cmd.BeginSample("BakeSDF.PrefixSum");

            m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.numElem, nVoxels);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.inBucketSum, ShaderProperties.inputBuffer, m_CounterBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.inBucketSum, ShaderProperties.resultBuffer, m_TmpBuffer);
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.inBucketSum,
                Mathf.CeilToInt((float)nVoxels / m_ThreadGroupSize), 1, 1);


            int nBlocks = iDivUp(nVoxels , m_ThreadGroupSize);
            if (nBlocks > m_ThreadGroupSize) //If the number of cells is bigger than m_ThreadGroupSize^2, (512^2, 64^3), apply prefix sum recursively
            {
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.toBlockSumBuffer, ShaderProperties.inputCounter, m_CounterBuffer);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.toBlockSumBuffer, ShaderProperties.inputBuffer, m_TmpBuffer);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.toBlockSumBuffer, ShaderProperties.resultBuffer, m_SumBlocksBuffer);
                m_Cmd.DispatchCompute(m_computeShader, m_Kernels.toBlockSumBuffer,
                    Mathf.CeilToInt((float)nVoxels / (m_ThreadGroupSize * m_ThreadGroupSize)), 1, 1);

                m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.numElem, nBlocks);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.inBucketSum, ShaderProperties.inputBuffer, m_SumBlocksBuffer);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.inBucketSum, ShaderProperties.resultBuffer, m_InSumBlocksBuffer);
                m_Cmd.DispatchCompute(m_computeShader, m_Kernels.inBucketSum,
                    Mathf.CeilToInt((float)nVoxels / (m_ThreadGroupSize * m_ThreadGroupSize)), 1, 1);

                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.blockSums, ShaderProperties.inputCounter, m_SumBlocksBuffer);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.blockSums, ShaderProperties.inputBuffer, m_InSumBlocksBuffer);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.blockSums, ShaderProperties.resultBuffer, m_SumBlocksAdditional);
                m_Cmd.DispatchCompute(m_computeShader, m_Kernels.blockSums,
                    Mathf.CeilToInt((float)nVoxels / (m_ThreadGroupSize * m_ThreadGroupSize * m_ThreadGroupSize)), 1, 1);

                m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.exclusive, 0);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.finalSum, ShaderProperties.inputBuffer, m_InSumBlocksBuffer);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.finalSum, ShaderProperties.auxBuffer, m_SumBlocksAdditional);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.finalSum, ShaderProperties.inputCounter, m_SumBlocksBuffer);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.finalSum, ShaderProperties.resultBuffer, m_AccumSumBlocks);
                m_Cmd.DispatchCompute(m_computeShader, m_Kernels.finalSum,
                    Mathf.CeilToInt((float)nVoxels / (m_ThreadGroupSize * m_ThreadGroupSize)), 1, 1);
            }
            else
            {
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.blockSums, ShaderProperties.inputCounter, m_CounterBuffer);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.blockSums, ShaderProperties.inputBuffer, m_TmpBuffer);
                m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.blockSums, ShaderProperties.resultBuffer, m_AccumSumBlocks);
                m_Cmd.DispatchCompute(m_computeShader, m_Kernels.blockSums,
                    Mathf.CeilToInt((float)nVoxels / (m_ThreadGroupSize * m_ThreadGroupSize)), 1, 1);
            }
            m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.numElem, nVoxels);
            m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.exclusive, 0);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.finalSum, ShaderProperties.inputBuffer, m_TmpBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.finalSum, ShaderProperties.auxBuffer, m_AccumSumBlocks);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.finalSum, ShaderProperties.inputCounter, m_CounterBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.finalSum, ShaderProperties.resultBuffer, m_AccumCounterBuffer);
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.finalSum,
                Mathf.CeilToInt((float)nVoxels / m_ThreadGroupSize), 1, 1);
            m_Cmd.EndSample("BakeSDF.PrefixSum");
        }

        void SurfaceClosing()
        {
            m_Cmd.BeginSample("BakeSDF.SurfaceClosing");
            if (m_SignPassesCount == 0)
            {
                m_InOutThreshold *= 6.0f;
            }
            m_Cmd.SetComputeFloatParam(m_computeShader, ShaderProperties.threshold, m_InOutThreshold);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.surfaceClosing, ShaderProperties.signMap, GetSignMapPrincipal(m_SignPassesCount));
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.surfaceClosing, ShaderProperties.voxelsTexture, GetTextureVoxelPrincipal(1));

            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.surfaceClosing, iDivUp(m_dimX, 4), iDivUp(m_dimY, 4), iDivUp(m_dimZ, 4));
            m_Cmd.EndSample("BakeSDF.SurfaceClosing");
        }

        RenderTexture GetTextureVoxelPrincipal(int step)
        {
            if (step % 2 == 0)
            {
                return m_textureVoxel;
            }
            else
            {
                return m_textureVoxelBis;
            }
        }

        RenderTexture GetTextureVoxelBis(int step)
        {
            if (step % 2 == 0)
            {
                return m_textureVoxelBis;
            }
            else
            {
                return m_textureVoxel;
            }
        }

        void JFA()
        {
            m_Cmd.BeginSample("BakeSDF.JFA");

            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.toTextureNormalized, ShaderProperties.voxelsBuffer, m_bufferVoxel);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.toTextureNormalized, ShaderProperties.voxelsTexture, GetTextureVoxelPrincipal(0));
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.toTextureNormalized, Mathf.CeilToInt(m_dimX / 4.0f),
                Mathf.CeilToInt(m_dimY / 4.0f), Mathf.CeilToInt(m_dimZ / 4.0f));

            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.jfa, ShaderProperties.voxelsTexture, GetTextureVoxelPrincipal(0), 0);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.jfa, ShaderProperties.voxelsTmpTexture, GetTextureVoxelBis(0), 0);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.copyTextures, ShaderProperties.voxelsTexture, GetTextureVoxelPrincipal(0), 0);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.copyTextures, ShaderProperties.voxelsTmpTexture, GetTextureVoxelBis(0), 0);

            // 1+JFA
            m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.offset, 1);
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.jfa, Mathf.CeilToInt(m_dimX / 4.0f),
                Mathf.CeilToInt(m_dimY / 4.0f), Mathf.CeilToInt(m_dimZ / 4.0f));
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.copyTextures, Mathf.CeilToInt(m_dimX / 4.0f),
                Mathf.CeilToInt(m_dimY / 4.0f), Mathf.CeilToInt(m_dimZ / 4.0f));

            m_nStepsJFA = Mathf.CeilToInt(Mathf.Log(m_maxResolution, 2));
            for (int level = 1; level <= m_nStepsJFA; level++)
            {
                int offset = Mathf.FloorToInt(Mathf.Pow(2, m_nStepsJFA - level) + 0.5f);
                m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.offset, offset);
                m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.jfa, ShaderProperties.voxelsTexture, GetTextureVoxelPrincipal(level), 0);
                m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.jfa, ShaderProperties.voxelsTmpTexture, GetTextureVoxelBis(level), 0);
                m_Cmd.DispatchCompute(m_computeShader, m_Kernels.jfa, Mathf.CeilToInt(m_dimX / 4.0f),
                    Mathf.CeilToInt(m_dimY / 4.0f), Mathf.CeilToInt(m_dimZ / 4.0f));
            }
            m_Cmd.EndSample("BakeSDF.JFA");
        }

        void GenerateRayMap()
        {
            m_Cmd.BeginSample("BakeSDF.Raymap");
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.generateRayMapLocal, ShaderProperties.accumCounter, m_AccumCounterBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.generateRayMapLocal, ShaderProperties.triangleIDs, m_TrianglesInVoxels);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.generateRayMapLocal, ShaderProperties.trianglesUV, m_TrianglesUV);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.generateRayMapLocal, ShaderProperties.rayMap, m_RayMap);

            m_Cmd.BeginSample("BakeSDF.LocalRaymap");
            int x, y, z;
            for (var i = 0; i < 8; i++)
            {
                x = i & 1;
                y = (i & 2) >> 1;
                z = (i & 4) >> 2;
                m_Cmd.SetComputeIntParams(m_computeShader, ShaderProperties.offsetRayMap, x, y, z);
                m_Cmd.DispatchCompute(m_computeShader, m_Kernels.generateRayMapLocal,
                    Mathf.CeilToInt(m_dimX / (2.0f * 8.0f)),
                    Mathf.CeilToInt(m_dimY / (2.0f * 8.0f)),
                    Mathf.CeilToInt(m_dimZ / (2.0f * 8.0f)));
            }
            m_Cmd.EndSample("BakeSDF.LocalRaymap");

            m_Cmd.BeginSample("BakeSDF.GlobalRaymap");

            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.rayMapScanX, ShaderProperties.rayMap, m_RayMap);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.rayMapScanY, ShaderProperties.rayMap, m_RayMap);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.rayMapScanZ, ShaderProperties.rayMap, m_RayMap);

            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.rayMapScanX,
                1,
                Mathf.CeilToInt(m_dimY / 8.0f),
                Mathf.CeilToInt(m_dimZ / 8.0f));
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.rayMapScanY,
                Mathf.CeilToInt(m_dimX / 8.0f),
                1,
                Mathf.CeilToInt(m_dimZ / 8.0f));
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.rayMapScanZ,
                Mathf.CeilToInt(m_dimX / 8.0f),
                Mathf.CeilToInt(m_dimY / 8.0f),
                1);
            m_Cmd.EndSample("BakeSDF.GlobalRaymap");

            m_Cmd.EndSample("BakeSDF.Raymap");
        }

        RenderTexture GetSignMapPrincipal(int step)
        {
            if (step % 2 == 0)
            {
                return m_SignMap;
            }

            return m_SignMapBis;
        }

        RenderTexture GetSignMapBis(int step)
        {
            if (step % 2 == 0)
            {
                return m_SignMapBis;
            }

            return m_SignMap;
        }

        void SignPass()
        {
            m_Cmd.BeginSample("BakeSDF.SignPass");
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.signPass6Rays, ShaderProperties.rayMap, m_RayMap);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.signPass6Rays, ShaderProperties.signMap, GetSignMapPrincipal(0));
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.signPass6Rays, Mathf.CeilToInt(m_dimX / 4.0f),
                Mathf.CeilToInt(m_dimY / 4.0f), Mathf.CeilToInt(m_dimZ / 4.0f));

            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.signPassNeighbors, ShaderProperties.rayMap, m_RayMap);
            int neighboursCount = 8;
            float normalizeFactor = 6;
            m_Cmd.SetComputeFloatParam(m_computeShader, ShaderProperties.normalizeFactor, normalizeFactor);
            m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.numNeighbours, neighboursCount);

            int signPasses = m_SignPassesCount;
            for (int i = 1; i <= signPasses; i++)
            {
                m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.passId, i);
                m_Cmd.SetComputeFloatParam(m_computeShader, ShaderProperties.normalizeFactor, normalizeFactor);
                m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.signPassNeighbors, ShaderProperties.signMap, GetSignMapPrincipal(i));
                m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.signPassNeighbors, ShaderProperties.signMapTmp, GetSignMapBis(i));
                m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.needNormalize, (i == signPasses) ? 1 : 0);
                m_Cmd.DispatchCompute(m_computeShader, m_Kernels.signPassNeighbors, Mathf.CeilToInt(m_dimX / 4.0f),
                    Mathf.CeilToInt(m_dimY / 4.0f), Mathf.CeilToInt(m_dimZ / 4.0f));
                normalizeFactor = normalizeFactor + neighboursCount * 6 * normalizeFactor;
            }

            m_Cmd.EndSample("BakeSDF.SignPass");
        }

        /// <summary>
        /// Performs the baking operation. After this function is called, the resulting SDF is accessible via the property MeshToSDFBaker.SdfTexture.
        /// </summary>
        public void BakeSDF()
        {
            m_Cmd.BeginSample("BakeSDF");
            UpdateCameras();
            m_Cmd.SetComputeIntParams(m_computeShader, "size", m_dimX, m_dimY, m_dimZ);
            CreateGraphicsBufferIfNeeded(ref m_bufferVoxel, m_dimX * m_dimY * m_dimZ,
                4 * sizeof(float));

            InitPrefixSumBuffers();

            int[] indices = m_Mesh.triangles;
            Vector3[] vertices = m_Mesh.vertices;
            nTriangles = indices.Length / 3;
            // upper bound of length of triangle list
            //TODO : Need to find a better way to either estimate, or use a readback (slow? how slow ? not always really possible)
            int upperBoundCount = Mathf.Min(Mathf.Max(m_dimX * m_dimY * m_dimZ, nTriangles) * 30, 1536 / 4 * 1 << 20);
            m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.upperBoundCount, upperBoundCount);
            ClearRenderTexturesAndBuffers();

            InitGeometryBuffers(indices, vertices, upperBoundCount);

            BuildGeometry();

            FirstDraw();

            PrefixSumCount();

            SecondDraw();

            GenerateRayMap();

            SignPass();

            SurfaceClosing();

            JFA();


            PerformDistanceTransformWinding();

            m_Cmd.EndSample("BakeSDF");

            if (m_OwnsCommandBuffer)
            {
                m_Cmd.ClearRandomWriteTargets();
                Graphics.ExecuteCommandBuffer(m_Cmd);
                m_Cmd.Clear();
            }
        }

        private void SecondDraw()
        {
            m_Cmd.BeginSample("BakeSDF.SecondDraw");
            for (var i = 0; i < 3; i++)
            {
                m_Cmd.SetRenderTarget(m_RenderTextureViews[i]);
                m_Cmd.SetRandomWriteTarget(3, m_TrianglesInVoxels, false);
                m_Cmd.SetRandomWriteTarget(2, m_AccumCounterBuffer, false);
                m_Cmd.SetViewProjectionMatrices(m_ViewMat[i], m_ProjMat[i]);
                m_Cmd.ClearRenderTarget(true, true, Color.black, 1.0f);
                m_Cmd.DrawProcedural(Matrix4x4.identity, m_Material[i], 1, MeshTopology.Triangles, nTriangles * 3);
            }

            m_Cmd.EndSample("BakeSDF.SecondDraw");
        }

        private void FirstDraw()
        {
            m_Cmd.BeginSample("BakeSDF.FirstDraw");

            for (var i = 0; i < 3; i++)
            {
                m_Material[i].SetInt("dimX", m_dimX);
                m_Material[i].SetInt("dimY", m_dimY);
                m_Material[i].SetInt("dimZ", m_dimZ);
                m_Material[i].SetInt("currentAxis", i);
                m_Material[i].SetBuffer("vertices", m_VerticesOutBuffer);
                m_Material[i].SetBuffer("coordFlip", m_CoordFlipBuffer);
            }

            for (var i = 0; i < 3; i++)
            {
                m_Cmd.ClearRandomWriteTargets();
                m_Cmd.SetRenderTarget(m_RenderTextureViews[i]);
                m_Cmd.SetRandomWriteTarget(4, m_AabbBuffer, false);
                m_Cmd.SetRandomWriteTarget(1, m_bufferVoxel, false);
                m_Cmd.SetRandomWriteTarget(2, m_CounterBuffer, false);
                m_Cmd.SetViewProjectionMatrices(m_ViewMat[i], m_ProjMat[i]);
                m_Cmd.ClearRenderTarget(true, true, Color.black, 1.0f);
                m_Cmd.DrawProcedural(Matrix4x4.identity, m_Material[i], 0, MeshTopology.Triangles, nTriangles * 3);
            }

            m_Cmd.EndSample("BakeSDF.FirstDraw");
        }

        private void BuildGeometry()
        {
            m_Cmd.BeginSample("BakeSDF.FakeGeometryShader");

            Vector3 minBoundsExtented = m_Center - m_SizeBox * 0.5f;
            Vector3 maxBoundsExtented = m_Center + m_SizeBox * 0.5f;

            m_Cmd.SetComputeFloatParams(m_computeShader, ShaderProperties.minBoundsExtended, minBoundsExtented.x, minBoundsExtented.y,
                minBoundsExtented.z);
            m_Cmd.SetComputeFloatParams(m_computeShader, ShaderProperties.maxBoundsExtended, maxBoundsExtented.x, maxBoundsExtented.y,
                maxBoundsExtented.z);

            m_Cmd.SetComputeFloatParam(m_computeShader, ShaderProperties.maxExtent, m_MaxExtent);

            m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.nTriangles, nTriangles);

            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.chooseDirectionTriangleOnly, ShaderProperties.indicesBuffer, m_IndicesBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.chooseDirectionTriangleOnly, ShaderProperties.verticesBuffer, m_VerticesBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.chooseDirectionTriangleOnly, ShaderProperties.coordFlipBuffer, m_CoordFlipBuffer);

            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.conservativeRasterization, ShaderProperties.indicesBuffer, m_IndicesBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.conservativeRasterization, ShaderProperties.verticesBuffer, m_VerticesBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.conservativeRasterization, ShaderProperties.verticesOutBuffer, m_VerticesOutBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.conservativeRasterization, ShaderProperties.coordFlipBuffer, m_CoordFlipBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.conservativeRasterization, ShaderProperties.aabbBuffer, m_AabbBuffer);

            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.generateTrianglesUV, ShaderProperties.rw_trianglesUV, m_TrianglesUV);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.generateTrianglesUV, ShaderProperties.indicesBuffer, m_IndicesBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.generateTrianglesUV, ShaderProperties.verticesBuffer, m_VerticesBuffer);

            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.generateTrianglesUV, Mathf.CeilToInt(nTriangles / 64.0f), 1, 1);
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.chooseDirectionTriangleOnly, Mathf.CeilToInt(nTriangles / 64.0f), 1, 1);

            for (var i = 0; i < 3; i++)
            {
                m_Cmd.SetComputeIntParam(m_computeShader, ShaderProperties.currentAxis, i);
                m_Cmd.SetComputeMatrixParam(m_computeShader, ShaderProperties.worldToClip, m_WorldToClip[i]);
                m_Cmd.DispatchCompute(m_computeShader, m_Kernels.conservativeRasterization, Mathf.CeilToInt(nTriangles / 64.0f), 1, 1);
            }

            m_Cmd.EndSample("BakeSDF.FakeGeometryShader");
        }

        private void InitGeometryBuffers(int[] indices, Vector3[] vertices, int upperBoundCount)
        {
            CreateGraphicsBufferIfNeeded(ref m_IndicesBuffer, indices.Length, sizeof(int));
            CreateGraphicsBufferIfNeeded(ref m_VerticesBuffer, vertices.Length, 3 * sizeof(float));
            CreateGraphicsBufferIfNeeded(ref m_VerticesOutBuffer, 3 * nTriangles, 4 * sizeof(float));
            CreateGraphicsBufferIfNeeded(ref m_CoordFlipBuffer, nTriangles, sizeof(int));
            CreateGraphicsBufferIfNeeded(ref m_AabbBuffer, nTriangles, 4 * sizeof(float));
            CreateGraphicsBufferIfNeeded(ref m_TrianglesInVoxels, upperBoundCount, sizeof(uint));
            CreateGraphicsBufferIfNeeded(ref m_TrianglesUV, nTriangles, 9 * sizeof(float));
            m_IndicesBuffer.SetData(indices);
            m_VerticesBuffer.SetData(vertices);
        }

        private void InitPrefixSumBuffers()
        {
            CreateGraphicsBufferIfNeeded(ref m_CounterBuffer, m_dimX * m_dimY * m_dimZ,
                sizeof(int));
            CreateGraphicsBufferIfNeeded(ref m_AccumCounterBuffer, m_dimX * m_dimY * m_dimZ,
                sizeof(int));
            CreateGraphicsBufferIfNeeded(ref m_AccumSumBlocks,
                Mathf.CeilToInt((float)m_dimX * m_dimY * m_dimZ / m_ThreadGroupSize), sizeof(int));

            CreateGraphicsBufferIfNeeded(ref m_SumBlocksBuffer,
                Mathf.CeilToInt((float)m_dimX * m_dimY * m_dimZ / m_ThreadGroupSize), sizeof(int));
            CreateGraphicsBufferIfNeeded(ref m_InSumBlocksBuffer,
                Mathf.CeilToInt((float)m_dimX * m_dimY * m_dimZ / m_ThreadGroupSize), sizeof(int));
            CreateGraphicsBufferIfNeeded(ref m_TmpBuffer, m_dimX * m_dimY * m_dimZ, sizeof(int));
            CreateGraphicsBufferIfNeeded(ref m_SumBlocksAdditional,
                Mathf.CeilToInt((float)m_dimX * m_dimY * m_dimZ / (m_ThreadGroupSize * m_ThreadGroupSize)),
                sizeof(int));
        }

        private void ClearRenderTexturesAndBuffers()
        {
            m_Cmd.BeginSample("BakeSDF.ClearTexturesAndBuffers");
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.clearTexturesAndBuffers, ShaderProperties.voxelsTexture, m_textureVoxel, 0);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.clearTexturesAndBuffers, ShaderProperties.voxelsTmpTexture, m_textureVoxelBis, 0);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.clearTexturesAndBuffers, ShaderProperties.rayMap, m_RayMap, 0);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.clearTexturesAndBuffers, ShaderProperties.signMap, m_SignMap, 0);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.clearTexturesAndBuffers, ShaderProperties.signMapTmp, m_SignMapBis);

            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.clearTexturesAndBuffers, ShaderProperties.voxelsBuffer, m_bufferVoxel);

            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.clearTexturesAndBuffers, ShaderProperties.counter, m_CounterBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.clearTexturesAndBuffers, ShaderProperties.accumCounter, m_AccumCounterBuffer);

            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.clearTexturesAndBuffers, Mathf.CeilToInt(m_dimX / 8.0f),
                Mathf.CeilToInt(m_dimY / 8.0f), Mathf.CeilToInt(m_dimZ / 8.0f));
            m_Cmd.EndSample("BakeSDF.ClearTexturesAndBuffers");
        }

        private void PerformDistanceTransformWinding()
        {
            m_Cmd.BeginSample("BakeSDF.DistanceTransform");
            m_Cmd.SetComputeFloatParam(m_computeShader, ShaderProperties.threshold, m_InOutThreshold);
            m_Cmd.SetComputeFloatParam(m_computeShader, ShaderProperties.sdfOffset, m_SdfOffset);

            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.distanceTransform, ShaderProperties.voxelsTexture, GetTextureVoxelPrincipal(m_nStepsJFA + 1));
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.distanceTransform, ShaderProperties.distanceTexture, m_DistanceTexture);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.distanceTransform, ShaderProperties.accumCounter, m_AccumCounterBuffer);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.distanceTransform, ShaderProperties.triangleIDs, m_TrianglesInVoxels);
            m_Cmd.SetComputeBufferParam(m_computeShader, m_Kernels.distanceTransform, ShaderProperties.trianglesUV, m_TrianglesUV);
            m_Cmd.SetComputeTextureParam(m_computeShader, m_Kernels.distanceTransform, ShaderProperties.signMap, GetSignMapPrincipal(m_SignPassesCount));
            m_Cmd.DispatchCompute(m_computeShader, m_Kernels.distanceTransform, Mathf.CeilToInt(m_dimX / 8.0f),
                Mathf.CeilToInt(m_dimY / 8.0f), Mathf.CeilToInt(m_dimZ / 8.0f));

            m_Cmd.EndSample("BakeSDF.DistanceTransform");
        }

        private RenderTexture RayMap => m_RayMap;
        private void ReleaseBuffersAndTextures()
        {
            //Release  textures.
            ReleaseRenderTexture(ref m_textureVoxel);
            ReleaseRenderTexture(ref m_textureVoxelBis);
            ReleaseRenderTexture(ref m_DistanceTexture);
            for (var i = 0; i < 3; i++)
            {
                ReleaseRenderTexture(ref m_RenderTextureViews[i]);
            }
            ReleaseRenderTexture(ref m_SignMap);
            ReleaseRenderTexture(ref m_RayMap);

            //Release  buffers.
            ReleaseGraphicsBuffer(ref m_bufferVoxel);
            ReleaseGraphicsBuffer(ref m_TrianglesUV);
            ReleaseGraphicsBuffer(ref m_TrianglesInVoxels);
            ReleaseGraphicsBuffer(ref m_IndicesBuffer);
            ReleaseGraphicsBuffer(ref m_VerticesBuffer);
            ReleaseGraphicsBuffer(ref m_VerticesOutBuffer);
            ReleaseGraphicsBuffer(ref m_CoordFlipBuffer);
            ReleaseGraphicsBuffer(ref m_AabbBuffer);
            ReleaseGraphicsBuffer(ref m_TmpBuffer);
            ReleaseGraphicsBuffer(ref m_AccumSumBlocks);
            ReleaseGraphicsBuffer(ref m_SumBlocksBuffer);
            ReleaseGraphicsBuffer(ref m_InSumBlocksBuffer);
            ReleaseGraphicsBuffer(ref m_SumBlocksAdditional);
            ReleaseGraphicsBuffer(ref m_bufferVoxel);
            ReleaseGraphicsBuffer(ref m_CounterBuffer);
            ReleaseGraphicsBuffer(ref m_AccumCounterBuffer);
        }

        /// <summary>
        /// Release all the buffers and textures created by the object. This must be called you are finished using the object.
        /// </summary>
        public void Dispose()
        {
            ReleaseBuffersAndTextures();
            GC.SuppressFinalize(this);
            m_IsDisposed = true;
        }

#if UNITY_EDITOR
        private string m_DefaultPath = "Assets/AllTests/VFXTests/SDFTests/";
        private void SaveWithComputeBuffer(RenderTexture tex, string assetName = "", bool oneChannel = true)
        {
            ComputeBuffer tmpBufferVoxel = new ComputeBuffer(m_dimX * m_dimY * m_dimZ, 4 * sizeof(float));

            int kernelIdCopy = m_computeShader.FindKernel("CopyToBuffer");
            m_computeShader.SetBuffer(kernelIdCopy, "voxelsBuffer", tmpBufferVoxel);
            m_computeShader.SetTexture(kernelIdCopy, "voxels", tex, 0);
            m_computeShader.Dispatch(kernelIdCopy, Mathf.CeilToInt(m_dimX / 8.0f), Mathf.CeilToInt(m_dimY / 8.0f), Mathf.CeilToInt(m_dimZ / 8.0f));
            Texture3D outTexture = new Texture3D(m_dimX, m_dimY, m_dimZ, oneChannel ? TextureFormat.RHalf : TextureFormat.RGBAHalf, false);
            outTexture.filterMode = FilterMode.Bilinear;
            outTexture.wrapMode = TextureWrapMode.Clamp;
            Color[] voxelArray = outTexture.GetPixels(0);
            tmpBufferVoxel.GetData(voxelArray);
            outTexture.SetPixels(voxelArray, 0);
            outTexture.Apply();
            string path = "";
            if (assetName == "")
            {
                path = EditorUtility.SaveFilePanelInProject("Save the SDF as", "SDF", "asset", "");
            }
            else
            {
                path = m_DefaultPath + assetName + ".asset";
            }

            if (path != "")
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(outTexture, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            tmpBufferVoxel.Release();
        }

#endif

        private void CreateGraphicsBufferIfNeeded(ref GraphicsBuffer gb, int length, int stride)
        {
            if (gb != null && gb.count == length && gb.stride == stride)
                return;

            ReleaseGraphicsBuffer(ref gb);
            gb = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, stride);
            m_IsDisposed = false;
        }

        private void ReleaseGraphicsBuffer(ref GraphicsBuffer gb)
        {
            if (gb != null)
                gb.Release();
            gb = null;
        }

        private void CreateRenderTextureIfNeeded(ref RenderTexture rt, RenderTextureDescriptor rtDesc)
        {
            if (rt != null
                && rt.width == rtDesc.width
                && rt.height == rtDesc.height
                && rt.volumeDepth == rtDesc.volumeDepth
                && rt.graphicsFormat == rtDesc.graphicsFormat)
            {
                return;
            }
            ReleaseRenderTexture(ref rt);
            rt = new RenderTexture(rtDesc);
            rt.hideFlags = HideFlags.DontSave;
            rt.Create();
            m_IsDisposed = false;
        }

        private void ReleaseRenderTexture(ref RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            rt = null;
        }

#if UNITY_EDITOR
        internal void SaveToAsset(string assetName = "")
        {
            SaveWithComputeBuffer(m_DistanceTexture);
        }

#endif


        static class ShaderProperties
        {
            internal static int indicesBuffer = Shader.PropertyToID("indices");
            internal static int verticesBuffer = Shader.PropertyToID("vertices");
            internal static int coordFlipBuffer = Shader.PropertyToID("coordFlip");
            internal static int verticesOutBuffer = Shader.PropertyToID("verticesOut");
            internal static int aabbBuffer = Shader.PropertyToID("aabb");
            internal static int worldToClip = Shader.PropertyToID("worldToClip");
            internal static int currentAxis = Shader.PropertyToID("currentAxis");
            internal static int voxelsBuffer = Shader.PropertyToID("voxelsBuffer");
            internal static int rw_trianglesUV = Shader.PropertyToID("rw_trianglesUV");
            internal static int trianglesUV = Shader.PropertyToID("trianglesUV");
            internal static int voxelsTexture = Shader.PropertyToID("voxels");
            internal static int voxelsTmpTexture = Shader.PropertyToID("voxelsTmp");
            internal static int rayMap = Shader.PropertyToID("rayMap");
            internal static int nTriangles = Shader.PropertyToID("nTriangles");
            internal static int minBoundsExtended = Shader.PropertyToID("minBoundsExtended");
            internal static int maxBoundsExtended = Shader.PropertyToID("maxBoundsExtended");
            internal static int maxExtent = Shader.PropertyToID("maxExtent");
            internal static int upperBoundCount = Shader.PropertyToID("upperBoundCount");
            internal static int counter = Shader.PropertyToID("counter");

            //Prefix sum
            internal static int inputBuffer = Shader.PropertyToID("Input");
            internal static int inputCounter = Shader.PropertyToID("inputCounter");
            internal static int auxBuffer = Shader.PropertyToID("auxBuffer");
            internal static int resultBuffer = Shader.PropertyToID("Result");
            internal static int numElem = Shader.PropertyToID("numElem");
            internal static int exclusive = Shader.PropertyToID("exclusive");

            //Copy kernels
            internal static int src = Shader.PropertyToID("src");
            internal static int dest = Shader.PropertyToID("dest");

            //Sign map
            internal static int signMap = Shader.PropertyToID("signMap");
            internal static int threshold = Shader.PropertyToID("threshold");
            internal static int signMapTmp = Shader.PropertyToID("signMapTmp");
            internal static int normalizeFactor = Shader.PropertyToID("normalizeFactor");
            internal static int numNeighbours = Shader.PropertyToID("numNeighbours");
            internal static int passId = Shader.PropertyToID("passId");
            internal static int needNormalize = Shader.PropertyToID("needNormalize");

            //JFA
            internal static int offset = Shader.PropertyToID("offset");

            //Ray Map
            internal static int offsetRayMap = Shader.PropertyToID("offsetRayMap");
            internal static int triangleIDs = Shader.PropertyToID("triangleIDs");
            internal static int accumCounter = Shader.PropertyToID("accumCounter");

            //Distance
            internal static int distanceTexture = Shader.PropertyToID("distanceTexture");
            internal static int sdfOffset = Shader.PropertyToID("sdfOffset");
        }

        internal class Kernels
        {
            internal int inBucketSum = -1;
            internal int blockSums = -1;
            internal int finalSum = -1;
            internal int toTextureNormalized = -1;
            internal int copyTextures = -1;
            internal int jfa = -1;
            internal int distanceTransform = -1;
            internal int copyBuffers = -1;
            internal int generateRayMapLocal = -1;
            internal int rayMapScanX = -1;
            internal int rayMapScanY = -1;
            internal int rayMapScanZ = -1;
            internal int signPass6Rays = -1;
            internal int signPassNeighbors = -1;
            internal int toBlockSumBuffer = -1;
            internal int clearTexturesAndBuffers = -1;
            internal int copyToBuffer = -1;
            internal int generateTrianglesUV = -1;
            internal int conservativeRasterization = -1;
            internal int chooseDirectionTriangleOnly = -1;
            internal int surfaceClosing = -1;
            internal Kernels(ComputeShader computeShader)
            {
                inBucketSum = computeShader.FindKernel("InBucketSum");
                blockSums = computeShader.FindKernel("BlockSums");
                finalSum = computeShader.FindKernel("FinalSum");
                toTextureNormalized = computeShader.FindKernel("ToTextureNormalized");
                copyTextures = computeShader.FindKernel("CopyTextures");
                jfa = computeShader.FindKernel("JFA");
                distanceTransform = computeShader.FindKernel("DistanceTransform");
                copyBuffers = computeShader.FindKernel("CopyBuffers");
                generateRayMapLocal = computeShader.FindKernel("GenerateRayMapLocal");
                rayMapScanX = computeShader.FindKernel("RayMapScanX");
                rayMapScanY = computeShader.FindKernel("RayMapScanY");
                rayMapScanZ = computeShader.FindKernel("RayMapScanZ");
                signPass6Rays = computeShader.FindKernel("SignPass6Rays");
                signPassNeighbors = computeShader.FindKernel("SignPassNeighbors");
                toBlockSumBuffer = computeShader.FindKernel("ToBlockSumBuffer");
                clearTexturesAndBuffers = computeShader.FindKernel("ClearTexturesAndBuffers");
                copyToBuffer = computeShader.FindKernel("CopyToBuffer");
                generateTrianglesUV = computeShader.FindKernel("GenerateTrianglesUV");
                conservativeRasterization = computeShader.FindKernel("ConservativeRasterization");
                chooseDirectionTriangleOnly = computeShader.FindKernel("ChooseDirectionTriangleOnly");
                surfaceClosing = computeShader.FindKernel("SurfaceClosing");
            }
        }
    }
}
