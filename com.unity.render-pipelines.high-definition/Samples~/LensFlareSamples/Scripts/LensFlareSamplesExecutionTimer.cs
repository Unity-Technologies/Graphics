using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LensFlareSamplesExecutionTimer : MonoBehaviour
{
    public float timerLength;
    public UnityEvent timerEvent;

    private float currentTime;

    void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime >= timerLength)
        {
            timerEvent.Invoke();
        }
    }
}
