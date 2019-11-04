using System;
using UnityEngine;
using System.Collections;

public class Oscilate : MonoBehaviour
{
    public float speed = 0.1f;
    public Vector2 start;
    public Vector2 stop;

    private float elapsed;
    Vector3 startPosition;
    private float time;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        elapsed += speed * Time.deltaTime;
        float l = Mathf.Sin(elapsed);
        transform.position = Vector2.Lerp(startPosition, l > 0 ? start : stop, Math.Abs(l));
    }
}
