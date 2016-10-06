using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ScriptableRenderLoop;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class RenderLoopWrapper : MonoBehaviour
{
    public delegate void Callback(RenderLoopWrapper wrapper, Camera[] cameras, RenderLoop renderLoop);

    public Callback callback;

    void OnEnable()
    {
        RenderLoop.renderLoopDelegate += Render;
    }

    void OnDisable()
    {
        RenderLoop.renderLoopDelegate -= Render;
    }

    bool Render(Camera[] cameras, RenderLoop loop)
    {
        callback(this, cameras, loop);
        return true;
    }
}
