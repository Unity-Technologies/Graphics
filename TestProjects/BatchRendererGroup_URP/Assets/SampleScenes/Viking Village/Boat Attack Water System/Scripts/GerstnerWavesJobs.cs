using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Rendering.Universal;
using WaterSystem.Data;

namespace WaterSystem
{
	/// <summary>
	/// C# Jobs system version of the Gerstner waves implimentation
	/// </summary>
    public static class GerstnerWavesJobs
    {
        //General variables
        public static bool Initialized;
        private static bool _firstFrame = true;
        private static bool _processing;
        private static int _waveCount;
        private static NativeArray<Wave> _waveData; // Wave data from the water system

        //Details for Buoyant Objects
        private static NativeArray<float3> _positions;
        private static int _positionCount;
        private static NativeArray<float3> _wavePos;
        private static NativeArray<float3> _waveNormal;
        private static JobHandle _waterHeightHandle;
        static readonly Dictionary<int, int2> Registry = new Dictionary<int, int2>();

        public static void Init()
        {
            //Wave data
            _waveCount = Water.Instance._waves.Length;
            _waveData = new NativeArray<Wave>(_waveCount, Allocator.Persistent);
            for (var i = 0; i < _waveData.Length; i++)
            {
                _waveData[i] = Water.Instance._waves[i];
            }

            _positions = new NativeArray<float3>(4096, Allocator.Persistent);
            _wavePos = new NativeArray<float3>(4096, Allocator.Persistent);
            _waveNormal = new NativeArray<float3>(4096, Allocator.Persistent);

            Initialized = true;
        }

        public static void Cleanup()
        {
            _waterHeightHandle.Complete();

            //Cleanup native arrays
            _waveData.Dispose();
            _positions.Dispose();
            _wavePos.Dispose();
            _waveNormal.Dispose();
        }

        public static void UpdateSamplePoints(ref NativeArray<float3> samplePoints, int guid)
        {
            CompleteJobs();

            if (Registry.TryGetValue(guid, out var offsets))
            {
                for (var i = offsets.x; i < offsets.y; i++) _positions[i] = samplePoints[i - offsets.x];
            }
            else
            {
                if (_positionCount + samplePoints.Length >= _positions.Length) return;
                
                offsets = new int2(_positionCount, _positionCount + samplePoints.Length);
                Registry.Add(guid, offsets);
                _positionCount += samplePoints.Length;
            }
        }

        public static void GetData(int guid, ref float3[] outPos, ref float3[] outNorm)
        {
            if (!Registry.TryGetValue(guid, out var offsets)) return;
            
            _wavePos.Slice(offsets.x, offsets.y - offsets.x).CopyTo(outPos);
            if(outNorm != null)
                _waveNormal.Slice(offsets.x, offsets.y - offsets.x).CopyTo(outNorm);
        }

        // Height jobs for the next frame
        public static void UpdateHeights()
        {
            if (_processing) return;
            
            _processing = true;

            // Buoyant Object Job
            var waterHeight = new HeightJob()
            {
                WaveData = _waveData,
                Position = _positions,
                OffsetLength = new int2(0, _positions.Length),
                Time = Time.time,
                OutPosition = _wavePos,
                OutNormal = _waveNormal
            };
                
            _waterHeightHandle = waterHeight.Schedule(_positionCount, 32);
                
            JobHandle.ScheduleBatchedJobs();

            _firstFrame = false;
        }

        private static void CompleteJobs()
        {
            if (_firstFrame || !_processing) return;
            
            _waterHeightHandle.Complete();
            _processing = false;
        }

        // Gerstner Height C# Job
        [BurstCompile]
        private struct HeightJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Wave> WaveData; // wave data stroed in vec4's like the shader version but packed into one
            [ReadOnly]
            public NativeArray<float3> Position;

            [WriteOnly]
            public NativeArray<float3> OutPosition;
            [WriteOnly]
            public NativeArray<float3> OutNormal;

            [ReadOnly]
            public float Time;
            [ReadOnly]
            public int2 OffsetLength;

            // The code actually running on the job
            public void Execute(int i)
            {
                if (i < OffsetLength.x || i >= OffsetLength.y - OffsetLength.x) return;
                
                var waveCountMulti = 1f / WaveData.Length;
                var wavePos = new float3(0f, 0f, 0f);
                var waveNorm = new float3(0f, 0f, 0f);

                for (var wave = 0; wave < WaveData.Length; wave++) // for each wave
                {
                    // Wave data vars
                    var pos = Position[i].xz;

                    var amplitude = WaveData[wave].amplitude;
                    var direction = WaveData[wave].direction;
                    var wavelength = WaveData[wave].wavelength;
                    var omniPos = WaveData[wave].origin;
                    ////////////////////////////////wave value calculations//////////////////////////
                    var w = 6.28318f / wavelength; // 2pi over wavelength(hardcoded)
                    var wSpeed = math.sqrt(9.8f * w); // frequency of the wave based off wavelength
                    const float peak = 0.8f; // peak value, 1 is the sharpest peaks
                    var qi = peak / (amplitude * w * WaveData.Length);

                    var windDir = new float2(0f, 0f);

                    direction = math.radians(direction); // convert the incoming degrees to radians
                    var windDirInput = new float2(math.sin(direction), math.cos(direction)) * (1 - WaveData[wave].onmiDir); // calculate wind direction - TODO - currently radians
                    var windOmniInput = (pos - omniPos) * WaveData[wave].onmiDir;

                    windDir += windDirInput;
                    windDir += windOmniInput;
                    windDir = math.normalize(windDir);
                    var dir = math.dot(windDir, pos - (omniPos * WaveData[wave].onmiDir));

                    ////////////////////////////position output calculations/////////////////////////
                    var calc = dir * w + -Time * wSpeed; // the wave calculation
                    var cosCalc = math.cos(calc); // cosine version(used for horizontal undulation)
                    var sinCalc = math.sin(calc); // sin version(used for vertical undulation)

                    // calculate the offsets for the current point
                    wavePos.x += qi * amplitude * windDir.x * cosCalc;
                    wavePos.z += qi * amplitude * windDir.y * cosCalc;
                    wavePos.y += sinCalc * amplitude * waveCountMulti; // the height is divided by the number of waves 

                    ////////////////////////////normal output calculations/////////////////////////
                    var wa = w * amplitude;
                    // normal vector
                    var norm = new float3(-(windDir.xy * wa * cosCalc),
                        1 - (qi * wa * sinCalc));
                    waveNorm += (norm * waveCountMulti) * amplitude;
                }
                OutPosition[i] = wavePos;
                OutNormal[i] = math.normalize(waveNorm.xzy);
            }
        }
    }
}
