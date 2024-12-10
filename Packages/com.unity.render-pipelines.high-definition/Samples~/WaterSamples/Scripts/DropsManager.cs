using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

public class DropsManager : MonoBehaviour
{
    
    private float lastTime = 0;

    public WaterSurface waterSurface;
    public DeformationManager deformationManager;
    private float gravity = -9.81f;
    private float lastSpeed = 0f;
    private float speed = 0f;
    private bool done = false;

    // Start is called before the first frame update
    void OnEnable()
    {
        lastTime = Time.time;
        lastSpeed = 0;
        speed = 0;
        this.transform.localPosition = Vector3.zero;
        done = false;
    }

    // Update is called once per frame
    void Update()
    {
        // While the drop is falling.
        if (!done)
        {
            float deltaTime = Time.time - lastTime;
            speed = lastSpeed + gravity * deltaTime;

            float position = this.transform.localPosition.y;
            position += (speed * deltaTime);

            this.transform.localPosition = new Vector3(0, position, 0);
        }
        
        // If the drop goes past the water surface.
        if(this.transform.position.y < waterSurface.transform.position.y && !done)
        {
            done = true;
            this.transform.localPosition = Vector3.zero;
            deformationManager.gameObject.SetActive(true);
            StartCoroutine(WaitForAndInit(1f));
        }
        else
        {
            lastTime = Time.time;
            lastSpeed = speed;
        }


        
    }

    IEnumerator WaitForAndInit(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        OnEnable();
    }



}
