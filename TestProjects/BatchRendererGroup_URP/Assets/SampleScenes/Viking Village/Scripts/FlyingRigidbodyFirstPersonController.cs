using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

[RequireComponent(typeof(RigidbodyFirstPersonController))]
public class FlyingRigidbodyFirstPersonController : MonoBehaviour {

	private new Rigidbody rigidbody;
	private RigidbodyFirstPersonController rigidbodyFPC;
	private HeadBob headBob;

	public bool flying = false;
	public float flyingDrag = 5f;
	public float flightToggleTimeThreshold = 0.5f;
	private float lastAscendKeyHit = float.MinValue;

	void Awake()
	{
		rigidbody = GetComponent<Rigidbody> ();
		rigidbodyFPC = GetComponent<RigidbodyFirstPersonController> ();
		headBob = GetComponentInChildren<HeadBob> ();
	}

	void Update ()
	{
		if (AscendKeyDoubleHit())
		{
			flying = !flying;
			rigidbody.useGravity = !flying;
			rigidbodyFPC.enabled = !flying;
			headBob.enabled = !flying;

			if (flying)
			{
				rigidbody.drag = flyingDrag;
			}
		}
		 
		if (flying)
		{
			rigidbodyFPC.mouseLook.LookRotation (transform, rigidbodyFPC.cam.transform);
		}
	}
	
	void FixedUpdate()
	{
		if (flying)
		{
			Vector2 input = GetInput();
			Vector3 verticalInput = Vector3.up * ((Input.GetButton("Jump") ? 1f : 0f) - (Input.GetButton("Crouch") ? 1f : 0f));

			if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) || Mathf.Abs(verticalInput.y) > float.Epsilon)
			{
				Vector3 desiredMove = rigidbodyFPC.cam.transform.forward*input.y + rigidbodyFPC.cam.transform.right*input.x;
				desiredMove += verticalInput;
				desiredMove = desiredMove.normalized*rigidbodyFPC.movementSettings.CurrentTargetSpeed;
				if (rigidbodyFPC.Velocity.sqrMagnitude <
				    (rigidbodyFPC.movementSettings.CurrentTargetSpeed*rigidbodyFPC.movementSettings.CurrentTargetSpeed))
				{
					rigidbody.AddForce(desiredMove, ForceMode.Impulse);
				}
			}
		}
	}

	private Vector2 GetInput()
	{
		Vector2 input = new Vector2
		{
			x = Input.GetAxis("Horizontal"),
			y = Input.GetAxis("Vertical")
		};
		rigidbodyFPC.movementSettings.UpdateDesiredTargetSpeed(input);
		return input;

	}

	private bool AscendKeyDoubleHit()
	{
		bool result = false;
		if (Input.GetButtonDown("Jump"))
		{
			result = Time.time - lastAscendKeyHit < flightToggleTimeThreshold;
			lastAscendKeyHit = Time.time;
		}
		return result;
	}
}
