using UnityEngine;

public class AnimatorUpdater : MonoBehaviour
{
    Animator m_Animator;
    public bool m_Update;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_Animator.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Update)
            m_Animator.Update(0.01f);
    }
}
