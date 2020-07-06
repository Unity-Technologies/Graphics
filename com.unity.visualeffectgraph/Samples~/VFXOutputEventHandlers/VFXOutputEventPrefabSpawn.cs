using System;

namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    public class VFXOutputEventPrefabSpawn : VFXOutputEventHandler
    {
        public uint instanceCount 
        { 
            get { return m_InstanceCount; }
            set { ResetInstances(value); }
        }
        [Header("Prefab Instances")]
        [SerializeField]
        protected uint m_InstanceCount = 5;
        public GameObject PrefabToSpawn;

        [Header("Event Attribute Usage")]
        public bool UsePosition = true;
        public bool UseAngle = true;
        public bool UseScale = true;
        public bool UseLifetime = true;

        private GameObject[] m_Instances;
        private float[] m_TTLs;

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetInstances(instanceCount);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            for (int i = 0; i < instanceCount; i++)
            {
                DisposeInstance(m_Instances[i]);
            }
        }

        void ResetInstances(uint newCount)
        {
            // Clean up Instances
            if (m_Instances == null)
            {
                m_Instances = new GameObject[newCount];
                for(uint i = 0; i < instanceCount; i++)
                {
                    m_Instances[i] = InitializeInstance();
                }
            }
            else
            {
                int maxCount = Math.Max((int)instanceCount, (int)newCount);
                GameObject[] newArray = new GameObject[newCount];
                for(int i = 0; i < maxCount; i++)
                {
                    if (i < instanceCount) // Create new Instances
                    {
                        newArray[i] = InitializeInstance();
                    }
                    else if(i > newCount) // Reap old Instances
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

            m_InstanceCount = newCount;
            m_TTLs = new float[newCount];
        }

        GameObject InitializeInstance()
        {
            if (PrefabToSpawn == null)
            {
                Debug.LogWarning("PrefabToSpawn is null : Instantiating default Game Object");
                return new GameObject();
            }
            else
            {
                PrefabToSpawn.SetActive(false);
                var go = Instantiate(PrefabToSpawn);
                go.SetActive(false);
                go.hideFlags = HideFlags.HideAndDontSave;
                return go;
            }
        }

        void DisposeInstance(GameObject instance)
        {
            if (instance == null)
                return;

            if (Application.isEditor && !Application.isPlaying)
                DestroyImmediate(instance);
            else
                Destroy(instance);
        }

        static readonly int positionID = Shader.PropertyToID("position");
        static readonly int angleID = Shader.PropertyToID("angle");
        static readonly int scaleID = Shader.PropertyToID("scale");
        static readonly int lifetimeID = Shader.PropertyToID("lifetime");

        public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
        {
            // Find Slot
            int freeIdx = -1;
            for(int i = 0; i <instanceCount; i++)
            {
                if (m_Instances[i] == null)
                    m_Instances[i] = InitializeInstance();

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

            if (UsePosition && eventAttribute.HasVector3(positionID))
                obj.transform.position = eventAttribute.GetVector3(positionID);

            if (UseAngle && eventAttribute.HasVector3(angleID))
                obj.transform.eulerAngles = eventAttribute.GetVector3(angleID);

            if (UseScale && eventAttribute.HasVector3(scaleID))
                obj.transform.localScale = eventAttribute.GetVector3(scaleID);

            if (UseLifetime && eventAttribute.HasFloat(lifetimeID))
                m_TTLs[freeIdx] = eventAttribute.GetFloat(lifetimeID);
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
            float dt = Time.deltaTime;

            for(int i = 0; i< instanceCount; i++)
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
