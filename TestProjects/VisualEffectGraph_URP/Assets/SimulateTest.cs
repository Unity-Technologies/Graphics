using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    [ExecuteInEditMode]
    public class SimulateTest : MonoBehaviour
    {
        void Start()
        {

        }

        void Update()
        {
            var vfx = GetComponent<VisualEffect>();
            vfx.pause = true;
            vfx.Simulate(Time.deltaTime);
        }
    }
}
