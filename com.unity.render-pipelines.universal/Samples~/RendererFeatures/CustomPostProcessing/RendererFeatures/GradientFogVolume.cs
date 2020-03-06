using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System;
using UnityEngine;

[Serializable, VolumeComponentMenu("Post-processing/Gradient Fog")]
public class GradientFogVolume : VolumeComponent
{
    [Tooltip("Near color.")]
    public ColorParameter nearColor = new ColorParameter(new Color(0,0,0,0), false, true, false);
    [Tooltip("Mid color.")]
    public ColorParameter midColor = new ColorParameter(new Color(0.5f,0.5f,0.5f,0), false, true, false);
    [Tooltip("Far color.")] 
    public ColorParameter farColor = new ColorParameter(new Color(1,1,1,0), false, true, false);
    [Tooltip("Distance where the fog will begin.")]
    public FloatParameter nearDistance = new FloatParameter(0);
    [Tooltip("Distance where the fog will end")]
    public FloatParameter farDistance = new FloatParameter(50);
    
}
