using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static Dictionary<InstanceType, PoolManager> Instances = null;
    public float maxCount = 16;
	
	public enum InstanceType
	{
		Deformer,
		Splash
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
            go.name = "Item_"+i;
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
        
        return null;
    }
}
