namespace UnityEngine.Experimental.Rendering
{
    public class HDLightFlag : MonoBehaviour
    {

        [Tooltip("How much to feather the clipped edge")]
        public float m_Feather = 1.0f;

        [GenerateHLSL]
        public struct LightClipPlaneData
        {
            public Vector4 plane;

            public float feather;
            public Vector3 unused;
        }

        public LightClipPlaneData ClipParams
        {
            get
            {
                return new LightClipPlaneData
                {
                    plane   = GetClipPlaneVector(),
                    feather = m_Feather * 0.1f
                };
            }
        }

        Vector4 GetClipPlaneVector()
        {
            Transform t = transform;
            Vector3 v = t.forward;
            float d = Vector3.Dot(t.position, v);
            return new Vector4(v.x, v.y, v.z, d);
        }

        private void OnValidate()
        {
            m_Feather = Mathf.Max(0, m_Feather);
        }

        void OnDrawGizmosSelected()
        {
            Matrix4x4 m = Matrix4x4.zero;
            Transform t = transform;
            m.SetTRS(t.position, t.rotation, new Vector3(1, 1, 0));
            Gizmos.matrix = m;
            Gizmos.DrawWireSphere(Vector3.zero, 1);
        }
    }
}
