using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinScale : MonoBehaviour
{
    [SerializeField] Vector3 min = new Vector3(1f, 1f, 1f);
    [SerializeField] Vector3 max = new Vector3(2f, 2f, 2f);
    [SerializeField] float frequency = 1f;
    [SerializeField] float fps = 60;

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale = Vector3.Lerp(min, max, Mathf.Sin(Mathf.PI * frequency * Time.frameCount / fps) * 0.5f + 0.5f);
    }
}
