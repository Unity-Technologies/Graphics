using UnityEngine;

public class Door : MonoBehaviour
{
    public bool open = false;
    [SerializeField] string switchButton = "";
    [SerializeField] KeyCode switchKey = KeyCode.None;

    [Header("Configure")]
    [Range(0.5f, 10)]
    [SerializeField] float rotationSpeed = 1;
    [Range(-360, 360)]
    [SerializeField] float closeAngle;
    [Range(-360, 360)]
    [SerializeField]
    float openAngle = 90;
    [SerializeField] Vector3 rotationOffset = Vector3.right;
    [SerializeField] Vector3 rotationAxis = Vector3.up;

    private float lerp = 0;
    private float angle;
    private Vector3 startPosition;

    // requires collider on gameobject to register mouse clicks
    private void OnMouseDown()
    {
        Animate();
    }    

    public void Animate()
    {
        open = !open;
    }

    private void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        if (switchButton != "") if (Input.GetButtonDown(switchButton)) Animate();
        if (switchKey != KeyCode.None) if (Input.GetKeyDown(switchKey)) Animate();

        if (open)
        {
            lerp += (Time.deltaTime * rotationSpeed);
        } else
        {
            lerp -= (Time.deltaTime * rotationSpeed);
        }

        lerp = Mathf.Clamp(lerp, 0, 1);
        angle = Mathf.Lerp(closeAngle, openAngle, lerp);
        transform.localEulerAngles = rotationAxis * angle;
        transform.localPosition =  transform.localRotation * (rotationOffset *-1) + (startPosition - (rotationOffset *-1));
    }

    #region Gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white * 0.8f;
        Gizmos.DrawLine(transform.position + transform.TransformDirection(rotationOffset), transform.position + transform.TransformDirection(rotationOffset) + (transform.TransformDirection(rotationAxis)));
        Gizmos.DrawLine(transform.position + transform.TransformDirection(rotationOffset), transform.position + transform.TransformDirection(rotationOffset) + (transform.TransformDirection(rotationAxis * -1)));
        Gizmos.DrawSphere(transform.position + transform.TransformDirection(rotationOffset), 0.05f);
        Gizmos.color = Color.red * 0.9f;
        Gizmos.DrawLine(transform.position + transform.TransformDirection(rotationOffset), transform.position + transform.TransformDirection(rotationOffset) + Quaternion.AngleAxis(closeAngle - angle, transform.TransformDirection(rotationAxis)) * transform.right);
        Gizmos.color = Color.green * 0.9f;
        Gizmos.DrawLine(transform.position + transform.TransformDirection(rotationOffset), transform.position + transform.TransformDirection(rotationOffset) + Quaternion.AngleAxis(openAngle - angle, transform.TransformDirection(rotationAxis)) * transform.right);
    }
    #endregion
}
