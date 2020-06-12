using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class SortingTest : MonoBehaviour
{
    public ComputeShader sortShader;

    public Material inputMat;
    public Material sortedMat0;
    public Material sortedMat1;
    public Material diffMat;

    private int sortKernelBitonic;
    //private int sortKernelMerge;
    private int mergeKernel;

    private ComputeBuffer inputBuffer;
    private ComputeBuffer sortedBuffer;
    private ComputeBuffer[] scratchBuffer = new ComputeBuffer[2];
    private ComputeBuffer deadElementCount;

    public int kElementCount = 1024;
    public int kGroupCount = 1;

    private int kCount { get { return kElementCount * kGroupCount; } }
    private int m_ElementCount;
    private int m_MaxPassCount;

    // Use this for initialization
    void Start()
    {
        m_ElementCount = kCount;
        m_MaxPassCount = Log2(kGroupCount);

        sortKernelBitonic = -1;
        if (sortShader != null)
            sortKernelBitonic = sortShader.FindKernel("BitonicPrePass");

        //sortKernelMerge = -1;
        //if (sortShader != null)
        //    sortKernelMerge = sortShader.FindKernel("MergeSort");

        mergeKernel = -1;
        if (sortShader != null)
            mergeKernel = sortShader.FindKernel("MergePass");

        inputBuffer = new ComputeBuffer(kCount, 8);
        //sortedBuffer0 = new ComputeBuffer(kCount, 8);
        //sortedBuffer1 = new ComputeBuffer(kCount, 8);

        for (int i = 0; i < 2; ++i)
            scratchBuffer[i] = new ComputeBuffer(kCount, 8);

        InitBuffer();

        /*sortedMat0.SetBuffer("buffer", sortedBuffer0);
        sortedMat0.SetInt("elementCount", kElementCount);
        sortedMat0.SetInt("groupCount", kGroupCount);*/

        /*sortedMat1.SetBuffer("buffer", sortedBuffer1);
        sortedMat1.SetInt("elementCount", kElementCount);
        sortedMat1.SetInt("groupCount", kGroupCount);*/

        /* diffMat.SetBuffer("buffer0", sortedBuffer0);
         diffMat.SetBuffer("buffer1", sortedBuffer1);
         diffMat.SetInt("elementCount", kElementCount);
         diffMat.SetInt("groupCount", kGroupCount);*/

        deadElementCount = new ComputeBuffer(1, 4);
        deadElementCount.SetData(new[] { 0 });
    }

    private struct KVP
    {
        public float key;
        public uint value;
    }

    private void InitBuffer()
    {
        Random.InitState(123456);
        var data = new KVP[kCount];
        for (uint i = 0; i < kCount; ++i)
        {
            KVP kvp;
            kvp.key = Random.value;
            kvp.value = i;
            data[i] = kvp;
        }

        inputBuffer.SetData(data);
    }

    private int Log2(int f)
    {
        return (int)((Mathf.Log(f) / Mathf.Log(2)) + 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            InitBuffer();

        if (Input.GetKey(KeyCode.RightArrow))
            m_ElementCount += 10;

        if (Input.GetKey(KeyCode.LeftArrow))
            m_ElementCount -= 10;

        m_ElementCount = Mathf.Clamp(m_ElementCount, 0, kCount);

        if (Input.GetKeyDown(KeyCode.UpArrow))
            ++m_MaxPassCount;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            --m_MaxPassCount;

        m_MaxPassCount = Mathf.Clamp(m_MaxPassCount, 0, Log2(kGroupCount));

        // sort
        Profiler.BeginSample("Sort");

        sortShader.SetInt("elementCount", m_ElementCount);

        sortShader.SetBuffer(sortKernelBitonic, "inputSequence", inputBuffer);
        sortShader.SetBuffer(sortKernelBitonic, "sortedSequence", scratchBuffer[0]);
        sortShader.SetBuffer(sortKernelBitonic, "deadElementCount", deadElementCount);
        sortShader.Dispatch(sortKernelBitonic, kGroupCount, 1, 1);

        sortedBuffer = scratchBuffer[0];

        int arraySize = kElementCount;
        for (int i = 0; i < m_MaxPassCount; ++i)
        {
            sortShader.SetInt("subArraySize", arraySize);
            sortShader.SetBuffer(mergeKernel, "inputSequence", scratchBuffer[0]);
            sortShader.SetBuffer(mergeKernel, "sortedSequence", scratchBuffer[1]);
            sortShader.SetBuffer(mergeKernel, "deadElementCount", deadElementCount);

            sortShader.Dispatch(mergeKernel, (kElementCount * kGroupCount) / 64, 1, 1);

            sortedBuffer = scratchBuffer[1];

            // swap
            ComputeBuffer tmp = scratchBuffer[0];
            scratchBuffer[0] = scratchBuffer[1];
            scratchBuffer[1] = tmp;

            arraySize <<= 1;
        }

        Profiler.EndSample();

        sortedMat1.SetBuffer("buffer", sortedBuffer);
        sortedMat1.SetInt("totalCount", m_ElementCount);
        sortedMat1.SetInt("elementCount", kElementCount);
        sortedMat1.SetInt("groupCount", kGroupCount);

        inputMat.SetBuffer("buffer", inputBuffer);
        inputMat.SetInt("totalCount", m_ElementCount);
        inputMat.SetInt("elementCount", kElementCount);
        inputMat.SetInt("groupCount", kGroupCount);

        /*sortShader.SetBuffer(sortKernelMerge, "inputSequence", inputBuffer);
        sortShader.SetBuffer(sortKernelMerge, "sortedSequence", sortedBuffer1);
        sortShader.Dispatch(sortKernelMerge, kGroupCount, 1, 1);*/
    }
}
