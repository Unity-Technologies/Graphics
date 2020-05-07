using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class ProbeVolumeManager
    {
        static private ProbeVolumeManager _instance = null;

        internal static ProbeVolumeManager manager
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProbeVolumeManager();
                }
                return _instance;
            }
        }

        public ProbeVolumePositioning positioning;

        private ProbeVolumeManager()
        {
            volumes = new List<ProbeVolume>();
            volumesSelected = new List<ProbeVolume>();

            positioning = new ProbeVolumePositioning();

        #if UNITY_EDITOR
            SubscribeBakingAPI();
        #endif
        }

        ~ProbeVolumeManager()
        {
        #if UNITY_EDITOR
            UnsubscribeBakingAPI();
        #endif
        }

        internal List<ProbeVolume> volumes = null;
        protected internal List<ProbeVolume> volumesSelected = null;

        internal void RegisterVolume(ProbeVolume volume)
        {
            if (volumes.Contains(volume))
                return;

            volumes.Add(volume);      
        }
        internal void DeRegisterVolume(ProbeVolume volume)
        {
            if (!volumes.Contains(volume))
                return;

            volumes.Remove(volume);

#if !PROBEVOLUMES_ENCODING_ADAPTIVE
            HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp != null)
                hdrp.ReleaseProbeVolumeFromAtlas(volume);
#endif
        }

#if UNITY_EDITOR
        void SubscribeBakingAPI()
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnProbesBakeCompleted;
            UnityEditor.Lightmapping.bakeCompleted += OnBakeCompleted;

            UnityEditor.Lightmapping.lightingDataCleared += OnLightingDataCleared;
            UnityEditor.Lightmapping.lightingDataAssetCleared += OnLightingDataAssetCleared;
        }

        void UnsubscribeBakingAPI()
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnProbesBakeCompleted;
            UnityEditor.Lightmapping.bakeCompleted -= OnBakeCompleted;

            UnityEditor.Lightmapping.lightingDataCleared -= OnLightingDataCleared;
            UnityEditor.Lightmapping.lightingDataAssetCleared -= OnLightingDataAssetCleared;
        }

        void OnProbesBakeCompleted()
        {
            var volumesCurrent = (volumesSelected.Count > 0) ? volumesSelected : volumes;
            foreach (var volume in volumesCurrent)
            {
                volume.OnProbesBakeCompleted();
            }
        }

        void OnBakeCompleted()
        {
            var volumesCurrent = (volumesSelected.Count > 0) ? volumesSelected : volumes;
            foreach (var volume in volumes)
            {
                volume.OnBakeCompleted();
            }

            if (volumesSelected.Count > 0)
            {
                // Go through and reenable all non-selected volumes now so that any following bakes will bake everything.
                foreach (ProbeVolume v in volumes)
                {
                    if (volumesSelected.Contains(v))
                        continue;

                    v.ForceBakingEnabled();
                }

                volumesSelected.Clear();
            }
        }

        void OnLightingDataCleared()
        {
            volumesSelected.Clear();

            foreach (var volume in volumes)
            {
                volume.OnLightingDataCleared();
            }
        }

        void OnLightingDataAssetCleared()
        {
            foreach (var volume in volumes)
            {
                volume.OnLightingDataAssetCleared();
            }
        }

        internal static void BakeSelected()
        {
            manager.volumesSelected.Clear();

            foreach (GameObject go in UnityEditor.Selection.gameObjects)
            {
                ProbeVolume probeVolume = go.GetComponent<ProbeVolume>();
                if (probeVolume)
                    manager.volumesSelected.Add(probeVolume);
            }

            foreach (ProbeVolume v in manager.volumes)
            {
                if (manager.volumesSelected.Contains(v))
                    continue;

                v.ForceBakingDisabled();
            }

            UnityEditor.Lightmapping.BakeAsync();
        }
