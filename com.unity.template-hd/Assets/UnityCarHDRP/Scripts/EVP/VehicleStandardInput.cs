//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;

namespace EVP
{

public class VehicleStandardInput : MonoBehaviour
	{
	public VehicleController target;

	public bool continuousForwardAndReverse = true;

	public enum ThrottleAndBrakeInput { SingleAxis, SeparateAxes };
	public ThrottleAndBrakeInput throttleAndBrakeInput = ThrottleAndBrakeInput.SingleAxis;

	public string steerAxis = "Horizontal";
	public string throttleAndBrakeAxis = "Vertical";
	public string throttleAxis = "Fire2";
	public string brakeAxis = "Fire3";
	public string handbrakeAxis = "Jump";
	public KeyCode resetVehicleKey = KeyCode.Return;

	bool m_doReset = false;


	void OnEnable ()
		{
		// Cache vehicle

		if (target == null)
			target = GetComponent<VehicleController>();
		}


	void Update ()
		{
		if (target == null) return;

		if (Input.GetKeyDown(resetVehicleKey)) m_doReset = true;
		}


	void FixedUpdate ()
		{
		if (target == null) return;

		// Read the user input

		float steerInput = Mathf.Clamp(Input.GetAxis(steerAxis), -1.0f, 1.0f);
		float handbrakeInput = Mathf.Clamp01(Input.GetAxis(handbrakeAxis));

		float forwardInput = 0.0f;
		float reverseInput = 0.0f;

		if (throttleAndBrakeInput == ThrottleAndBrakeInput.SeparateAxes)
			{
			forwardInput = Mathf.Clamp01(Input.GetAxis(throttleAxis));
			reverseInput = Mathf.Clamp01(Input.GetAxis(brakeAxis));
			}
		else
			{
			forwardInput = Mathf.Clamp01(Input.GetAxis(throttleAndBrakeAxis));
			reverseInput = Mathf.Clamp01(-Input.GetAxis(throttleAndBrakeAxis));
			}

		// Translate forward/reverse to vehicle input

		float throttleInput = 0.0f;
		float brakeInput = 0.0f;

		if (continuousForwardAndReverse)
			{
			float minSpeed = 0.1f;
			float minInput = 0.1f;

			if (target.speed > minSpeed)
				{
				throttleInput = forwardInput;
				brakeInput = reverseInput;
				}
			else
				{
				if (reverseInput > minInput)
					{
					throttleInput = -reverseInput;
					brakeInput = 0.0f;
					}
				else if (forwardInput > minInput)
					{
					if (target.speed < -minSpeed)
						{
						throttleInput = 0.0f;
						brakeInput = forwardInput;
						}
					else
						{
						throttleInput = forwardInput;
						brakeInput = 0;
						}
					}
				}
			}
		else
			{
			bool reverse = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

			if (!reverse)
				{
				throttleInput = forwardInput;
				brakeInput = reverseInput;
				}
			else
				{
				throttleInput = -reverseInput;
				brakeInput = 0;
				}
			}

		// Apply input to vehicle

		target.steerInput = steerInput;
		target.throttleInput = throttleInput;
		target.brakeInput = brakeInput;
		target.handbrakeInput = handbrakeInput;

		// Do a vehicle reset

		if (m_doReset)
			{
			target.ResetVehicle();
			m_doReset = false;
			}
		}

	/* Code from Tim Korving for better handling the continuous forward
		and reverse mode. To be adapted and tested.

	void HandleVerticalInputModeInterrupt()                                         // Handle Interrupt input mode for forward reverse
		{
		if (m_MoveState == VERTICAL_INPUT_STATE.STATIONARY)
			{
			if (m_ForwardInput >= m_MinInput)                                       // If forward input...
				{
				ChangeVerticalInputState(VERTICAL_INPUT_STATE.FORWARD);
				m_ThrottleInput = m_ForwardInput;                                   // Throttle is forward input
				m_BrakeInput = 0f;                                                  // Release the brakes
				}
			else if (m_ReverseInput >= m_MinInput)                                  // If reverse input...
				{
				ChangeVerticalInputState(VERTICAL_INPUT_STATE.REVERSE);
				m_ThrottleInput = -m_ReverseInput;                                  // Throttle is inverse reverse input (eek)
				m_BrakeInput = 0f;                                                  // Release the brakes
				}
			else
				{
				ChangeVerticalInputState(VERTICAL_INPUT_STATE.STATIONARY);
				}
			}
		else if (m_MoveState == VERTICAL_INPUT_STATE.FORWARD)
			{
			if (m_EVPController.speed >= m_MinSpeed)                                // Currently in forward motion
				{
				m_ThrottleInput = m_ForwardInput;                                   // Throttle is forward input
				m_BrakeInput = m_ReverseInput;                                      // Brake is reverse input
				}
			else if (m_ForwardInput < m_MinInput && m_ReverseInput < m_MinInput)
				{
				ChangeVerticalInputState(VERTICAL_INPUT_STATE.STATIONARY);
				m_BrakeInput = 0f;
				m_ForwardInput = 0f;
				m_ReverseInput = 0f;
				}
			}
		else if (m_MoveState == VERTICAL_INPUT_STATE.REVERSE)
			{
			if (m_EVPController.speed <= -m_MinSpeed)                               // Currently in backward motion
				{
				m_ThrottleInput = -m_ReverseInput;                                  // Throttle is inverse reverse input (?)
				m_BrakeInput = m_ForwardInput;                                      // Brake is forward input
				}
			else if (m_ForwardInput < m_MinInput && m_ReverseInput < m_MinInput)
				{
				ChangeVerticalInputState(VERTICAL_INPUT_STATE.STATIONARY);
				m_BrakeInput = 0f;
				m_ForwardInput = 0f;
				m_ReverseInput = 0f;
				}
			}
		}
	*/
	}
}