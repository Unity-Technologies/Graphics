using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class MoveWave : MonoBehaviour
{

    public bool autoplay = true;

    [Range(0, 1)]
    public float stateOverride;
    
    //Time the wave should take to go from start to end. 
    public float speedWave = 1;

    //X is the starting X position, Y is the end X position for the wave
    public Vector2 positions = new Vector2(-100,0);
    public Material deformer = null;
    public VisualEffect vfxLip = null;
    
    
    internal float startTime;
    
    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        float stateWave = GetState();

        if(vfxLip != null)
            vfxLip.SetFloat("_factorWave", stateWave);

        if (deformer != null)
            deformer.SetFloat("_State", stateWave * 2);

        // Lerping the X position of the wave. 
        this.transform.position = new Vector3(Mathf.Lerp(positions.x, positions.y, stateWave), 0.0f, 0.0f);
        
        // If the state goes above one, we need to restart the wave. 
        if(stateWave > 1) 
            startTime = Time.realtimeSinceStartup;
    }

    public float GetState()
	{
        if (!autoplay) return stateOverride;

        return (Time.realtimeSinceStartup - startTime)/speedWave;
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(MoveWave))]
public class MoveWaveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        var anim = (target as MoveWave);

        if (anim.autoplay && GUILayout.Button("Reset"))
        {
            anim.startTime = Time.realtimeSinceStartup;
        }
    }
}

#endif

