using UnityEngine;

public class SimpleCarContoller : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] string forwardInput = "Vertical";
    [SerializeField] float maxSpeed = 1;
    float speed;
    [SerializeField] Vector3 bodyForward = Vector3.right;
    [SerializeField] float steeringOffset = -3;
    float distanceTravelled;
    [SerializeField] string steeringInput = "Horizontal";
    [SerializeField] float maxAngle = 30;
    [SerializeField] float tireRadius = 45;
    [SerializeField] Vector3 rotationOffset = Vector3.zero;
    [SerializeField] Vector3 wheelForward = Vector3.right;

    [Header("Wheels")]
    [SerializeField] Transform[] forwardWheels;
    [SerializeField] Transform[] frontBrakes;
    [SerializeField] Transform[] rearWheels;

    float steeringAngle;
    Quaternion startRotation;
    
    private void Update()
    {
        // wheel rotation
        steeringAngle += Input.GetAxis(steeringInput);
        steeringAngle = Mathf.Lerp(steeringAngle, 0, Time.deltaTime * (Mathf.Abs(Input.GetAxis(forwardInput)) * 3));
        steeringAngle = Mathf.Clamp(steeringAngle, maxAngle * -1, maxAngle);
        Quaternion forwardRotation = Quaternion.AngleAxis(distanceTravelled * (tireRadius * Mathf.PI), wheelForward) * Quaternion.Euler(rotationOffset);
        Quaternion steeringRotation = Quaternion.AngleAxis(steeringAngle, Vector3.up);
        for (int i = 0; i < forwardWheels.Length; i++)
        {
            forwardWheels[i].localRotation = steeringRotation * forwardRotation;
        }
        for (int i = 0; i < frontBrakes.Length; i++)
        {
            frontBrakes[i].localRotation = steeringRotation;
        }
        for (int i = 0; i < rearWheels.Length; i++)
        {
            rearWheels[i].localRotation = forwardRotation;
        }

        // forward movement
        speed = Mathf.Lerp(speed, maxSpeed * Input.GetAxis(forwardInput) * Time.deltaTime, Time.deltaTime * 5);
        Vector3 moveDirection = transform.TransformDirection(bodyForward) * speed;
        distanceTravelled += speed;
        transform.position += moveDirection;
        Vector3 rotationPoint = transform.position + (transform.TransformDirection(bodyForward) * steeringOffset);
        transform.RotateAround(rotationPoint, Vector3.up, steeringAngle * speed);
    }

    private void OnDrawGizmos()
    {
        Vector3 rotationPoint = transform.position + (transform.TransformDirection(bodyForward) * steeringOffset);
        Gizmos.DrawLine(rotationPoint, rotationPoint + Vector3.up);
    }
}
