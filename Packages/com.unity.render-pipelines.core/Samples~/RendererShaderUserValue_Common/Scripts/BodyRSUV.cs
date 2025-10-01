using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[ExecuteAlways]
public class BodyRSUV : MonoBehaviour
{
    public enum SkinColor
    {
        Light, 
        Yellow,
        DarkToned, 
        RedToned,
        BrownToned
    }

    public enum ClothesColor
    {
        Neutral,
        LightGray,
        DarkGray,
        Red,
        Green,
        Blue,
        Gold
    }

    public enum HeadGear
    {
        None,
        Helmet,
        Crown

    }
    public enum HairColor
    {
        Black,
        DarkBrown,
        LightBrown,
        MediumRed,
        DeepRed,
        Blond,
        ExtremeBlond,
        White
    }
    public enum HairStyle
    {
        None,
        Bun,
        PonyTail,
        Tintin,
        Long
    }
    public enum EyebrowsStyle
    {
        None,
        Monobrow,
        Thin
    }
    public enum BeardStyle
    {
        None,
        Large,
        Small
    }

    public enum AnimationState
    {
        Idle,
        Jump
    }

    public bool randomizeOnStart = true;
    public SkinColor skinColor = SkinColor.Light;
    public ClothesColor clothesColor = ClothesColor.Neutral;
    public HairColor hairColor = HairColor.Black;

    private HeadGear lastHeadGear = HeadGear.None;
    public HeadGear headgear = HeadGear.None;
    private HairStyle lastHairStyle = HairStyle.None;
    public HairStyle hairStyle = HairStyle.None;
    public EyebrowsStyle eyebrowsStyle = EyebrowsStyle.None;
    public BeardStyle beardStyle = BeardStyle.None;

    public float bellySize = 0;

    private float scaleVariance = 0.1f;

    private int vertexAnimationMaxTransition = 7;  
    private int vertexAnimationTransitionLerpFactor = 0;  
    private int vertexAnimationAnimIndexFrom = 0;  
    private int vertexAnimationAnimIndexTo = 0;  
    private int vertexAnimationFps = 50;  
    private int vertexAnimationFpsOffset = 0;
    private float vertexAnimationTimeOffset = 0; 

    AnimationState animationState = AnimationState.Idle;

    public List<SkinnedMeshRenderer> skinnedMeshRenderers = null;
    public List<MeshRenderer> meshRenderers = null;

    private int frameTimeMax = 127; // FrameTIme is encoded on 7 bits so its range is [0-127]
    private int frameOffset = 0;
    private int frameTime = 0;
    uint data = 0x00000000; // All bits set to 0

    void Start()
    {
        vertexAnimationFpsOffset = UnityEngine.Random.Range(-3, +3);
        vertexAnimationTimeOffset = UnityEngine.Random.Range(-50, 50)/100f;
        vertexAnimationTransitionLerpFactor = vertexAnimationMaxTransition;

        SetAnimationState(AnimationState.Idle);

        if (randomizeOnStart)
            Randomize();

        UpdateData();
    }

    void OnEnable()
    {
        Start();
    }

    void OnValidate()
    {
        UpdateData();
    }

    void Update()
    {
        UpdateVertexAnimationData();

         switch (animationState)
         { 
            case AnimationState.Jump:
                if (frameTime >= frameTimeMax - 1)
                    SetAnimationState(AnimationState.Idle);
                break;
         }

    }

    void UpdateVertexAnimationData()
    {
        float time = Time.realtimeSinceStartup + vertexAnimationTimeOffset;
        float fps = vertexAnimationFps + vertexAnimationFpsOffset;

        if(vertexAnimationTransitionLerpFactor < vertexAnimationMaxTransition)
            vertexAnimationTransitionLerpFactor += 1;

        int adjustedTime = Mathf.FloorToInt(time * fps) + frameOffset;

        // FrameTime should always be in the range [0,127]
        frameTime = (int)Mathf.Repeat(adjustedTime, frameTimeMax);

        data = HelpersRSUV.EncodeData(data, frameTime, 20, 7);
        data = HelpersRSUV.EncodeData(data, vertexAnimationTransitionLerpFactor, 27, 3);
        data = HelpersRSUV.EncodeData(data, vertexAnimationAnimIndexFrom, 30, 1);
        data = HelpersRSUV.EncodeData(data, vertexAnimationAnimIndexTo, 31, 1);

        UpdateRenderers();
    }

