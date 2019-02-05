using System;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    [Flags]
    public enum DrawGroups
    {
        Background = 1 << 0,
        Foreground = 1 << 1,
        Overlay = 1 << 2,
    }
    
    [Serializable]
    public class DrawGroup
    {
        [SerializeField] DrawGroups m_Mask = DrawGroups.Background;
        [SerializeField] bool m_OverrideCamera = false;
        [SerializeField] float m_FOV = 90.0f;
        [SerializeField] LayerMask m_LayerMask = -1;

        public DrawGroups mask
        {
            get => m_Mask;
            set => m_Mask = value;
        }

        public bool overrideCamera
        {
            get => m_OverrideCamera;
            set => m_OverrideCamera = value;
        }

        public float fov
        {
            get => m_FOV;
            set => m_FOV = value;
        }

        public LayerMask layerMask
        {
            get => m_LayerMask;
            set => m_LayerMask = value;
        }
    }
}

