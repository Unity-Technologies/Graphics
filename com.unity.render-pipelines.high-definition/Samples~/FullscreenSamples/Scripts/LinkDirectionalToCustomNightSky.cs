using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LinkDirectionalToCustomNightSky : MonoBehaviour
{
    public Material SkyMat;
    public Transform Moonlight_transform;

    void Update()
    {
        SkyMat.SetVector("_Moonlight_Forward_Direction", Moonlight_transform.forward);
    }

}
