using UnityEngine;
using System.Collections;

public class ControlSwitch : MonoBehaviour {

	public KeyCode toggleKey = KeyCode.C;
	public GameObject manualController;
	public GameObject automaticController;

	private bool useAutomaticControl = false;
	private const string automaticControlDefaultCMDLineArgument = "-automaticControl";

	void Awake()
	{
		Application.targetFrameRate = 60;
		useAutomaticControl = HasCommandLineArgument (automaticControlDefaultCMDLineArgument);
		SetControllerState (true);
	}

	void Update()
	{
		if (Input.GetKeyDown (toggleKey)) {
			Toggle ();
		}
	}

	void Toggle()
	{
		useAutomaticControl = !useAutomaticControl;
		SetControllerState(useAutomaticControl);
	}

	void SetControllerState(bool useAutomaticControl)
	{
		manualController.SetActive (!useAutomaticControl);
		automaticController.SetActive (useAutomaticControl);
	}

	bool HasCommandLineArgument(string argument)
	{
		string[] passedArguments = System.Environment.GetCommandLineArgs ();
		foreach (string passedArgument in passedArguments) {
			if (passedArgument.Equals(argument))
				return true;
		}
		return false;
	}
}
