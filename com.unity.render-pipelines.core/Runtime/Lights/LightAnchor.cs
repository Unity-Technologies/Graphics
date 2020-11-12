namespace UnityEngine
{
    [AddComponentMenu("Rendering/Light Anchor")]
    [RequireComponent(typeof(Light))]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class LightAnchor : MonoBehaviour
    {
        [SerializeField]
        private float m_Distance = 3.0f;
        [SerializeField]
        private bool m_UpIsWorldSpace = true;

        public float distance { get{ return m_Distance; } set{ m_Distance = value; } }
        public bool upIsWorldSpace { get { return m_UpIsWorldSpace; } set { m_UpIsWorldSpace = value; } }

        public void Start()
        {
        }

        public void Update()
        {
        }
    }
}
