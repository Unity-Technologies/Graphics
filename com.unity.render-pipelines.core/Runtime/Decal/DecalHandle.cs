namespace UnityEngine.Rendering
{
    public class DecalHandle
    {
        public const int kInvalidIndex = -1;

        public DecalHandle(int index, int materialID)
        {
            m_MaterialID = materialID;
            m_Index = index;
        }

        public static bool IsValid(DecalHandle handle)
        {
            if (handle == null)
                return false;
            if (handle.m_Index == kInvalidIndex)
                return false;
            return true;
        }

        public int m_MaterialID;    // identifies decal set
        public int m_Index;         // identifies decal within the set
    }
}
