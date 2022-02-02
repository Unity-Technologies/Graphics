using UnityEngine;

public class DelayedCookieDisable : MonoBehaviour
{
    private int counter = 0;

    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log($"Start counter: {counter}");
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log($"Update counter: {counter}");
        if (counter > 1)
        {
            Light l = gameObject.GetComponent<Light>();
            l.cookie = null;
        }
        counter++;
    }
}
