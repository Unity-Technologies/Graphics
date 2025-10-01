using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[ExecuteAlways]
[ExecuteInEditMode]
public class LookAt : MonoBehaviour
{
    public Transform target = null;

    // Update is called once per frame
    void Start()
    {
        if (target != null)
        {
            Vector3 targetPostition = new Vector3(target.position.x,
                                       this.transform.position.y,
                                       target.position.z);
            Vector3 relativePos = targetPostition - transform.position;

            // the second argument, upwards, defaults to Vector3.up
            Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
            transform.rotation = rotation;
        }
    }

    private void OnEnable()
    {
        Start();
    }
}
