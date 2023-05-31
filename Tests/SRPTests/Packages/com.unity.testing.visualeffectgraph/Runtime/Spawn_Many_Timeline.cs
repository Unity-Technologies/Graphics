using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    public class Spawn_Many_Timeline : MonoBehaviour
    {
        [SerializeField] private GameObject m_Prefab;

        void Start()
        {
            for (int instanceIndex = 0; instanceIndex < 8; ++instanceIndex)
            {
                Instantiate(m_Prefab, new Vector3(instanceIndex % 8, 0, (float)instanceIndex / 8), Quaternion.identity);
            }
        }
    }
}
