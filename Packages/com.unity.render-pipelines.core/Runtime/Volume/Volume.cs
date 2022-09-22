using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A generic Volume component holding a <see cref="VolumeProfile"/>.
    /// </summary>
    [CoreRPHelpURL("Volumes", "com.unity.render-pipelines.high-definition")]
    [ExecuteAlways]
    [AddComponentMenu("Miscellaneous/Volume")]
    public class Volume : MonoBehaviour, IVolume
    {
        [SerializeField, FormerlySerializedAs("isGlobal")]
        private bool m_IsGlobal = true;

        /// <summary>
        /// Specifies whether to apply the Volume to the entire Scene or not.
        /// </summary>
        [Tooltip("When enabled, the Volume is applied to the entire Scene.")]
        public bool isGlobal
        {
            get => m_IsGlobal;
            set => m_IsGlobal = value;
        }

        /// <summary>
        /// The Volume priority in the stack. A higher value means higher priority. This supports negative values.
        /// </summary>
        [Tooltip("When multiple Volumes affect the same settings, Unity uses this value to determine which Volume to use. A Volume with the highest Priority value takes precedence.")]
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
                        m_InternalProfile.name = sharedProfile.name;

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

        internal List<Collider> m_Colliders = new List<Collider>();

        /// <summary>
        /// The colliders of the volume if <see cref="isGlobal"/> is false
        /// </summary>
        public List<Collider> colliders => m_Colliders;

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

            GetComponents(m_Colliders);
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

#if UNITY_EDITOR
            // In the editor, we refresh the list of colliders at every frame because it's frequent to add/remove them
            GetComponents(m_Colliders);
#endif
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
    }
}
