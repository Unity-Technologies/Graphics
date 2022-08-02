using UnityEngine;

public class AnimatorUpdater : MonoBehaviour
{
    Animator m_Animator;
    public bool pause;

    public float animSpeed = 0.01f;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_Animator.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!pause)
            m_Animator.Update(animSpeed);
    }
}
