using System;

namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    class VFXOutputEventPrefabSpawn : VFXOutputEventHandler
    {
        public override bool canExecuteInEditor => true;

        public uint instanceCount
        {
            get { return m_InstanceCount; }
            set { m_InstanceCount = value; ResetInstances(); }
        }

        public GameObject prefabToSpawn
        {
            get { return m_PrefabToSpawn; }
            set { m_PrefabToSpawn = value; DisposeInstances(); ResetInstances(); }
        }
        public bool parentInstances
        {
            get { return m_ParentInstances; }
            set { m_ParentInstances = value; ResetInstances(); }
        }

        [Header("Prefab Instances")]
        [SerializeField, Tooltip("The maximum number of prefabs that can be active at a time")]
        protected uint m_InstanceCount = 5;
        [SerializeField, Tooltip("The prefab to enable upon event received. Prefabs are created as hidden and stored in a pool, upon enabling this behavior. Upon receiving an event a prefab from the pool is enabled and will be disabled when reaching its lifetime.")]
        protected GameObject m_PrefabToSpawn;
        [SerializeField, Tooltip("Whether to attach prefab instances to current game object. Use this setting to treat position and angle attributes as local space.")]
        protected bool m_ParentInstances;

        [Header("Event Attribute Usage")]
        [Tooltip("Whether to use the position attribute to set prefab position on spawn")]
        public bool usePosition = true;
        [Tooltip("Whether to use the angle attribute to set prefab rotation on spawn")]
        public bool useAngle = true;
        [Tooltip("Whether to use the scale attribute to set prefab localScale on spawn")]
        public bool useScale = true;
        [Tooltip("Whether to use the lifetime attribute to determine how long the prefab will be enabled")]
        public bool useLifetime = true;

        private GameObject[] m_Instances;
        private float[] m_TTLs;

        bool isEditorPreview => Application.isEditor && !Application.isPlaying;

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetInstances();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DisposeInstances();
        }

        private void OnDestroy()
        {
            DisposeInstances();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Workaround call as you can't use neither Destroy() nor DestroyImmediate()
            // in order to destroy GameObjects in OnValidate()
            //
            // https://forum.unity.com/threads/onvalidate-and-destroying-objects.258782/
            
            if(enabled)
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    // In some cases (enter playmode), this delay call is performed *after* object has been destroyed.
                    if (this == null)
                        return;

                    DisposeInstances();

                    if (enabled)
                        ResetInstances();
                };
#endif
        }

        public void ReloadPrefab()
        {
            DisposeInstances();
            ResetInstances();
        }

        void ResetInstances()
        {
            if (m_PrefabToSpawn == null || m_InstanceCount == 0)
            {
                DisposeInstances();
                return;
            }

            // Clean up Instances
            if (m_Instances == null)
            {
                m_Instances = new GameObject[instanceCount];
                for (int i = 0; i < m_Instances.Length; i++)
                {
                    m_Instances[i] = InitializeInstance(i);
                }
            }
            else
            {
                int maxCount = Math.Max((int)instanceCount, (int)instanceCount);
                GameObject[] newArray = new GameObject[instanceCount];
                for (int i = 0; i < maxCount; i++)
                {
                    if (i < instanceCount) // Create new Instances
                    {
                        newArray[i] = InitializeInstance(i);
                    }
                    else if (i > instanceCount) // Reap old Instances
                    {
                        DisposeInstance(m_Instances[i]);
                    }
                    else // Otherwise transfer data, and reset.
                    {
                        newArray[i] = m_Instances[i];
                        newArray[i].SetActive(false);
                    }
                }
                m_Instances = newArray;
            }

            // Initialize TTLs
            m_TTLs = new float[instanceCount];
        }

        private GameObject InitializeInstance(int index)
        {
            var go = Instantiate(m_PrefabToSpawn);
            go.name = $"#{index} - {m_PrefabToSpawn.name}";
            go.SetActive(false);
            //go.hideFlags = HideFlags.DontSaveInEditor;
            go.hideFlags = HideFlags.HideAndDontSave;
            return go;
        }

        private void DisposeInstances()
        {
            if (m_Instances == null)
                return;

            for (int i = 0; i < m_Instances.Length; i++)
            {
                DisposeInstance(m_Instances[i]);
            }
            m_Instances = null;
        }

        private void DisposeInstance(GameObject instance)
        {
            if (instance == null)
                return;

            if (isEditorPreview)
                DestroyImmediate(instance);
            else
                Destroy(instance);
        }

        static readonly int k_PositionID = Shader.PropertyToID("position");
        static readonly int k_AngleID = Shader.PropertyToID("angle");
        static readonly int k_ScaleID = Shader.PropertyToID("scale");
        static readonly int k_LifetimeID = Shader.PropertyToID("lifetime");

        public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
        {
            if (m_Instances == null)
                return;

            // Find Slot
            int freeIdx = -1;
            for(int i = 0; i < m_Instances.Length; i++)
            {
                if (m_Instances[i] == null)
                    m_Instances[i] = InitializeInstance(i);

                if(!m_Instances[i].activeInHierarchy)
                {
                    freeIdx = i;
                    break;
                }
            }

            if (freeIdx == -1)   // can't find an slot available, discarding...
                return;

            // Activate Item if available
            var obj = m_Instances[freeIdx];
            obj.SetActive(true);
            obj.transform.parent = m_ParentInstances ? transform : null;

            if (usePosition && eventAttribute.HasVector3(k_PositionID))
            {
                if(parentInstances)
                    obj.transform.localPosition = eventAttribute.GetVector3(k_PositionID);
                else
                    obj.transform.position = eventAttribute.GetVector3(k_PositionID);
            }

            if (useAngle && eventAttribute.HasVector3(k_AngleID))
            {
                if (parentInstances)
                    obj.transform.localEulerAngles = eventAttribute.GetVector3(k_AngleID);
                else
                    obj.transform.eulerAngles = eventAttribute.GetVector3(k_AngleID);
            }

            if (useScale && eventAttribute.HasVector3(k_ScaleID))
                obj.transform.localScale = eventAttribute.GetVector3(k_ScaleID);

            if (useLifetime && eventAttribute.HasFloat(k_LifetimeID))
                m_TTLs[freeIdx] = eventAttribute.GetFloat(k_LifetimeID);
            else
                m_TTLs[freeIdx] = float.NegativeInfinity;

            
            var handlers = obj.GetComponentsInChildren<VFXOutputEventPrefabAttributeHandler>();
            foreach(var handler in handlers)
            {
                handler.OnVFXEventAttribute(eventAttribute, m_VisualEffect);
            }

        }

        void Update()
        {
            // Manage Life/Death before gathering events
            UpdateInstanceTTL();
        }

        void UpdateInstanceTTL()
        {
            if (m_Instances == null)
                return;

            float dt = Time.deltaTime;

            for(int i = 0; i< m_Instances.Length; i++)
            {
                // Negative infinity for non-time managed
                if (m_TTLs[i] == float.NegativeInfinity)
                    continue;   

                // Else, manage time
                if (m_TTLs[i] <= 0.0f && m_Instances[i].activeInHierarchy)
                    m_Instances[i].SetActive(false);
                else
                    m_TTLs[i] -= dt;
            }
        }
    }
}
