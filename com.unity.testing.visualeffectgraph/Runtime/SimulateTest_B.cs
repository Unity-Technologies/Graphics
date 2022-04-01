using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    [ExecuteInEditMode]
    public class SimulateTest_B : MonoBehaviour
    {
        float m_WaitTime = 0.1f;

        void Update()
        {
            var vfx = GetComponent<VisualEffect>();
            m_WaitTime -= Time.deltaTime;
            if (m_WaitTime < 0.0f)
            {
                m_WaitTime = 1.0f;

                vfx.Reinit();

                for (int i = 0; i < 2; ++i)
                {
                    vfx.Simulate(0.1f, 10);
                    vfx.Simulate(0.05f, 20);
                    vfx.Simulate(0.2f, 5);
                    vfx.Simulate(0.05f, 20);
                    vfx.Simulate(0.3333f, 3);
                }
            }
            vfx.pause = true;
        }
    }
}
