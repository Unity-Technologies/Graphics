using UnityEngine;
using System.Collections;

public class twinkle : MonoBehaviour
{
    private float baseIntensity = 0.0f;
    private Light torchLight = null;
    private Renderer fire = null;
	private Material fireSource = null;

    // Use this for initialization
    void Start()
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
        foreach( Renderer curRenderer in renderers )
        {
            if (curRenderer.gameObject.name == "fx_fire")
            {
                fire = curRenderer;
                break;
            }
			foreach (Material mat in curRenderer.materials)
			{
				if(mat.name.StartsWith("mat_torch_01"))
					fireSource = mat;
			}
        }

        torchLight = gameObject.GetComponentInChildren<Light>();

        if (torchLight && fire)
        {
            baseIntensity = torchLight.intensity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (torchLight && fire)
        {
			torchLight.transform.position = (torchLight.transform.position*0.7f)+(fire.bounds.center*0.3f);
            torchLight.intensity = baseIntensity + fire.bounds.size.magnitude;
        }
		if(fireSource)
		{
			float val = 2.3f + fire.bounds.size.magnitude/3;
			fireSource.SetVector("_EmissionColor", new Vector4(val, val,val, val));
		}
	}
}
