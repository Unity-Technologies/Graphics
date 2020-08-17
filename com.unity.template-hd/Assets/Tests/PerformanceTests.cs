using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Unity.PerformanceTesting;
using UnityEngine.Profiling;
using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

//documentation:
//https://docs.unity3d.com/Packages/com.unity.test-framework.performance@1.3/manual/index.html?_ga=2.60015021.168522641.1574682439-1601175435.1544520344
public class PerformanceTests : IPrebuildSetup
{
	private static string[] ScenesToTest()
	{
		return new[]
		{
			"SampleScene", 		
		};
	}
	
	public void Setup(){}
	
	public static IEnumerable<ProfilingSampler> GetMarkers(HDCamera hDCamera, bool profileEveryMarkers, string[] markers)
    {
        yield return hDCamera.profilingSampler;
        if(profileEveryMarkers || markers.Length > 0){
			foreach (var val in System.Enum.GetValues(typeof(HDProfileId)))
			{
				//If the list is empty or the list contains the markers
				if(markers.Length > 0 && Array.Exists(markers, element => element == val.ToString()) || markers.Length <= 0){
					yield return ProfilingSampler.Get((HDProfileId)val);
				}
				
			}
		}
    }


    [UnityTest, Performance, Version("1")]
    public IEnumerator Test_PerformanceTests([ValueSource("ScenesToTest")]string sceneName)
    {
	    SceneManager.LoadScene(sceneName,LoadSceneMode.Single);
	    yield return null;

		//We don't want to run test if there is no setting
		var settings = UnityEngine.Object.FindObjectOfType<PerformanceTestSettingsCustom>();
		if(settings == null) Assert.Ignore("Ignored because PerformanceTestSettingsCustom cannot be found in the scene.");
		
		//Run setup
		settings.SetUp();
		
		// ProfilingSampler profilingSampler = ProfilingSampler.Get(HDProfileId.GBuffer);
		settings.hdCamera.profilingSampler.enableRecording = true;
		
		//Do frame delay manually
		for (int i = 0; i < settings.recordAtFrame; i++) yield return null;

		//SAMPLE BEFORE RUN ==================================
		//Memory
	    Measure.Custom(settings.def_allocatedGfxMem, Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f);
	    Measure.Custom(settings.def_allocatedMem, Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
		
		// if (settings.hdCamera.profilingSampler.cpuElapsedTime > 0)
			// Measure.Custom(settings.cpuSample, settings.hdCamera.profilingSampler.cpuElapsedTime);
		
		// if (settings.hdCamera.profilingSampler.inlineCpuElapsedTime > 0)
			// Measure.Custom(settings.inlineCPUSample, settings.hdCamera.profilingSampler.inlineCpuElapsedTime);
		
		// if (settings.hdCamera.profilingSampler.gpuElapsedTime > 0)
			// Measure.Custom(settings.gpuSample, settings.hdCamera.profilingSampler.gpuElapsedTime);
		
		yield return MeasureProfilingSamplers(GetMarkers(settings.hdCamera, settings.profileEveryMarkers, settings.profilerMarkers), settings.recordAtFrame, settings.measureCount);
		
		//RUN ==================================
		//Profiler
        yield return Measure.Frames()
		// .WarmupCount(settings.frameDelay) //default is 1
		.ProfilerMarkers(settings.profilerMarkers)
		.MeasurementCount(settings.measureCount)
		// .SampleGroup("test")
		.Run();
		
		settings.hdCamera.profilingSampler.enableRecording = false;

		//SAMPLE AFTER RUN ===================================
		//Memory
	    Measure.Custom(settings.def_allocatedGfxMem, Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f);
	    Measure.Custom(settings.def_allocatedMem, Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
    }
	
	protected IEnumerator MeasureProfilingSamplers(IEnumerable<ProfilingSampler> samplers, int warmupFramesCount = 20, int measureFrameCount = 30)
    {
        // Enable all the markers
        foreach (var sampler in samplers)
            sampler.enableRecording = true;

        // Allocate all sample groups:
        var sampleGroups = new Dictionary<ProfilingSampler, (SampleGroup cpu, SampleGroup inlineCPU, SampleGroup gpu)>();
        foreach (var sampler in samplers){
			CreateSampleGroups(sampler);
		}

        // Wait for the markers to be initialized
        for (int i = 0; i < warmupFramesCount; i++)
            yield return null;

        for (int i = 0; i < measureFrameCount; i++)
        {
            foreach (var sampler in samplers){
                MeasureTime(sampler);
			}
            yield return null;
        }

        // disable all the markers
        foreach (var sampler in samplers)
            sampler.enableRecording = false;

        void CreateSampleGroups(ProfilingSampler sampler)
        {
            SampleGroup cpuSample = new SampleGroup("CPU "+sampler.name, SampleUnit.Millisecond, false);
            SampleGroup gpuSample = new SampleGroup("GPU "+sampler.name, SampleUnit.Millisecond, false);
            SampleGroup inlineCPUSample = new SampleGroup("CPU inline "+sampler.name, SampleUnit.Millisecond, false);
            sampleGroups[sampler] = (cpuSample, inlineCPUSample, gpuSample);
        }

        void MeasureTime(ProfilingSampler sampler)
        {
            if (sampler.cpuElapsedTime > 0)
                Measure.Custom(sampleGroups[sampler].cpu, sampler.cpuElapsedTime);
            if (sampler.gpuElapsedTime > 0)
                Measure.Custom(sampleGroups[sampler].gpu, sampler.gpuElapsedTime);
            if (sampler.inlineCpuElapsedTime > 0)
                Measure.Custom(sampleGroups[sampler].inlineCPU, sampler.inlineCpuElapsedTime);
        }
    }
}
