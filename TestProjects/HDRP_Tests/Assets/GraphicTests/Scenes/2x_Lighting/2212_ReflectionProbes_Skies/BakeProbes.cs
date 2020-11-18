using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;

public class BakeProbes : MonoBehaviour
{
    public ReflectionProbe[] refProbe;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (var probe in refProbe)
        {
            Lightmapping.BakeReflectionProbe(probe, "Assets/" + probe.ToString() + ".hdr");
            HDAdditionalReflectionData addRefData = probe.gameObject.GetComponent<HDAdditionalReflectionData>();

            Texture texture = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + probe.ToString() + ".hdr", typeof(Texture));
            addRefData.bakedTexture = texture;
            yield return null;
        }
    }

}
