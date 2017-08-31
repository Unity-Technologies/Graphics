using UnityEngine;
using System.Collections;

public class BoxSpawner : MonoBehaviour
{
    public GameObject boxTemplate;

    private float elapsed;

    // Use this for initialization
    void Start()
    {
        elapsed = 0;
    }

    // Update is called once per frame
    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed > 1)
        {
            elapsed = 0;
            var go = Instantiate(boxTemplate);
            var pos = new Vector2(Random.Range(-5.0f, 5.0f), 7.0f);
            go.transform.position = pos;
            Destroy(go, 10.0f);
        }
    }
}
