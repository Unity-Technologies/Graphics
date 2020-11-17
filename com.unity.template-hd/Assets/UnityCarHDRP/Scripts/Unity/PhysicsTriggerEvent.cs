using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PhysicsTriggerEvent : MonoBehaviour
{

    [SerializeField] UnityEvent eventToTrigger;
    float lastTrigger = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.unscaledTime > lastTrigger + 0.2f)
        {
            eventToTrigger.Invoke();
            lastTrigger = Time.unscaledTime;
            if (GetComponent<AudioSource>() != null) GetComponent<AudioSource>().Play();
        }
        
        
    }


}
