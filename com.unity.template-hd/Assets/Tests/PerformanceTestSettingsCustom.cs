using System.Collections;
using System.Collections.Generic;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class PerformanceTestSettingsCustom : MonoBehaviour
{
    //public GameObject objectToMeasure;
    public int recordAtFrame = 20;
    public int measureCount = 1000;

    [Header("AllocatedMemory")]
    public SampleUnit unitAllocatedMem = SampleUnit.Megabyte;

    [Header("AllocatedMemoryForGraphicsDevice")]
    public SampleUnit unitAllocatedGfxDriverMem = SampleUnit.Megabyte;
	
    public Camera cameraToProfile;
	[Header("Profiler Markers")]
	public bool profileEveryMarkers = false;
    public string[] profilerMarkers;
	
    [HideInInspector] public HDCamera hdCamera;
    [HideInInspector] public SampleGroup def_allocatedMem;
    [HideInInspector] public SampleGroup def_allocatedGfxMem;
    [HideInInspector] public SampleGroup cpuSample;
    [HideInInspector] public SampleGroup inlineCPUSample;
    [HideInInspector] public SampleGroup gpuSample;

    private bool doneSetUp = false;

    public void SetUp()
    {
        if(!doneSetUp)
        {
			hdCamera = HDCamera.GetOrCreate(cameraToProfile, 0);
            def_allocatedGfxMem = new SampleGroup("TotalAllocatedMemoryForGraphicsDriver", unitAllocatedGfxDriverMem, true);
            def_allocatedMem = new SampleGroup("TotalAllocatedMemory", unitAllocatedMem, true);
			cpuSample = new SampleGroup("CPU Timing", SampleUnit.Millisecond, false);
            inlineCPUSample = new SampleGroup("CPU Inline Timing", SampleUnit.Millisecond, false);
            gpuSample = new SampleGroup("GPU Timing", SampleUnit.Millisecond, false);

            doneSetUp = true;
        }
    }
}
