using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[ExecuteAlways]
public class HealthRSUV : MonoBehaviour
{
    public bool randomizeOnStart = true;
    public bool showHealthBar = true;
    public float healthBarOpacity = 1;
    public float health = 1;
    public MeshRenderer meshRenderer = null;

    uint data = 0x00000000; // All bits set to 0

    void Start()
    {
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

    void UpdateData()
    {
        health = Mathf.Clamp01(health);
        float stepHealth = Mathf.CeilToInt(7*(health  + 0.001f * health)) / 8f;
        int health07 = Mathf.RoundToInt(stepHealth * 7);

        healthBarOpacity = Mathf.Clamp01(healthBarOpacity);
        float stepHealthBarOpacity = Mathf.CeilToInt(7 * (healthBarOpacity + 0.001f * healthBarOpacity)) / 8f;
        int healthBarOpacity07 = Mathf.RoundToInt(stepHealthBarOpacity * 7);

        data = HelpersRSUV.EncodeData(data, health07, 0, 3);
        data = HelpersRSUV.SetBit(data, 3, showHealthBar);
        data = HelpersRSUV.EncodeData(data, healthBarOpacity07, 4, 3);

        UpdateRenderers();
    }

    private void UpdateRenderers()
    {
        if (meshRenderer == null)
            throw new NullReferenceException("The variable meshRenderer has not been set properly.");

        meshRenderer.SetShaderUserValue(data);
    }

    public void Randomize()
    {
        health = UnityEngine.Random.Range(0.1f, 1f);
        UpdateData();
    }

    public void SetHealth(float health)
    {
        this.health = health;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        health = Mathf.Clamp01(health);
        UpdateData();
    }

}