#endif

        private void adaptiveExample()
        {
            ProbeReferenceVolume refvol = new ProbeReferenceVolume(64, 1024 * 1024 * 1204);
            refvol.SetGridDensity(0.25f, 4);

            ProbeReferenceVolume.Volume vol;
            vol.X = new Vector3(1.0f, 0.0f, 0.0f);
            vol.Y = new Vector3(0.0f, 1.0f, 0.0f);
            vol.Z = new Vector3(0.0f, 0.0f, 1.0f);
            vol.corner = new Vector3(3.5f, 2.7f, -1.4f);

            ProbeReferenceVolume.SubdivisionDel subdivDel = (List<ProbeReferenceVolume.Brick> inBricks, List<ProbeReferenceVolume.Brick> outBricks) =>
            {
                outBricks = inBricks;
            };

            // get a list of bricks for this volume
            List<ProbeReferenceVolume.Brick> sortedBricks = new List<ProbeReferenceVolume.Brick>();
            int numProbes;
            refvol.CreateBricks(ref vol, subdivDel, sortedBricks, out numProbes);

            // convert the brick data into actual probe positions
            Vector3[] probePositions = new Vector3[numProbes];
            refvol.ConvertBricks(sortedBricks, probePositions);

            // call lightmappers fragment API here to bake probes
            int jobId = 0x0eefbeef;
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(jobId, probePositions);

            // async magic

            // Fetch results from lightmapper
            var sh        = new Unity.Collections.NativeArray<SphericalHarmonicsL2>(numProbes, Unity.Collections.Allocator.Temp, Unity.Collections.NativeArrayOptions.UninitializedMemory);
            var validity  = new Unity.Collections.NativeArray<float>(numProbes, Unity.Collections.Allocator.Temp, Unity.Collections.NativeArrayOptions.UninitializedMemory);
            var octaDepth = new Unity.Collections.NativeArray<float>(numProbes * 8 * 8, Unity.Collections.Allocator.Temp, Unity.Collections.NativeArrayOptions.UninitializedMemory);
            bool succeeded = UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(jobId, sh, validity, octaDepth);
            Debug.Assert(succeeded);

            // extract L1
            SphericalHarmonicsL1[] shl1 = new SphericalHarmonicsL1[numProbes];
            for (int i = 0; i < shl1.Length; i++)
            {
                shl1[i].shAr = new Vector4(sh[i][0, 3], sh[i][0, 1], sh[i][0, 2], sh[i][0, 0] - sh[i][0, 6]);
                shl1[i].shAg = new Vector4(sh[i][1, 3], sh[i][1, 1], sh[i][1, 2], sh[i][1, 0] - sh[i][1, 6]);
                shl1[i].shAb = new Vector4(sh[i][2, 3], sh[i][2, 1], sh[i][2, 2], sh[i][2, 0] - sh[i][2, 6]);
            }

            // change encoding so L1 is bounded by L0 (this can be folded into the above function
            for (int i = 0; i < shl1.Length; i++)
            {
                // TODO: do the actual calculation, this is just dummy code
                shl1[i].shAr[1] = shl1[i].shAr[1];
                shl1[i].shAr[2] = shl1[i].shAr[2];
                shl1[i].shAr[3] = shl1[i].shAr[3];

                shl1[i].shAg[1] = shl1[i].shAg[1];
                shl1[i].shAg[2] = shl1[i].shAg[2];
                shl1[i].shAg[3] = shl1[i].shAg[3];

                shl1[i].shAb[1] = shl1[i].shAb[1];
                shl1[i].shAb[2] = shl1[i].shAb[2];
                shl1[i].shAb[3] = shl1[i].shAb[3];
            }

            // create a data set of textures that contains the encoded SH data            
            ProbeBrickPool.DataLocation loc = ProbeBrickPool.CreateDataLocation(numProbes, false);
            ProbeBrickPool.FillDataLocation(ref loc, shl1);

            // figure out a way to compress these textures to BC6 and BC7

            // store this somewhere on disk

            // load it back from disk

            // add it to the refvol
        }
    }
}
