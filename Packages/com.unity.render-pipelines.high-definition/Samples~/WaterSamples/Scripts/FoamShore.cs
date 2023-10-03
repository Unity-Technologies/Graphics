using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class FoamShore : MonoBehaviour
{
	
	public Material deformerCustomMaterial = null;
	public WaterDeformer waterDeformer = null;
    public float minAliveTime = 6f;
    public float maxAliveTime = 8f;
    public float maxPosition = 5f;
    public float maxSize = 15f;
	
	// Material property block to be able to override NormalizedAliveTime on the shared material.
    private MaterialPropertyBlock mpb = null;
    private float aliveTime = 0f;
    private float startTime = 0f;
    private DecalProjector m_DecalProjectorComponent = null;

	void Awake()
	{
		m_DecalProjectorComponent = this.GetComponent<DecalProjector>();
		
		// Instantiate a new decal material to avoid sharing it between prefab instances.
        m_DecalProjectorComponent.material = new Material(m_DecalProjectorComponent.material);
	}
	
    public void OnEnable()
    {
		// Create a new material property block and assign it to the water deformer to override some of its material property. 
		mpb = new MaterialPropertyBlock();
		waterDeformer.SetPropertyBlock(mpb);
		
        startTime = Time.realtimeSinceStartup;
        aliveTime = Random.Range(minAliveTime, maxAliveTime);
		
		Reset();
    }


    // Update is called once per frame
    void Update()
    {
        float currentTime = Time.realtimeSinceStartup - startTime;
        float normalizedAliveTime = (currentTime % aliveTime) / aliveTime;
        
        float lerpFactorSize = 1 - Mathf.Abs( 4 * Mathf.Pow((normalizedAliveTime - 0.5f), 2));  //Inverted bell curve
        float lerpFactorOpacity = normalizedAliveTime <= 0.5 ? lerpFactorSize : 1;
        float lerpFactorContrast = normalizedAliveTime <= 0.5 ? 0 : 1 - lerpFactorSize;
        
        Vector3 decalProjectorSize = m_DecalProjectorComponent.size;
        decalProjectorSize.y = Mathf.Lerp(0, maxSize, lerpFactorSize);
        m_DecalProjectorComponent.size = decalProjectorSize;

        Vector3 p = this.transform.localPosition;
        p.x = Mathf.Lerp(0, maxPosition, lerpFactorSize);
        this.transform.localPosition = p;
        
		// Setting decal material
        m_DecalProjectorComponent.material.SetFloat("_Opacity", lerpFactorOpacity);
        m_DecalProjectorComponent.material.SetFloat("_NormalizedAliveTime", normalizedAliveTime);
        m_DecalProjectorComponent.material.SetFloat("_ContrastNormalized", lerpFactorContrast);
		
		// Setting _NormalizedAliveTime on the property block to be able to share materials between deformers.
		mpb.SetFloat("_NormalizedAliveTime", normalizedAliveTime);
		
		Vector2 regionSize = waterDeformer.regionSize;
		regionSize.y = Mathf.Lerp(0, maxSize, lerpFactorSize);
		waterDeformer.regionSize = regionSize;
		
		
		waterDeformer.amplitude = lerpFactorSize * 0.7f;

		// If we are at the end of the animation, disable the decal
        if(normalizedAliveTime > 0.99)
        {
			m_DecalProjectorComponent.material.SetFloat("_Opacity", 0);
			waterDeformer.regionSize = new Vector2(regionSize.x, 0);
			m_DecalProjectorComponent.size = new Vector3(decalProjectorSize.x, 0, decalProjectorSize.z);
            this.gameObject.SetActive(false);
        }
		
        
        
    }
    
    public void Reset()
    {
        int randomDecalIndexX = Random.Range(0,2);  //Random.Range for int is maxExclusive. 
        int randomDecalIndexY = Random.Range(0,2);  //Random.Range for int is maxExclusive. 
        
        m_DecalProjectorComponent.material.SetFloat("_Opacity", 0);
    }
        
}
