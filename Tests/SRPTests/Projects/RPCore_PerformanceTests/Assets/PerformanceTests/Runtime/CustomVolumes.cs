using System;
using UnityEngine;
using UnityEngine.Rendering;

// 50x10 = 500 parameters in CustomVolume1..CustomVolume50

[Serializable]
public abstract class CustomVolumeBase : VolumeComponent
{
    public FloatParameter param1 = new (0.1f);
    public MinFloatParameter param2 = new (0.2f, 0f);
    public IntParameter param3 = new (5);
    public ClampedIntParameter param4 = new (10, 5, 50);
    public FloatRangeParameter param5 = new (Vector2.right, 0f, 1f);
    public ColorParameter param6 = new (Color.green);
    public Vector2Parameter param7 = new (Vector2.one);
    public Vector3Parameter param8 = new (Vector3.one);
    public Vector4Parameter param9 = new (Vector4.one);
    public TextureParameter param10 = new (null);

    public bool IsActive() => true;
}

[Serializable] public class CustomVolume1 : CustomVolumeBase {}
[Serializable] public class CustomVolume2 : CustomVolumeBase {}
[Serializable] public class CustomVolume3 : CustomVolumeBase {}
[Serializable] public class CustomVolume4 : CustomVolumeBase {}
[Serializable] public class CustomVolume5 : CustomVolumeBase {}
[Serializable] public class CustomVolume6 : CustomVolumeBase {}
[Serializable] public class CustomVolume7 : CustomVolumeBase {}
[Serializable] public class CustomVolume8 : CustomVolumeBase {}
[Serializable] public class CustomVolume9 : CustomVolumeBase {}
[Serializable] public class CustomVolume10 : CustomVolumeBase {}
[Serializable] public class CustomVolume11 : CustomVolumeBase {}
[Serializable] public class CustomVolume12 : CustomVolumeBase {}
[Serializable] public class CustomVolume13 : CustomVolumeBase {}
[Serializable] public class CustomVolume14 : CustomVolumeBase {}
[Serializable] public class CustomVolume15 : CustomVolumeBase {}
[Serializable] public class CustomVolume16 : CustomVolumeBase {}
[Serializable] public class CustomVolume17 : CustomVolumeBase {}
[Serializable] public class CustomVolume18 : CustomVolumeBase {}
[Serializable] public class CustomVolume19 : CustomVolumeBase {}
[Serializable] public class CustomVolume20 : CustomVolumeBase {}
[Serializable] public class CustomVolume21 : CustomVolumeBase {}
[Serializable] public class CustomVolume22 : CustomVolumeBase {}
[Serializable] public class CustomVolume23 : CustomVolumeBase {}
[Serializable] public class CustomVolume24 : CustomVolumeBase {}
[Serializable] public class CustomVolume25 : CustomVolumeBase {}
[Serializable] public class CustomVolume26 : CustomVolumeBase {}
[Serializable] public class CustomVolume27 : CustomVolumeBase {}
[Serializable] public class CustomVolume28 : CustomVolumeBase {}
[Serializable] public class CustomVolume29 : CustomVolumeBase {}
[Serializable] public class CustomVolume30 : CustomVolumeBase {}
[Serializable] public class CustomVolume31 : CustomVolumeBase {}
[Serializable] public class CustomVolume32 : CustomVolumeBase {}
[Serializable] public class CustomVolume33 : CustomVolumeBase {}
[Serializable] public class CustomVolume34 : CustomVolumeBase {}
[Serializable] public class CustomVolume35 : CustomVolumeBase {}
[Serializable] public class CustomVolume36 : CustomVolumeBase {}
[Serializable] public class CustomVolume37 : CustomVolumeBase {}
[Serializable] public class CustomVolume38 : CustomVolumeBase {}
[Serializable] public class CustomVolume39 : CustomVolumeBase {}
[Serializable] public class CustomVolume40 : CustomVolumeBase {}
[Serializable] public class CustomVolume41 : CustomVolumeBase {}
[Serializable] public class CustomVolume42 : CustomVolumeBase {}
[Serializable] public class CustomVolume43 : CustomVolumeBase {}
[Serializable] public class CustomVolume44 : CustomVolumeBase {}
[Serializable] public class CustomVolume45 : CustomVolumeBase {}
[Serializable] public class CustomVolume46 : CustomVolumeBase {}
[Serializable] public class CustomVolume47 : CustomVolumeBase {}
[Serializable] public class CustomVolume48 : CustomVolumeBase {}
[Serializable] public class CustomVolume49 : CustomVolumeBase {}
[Serializable] public class CustomVolume50 : CustomVolumeBase {}

