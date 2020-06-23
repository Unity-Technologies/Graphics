using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    class EllipsoidOccluderManager
    {
        static private EllipsoidOccluderManager _instance = null;

        public static EllipsoidOccluderManager manager
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EllipsoidOccluderManager();
                }
                return _instance;
            }
        }

        private EllipsoidOccluderManager()
        {
            volumes = new List<EllipsoidOccluder>();
        }

        private List<EllipsoidOccluder> volumes = null;

        public void RegisterCapsule(EllipsoidOccluder volume)
        {
            volumes.Add(volume);
        }

        public void DeRegisterCapsule(EllipsoidOccluder volume)
        {
            if (volumes.Contains(volume))
            {
                volumes.Remove(volume);
            }
        }

        public bool ContainsVolume(EllipsoidOccluder volume) => volumes.Contains(volume);

        public List<EllipsoidOccluder> PrepareEllipsoidOccludersData(CommandBuffer cmd, HDCamera currentCam, float time)
        {
            return volumes;
        }
    }
}