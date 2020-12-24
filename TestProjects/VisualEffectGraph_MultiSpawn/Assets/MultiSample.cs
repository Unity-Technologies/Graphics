using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class MultiSample : MonoBehaviour
{
    float m_Wait = 1.0f;
    uint m_SpawnIndex = 0u;

    void Update()
    {
        m_Wait -= Time.deltaTime;
        if (m_Wait < 0.0f)
        {
            m_Wait = 1.0f;
            var spawnCount = Random.Range(10, 30);
            for (int i = 0; i < spawnCount; ++i)
            {
                var eventName = string.Format("fire_{0}", m_SpawnIndex);

                var attr = gameObject.GetComponent<VisualEffect>().CreateVFXEventAttribute();
                var randPos = Random.insideUnitCircle;
                attr.SetVector3("position", new Vector3(randPos.x, 0.0f, randPos.y));
                var color = Color.HSVToRGB(Random.value, 1.0f, 1.0f);
                attr.SetVector3("color", new Vector3(color.r, color.g, color.b));
                gameObject.GetComponent<VisualEffect>().SendEvent(eventName, attr);

                m_SpawnIndex++;
                if (m_SpawnIndex >= 32) m_SpawnIndex = 0u;
            }
        }
    }
}
