using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveObjectSin : MonoBehaviour
{
    public float ClampValue = 1.0f;
    public float Amplitude = 1.0f;
    public float SpeedUp = 1.0f;
    float originalZ = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
        originalZ = gameObject.transform.localPosition.z;
    }

    // Update is called once per frame
    void Update()
    {
        float sinusRange = Mathf.Clamp(Mathf.Sin(Time.time * SpeedUp), -ClampValue, ClampValue);
        if (sinusRange > -ClampValue || sinusRange < ClampValue)
        gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, originalZ + sinusRange * Amplitude);
    }
}
