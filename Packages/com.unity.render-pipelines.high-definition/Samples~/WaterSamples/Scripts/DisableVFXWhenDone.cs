using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class DisableVFXWhenDone : MonoBehaviour
{

    public float delay = 0.1f;
	private VisualEffect vfx = null;
	private float startTime = 0f;
    
    
	void OnEnable()
	{
		vfx = this.GetComponent<VisualEffect>();
        startTime = Time.realtimeSinceStartup;
	}
	
    // Update is called once per frame
    void Update()
    {
        // We don't start to check if the VFX has any system awake until a few ms to avoid having it disabled before it even started. 
        if (Time.realtimeSinceStartup - startTime >= delay)
        {
            if (vfx != null && !vfx.HasAnySystemAwake())
            {
                this.gameObject.SetActive(false);
            }
        }
    }
}
