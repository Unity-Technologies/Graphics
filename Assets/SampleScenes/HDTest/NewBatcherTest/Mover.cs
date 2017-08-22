using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class Mover : MonoBehaviour {


    Transform[] m_Transforms;
    float       m_Phase0;
    private bool m_Pause = false;

    // Use this for initialization
    void Start ()
    {
        m_Transforms = (Object.FindObjectsOfType(typeof(GameObject)) as GameObject[]).Where(o => o.name.Contains("Cube") || o.name.Contains("Capsule") || o.name.Contains("Cylinder") || o.name.Contains("Sphere")).Select(g=>g.transform).ToArray();
    }

    // Update is called once per frame
    void Update ()
    {
        if ( !m_Pause )
        {
            float innerPhase = m_Phase0;
            foreach (Transform t in m_Transforms)
            {
                float yd = 2.0f * Mathf.Sin(innerPhase);
                t.localPosition = new Vector3(t.localPosition.x, Mathf.Abs(yd), t.localPosition.z);
                innerPhase += 0.01f;
            }
            m_Phase0 += Time.deltaTime * 2.0f;
        }
    }

    void OnGUI()
    {
        m_Pause = GUI.Toggle(new Rect(10, 10, 200, 20), m_Pause, "Pause");
    }

}
