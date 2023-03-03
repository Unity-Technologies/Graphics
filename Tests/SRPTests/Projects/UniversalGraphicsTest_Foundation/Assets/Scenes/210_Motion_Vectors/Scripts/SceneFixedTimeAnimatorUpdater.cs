using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneFixedTimeAnimatorUpdater : MonoBehaviour
{
    const float kFrameRate = 30.0f;
    List<Animator> m_SceneAnimators;

    void Start()
    {
        m_SceneAnimators = new List<Animator>();
        var foundObjects = FindObjectsOfType(typeof(Animator),true);
        if (foundObjects != null)
        {
            foreach (var obj in foundObjects)
            {
                if (obj is Animator)
                {
                    Animator newAnimator = obj as Animator;
                    m_SceneAnimators.Add(newAnimator);

                    // We don't want to have the animator enabled as we want to make it's update deterministic
                    // by calling it with a fixed delta time manually
                    newAnimator.enabled = false;
                }
            }
        }
    }

    void Update()
    {
        foreach(var anim in m_SceneAnimators)
            anim.Update(1.0f/kFrameRate);
    }
}
