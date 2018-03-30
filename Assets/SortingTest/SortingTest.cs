using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortingTest : MonoBehaviour
{
    public ComputeShader sortShader;

    public Material inputMat;
    public Material sortedMat;

    private int sortKernel;
    private ComputeBuffer inputBuffer;
    private ComputeBuffer sortedBuffer;

    public int kElementCount = 1024;
    public int kGroupCount = 1;

    private int kCount { get { return kElementCount * kGroupCount; } }

    // Use this for initialization
    void Start()
    {
        sortKernel = -1;
        if (sortShader != null)
            sortKernel = sortShader.FindKernel("BitonicSort");

        inputBuffer = new ComputeBuffer(kCount, 8);
        sortedBuffer = new ComputeBuffer(kCount, 8);

        InitBuffer();

        inputMat.SetBuffer("buffer", inputBuffer);
        inputMat.SetInt("elementCount", kElementCount);
        inputMat.SetInt("groupCount", kGroupCount);

        sortedMat.SetBuffer("buffer", sortedBuffer);
        sortedMat.SetInt("elementCount", kElementCount);
        sortedMat.SetInt("groupCount", kGroupCount);
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
        sortShader.SetBuffer(sortKernel, "inputSequence", inputBuffer);
        sortShader.SetBuffer(sortKernel, "sortedSequence", sortedBuffer);
        sortShader.SetInt("elementCount", kCount);
        sortShader.Dispatch(sortKernel, kGroupCount, 1, 1);
    }
}
