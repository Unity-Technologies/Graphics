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
        [SerializeField]
        bool m_UseGameViewCamera = true;

        /// <summary>
        /// Distance from the light's anchor point
        /// </summary>
        public float distance { get{ return m_Distance; } set{ m_Distance = value; } }
        /// <summary>
        /// Should Up be in World or Camera Space
        /// </summary>
        public bool upIsWorldSpace { get { return m_UpIsWorldSpace; } set { m_UpIsWorldSpace = value; } }
        /// <summary>
        /// Select if we use GameView or SceneView
        /// </summary>
        public bool useGameViewCamera { get { return m_UseGameViewCamera; } set { m_UseGameViewCamera = value; } }

        /// <summary>
        /// Position of the light's anchor point
        /// </summary>
        public Vector3 anchorPosition
        {
            get { return transform.position + transform.forward * m_Distance; }
        }

        public void Update()
        {
        }
    }
}
