using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Custom Resolutions", menuName = "Testing/Custom Resolutions ScriptableObject", order = 100)]
public class CustomResolutionSettings : ScriptableObject
{
    public CustomResolutionFields[] fields;
}
