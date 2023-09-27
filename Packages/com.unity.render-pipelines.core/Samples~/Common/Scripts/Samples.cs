using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Samples
{
    public string introduction;
    public Sample[] samples;
    private Dictionary<GameObject, int> prefabToSample;

    public static Samples CreateFromJSON(string jsonString, GameObject[] prefabs = null)
    {
        var newSamples = JsonUtility.FromJson<Samples>(jsonString);

        if (prefabs != null)
        {
            newSamples.prefabToSample = new Dictionary<GameObject, int>();

            foreach (var go in prefabs)
            {
                int index = System.Array.FindIndex(newSamples.samples, s => s.prefabName == jsonString);
                if (index >= 0)
                    newSamples.prefabToSample.Add(go, index);
            }
        }

        return newSamples;
    }
    
    public Sample FindSampleWithPrefab(GameObject prefab)
    {
        if ( prefabToSample.ContainsKey(prefab) )
            return samples[prefabToSample[prefab]];

        foreach(Sample sample in samples)
            if (sample.prefabName == prefab.name)
                return sample;
        
        Debug.LogWarning($"Sample not found with prefabName: {prefab.name}");
        return null;
    }
}

[System.Serializable]
public class Sample
{
    public string title;
    public string prefabName;
    public string description;
}