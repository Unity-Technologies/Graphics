using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CubeController : MonoBehaviour
{
    public Material[] materials;
    public Material fogOfWarMaterial;
    public Material cookieMaterial;
    public float defaultCubeHeight = 0.22f;

    [Header("Movement")]
    public float moveForce = 10f;
    public float maxSpeed = 5f;
    public float groundDrag = 5f;
    public float airDrag = 1f;

    [Header("Jump")]
    public float jumpForce = 4f;

    [Header("Dash")]
    public float dashForce = 30f;
    public float doubleTapTime = 0.25f;

    public float speedRotation = 50f;


    [Header("Static Detection")]
    public float velocityEpsilon = 0.05f;
    public float accelerationEpsilon = 0.2f;

    private bool isStatic;

    private float staticTime = 0.0f;
    // Min time to be considered static
    private float minStaticTime = 0.25f;
    private Vector3 lastVelocity;

    private Rigidbody rb;
    private bool isGrounded;
    private bool canDoubleJump;

    private float lastLeftTapTime;
    private float lastRightTapTime;


    private float currentVerticalCompression = 0f;

    private bool jumped = false;
    private float jumpTime = 0f;
    private int currentDirection = 1;
    private float currentInput = 0f;
    private float targetInput = 0f;

    private float currentAcceleration = 0f;
    public float speedAnimAcceleration = 10f;
    public float speedAnimMultiplier = 1f;

    public AnimationCurve jumpingAnimation;
    public float jumpingAnimationDuration = 0.5f;

    private bool landed = false;
    private float landTime = 0f;
    public AnimationCurve landingAnimation;
    public float landingAnimationDuration = 0.5f;

    private Vector3 lastPosition = Vector3.zero;

    private GameObject light = null;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        light = this.transform.Find("Point Light").gameObject;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleDashInput();
        ApplyDrag();
        LimitSpeed();
    }

    void HandleMovement()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            currentInput = -1f;
            currentDirection = -1;
            targetInput = -currentInput/2f;
            speedAnimMultiplier = 1.5f;
        }
        else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            currentInput = 1f;
            currentDirection = 1;
            targetInput = -currentInput/2f;
            speedAnimMultiplier = 1.5f;
        }
        else
        {
            //This is for the cube to "wobble" back when no input
            currentInput = Mathf.Lerp(currentInput, targetInput, Time.deltaTime * speedAnimAcceleration * speedAnimMultiplier);

            if(Mathf.Abs(currentInput - targetInput) < 0.01f)
            {
                targetInput /= -2f;
                speedAnimMultiplier *= 1.5f;
                //If the wobble is finished, we stop.
                if(targetInput < 0.01f)
                {
                    currentInput = 0f;
                    targetInput = 0f;
                    speedAnimMultiplier = 1f;
                }
            }
        }

        rb.AddForce(Vector3.right * currentInput * moveForce, ForceMode.Force);
    }

    void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (isGrounded)
            {
                Jump();
                canDoubleJump = true;
            }
            else if (canDoubleJump)
            {
                Jump();
                canDoubleJump = false;
            }
        }


    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
        jumped = true;
        jumpTime = Time.realtimeSinceStartup;
    }

    void HandleDashInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
        {
            if (Time.time - lastLeftTapTime < doubleTapTime)
                Dash(Vector3.left);

            lastLeftTapTime = Time.time;
        }

        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
        {
            if (Time.time - lastRightTapTime < doubleTapTime)
                Dash(Vector3.right);

            lastRightTapTime = Time.time;
        }
    }

    void Dash(Vector3 direction)
    {
        currentInput = direction.x * dashForce;
        rb.AddForce(direction * dashForce, ForceMode.Impulse);
    }

    void ApplyDrag()
    {
        rb.linearDamping = isGrounded ? groundDrag : airDrag;
        if(isGrounded)
        {
            
        }
    }

    void LimitSpeed()
    {
        Vector3 vel = rb.linearVelocity;

        if (Mathf.Abs(vel.x) > maxSpeed)
        {
            vel.x = Mathf.Sign(vel.x) * maxSpeed;
            rb.linearVelocity = vel;
        }
    }

    public void Reset()
    {
        this.transform.localPosition = Vector3.zero;
        this.transform.localEulerAngles = new Vector3(0,180,0);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 acceleration = (velocity - lastVelocity) / Time.fixedDeltaTime;

        bool nearlyStopped =
            velocity.magnitude < velocityEpsilon &&
            acceleration.magnitude < accelerationEpsilon;

        if (nearlyStopped)
            staticTime += Time.fixedDeltaTime;
        else
            staticTime = 0;

        isStatic = staticTime >= minStaticTime;

        // This is to be able to re jump once when you are stuck on a wall. 
        if (isStatic)
            canDoubleJump = true;

        lastVelocity = velocity;
    }

    private void LateUpdate()
    {
        if (jumped)
        {
            float t = (Time.realtimeSinceStartup - jumpTime) / jumpingAnimationDuration;
            currentVerticalCompression = jumpingAnimation.Evaluate(t);
            if(t >= 1)
            {
                jumped = false;
                currentVerticalCompression = 1f;
            }

            foreach (Material mat in materials)
                mat.SetFloat("_VerticalStretch", currentVerticalCompression);
        }

        if (landed)
        {
            float t = (Time.realtimeSinceStartup - landTime) / landingAnimationDuration;
            currentVerticalCompression = landingAnimation.Evaluate(t);
            if (t >= 1)
            {
                landed = false;
                currentVerticalCompression = 0f;
            }

            foreach (Material mat in materials)
                mat.SetFloat("_VerticalStretch", currentVerticalCompression);
        }

        if(cookieMaterial)
            cookieMaterial.SetFloat("_VerticalStretch", currentVerticalCompression);

        float currentVerticalDisplacement = currentVerticalCompression * 0.3f;
        float currentCubeHeight = defaultCubeHeight * (1 + currentVerticalDisplacement);
        float currentLightHeight = currentCubeHeight / 2f;
        

        currentAcceleration = Mathf.Lerp(currentAcceleration, currentInput, Time.deltaTime * speedAnimAcceleration);
        float currentHorizontalDisplacement = (currentAcceleration * currentDirection);
        float currentLightStretch = Mathf.Lerp(0, (defaultCubeHeight / 2f) * 0.15f, currentHorizontalDisplacement);
        foreach (Material mat in materials)
            mat.SetFloat("_HorizontalStretch", currentAcceleration * -currentDirection);

        if (light != null)
            light.transform.localPosition = new Vector3(currentLightStretch, currentLightHeight, 0);


        float currentDirectionNormalized = Mathf.InverseLerp(-1, 1, currentDirection);
        float yRotTarget = currentDirection > 0 ? 180 : 0;// Mathf.Lerp(0, 180)
        float yRot = this.transform.localEulerAngles.y;
        yRot = Mathf.Lerp(yRot, yRotTarget, Time.deltaTime * speedRotation);
        this.transform.localEulerAngles = new Vector3(0, yRot, 0);
        //fogOfWarMaterial.SetInt("_Direction", currentDirection);


    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
        {
            landed = true;
            landTime = Time.realtimeSinceStartup;
            isGrounded = true;
            jumped = false;
        }

        //if (Mathf.Abs(collision.contacts[0].normal.x) > 0.5f)
        //{
            
        //}
    }
}
