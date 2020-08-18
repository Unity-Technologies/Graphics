namespace UnityEngine.Rendering
{
    public struct APVRuntimeResources
    {
        public ComputeBuffer index;
        public Texture3D L0;
        public Texture3D L1_R;
        public Texture3D L1_G;
        public Texture3D L1_B;

        public bool IsValid() { return index != null && L0 != null && L1_R != null && L1_G != null && L1_B != null; }
        public void Clear() { index = null; L0 = L1_R = L1_G = L1_B = null; }
    }
}
