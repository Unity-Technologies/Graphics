using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class DisableVFXWhenDone : MonoBehaviour
{

	private VisualEffect vfx = null;
	
	void Start()
	{
		vfx = this.GetComponent<VisualEffect>();
	}
	
    // Update is called once per frame
    void Update()
    {
        if (vfx != null && !vfx.HasAnySystemAwake())
		{
			this.gameObject.SetActive(false);
		}
    }
}
