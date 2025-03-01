#if UNITY_STANDALONE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class SceneFixedTimePlayableDirectorUpdater : MonoBehaviour
{
    const float kFrameRate = 30.0f;
    private List<PlayableDirector> m_ScenePlayableDirectors;

    void Start()
    {
        m_ScenePlayableDirectors = new List<PlayableDirector>();
        var foundObjects = FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (foundObjects != null)
        {
            foreach (var obj in foundObjects)
            {
                if (obj is PlayableDirector)
                {
                    PlayableDirector currentDirector = obj as PlayableDirector;
                    m_ScenePlayableDirectors.Add(currentDirector);
                    currentDirector.timeUpdateMode = DirectorUpdateMode.Manual;
                }
            }
        }
    }

    void Update()
    {
        foreach (var player in m_ScenePlayableDirectors)
            player.playableGraph.Evaluate(1/kFrameRate);
    }
}
#endif
