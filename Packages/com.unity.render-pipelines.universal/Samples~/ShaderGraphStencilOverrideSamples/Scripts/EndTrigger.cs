using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            FindAnyObjectByType<GameManager>().CompleteLevel();
        }
    }
}
