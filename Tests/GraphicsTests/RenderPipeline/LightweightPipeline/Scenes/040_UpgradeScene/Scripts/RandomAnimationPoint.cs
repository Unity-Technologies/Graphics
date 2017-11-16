using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RandomAnimationPoint : MonoBehaviour {

    public int seed;
	public Animator anim;

    void OnEnable()
	{
        if (anim != null)
        {
            Random.InitState(seed);
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            anim.Play("anim" + Random.Range(0, 2), 0, Random.Range(0.0f, 1.0f));
            anim.speed = 0f;
            anim.Update(Time.deltaTime);
        }
    }

    void OnRenderObject(){
        if(anim != null)
            anim.Update(Time.deltaTime);
    }

}
