using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Type
{
    Circle,
    Side,
    Rotate
};
public class Motion : MonoBehaviour
{

    float angle = 0;
    float dist = 0;
    float dir = 1.0f;
    public float speed = (2 * Mathf.PI) / 5.0f;
    public float length = 5;
    public Type type = Type.Circle;
    public Vector3 axisSide = new Vector3(1, 0, 0);
    public bool perFrame = true;

    Vector3 originalPos;
    Vector3 originalRot;

    // Start is called before the first frame update
    void Start()
    {
        originalPos = transform.position;
        originalRot = transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        if (type == Type.Circle)
        {
            if(perFrame)
            {
                float fixedStep = speed / 30.0f;
                angle += fixedStep;
            }
            else
            {
                angle += speed * Time.deltaTime;
            }
            float x = Mathf.Cos(angle) * length;
            float y = Mathf.Sin(angle) * length;
            Vector3 offset = new Vector3(x, y, 0.0f);
            transform.position = originalPos + offset;
        }
        if (type == Type.Side)
        {
            float step = speed * Time.deltaTime;
            if (perFrame)
            {
                step = speed / 30.0f;
            }
            dist += dir * step;
            if (dist > length)
            {
                dir = -1.0f;
            }
            else if (dist < -length)
            {
                dir = 1.0f;
            }

            Vector3 offset = axisSide.normalized * dist;// new Vector3(dist, 0.0f, 0.0f);
            transform.position = originalPos + offset;

        }
        if (type == Type.Rotate)
        {
            float step = speed * Time.deltaTime;
            if (perFrame)
            {
                step = speed / 30.0f;
            }

            dist += dir * step / 180.0f;

            Vector3 rot = originalRot + axisSide * dist;

            transform.Rotate(axisSide, step);
        }
    }
}
