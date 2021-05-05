using UnityEngine;

public class AnimatorUpdater : MonoBehaviour
{
    Animator m_animator;

    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_animator.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        m_animator.Update(0.01f);
    }
}
