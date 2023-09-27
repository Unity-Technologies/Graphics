using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

public class DeformationManager : MonoBehaviour
{
    
    private float startTime = 0;
    private float currentTime = 0;
    
    public float defaultAmplitude = 0.075f;
    private float amplitude = 0f;
    
    public float defaultSpeed = 0.5f;
    private float speed = 0f;
    
    void Awake()
    {
        if (speed == 0)
            speed = defaultSpeed;
        
        if (amplitude == 0)
            amplitude = defaultAmplitude;
    }
    
    // Start is called before the first frame update
    void OnEnable()
    {
        startTime = Time.realtimeSinceStartup;
        this.transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        currentTime = Time.realtimeSinceStartup - startTime;

        float normalizedTime = Mathf.Clamp01(currentTime * speed);
        if(normalizedTime >= 1)
        {
            this.gameObject.SetActive(false);
            currentTime = 0;
            amplitude = defaultAmplitude;
        }
        
		// We animate the deformer to make it look like a travelling ripple
        this.transform.localScale = new Vector3((normalizedTime + 0.1f), 1, (normalizedTime + 0.1f));
        this.GetComponent<WaterDeformer>().amplitude = (1 - normalizedTime) * amplitude;
            
    }
    
    public void SetAmplitude(float f)
    {
        amplitude = f;
    }
    
    public void SetSpeed(float f)
    {
        speed = f;
    }

    
}
