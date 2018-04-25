using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DensityVolumeManager 
    {
        static private DensityVolumeManager _instance = null;
        private DensityVolumeManager()
        {
            volumes = new List<HomogeneousDensityVolume>();
        }

        public static DensityVolumeManager manager
        {
            get 
            {
                if (_instance == null)
                {
                    _instance = new DensityVolumeManager();
                }
                return _instance;
            }
        }

        private List<HomogeneousDensityVolume> volumes = null;

        public void RegisterVolume(HomogeneousDensityVolume volume)
        {
            volumes.Add(volume);
        }

        public void DeRegisterVolume(HomogeneousDensityVolume volume)
        {
            if (volumes.Contains(volume))
            {
                volumes.Remove(volume);
            }
        }

        public HomogeneousDensityVolume[] GetAllVolumes()
        {
            return volumes.ToArray();
        }
    }
}
