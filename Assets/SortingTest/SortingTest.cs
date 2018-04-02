using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortingTest : MonoBehaviour
{
    public ComputeShader sortShader;

    public Material inputMat;
    public Material sortedMat0;
    public Material sortedMat1;
    public Material diffMat;

    private int sortKernelBitonic;
    private int sortKernelMerge;
    private ComputeBuffer inputBuffer;
    private ComputeBuffer sortedBuffer0;
    private ComputeBuffer sortedBuffer1;

    public int kElementCount = 1024;
    public int kGroupCount = 1;

    private int kCount { get { return kElementCount * kGroupCount; } }

    // Use this for initialization
    void Start()
    {
        sortKernelBitonic = -1;
        if (sortShader != null)
            sortKernelBitonic = sortShader.FindKernel("BitonicSort");

        sortKernelMerge = -1;
        if (sortShader != null)
            sortKernelMerge = sortShader.FindKernel("MergeSort");

        inputBuffer = new ComputeBuffer(kCount, 8);
        sortedBuffer0 = new ComputeBuffer(kCount, 8);
        sortedBuffer1 = new ComputeBuffer(kCount, 8);

        InitBuffer();

        inputMat.SetBuffer("buffer", inputBuffer);
        inputMat.SetInt("elementCount", kElementCount);
        inputMat.SetInt("groupCount", kGroupCount);

        sortedMat0.SetBuffer("buffer", sortedBuffer0);
        sortedMat0.SetInt("elementCount", kElementCount);
        sortedMat0.SetInt("groupCount", kGroupCount);

        sortedMat1.SetBuffer("buffer", sortedBuffer1);
        sortedMat1.SetInt("elementCount", kElementCount);
        sortedMat1.SetInt("groupCount", kGroupCount);

        diffMat.SetBuffer("buffer0", sortedBuffer0);
        diffMat.SetBuffer("buffer1", sortedBuffer1);
        diffMat.SetInt("elementCount", kElementCount);
        diffMat.SetInt("groupCount", kGroupCount);
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

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
            InitBuffer();

        // sort
        sortShader.SetInt("elementCount", kCount);

        sortShader.SetBuffer(sortKernelBitonic, "inputSequence", inputBuffer);
        sortShader.SetBuffer(sortKernelBitonic, "sortedSequence", sortedBuffer0);
        sortShader.Dispatch(sortKernelBitonic, kGroupCount, 1, 1);

        sortShader.SetBuffer(sortKernelMerge, "inputSequence", inputBuffer);
        sortShader.SetBuffer(sortKernelMerge, "sortedSequence", sortedBuffer1);
        sortShader.Dispatch(sortKernelMerge, kGroupCount, 1, 1);
    }
}
