using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Light))]
[ExecuteInEditMode]
public class TimeOfDay : MonoBehaviour
{
    [Tooltip("Time of day normalized between 0 and 24h. For example 6.5 amount to 6:30am.")]
    public float timeOfDay = 12f;

    [SerializeField]
    [Tooltip("Sets the speed at which the time of day passes.")]
    float timeSpeed = 1f;

    // Paris Office coordinates. 
    float latitude = 48.83402f;
    float longitude = 2.367259f;

    // Arbitrary date to have the sunset framed in the camera frustum. 
    DateTime date = new DateTime(2024, 4, 21).Date;
    DateTime time;

    [SerializeField, HideInInspector]
	private GUIStyle sliderStyle;

    static internal TimeOfDay instance;

	private void OnEnable()
	{
        instance = this;
	}

	private void Awake()
    {
        GetHoursMinutesSecondsFromTimeOfDay(out var hours, out var minutes, out var seconds);
        time = date + new TimeSpan(hours, minutes, 0);
    }

    private void OnValidate()
    {
        GetHoursMinutesSecondsFromTimeOfDay(out var hours, out var minutes, out var seconds);
        time = date + new TimeSpan(hours, minutes, seconds);
        SetSunPosition();
    }

    void Update()
    {
        timeOfDay += timeSpeed * Time.deltaTime;

        //This is for the variable to loop for easier use.
        if(timeOfDay > 24f)
            timeOfDay = 0f;

        if (timeOfDay < 0f)
            timeOfDay = 24f;

        SetSunPosition();
    }

    void SetSunPosition()
    {
        CalculateSunPosition(time, latitude, longitude, timeOfDay, out var azi, out var alt);

        if (double.IsNaN(azi))
            azi = transform.localRotation.y;

        Vector3 angles = new Vector3((float)alt, (float)azi, 0);
        transform.localRotation = Quaternion.Euler(angles);
    }

    public void CalculateSunPosition(DateTime dateTime, double latitude, double longitude, float timeOfDay, out double outAzimuth, out double outAltitude)
    {
        float declination = -23.45f * Mathf.Cos(Mathf.PI * 2f * (dateTime.DayOfYear + 10) / 365f);

        float localSolarTime = timeOfDay;
        float localHourAngle = 15f * (localSolarTime - 12f);
        localHourAngle *= Mathf.Deg2Rad;

        declination *= Mathf.Deg2Rad;
        float latRad = (float)latitude * Mathf.Deg2Rad;

        float lat_sin = Mathf.Sin(latRad);
        float lat_cos = Mathf.Cos(latRad);

        float hour_cos = Mathf.Cos(localHourAngle);

        float declination_sin = Mathf.Sin(declination);
        float declination_cos = Mathf.Cos(declination);

        float elevation = Mathf.Asin(declination_sin * lat_sin + declination_cos * lat_cos * hour_cos);
        float elevation_cos = Mathf.Cos(elevation);
        float azimuth = Mathf.Acos((declination_sin * lat_cos - declination_cos * lat_sin * hour_cos) / elevation_cos);

        elevation *= Mathf.Rad2Deg;
        azimuth *= Mathf.Rad2Deg;

        if (localHourAngle >= 0f)
            azimuth = 360 - azimuth;

        outAltitude = elevation;
        outAzimuth = azimuth;
    }

    private void GetHoursMinutesSecondsFromTimeOfDay(out int hours, out int minutes, out int seconds)
    {
        hours = Mathf.FloorToInt(timeOfDay);
        minutes = Mathf.FloorToInt((timeOfDay - hours) * 60f);
        seconds = Mathf.FloorToInt((timeOfDay - hours - (minutes / 60f)) * 60f * 60f);
    }

    #if UNITY_EDITOR
    void OnGUI()
    {
        DrawWindow();

        // Force repaint of game view
        System.Reflection.Assembly assembly = typeof(EditorWindow).Assembly;
        Type type = assembly.GetType("UnityEditor.GameView");
        EditorUtility.SetDirty(EditorWindow.GetWindow(type, false, null, false));
    }

    internal void DrawWindow()
    {
        Handles.BeginGUI();

        float windowHeight = 15 + 30;
        GUI.Window(0, new Rect(Screen.width * 0.1f, Screen.height * 0.89f, Screen.width * 0.8f, windowHeight), Window_StatusPanel, "", GUIStyle.none);

        Handles.EndGUI();
    }

    public static float hSliderValue = 0.0F;
    internal static void Window_StatusPanel(int windowID)
    {
        if (instance == null)
            return;

        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 16;
        textStyle.normal.textColor = Color.white;
        textStyle.fontStyle = FontStyle.Bold;

        GUI.color = Color.white;
        EditorGUI.BeginChangeCheck();
        GUI.Label(new Rect(Screen.width * 0.0f, 0, Screen.width * 0.1f, 30), "Midnight", textStyle);
        GUI.Label(new Rect(Screen.width * 0.39f, 0, Screen.width * 0.02f, 30), "Noon", textStyle);
        float timeOfDay = GUI.HorizontalSlider(new Rect(Screen.width * 0.015f, 25, Screen.width * 0.77f, 8), instance.timeOfDay, 0.0F, 24.0F, instance.sliderStyle, GUI.skin.horizontalSliderThumb);
        GUI.Label(new Rect(Screen.width * 0.7625f, 0, Screen.width * 0.1f, 30), "Midnight", textStyle);

        if (EditorGUI.EndChangeCheck())
            instance.timeOfDay = timeOfDay;
    }
    #endif
}

#if UNITY_EDITOR
[CustomEditor( typeof( TimeOfDay ) )]
public class DrawLineEditor : Editor
{
    void OnSceneGUI()
    {
        var t = target as TimeOfDay;
        t.DrawWindow();
    }
}
#endif