// Custom Volumes for parameter interpolation tests

[Serializable]
public class CustomVolumeFloatParams : VolumeComponent
{
    public FloatParameter param1 = new (0.1f);
    public FloatParameter param2 = new (0.2f);
    public FloatParameter param3 = new (0.3f);
    public FloatParameter param4 = new (0.4f);
    public FloatParameter param5 = new (0.5f);
    public FloatParameter param6 = new (0.6f);
    public FloatParameter param7 = new (0.7f);
    public FloatParameter param8 = new (0.8f);
    public FloatParameter param9 = new (0.9f);
    public FloatParameter param10 = new (1.0f);

    public bool IsActive() => true;
}

[Serializable]
public class CustomVolumeIntParams : VolumeComponent
{
    public IntParameter param1 = new (1);
    public IntParameter param2 = new (2);
    public IntParameter param3 = new (3);
    public IntParameter param4 = new (4);
    public IntParameter param5 = new (5);
    public IntParameter param6 = new (6);
    public IntParameter param7 = new (7);
    public IntParameter param8 = new (8);
    public IntParameter param9 = new (9);
    public IntParameter param10 = new (10);

    public bool IsActive() => true;
}

[Serializable]
public class CustomVolumeVector4Params : VolumeComponent
{
    public Vector4Parameter param1 = new (Vector4.one);
    public Vector4Parameter param2 = new (Vector4.one);
    public Vector4Parameter param3 = new (Vector4.one);
    public Vector4Parameter param4 = new (Vector4.one);
    public Vector4Parameter param5 = new (Vector4.one);
    public Vector4Parameter param6 = new (Vector4.one);
    public Vector4Parameter param7 = new (Vector4.one);
    public Vector4Parameter param8 = new (Vector4.one);
    public Vector4Parameter param9 = new (Vector4.one);
    public Vector4Parameter param10 = new (Vector4.one);

    public bool IsActive() => true;
}

[Serializable]
public class CustomVolumeColorParams : VolumeComponent
{
    public ColorParameter param1 = new (Color.blue);
    public ColorParameter param2 = new (Color.blue);
    public ColorParameter param3 = new (Color.blue);
    public ColorParameter param4 = new (Color.blue);
    public ColorParameter param5 = new (Color.blue);
    public ColorParameter param6 = new (Color.blue);
    public ColorParameter param7 = new (Color.blue);
    public ColorParameter param8 = new (Color.blue);
    public ColorParameter param9 = new (Color.blue);
    public ColorParameter param10 = new (Color.blue);

    public bool IsActive() => true;
}

[Serializable]
public class CustomVolumeFloatRangeParams : VolumeComponent
{
    public FloatRangeParameter param1 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);
    public FloatRangeParameter param2 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);
    public FloatRangeParameter param3 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);
    public FloatRangeParameter param4 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);
    public FloatRangeParameter param5 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);
    public FloatRangeParameter param6 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);
    public FloatRangeParameter param7 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);
    public FloatRangeParameter param8 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);
    public FloatRangeParameter param9 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);
    public FloatRangeParameter param10 = new(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);

    public bool IsActive() => true;
}

[Serializable]
public class CustomVolumeAnimationCurveParams : VolumeComponent
{
    public AnimationCurveParameter param1 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
    public AnimationCurveParameter param2 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
    public AnimationCurveParameter param3 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
    public AnimationCurveParameter param4 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
    public AnimationCurveParameter param5 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
    public AnimationCurveParameter param6 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
    public AnimationCurveParameter param7 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
    public AnimationCurveParameter param8 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
    public AnimationCurveParameter param9 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
    public AnimationCurveParameter param10 = new (new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));

    public bool IsActive() => true;
}
