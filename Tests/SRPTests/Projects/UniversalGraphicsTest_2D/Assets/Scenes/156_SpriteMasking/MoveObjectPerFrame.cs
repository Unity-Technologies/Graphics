using System.Collections;
using UnityEngine;

public class MoveObjectPerFrame : MonoBehaviour
{
    public Vector3[] offsets;

    // Use this for initialization
    IEnumerator Start()
    {
        foreach (var p in offsets)
        {
            transform.position += p;
            yield return 0;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 from = transform.position;
        foreach (var p in offsets)
        {
            Gizmos.color = Color.yellow;
            Vector3 to = from + p;
            Gizmos.DrawLine(from, to);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(to, 0.1f);
            from = to;
        }
    }
}
