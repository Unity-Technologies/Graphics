using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QualitySetter : MonoBehaviour
{
    public void SetHighQuality()
    {
    	QualitySettings.SetQualityLevel(1, true);
    }

    public void SetMediumQuality()
    {
    	QualitySettings.SetQualityLevel(2, true);
    }

    public void SetLowQuality()
    {
    	QualitySettings.SetQualityLevel(3, true);
    }

}
