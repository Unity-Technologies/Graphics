using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A generic Volume component holding a <see cref="VolumeProfile"/>.
    /// </summary>
    [HelpURL(Documentation.baseURLHDRP + Documentation.version + Documentation.subURL + "Volumes" + Documentation.endURL)]
    [ExecuteAlways]
    [AddComponentMenu("Miscellaneous/Volume")]
    public class Volume : MonoBehaviour
    {
        /// <summary>
        /// Specifies whether to apply the Volume to the entire Scene or not.
        /// </summary>
        [Tooltip("When enabled, HDRP applies this Volume to the entire Scene.")]
        public bool isGlobal = true;

        /// <summary>
        /// The Volume priority in the stack. A higher value means higher priority. This supports negative values.
        /// </summary>
        [Tooltip("Sets the Volume priority in the stack. A higher value means higher priority. You can use negative values.")]
        public float priority = 0f;

        /// <summary>
        /// The outer distance to start blending from. A value of 0 means no blending and Unity applies
        /// the Volume overrides immediately upon entry.
        /// </summary>
        [Tooltip("Sets the outer distance to start blending from. A value of 0 means no blending and Unity applies the Volume overrides immediately upon entry.")]
        public float blendDistance = 0f;

        /// <summary>
        /// The total weight of this volume in the Scene. 0 means no effect and 1 means full effect.
        /// </summary>
        [Range(0f, 1f), Tooltip("Sets the total weight of this Volume in the Scene. 0 means no effect and 1 means full effect.")]
        public float weight = 1f;

        /// <summary>
        /// The shared Profile that this Volume uses.
        /// Modifying <c>sharedProfile</c> changes every Volumes that uses this Profile and also changes
        /// the Profile settings stored in the Project.
        /// </summary>
        /// <remarks>
        /// You should not modify Profiles that <c>sharedProfile</c> returns. If you want
        /// to modify the Profile of a Volume, use <see cref="profile"/> instead.
        /// </remarks>
        /// <seealso cref="profile"/>
        public VolumeProfile sharedProfile = null;

        /// <summary>
        /// Gets the first instantiated <see cref="VolumeProfile"/> assigned to the Volume.
        /// Modifying <c>profile</c> changes the Profile for this Volume only. If another Volume
        /// uses the same Profile, this clones the shared Profile and starts using it from now on.
        /// </summary>
        /// <remarks>
        /// This property automatically instantiates the Profile and make it unique to this Volume
        /// so you can safely edit it via scripting at runtime without changing the original Asset
        /// in the Project.
        /// Note that if you pass your own Profile, you must destroy it when you finish using it.
        /// </remarks>
        /// <seealso cref="sharedProfile"/>
        public VolumeProfile profile
        {
            get
            {
                if (m_InternalProfile == null)
                {
                    m_InternalProfile = ScriptableObject.CreateInstance<VolumeProfile>();

                    if (sharedProfile != null)
                    {
                        foreach (var item in sharedProfile.components)
                        {
                            var itemCopy = Instantiate(item);
                            m_InternalProfile.components.Add(itemCopy);
                        }
                    }
                }

                return m_InternalProfile;
            }
            set => m_InternalProfile = value;
        }

        internal VolumeProfile profileRef => m_InternalProfile == null ? sharedProfile : m_InternalProfile;

        /// <summary>
        /// Checks if the Volume has an instantiated Profile or if it uses a shared Profile.
        /// </summary>
        /// <returns><c>true</c> if the profile has been instantiated.</returns>
        /// <seealso cref="profile"/>
        /// <seealso cref="sharedProfile"/>
        public bool HasInstantiatedProfile() => m_InternalProfile != null;

        // Needed for state tracking (see the comments in Update)
        int m_PreviousLayer;
        float m_PreviousPriority;
        VolumeProfile m_InternalProfile;

        void OnEnable()
        {
            m_PreviousLayer = gameObject.layer;
            VolumeManager.instance.Register(this, m_PreviousLayer);
        }

        void OnDisable()
        {
            VolumeManager.instance.Unregister(this, gameObject.layer);
        }

        void Update()
        {
            // Unfortunately we need to track the current layer to update the volume manager in
            // real-time as the user could change it at any time in the editor or at runtime.
            // Because no event is raised when the layer changes, we have to track it on every
            // frame :/
            UpdateLayer();

            // Same for priority. We could use a property instead, but it doesn't play nice with the
            // serialization system. Using a custom Attribute/PropertyDrawer for a property is
            // possible but it doesn't work with Undo/Redo in the editor, which makes it useless for
            // our case.
            if (priority != m_PreviousPriority)
            {
                VolumeManager.instance.SetLayerDirty(gameObject.layer);
                m_PreviousPriority = priority;
            }
        }

        internal void UpdateLayer()
        {
            int layer = gameObject.layer;
            if (layer != m_PreviousLayer)
            {
                VolumeManager.instance.UpdateVolumeLayer(this, m_PreviousLayer, layer);
                m_PreviousLayer = layer;
            }
        }

#if UNITY_EDITOR
        // TODO: Look into a better volume previsualization system
        List<Collider> m_TempColliders;

        void OnDrawGizmos()
        {
            if (m_TempColliders == null)
                m_TempColliders = new List<Collider>();

            var colliders = m_TempColliders;
            GetComponents(colliders);

            if (isGlobal || colliders == null)
                return;

            var scale = transform.localScale;
            var invScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
            Gizmos.color = CoreRenderPipelinePreferences.volumeGizmoColor;

            // Draw a separate gizmo for each collider
            foreach (var collider in colliders)
            {
                if (!collider.enabled)
                    continue;

                // We'll just use scaling as an approximation for volume skin. It's far from being
                // correct (and is completely wrong in some cases). Ultimately we'd use a distance
                // field or at least a tesselate + push modifier on the collider's mesh to get a
                // better approximation, but the current Gizmo system is a bit limited and because
                // everything is dynamic in Unity and can be changed at anytime, it's hard to keep
                // track of changes in an elegant way (which we'd need to implement a nice cache
                // system for generated volume meshes).
                switch (collider)
                {
                    case BoxCollider c:
                        Gizmos.DrawCube(c.center, c.size);
                        break;
                    case SphereCollider c:
                        // For sphere the only scale that is used is the transform.x
                        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * scale.x);
                        Gizmos.DrawSphere(c.center, c.radius);
                        break;
                    case MeshCollider c:
                        // Only convex mesh m_Colliders are allowed
                        if (!c.convex)
                            c.convex = true;

                        // Mesh pivot should be centered or this won't work
                        Gizmos.DrawMesh(c.sharedMesh);
                        break;
                    default:
                        // Nothing for capsule (DrawCapsule isn't exposed in Gizmo), terrain, wheel and
                        // other m_Colliders...
                        break;
                }
            }

            colliders.Clear();
        }

#endif
    }
}
