using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static Dictionary<InstanceType, PoolManager> Instances = null;
    public float maxCount = 16;
    // Check this if you want to re-use the item after they have been used once if they stay active. 
    public bool recycle = false;
    
    private int recycleIndex = 0;
	
	public enum InstanceType
	{
		Deformer,
		Splash,
		Ball
	}

	public InstanceType type;
    public GameObject prefab;

    void Awake()
    {
        if (Instances == null)
			Instances = new Dictionary<InstanceType, PoolManager>();

		if (!Instances.ContainsKey(type))
			Instances.Add(type, this);
    }
    
    void Start()
    {
        for(int i=0; i<maxCount; i++)
        {
            GameObject go = Instantiate(prefab);
            go.transform.parent = this.transform;
            go.name = type.ToString()+"_"+i;
            go.SetActive(false);
        }
    }
    
    public GameObject getNextAvailable()
    {
        foreach(Transform child in this.transform)
        {
            if(!child.gameObject.activeSelf)   
                return child.gameObject;
        }
        
        // This is to re-use items that have already been used to avoid stopping the system.
        if (recycle)
        {
            if (this.transform.childCount > recycleIndex)
            {
                // If we already circled through all items we go back to the first one. 
                if (recycleIndex >= (maxCount-1))
                    recycleIndex = 0;
                
                GameObject recycled = this.transform.GetChild(recycleIndex).gameObject;
                recycleIndex++;
                return recycled;
                
            }
        }
        
        return null;
    }
}
