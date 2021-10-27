using System;
using UnityEngine;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

public class MoveCameraOnAwake : MonoBehaviour
{
    public int WaitFrames = 0;
    public CameraMovement[] cameramovement;

    [Serializable]
    public struct CameraMovement
    {
        public Transform transform;
        public Vector3 position;
    }

    void Start()
    {
        StartCoroutine(WaitForFrames());
    }

    public IEnumerator WaitForFrames()
    {
        for (int i = 0; i < WaitFrames; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        for (int i = 0; i < cameramovement.Length; i++)
        {
            cameramovement[i].transform.position = cameramovement[i].position;
        }
    }
}
