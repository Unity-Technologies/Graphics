using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    [ExecuteInEditMode]
    public class SimulateTest_B : MonoBehaviour
    {
        void Start()
        {
            var vfx = GetComponent<VisualEffect>();

            vfx.Simulate(0.1f, 10);
            vfx.Simulate(0.05f, 20);
            vfx.Simulate(0.2f, 5);
            vfx.Simulate(0.05f, 20);

            vfx.pause = true;
        }

        void Update()
        {
            var vfx = GetComponent<VisualEffect>();
            vfx.pause = true;
        }
    }
}
