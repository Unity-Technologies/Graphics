using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class MaskVolumeManager : IMaskVolumeList
    {
        static private MaskVolumeManager _instance = null;

        internal static MaskVolumeManager manager
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MaskVolumeManager();
                }
                return _instance;
            }
        }
        
        List<MaskVolume> m_Volumes = new List<MaskVolume>();

        List<IMaskVolumeList> m_AdditionalMaskLists = new List<IMaskVolumeList>();
        List<MaskVolumeHandle> m_VolumeHandles = new List<MaskVolumeHandle>();

        internal List<MaskVolumeHandle> CollectVolumesToRender()
        {
            m_VolumeHandles.Clear();
            var count = m_Volumes.Count;
            for (int i = 0; i < count; i++)
                m_VolumeHandles.Add(new MaskVolumeHandle(this, i));
            foreach (var list in m_AdditionalMaskLists)
            {
                count = list.GetVolumeCount();
                for (int i = 0; i < count; i++)
                    m_VolumeHandles.Add(new MaskVolumeHandle(list, i));
            }
            return m_VolumeHandles;
        }

        internal void RegisterVolume(MaskVolume volume)
        {
            if (m_Volumes.Contains(volume))
                return;

            m_Volumes.Add(volume);
        }
        
        internal void DeRegisterVolume(MaskVolume volume)
        {
            var index = m_Volumes.IndexOf(volume);
            if (index == -1)
                return;

            m_Volumes.RemoveAt(index);
        }

        public void AddMaskList(IMaskVolumeList list)
        {
            m_AdditionalMaskLists.Add(list);
        }
        
        public void RemoveMaskList(IMaskVolumeList list)
        {
            m_AdditionalMaskLists.Remove(list);
        }

        int IMaskVolumeList.GetVolumeCount() => m_Volumes.Count;
        bool IMaskVolumeList.IsDataAssigned(int i) => m_Volumes[i].IsDataAssigned();
        bool IMaskVolumeList.IsDataUpdated(int i) => m_Volumes[i].dataUpdated;
        Vector3Int IMaskVolumeList.GetResolution(int i) => m_Volumes[i].GetResolution();
        
        Vector3 IMaskVolumeList.GetPosition(int i) => m_Volumes[i].transform.position;
        Quaternion IMaskVolumeList.GetRotation(int i) => m_Volumes[i].transform.rotation;
        ref MaskVolumeArtistParameters IMaskVolumeList.GetParameters(int i) => ref m_Volumes[i].parameters;
        MaskVolume.MaskVolumeAtlasKey IMaskVolumeList.ComputeMaskVolumeAtlasKey(int i) => m_Volumes[i].ComputeMaskVolumeAtlasKey();

        int IMaskVolumeList.GetDataSHL0Length(int i) => m_Volumes[i].GetPayload().dataSHL0.Length;
        void IMaskVolumeList.SetDataSHL0(CommandBuffer cmd, int i, ComputeBuffer buffer) => cmd.SetComputeBufferData(buffer, m_Volumes[i].GetPayload().dataSHL0);

#if UNITY_EDITOR
        bool IMaskVolumeList.IsHiddenInScene(int i) => UnityEditor.SceneVisibilityManager.instance.IsHidden(m_Volumes[i].gameObject);
#endif
    }
}
