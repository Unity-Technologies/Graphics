using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SineMover : MonoBehaviour
{
    public Vector3 amount;
    public float speed;

    private Vector3 startPos;
    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = startPos + amount * Mathf.Sin(Time.time * speed);
    }
}
