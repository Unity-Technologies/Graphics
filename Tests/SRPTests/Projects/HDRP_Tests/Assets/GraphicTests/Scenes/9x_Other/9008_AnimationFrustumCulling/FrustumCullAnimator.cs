using System.Diagnostics;
using System.Numerics;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FrustumCullAnimator : MonoBehaviour
{
    private Animator _anim;
    private Renderer _renderer;
    private Camera _mainCamera;
    private int _frameCounter;

    private void Start()
    {
        _anim = GetComponent<Animator>();
        _renderer = GetComponentInChildren<Renderer>();
        _mainCamera = Camera.main;
        _frameCounter = 0;
    }

    private void Update()
    {
        UnityEngine.Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
        bool isVisible = GeometryUtility.TestPlanesAABB(planes, _renderer.bounds);

        _anim.SetBool("PlayAnimation", isVisible);


        _frameCounter++;
        if (_frameCounter == 5)
        {
            transform.position += new UnityEngine.Vector3(0, 0, -3);
        }
    }
}
