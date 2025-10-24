using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(PlayerInput))]
public class CameraManagement : MonoBehaviour
{

    [Header("Cameras")]
    public Camera baseCamera;     
    public Camera overlayCamera;

    private PlayerInput playerInput;
    private InputAction fireAction;

    private float baseFOV;
    private float overlayFOV;
    private bool isOverlayActive = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        // Check if the cameras are corectly assigned
        if (baseCamera == null || overlayCamera == null)
        {
            Debug.LogError("BaseCamera and OverlayCamera have to be assigned in the PlayerInput!");
            enabled = false;
            return;
        }

        // Get FOV
        baseFOV = baseCamera.fieldOfView;
        overlayFOV = overlayCamera.fieldOfView;

        // Prepare URP stack
        var baseData = baseCamera.GetUniversalAdditionalCameraData();
        var overlayData = overlayCamera.GetUniversalAdditionalCameraData();
        overlayData.renderType = CameraRenderType.Overlay;

        if (!baseData.cameraStack.Contains(overlayCamera))
            baseData.cameraStack.Add(overlayCamera);

        // Overlay off at start
        overlayCamera.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        fireAction = playerInput.actions["Fire"];
        fireAction.performed += OnFirePerformed;
        fireAction.canceled += OnFireCanceled;
    }

    private void OnDisable()
    {
        if (fireAction != null)
        {
            fireAction.performed -= OnFirePerformed;
            fireAction.canceled -= OnFireCanceled;
        }
    }

    private void OnFirePerformed(InputAction.CallbackContext ctx)
    {
        isOverlayActive = true;
        overlayCamera.gameObject.SetActive(true);
        baseCamera.fieldOfView = overlayFOV;
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        isOverlayActive = false;
        overlayCamera.gameObject.SetActive(false);
        baseCamera.fieldOfView = baseFOV;
    }

    private void LateUpdate()
    {
        // Synchronise base and overlay camera rotation to avoid a jerky effect
        if (isOverlayActive && overlayCamera != null && baseCamera != null)
        {
            overlayCamera.transform.rotation = baseCamera.transform.rotation;
        }
    }
}

