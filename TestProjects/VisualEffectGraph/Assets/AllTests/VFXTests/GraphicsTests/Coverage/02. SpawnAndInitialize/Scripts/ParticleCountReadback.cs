using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

[ExecuteInEditMode]
public class ParticleCountReadback : MonoBehaviour
{

    public VisualEffect vf;
    public float particlesAlive = 0f;
    public TextMesh text;

    // Use this for initialization
    IEnumerator Start()
    {
        while (true)
        {
            if (!vf)
                vf = GetComponent<VisualEffect>();

            particlesAlive = vf.aliveParticleCount;

            if(text)
                text.text = particlesAlive.ToString();
            yield return null;
        }
    }
}