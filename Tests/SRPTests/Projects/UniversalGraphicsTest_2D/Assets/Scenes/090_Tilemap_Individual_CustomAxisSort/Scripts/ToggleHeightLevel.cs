using UnityEngine;
using UnityEngine.Tilemaps;

public class ToggleHeightLevel : MonoBehaviour
{
    public float height = 0.0f;
    public Collider2D[] enableCollider;
    public Collider2D[] disableCollider;

    private void OnTriggerExit2D(Collider2D other)
    {
        var go = other.gameObject;
        if (go == null || !other.gameObject.CompareTag("Player") || go.transform.parent.position.z == height)
            return;

        if (enableCollider != null)
        {
            foreach (var collider in enableCollider)
                collider.gameObject.SetActive(true);
        }
        if (disableCollider != null)
        {
            foreach (var collider in disableCollider)
                collider.gameObject.SetActive(false);
        } 

        var position = go.transform.position;
        var newPosition = new Vector3(position.x, position.y, height);
        go.transform.parent.position = newPosition;
    }
}
