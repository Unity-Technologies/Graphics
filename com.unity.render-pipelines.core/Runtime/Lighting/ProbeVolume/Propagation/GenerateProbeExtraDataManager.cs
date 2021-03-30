using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    // NEW PLAN!
    // - Still go through the physics ray cast. 
    // - Have a dictionary with key <MeshRender> and as value a list of <UVs>.
    // - As a probe requests something to be added is given back a request ticket with an ID that can be used to retrieve back the data when done. 
    // - Have a pool of unwrapped meshes with a RTHandles pool (say 128 with 128x128). One entry per key. 
    // - When pool is full extract the requests from the pool
    // - Put back in extra data that's it, dabadee dabada. 
    public class ProbeDynamicGIExtraDataManager
    {
        private const int kUnwrappedTextureSize = 32;
        private const int kPoolSize = 256;

        private const int kRWComputeBuffersSize = 8192;


        internal struct ExtraDataRequests
        {
            internal Vector2 uv;
            internal int requestIndex;
        }

        internal struct ExtraDataRequestOutput
        {
            internal Vector3 albedo;
        }

        internal struct RequestInput
        {
            internal MeshRenderer renderer;
            internal Mesh mesh;
            internal int subMesh;
        }

        internal Dictionary<RequestInput, List<ExtraDataRequests>> requestsList = new Dictionary<RequestInput, List<ExtraDataRequests>>();
        internal List<ExtraDataRequestOutput> extraRequestsOutput = new List<ExtraDataRequestOutput>();


        internal RTHandle unwrappedPool;
        internal ComputeBuffer readbackBuffer;
        internal ComputeBuffer inputBuffer;

        // TODO_FCC: WILL BE MOVED WHEN MOVING TO HDRP.
        private ComputeShader m_ExtractionShader = null;

        private int nextUnwrappedDst = 0;

        internal int EnqueueRequest(RequestInput input, Vector2 uv)
        {
            ExtraDataRequestOutput output;
            output.albedo = new Vector3(0.0f, 0.0f, 0.0f);


            int requestIndex = extraRequestsOutput.Count;
            extraRequestsOutput.Add(output);

            ExtraDataRequests request;
            request.requestIndex = requestIndex;
            request.uv = uv;

            if (!requestsList.ContainsKey(input))
            {
                requestsList.Add(input, new List<ExtraDataRequests>());
            }

            requestsList[input].Add(request);

            return requestIndex;
        }

        internal bool UnwrapInput(RequestInput input, out bool poolFull)
        {
            poolFull = (nextUnwrappedDst == kPoolSize - 1);


            Material material = input.renderer.sharedMaterials[input.subMesh];

            var passIdx = -1;
            for (int i = 0; i < material.passCount; ++i)
            {
                if (material.GetPassName(i).IndexOf("DynamicGIDataGen") >= 0)
                {
                    passIdx = i;
                    break;
                }

            }
            if (passIdx >= 0)
            {
                material.SetPass(passIdx);
                Graphics.SetRenderTarget(unwrappedPool, 0, CubemapFace.Unknown, nextUnwrappedDst);
                GL.Clear(false, true, Color.black);
                Graphics.DrawMeshNow(input.mesh, Matrix4x4.identity, 0);
            }

            nextUnwrappedDst = (nextUnwrappedDst + 1) % kPoolSize;

            return passIdx >= 0;
        }

        public void ExecutePendingRequests()
        {
            nextUnwrappedDst = 0;
            if (requestsList.Keys.Count > 0)
            {
                bool poolFull = false;
                var keys = requestsList.Keys;
                RequestInput[] keysArray = new RequestInput[keys.Count];
                keys.CopyTo(keysArray, 0);

                int firstKeyForBatch = 0;
                for (int i=0; i < keys.Count; ++i)
                {
                    UnwrapInput(keysArray[i], out poolFull);
                    if (poolFull)
                    {
                        ExtractData(firstKeyForBatch, i);
                        firstKeyForBatch = i + 1;
                    }
                }

                if ((keys.Count - firstKeyForBatch - 1) > 0)
                {
                    ExtractData(firstKeyForBatch, keys.Count-1);
                }
            }
        }

        public void ClearContent()
        {
            requestsList.Clear();
            extraRequestsOutput.Clear();
            nextUnwrappedDst = 0;
        }

        private void PerformDataExtraction(List<ExtraDataRequests> inputs, List<int> dstRequestIndices)
        {
            if (m_ExtractionShader != null)
            {
                CommandBuffer cmd = CommandBufferPool.Get("");

                inputBuffer.SetData(inputs, 0, 0, inputs.Count);

                // ALL THIS SHOULD BE MOVED
                int kernel = 0;
                cmd.SetComputeTextureParam(m_ExtractionShader, kernel, Shader.PropertyToID("_UnwrappedDataPool"), unwrappedPool);
                cmd.SetComputeIntParam(m_ExtractionShader, Shader.PropertyToID("_RequestBatchSize"), inputs.Count);
                cmd.SetComputeBufferParam(m_ExtractionShader, kernel, Shader.PropertyToID("_RequestsInputData"), inputBuffer);
                cmd.SetComputeBufferParam(m_ExtractionShader, kernel, Shader.PropertyToID("_RWRequestsOutputData"), readbackBuffer);

                int dispatchX = (inputs.Count + 63) / 64;
                cmd.DispatchCompute(m_ExtractionShader, kernel, dispatchX, 1, 1);

                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                ExtraDataRequestOutput[] outputs = new ExtraDataRequestOutput[inputs.Count];
                readbackBuffer.GetData(outputs, 0, 0, inputs.Count);

                for (int i = 0; i < inputs.Count; ++i)
                {
                    int dstIndex = dstRequestIndices[i];
                    extraRequestsOutput[dstIndex] = outputs[i];
                }
            }
            else
            {
                Debug.Log("NULL SHADER");
            }
        }

        // Process all inputs
        //      - Unwrap all
        //      - Retrieve data for all
        // If reached the pool size while unwrapping, repeat the above process per pool size.

        private void ExtractData(int keyStart, int keyEnd)
        {
            var keys = requestsList.Keys;
            RequestInput[] keysArray = new RequestInput[keys.Count];
            keys.CopyTo(keysArray, 0);

            List<ExtraDataRequests> inputs = new List<ExtraDataRequests>();
            // Yucky, will fix. 
            List<int> dstRequestIndices = new List<int>();


            for (int i=keyStart; i <= keyEnd; ++i)
            {
                var currKey = keysArray[i];
                var listForKey = requestsList[currKey];
                foreach (var request in listForKey)
                {
                    // Modified the request so that we have texture array index in request index as we don't need the former in the shader. 
                    ExtraDataRequests moddedRequest = request;
                    moddedRequest.requestIndex = i;
                    if (inputs.Count == kRWComputeBuffersSize)
                    {
                        PerformDataExtraction(inputs, dstRequestIndices);
                        dstRequestIndices.Clear();
                        inputs.Clear();
                    }
                    dstRequestIndices.Add(request.requestIndex);
                    inputs.Add(moddedRequest);
                }
            }

            if (inputs.Count > 0)
            {
                PerformDataExtraction(inputs, dstRequestIndices);
            }
        }

        internal ExtraDataRequestOutput RetrieveRequestOutput(int requestIndex)
        {
            Debug.Assert(requestIndex < extraRequestsOutput.Count);
            return extraRequestsOutput[requestIndex];
        }

        private ProbeDynamicGIExtraDataManager()
        { }

        public void Allocate()
        {
            Dispose(); // To avoid double alloc.

#if UNITY_EDITOR
            m_ExtractionShader = (ComputeShader)UnityEditor.AssetDatabase.LoadAssetAtPath("Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/Propagation/ExtractGIData.compute", typeof(ComputeShader));
#endif

            unwrappedPool = RTHandles.Alloc(kUnwrappedTextureSize, kUnwrappedTextureSize, slices: kPoolSize, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "Extra Data Dynamic GI Pool");
            readbackBuffer = new ComputeBuffer(kRWComputeBuffersSize, Marshal.SizeOf<ExtraDataRequestOutput>());
            inputBuffer = new ComputeBuffer(kRWComputeBuffersSize, Marshal.SizeOf<ExtraDataRequests>());
            nextUnwrappedDst = 0;
        }

        public void Dispose()
        {
            RTHandles.Release(unwrappedPool);
            CoreUtils.SafeRelease(readbackBuffer);
            CoreUtils.SafeRelease(inputBuffer);
            if (m_ExtractionShader != null)
                CoreUtils.Destroy(m_ExtractionShader);
        }

        private static ProbeDynamicGIExtraDataManager s_Instance = new ProbeDynamicGIExtraDataManager();
        public static ProbeDynamicGIExtraDataManager instance { get { return s_Instance; } }



    }

}
