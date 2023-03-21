using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class FoamShore : MonoBehaviour
{
    public float minAliveTime = 6f;
    public float maxAliveTime = 8f;
    public float maxPosition = 5f;
    public float maxSize = 15f;
    
    private float aliveTime = 0f;
    private float startTime = 0f;
    private DecalProjector m_DecalProjectorComponent;

    public void OnEnable()
    {
        startTime = Time.realtimeSinceStartup;
        
        aliveTime = Random.Range(minAliveTime, maxAliveTime);
        m_DecalProjectorComponent = this.GetComponent<DecalProjector>();
        
        // Instantiate a new decal material to avoid sharing it between prefab instances.
        m_DecalProjectorComponent.material = new Material(m_DecalProjectorComponent.material);
        Reset();
    }
    
    void Start()
    {
        this.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        float currentTime = Time.realtimeSinceStartup - startTime;
        float normalizedAliveTime = (currentTime % aliveTime) / aliveTime;
        
        float lerpFactorSize = 1 - Mathf.Abs( 4 * Mathf.Pow((normalizedAliveTime - 0.5f), 2));  //Inverted bell curve
        float lerpFactorOpacity = normalizedAliveTime <= 0.5 ? lerpFactorSize : 1;
        float lerpFactorContrast = normalizedAliveTime <= 0.5 ? 0 : 1 - lerpFactorSize;
        
        Vector3 size = m_DecalProjectorComponent.size;
        size.y = Mathf.Lerp(0, maxSize, lerpFactorSize);
        m_DecalProjectorComponent.size = size;

        Vector3 p = this.transform.localPosition;
        p.x = Mathf.Lerp(0, maxPosition, lerpFactorSize);
        this.transform.localPosition = p;
        
        m_DecalProjectorComponent.material.SetFloat("_Opacity", lerpFactorOpacity);
        m_DecalProjectorComponent.material.SetFloat("_NormalizedAliveTime", normalizedAliveTime);
        m_DecalProjectorComponent.material.SetFloat("_ContrastNormalized", lerpFactorContrast);
        
        if(normalizedAliveTime > 0.99){
            this.transform.parent.GetComponent<ShoreDecalTrigger>().setIsInstantiated(false);
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
