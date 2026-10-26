using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Text timeText = null;
    private float timeStart = 0f;

   
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Reset();
    }

    void Update()
    {
        UpdateTime();
        HandleInputs();
    }

    void UpdateTime()
    {
        float timeElapsed = Time.realtimeSinceStartup - timeStart;
        if (timeText != null)
            timeText.text = FormatTime(timeElapsed);

    }

    void HandleInputs()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rKey.isPressed)
            Reset();
    }

    public void CompleteLevel()
    {
        float timeElapsed = Time.realtimeSinceStartup - timeStart;
        Debug.Log("Level Complete! Time: " + FormatTime(timeElapsed));
        Reset();
    }

    public void Reset()
    {
        timeStart = Time.realtimeSinceStartup;
        FindAnyObjectByType<CubeController>().Reset();
    }

    public static string FormatTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);

        int minutes = time.Minutes;
        int secs = time.Seconds;
        int milliseconds = time.Milliseconds;

        return string.Format("{0:00}:{1:00}:{2:000}",
                             minutes, secs, milliseconds);
    }
}
