using UnityEngine;
using UnityEngine.InputSystem;

public class JumpOnClick : MonoBehaviour
{

    void Update()
    {
        // Check for left mouse button click using the new Input System
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Create a ray from the camera to the mouse position
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;

                // Perform the raycast
                if (Physics.Raycast(ray, out hit))
                {
                    // Try to get the BodyRSUV from the clicked object
                    if (hit.collider.TryGetComponent<BodyRSUV>(out BodyRSUV body))
                        body.SetAnimationState(BodyRSUV.AnimationState.Jump);                  
                }
            }

        }
    }
}
