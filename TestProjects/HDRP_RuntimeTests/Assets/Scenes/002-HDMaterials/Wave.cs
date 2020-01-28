using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Wave : MonoBehaviour
{
    public float amplitude = 1;
    public float shift = 0.1f;
    public float speed = 0.5f;

    [HideInInspector]
    public float time;

    List<Transform> children = new List<Transform>();
    List<Vector3> startPos = new List<Vector3>();

    void OnEnable()
    {
        children.Clear();
        startPos.Clear();
        foreach (Transform child in transform)
        {
            children.Add(child);
            startPos.Add(new Vector3(child.transform.position.x, transform.position.y, transform.position.z));
        }
    }

    void Update()
    {
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] == null)
            {
                OnEnable();
                break;
            }

            children[i].transform.position = startPos[i] + children[i].transform.forward * Mathf.Sin(children[i].transform.position.x * shift + time * speed) * amplitude;
        }
    }
}