    void UpdateData()
    {

        bellySize = Mathf.Clamp01(bellySize);
        float step = Mathf.CeilToInt(3 * (bellySize + 0.001f * bellySize)) / 4f;
        int bellySize03 = Mathf.RoundToInt(step * 3);

        CheckInconsistencies();

        data = HelpersRSUV.EncodeData(data, (int)skinColor, 0, 3);
        data = HelpersRSUV.EncodeData(data, (int)clothesColor, 3, 3);
        data = HelpersRSUV.EncodeData(data, bellySize03, 6, 2);
        data = HelpersRSUV.EncodeData(data, (int)hairColor, 8, 3);
        data = HelpersRSUV.EncodeData(data, (int)hairStyle, 11, 3);
        data = HelpersRSUV.EncodeData(data, (int)eyebrowsStyle, 14, 2);
        data = HelpersRSUV.EncodeData(data, (int)beardStyle, 16, 2);
        data = HelpersRSUV.EncodeData(data, (int)headgear, 18, 2);

        UpdateRenderers();

    }

    private void UpdateRenderers()
    {
        if (skinnedMeshRenderers.Count + meshRenderers.Count == 0)
            throw new NullReferenceException("The renderers has not been set properly.");

        foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
            smr.SetShaderUserValue(data);

        foreach (MeshRenderer mr in meshRenderers)
            mr.SetShaderUserValue(data);
    }

    public void Randomize()
    {
        SetSkinColor((SkinColor) UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(SkinColor)).Length));
        SetClothColor((ClothesColor)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ClothesColor)).Length - 1)); // - 1 To Avoid having the gold color reserved for the king
        SetBellySize(UnityEngine.Random.Range(0f, 100f)/100f);
        SetHairColor((HairColor)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(HairColor)).Length));
        SetEyeBrowsStyle((EyebrowsStyle)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(EyebrowsStyle)).Length));
        SetBeardStyle((BeardStyle)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(BeardStyle)).Length));
        SetHeadGear((HeadGear)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(HeadGear)).Length - 1)); //-1 because we want to make sure the crown is not selected when randomizing
        SetHairStyle((HairStyle)UnityEngine.Random.Range(1, System.Enum.GetValues(typeof(HairStyle)).Length));

        CheckInconsistencies();

        this.transform.localScale = Vector3.one * (1 + UnityEngine.Random.Range(-scaleVariance, scaleVariance));
       
        UpdateData();
    }

    public void SetAnimationState(AnimationState newAnimationState)
    {

        vertexAnimationAnimIndexFrom = (int)animationState;
        vertexAnimationAnimIndexTo = (int)newAnimationState;
        if(newAnimationState != animationState)
        {
            if(newAnimationState == AnimationState.Jump)
                frameOffset += (frameTimeMax - frameTime);

            vertexAnimationTransitionLerpFactor = 0;
        }

        animationState = newAnimationState;

        UpdateVertexAnimationData();

    }

    public void SetSkinColor(SkinColor skinColor)
    {
        this.skinColor = skinColor;
    }

    public void SetClothColor(ClothesColor clothColor)
    {
        this.clothesColor = clothColor;
    }

    public void SetBellySize(float bellySize)
    {
        this.bellySize = bellySize;
    }

    public void SetHairColor(HairColor hairColor)
    {
        this.hairColor = hairColor;
    }

    public void SetHairStyle(HairStyle hairStyle)
    {
        lastHairStyle = this.hairStyle;
        this.hairStyle = hairStyle;
    }

    public void SetHeadGear(HeadGear headGear)
    {
        lastHeadGear = this.headgear;
        this.headgear = headGear;
    }

    public void SetEyeBrowsStyle(EyebrowsStyle eyebrowsStyle)
    {
        this.eyebrowsStyle = eyebrowsStyle;
    }

    public void SetBeardStyle(BeardStyle beardStyle)
    {
        this.beardStyle = beardStyle;
    }

    public void CheckInconsistencies()
    {
        if(lastHairStyle != this.hairStyle)
        {            
            if (hairStyle != HairStyle.None)
                SetHeadGear(HeadGear.None);

            SetHairStyle(this.hairStyle);
            SetHeadGear(this.headgear);
            return;

        }

        if (lastHeadGear != this.headgear)
        {
            if (headgear != HeadGear.None)
                SetHairStyle(HairStyle.None);

            SetHeadGear(this.headgear);
            SetHairStyle(this.hairStyle);
            return;
        }
        

    }


}
