using System;

namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    class VFXOutputEventPrefabSpawn : VFXOutputEventAbstractHandler
    {
        public override bool canExecuteInEditor => true;
        public uint instanceCount => m_InstanceCount;
        public GameObject prefabToSpawn => m_PrefabToSpawn;
        public bool parentInstances => m_ParentInstances;

#pragma warning disable 414, 649
        [SerializeField, Tooltip("The maximum number of prefabs that can be active at a time")]
        uint m_InstanceCount = 5;
        [SerializeField, Tooltip("The prefab to enable upon event received. Prefabs are created as hidden and stored in a pool, upon enabling this behavior. Upon receiving an event a prefab from the pool is enabled and will be disabled when reaching its lifetime.")]
        GameObject m_PrefabToSpawn;
        [SerializeField, Tooltip("Whether to attach prefab instances to current game object. Use this setting to treat position and angle attributes as local space.")]
        bool m_ParentInstances;
#pragma warning restore 414, 649

#if UNITY_EDITOR
        bool m_Dirty = true;
#endif

        [Tooltip("Whether to use the position attribute to set prefab position on spawn")]
        public bool usePosition = true;
        [Tooltip("Whether to use the angle attribute to set prefab rotation on spawn")]
        public bool useAngle = true;
        [Tooltip("Whether to use the scale attribute to set prefab localScale on spawn")]
        public bool useScale = true;
        [Tooltip("Whether to use the lifetime attribute to determine how long the prefab will be enabled")]
        public bool useLifetime = true;

        static readonly GameObject[] k_EmptyGameObjects = new GameObject[0];
        static readonly float[] k_EmptyTimeToLive = new float[0];
        GameObject[] m_Instances = k_EmptyGameObjects;
        float[] m_TimesToLive = k_EmptyTimeToLive;

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach (var instance in m_Instances)
                instance.SetActive(false);
        }

        void OnDestroy()
        {
            DisposeInstances();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            m_Dirty = true;
        }
#endif

        void DisposeInstances()
        {
            foreach (var instance in m_Instances)
            {
                if (instance)
                {
                    if (Application.isPlaying)
                        Destroy(instance);
                    else
                        DestroyImmediate(instance);
                }
            }
            m_Instances = k_EmptyGameObjects;
            m_TimesToLive = k_EmptyTimeToLive;
        }

        static readonly int k_PositionID = Shader.PropertyToID("position");
        static readonly int k_AngleID = Shader.PropertyToID("angle");
        static readonly int k_ScaleID = Shader.PropertyToID("scale");
        static readonly int k_LifetimeID = Shader.PropertyToID("lifetime");

        void UpdateHideFlag(GameObject instance)
        {
            instance.hideFlags = HideFlags.HideAndDontSave;
            //We are using HideInHierarchy to prevent unexpected deletion in edit mode.
            //instance.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        }

        void CheckAndRebuildInstances()
        {
            bool rebuild = m_Instances.Length != m_InstanceCount;
#if UNITY_EDITOR
            if (m_Dirty)
            {
                rebuild = true;
                m_Dirty = false;
            }
#endif
            if (rebuild)
            {
                DisposeInstances();
                if (m_PrefabToSpawn != null && m_InstanceCount != 0)
                {
                    m_Instances = new GameObject[m_InstanceCount];
                    m_TimesToLive = new float[m_InstanceCount];
#if UNITY_EDITOR
                    var prefabAssetType = UnityEditor.PrefabUtility.GetPrefabAssetType(m_PrefabToSpawn);
#endif
                    for (int i = 0; i < m_Instances.Length; i++)
                    {
                        GameObject newInstance = null;
#if UNITY_EDITOR
                        if (prefabAssetType != UnityEditor.PrefabAssetType.NotAPrefab)
                            newInstance = UnityEditor.PrefabUtility.InstantiatePrefab(m_PrefabToSpawn) as GameObject;

                        if (newInstance == null)
                            newInstance = Instantiate(m_PrefabToSpawn);
#else
                        newInstance = Instantiate(m_PrefabToSpawn);
#endif
                        newInstance.name = $"{name} - #{i} - {m_PrefabToSpawn.name}";
                        newInstance.SetActive(false);
                        newInstance.transform.parent = m_ParentInstances ? transform : null;
                        UpdateHideFlag(newInstance);

                        m_Instances[i] = newInstance;
                        m_TimesToLive[i] = float.NegativeInfinity;
                    }
                }
            }
        }

        public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
        {
            CheckAndRebuildInstances();

            int freeIdx = -1;
            for (int i = 0; i < m_Instances.Length; i++)
            {
                if (!m_Instances[i].activeSelf)
                {
                    freeIdx = i;
                    break;
                }
            }

            if (freeIdx != -1)
            {
                var availableInstance = m_Instances[freeIdx];
                availableInstance.SetActive(true);
                if (usePosition && eventAttribute.HasVector3(k_PositionID))
                {
                    if (m_ParentInstances)
                        availableInstance.transform.localPosition = eventAttribute.GetVector3(k_PositionID);
                    else
                        availableInstance.transform.position = eventAttribute.GetVector3(k_PositionID);
                }

                if (useAngle && eventAttribute.HasVector3(k_AngleID))
                {
                    if (parentInstances)
                        availableInstance.transform.localEulerAngles = eventAttribute.GetVector3(k_AngleID);
                    else
                        availableInstance.transform.eulerAngles = eventAttribute.GetVector3(k_AngleID);
                }

                if (useScale && eventAttribute.HasVector3(k_ScaleID))
                    availableInstance.transform.localScale = eventAttribute.GetVector3(k_ScaleID);

                if (useLifetime && eventAttribute.HasFloat(k_LifetimeID))
                    m_TimesToLive[freeIdx] = eventAttribute.GetFloat(k_LifetimeID);
                else
                    m_TimesToLive[freeIdx] = float.NegativeInfinity;

                var handlers = availableInstance.GetComponentsInChildren<VFXOutputEventPrefabAttributeAbstractHandler>();
                foreach (var handler in handlers)
                    handler.OnVFXEventAttribute(eventAttribute, m_VisualEffect);
            } //Else, can't find an instance available, ignoring.
        }

        void Update()
        {
            if (Application.isPlaying || (executeInEditor && canExecuteInEditor))
            {
                CheckAndRebuildInstances();

                var dt = Time.deltaTime;
                for (int i = 0; i < m_Instances.Length; i++)
                {
#if UNITY_EDITOR
                    //Reassign hide flag, "open prefab" could have resetted this hide flag.
                    UpdateHideFlag(m_Instances[i]);
#endif
                    // Negative infinity for non-time managed
                    if (m_TimesToLive[i] == float.NegativeInfinity)
                        continue;

                    // Else, manage time
                    if (m_TimesToLive[i] <= 0.0f && m_Instances[i].activeSelf)
                        m_Instances[i].SetActive(false);
                    else
                        m_TimesToLive[i] -= dt;
                }
            }
            else
            {
                DisposeInstances();
            }
        }
    }
}
