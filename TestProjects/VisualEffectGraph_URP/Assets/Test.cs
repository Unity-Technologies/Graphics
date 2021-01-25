using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class Test : MonoBehaviour
{
    void Start()
    {
        
    }

    float m_WaitTime = 0.5f;

    void Update()
    {
        m_WaitTime -= Time.deltaTime;
        if (m_WaitTime < 0)
        {
            m_WaitTime = 0.5f;
            var vfx = gameObject.GetComponent<VisualEffect>();

            var vfxEventAttribute = vfx.CreateVFXEventAttribute();

            vfxEventAttribute.SetFloat("spawnCount", 4.0f);
            vfxEventAttribute.SetVector3("color", new Vector3(1, 0, 0));
            vfx.SendEvent("abcd", vfxEventAttribute);

            vfxEventAttribute.SetFloat("spawnCount", 3.0f);
            vfxEventAttribute.SetVector3("color", new Vector3(0, 1, 0));
            vfx.SendEvent("abcd", vfxEventAttribute);

            vfxEventAttribute.SetFloat("spawnCount", 2.0f);
            vfxEventAttribute.SetVector3("color", new Vector3(0, 0, 1));
            vfx.SendEvent("abcd", vfxEventAttribute);

        }
    }
}
