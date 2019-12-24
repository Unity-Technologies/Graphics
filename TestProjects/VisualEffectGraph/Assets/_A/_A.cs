using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class _A : MonoBehaviour
{
    public GameObject m_LightReference;

    void Start()
    {
        
    }

    class LightInstance
    {
        public GameObject light;
        public float lifeTime;
        public float age;
    }

    List<LightInstance> m_lights = new List<LightInstance>();

    void Update()
    {
        var currentLight = m_lights.ToArray().ToList();
        m_lights.Clear();
        foreach (var light in currentLight)
        {
            light.age += Time.deltaTime;
            if (light.age > light.lifeTime)
            {
                ScriptableObject.DestroyImmediate(light.light);
            }
            else
            {
                float t = light.age / light.lifeTime;
                light.light.GetComponent<Light>().intensity = Mathf.Lerp(0.3f, 0.0f, t);
                m_lights.Add(light);
            }
        }

        var vfx = GetComponent<VisualEffect>();
        var eventAttribute = vfx.CreateVFXEventAttribute();
        for (uint i = 0; i < 2; ++i)
        {
            vfx.WIP_GET_OUTPUT_EVENT(eventAttribute, i);
            var spawnCount = eventAttribute.GetFloat("spawnCount");
            if (spawnCount >= 1.0f)
            {
                var instance = new LightInstance();
                instance.light = GameObject.Instantiate(m_LightReference);
                instance.light.GetComponent<Transform>().position = eventAttribute.GetVector3("position") + new Vector3(0.0f, 0.1f, 0.0f);
                var c = eventAttribute.GetVector3("color");
                instance.light.GetComponent<Light>().color = new Color(c.x, c.y, c.z);
                instance.lifeTime = eventAttribute.GetFloat("lifetime");
                instance.light.SetActive(true);
                m_lights.Add(instance);
            }
        }
    }
}
