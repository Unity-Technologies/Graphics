using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class CheckAABB : MonoBehaviour
{
    // Start is called before the first frame update
    private VisualEffect vfx;
    private int m_SystemNameID = Shader.PropertyToID("System");
    private uint capacity;
    private GraphicsBuffer aabbBuffer;
    private List<Color> colorList = new List<Color>();
    void Start()
    {
        vfx = GetComponent<VisualEffect>();
        capacity = vfx.GetParticleSystemInfo(m_SystemNameID).capacity;
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnDisable()
    {
        colorList.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if(vfx == null)
             vfx = vfx = GetComponent<VisualEffect>();
        if(capacity == 0u)
            capacity = vfx.GetParticleSystemInfo(m_SystemNameID).capacity;
        aabbBuffer = vfx.GetAabbBuffer(m_SystemNameID);
        if (aabbBuffer != null && aabbBuffer.IsValid())
        {
            float[] aabbArray = new float[capacity * 6u];
            aabbBuffer.GetData(aabbArray);
            for (int i = 0; i < capacity; i++)
            {
                Vector3 minBox = new Vector3(aabbArray[6 * i], aabbArray[6 * i + 1], aabbArray[6 * i + 2]);
                Vector3 maxBox = new Vector3(aabbArray[6 * i + 3], aabbArray[6 * i + 4], aabbArray[6 * i + 5]);
                minBox = transform.TransformPoint(minBox);
                maxBox = transform.TransformPoint(maxBox);
                Vector3 center = 0.5f * (minBox + maxBox);
                Vector3 size = (maxBox - minBox);
                if (colorList.Count < i + 1)
                {
                    Color color = Random.ColorHSV(0.0f, 0.02f, 1, 1, 1, 1, 0.7f, 0.7f);
                    colorList.Add(color);
                }

                Gizmos.color = colorList[i];
                Gizmos.DrawCube(center, size);
            }
        }
    }
}
