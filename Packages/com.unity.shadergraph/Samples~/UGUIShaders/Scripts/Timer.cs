using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A simple Timer to demonstrate Progress Bars
/// </summary>
public class Timer : MonoBehaviour
{
    [SerializeField] float timerMin = 3f, timerMax = 10f;
    float timer = 10f;
    [SerializeField] bool randomStart;

    public UnityEvent<float> timerEvent;

    float _time = 0;

    public float TheTime
    {
        get => _time;
        set
        {
            _time = value;
            if (_time > 1f)
                _time = 0;
            timerEvent?.Invoke(_time);
        }
    }

    private void Start()
    {
        timer = Random.Range(timerMin, timerMax);

        if (randomStart)
            TheTime = Random.Range(0f, 1f);
    }

    private void Update()
    {
        TheTime += Time.deltaTime / timer;
    }
}
