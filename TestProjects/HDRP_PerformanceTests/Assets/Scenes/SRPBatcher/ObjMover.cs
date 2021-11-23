using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjMover : MonoBehaviour {

    public GameObject  m_obj;
    public float       m_radius;
    public float       m_speed;
    public float       m_phase0;

    private Vector3 m_origin;
    private float   m_angle;

	// Use this for initialization
	void Start ()
    {
        m_origin = m_obj.transform.position;
        m_angle = m_phase0;
	}

	// Update is called once per frame
	void Update ()
    {
        Vector3 pos = m_origin + new Vector3(m_radius * Mathf.Cos(m_angle), 0.0f, m_radius * Mathf.Sin(m_angle));
        m_obj.transform.position = pos;
        m_angle += Time.fixedDeltaTime * m_speed;
	}
}
