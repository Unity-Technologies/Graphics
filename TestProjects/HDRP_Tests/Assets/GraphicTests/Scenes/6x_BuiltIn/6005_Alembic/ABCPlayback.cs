using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Formats.Alembic;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Importer;

public class ABCPlayback : MonoBehaviour
{
    public GameObject[] ListOfThings;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (GameObject obj in ListOfThings )
        {
            //obj.GetComponent<AlembicStreamPlayer>().Time = 1;
        }
        
    }
}
