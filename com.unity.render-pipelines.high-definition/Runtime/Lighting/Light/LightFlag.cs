namespace UnityEngine.Rendering.HighDefinition
{
    public class LightOcclusionPlane : MonoBehaviour
    {
        [Tooltip("How much to feather the clipped edge")]
        public float m_Feather = 1.0f;

        public OcclusionPlaneData flagData
        {
            get
            {
                return new OcclusionPlaneData
                {
                    plane   = GetFlagPlane(),
                    feather = m_Feather * 0.1f
                };
            }
        }

        Vector4 GetFlagPlane()
        {
            var t = transform;
            var v = t.forward;
            float d = Vector3.Dot(t.position, v);
            return new Vector4(v.x, v.y, v.z, d);
        }

        private void OnValidate()
        {
            m_Feather = Mathf.Max(0, m_Feather);
        }

        void OnDrawGizmosSelected()
        {
            var m = Matrix4x4.zero;
            var t = transform;
            m.SetTRS(t.position, t.rotation, new Vector3(1, 1, 0));
            Gizmos.matrix = m;
            Gizmos.DrawWireSphere(Vector3.zero, 1);
        }
    }
}