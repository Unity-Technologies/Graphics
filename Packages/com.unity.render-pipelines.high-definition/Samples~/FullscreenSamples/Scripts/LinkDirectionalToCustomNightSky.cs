using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class LinkDirectionalToCustomNightSky : MonoBehaviour
{
    public Material SkyMat;
    Light SceneDirectionalLight;
    public bool findDirectional = false;

    private void OnEnable()
    {
        findDirLight();
    }

    void Update()
    {
        if (SceneDirectionalLight != null)
        {
            SkyMat.SetVector("_Moonlight_Forward_Direction", SceneDirectionalLight.gameObject.transform.forward);
        }

        if (findDirectional)
        {
            findDirLight();
            if(SceneDirectionalLight != null){print("Directional Light for Custom Night Sky is "+SceneDirectionalLight.name);}
            findDirectional = false;
        }
    }

    void findDirLight()
    {
        foreach (Light light in GameObject.FindObjectsOfType<Light>())
            {
                if(light.type == LightType.Directional)
                {
                    SceneDirectionalLight = light;
                }
            }
    }

}
