using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hidden : MonoBehaviour
{
    void OnValidate()
    {
        this.gameObject.hideFlags = HideFlags.HideInHierarchy;
    }

}
