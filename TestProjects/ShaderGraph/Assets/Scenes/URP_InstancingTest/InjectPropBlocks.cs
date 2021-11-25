using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class InjectPropBlocks : MonoBehaviour
{
    public bool randomPerInstance;

    void Update()
    {
        MaterialPropertyBlock props = new MaterialPropertyBlock();

        if (!randomPerInstance)
        {
            Random.InitState(79);
            float r = Random.Range(0.0f, 1.0f);
            float g = Random.Range(0.0f, 1.0f);
            float b = Random.Range(0.0f, 1.0f);
            props.SetColor("InstancedColor", new Color(r, g, b));
            props.SetFloat("InstancedFreq", Random.Range(1.0f, 2.0f));
        }
        else
            Random.InitState(43);

        foreach (Transform childXform in transform)
        {
            if (randomPerInstance)
            {
                float r = Random.Range(0.0f, 1.0f);
                float g = Random.Range(0.0f, 1.0f);
                float b = Random.Range(0.0f, 1.0f);
                props.SetColor("InstancedColor", new Color(r, g, b));
                props.SetFloat("InstancedFreq", Random.Range(1.0f, 2.0f));
            }

            var childObj = childXform.gameObject;
            var renderer = childObj.GetComponent<MeshRenderer>();
            renderer.SetPropertyBlock(props);
        }
    }
}
