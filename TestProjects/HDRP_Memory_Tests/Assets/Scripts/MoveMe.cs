using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveMe : MonoBehaviour
{
    public float _speed = 10;
    public float _direction = 1;

    // Update is called once per frame
    void Update()
    {
        if (transform.position.x > 15)
        {
            _direction = -1;
        }
        if (transform.position.x < 0)
        {
            _direction = 1;
        }
        transform.Translate(new Vector3(_direction, 0, 0) * _speed * Time.deltaTime);
    }
}
