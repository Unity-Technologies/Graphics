using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InstantiateObjects : MonoBehaviour
{
    public Vector3 startPosition;
    public int numOfInstancesX = 10;
    public int numOfInstancesZ = 10;
    public float spacing = 1;
    public GameObject[] srcObjs;


    void Awake()
    {
        Vector3 pos = startPosition;

        int counter = 0;
        for (int x = 0; x < numOfInstancesX; x++)
        {
            pos.x = startPosition.x;
            for (int z = 0; z < numOfInstancesZ; z++)
            {
                GameObject obj = Instantiate(srcObjs[counter % srcObjs.Length], Vector3.zero, Quaternion.identity, this.transform);

                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localPosition = pos;

                pos.x += spacing;

                counter++;
            }
            pos.z += spacing;
        }
    }
}
