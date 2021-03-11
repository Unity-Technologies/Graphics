using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    public class OutputEventTest : MonoBehaviour
    {
        static private string s_outputEventName = "Test_Output_Event";
        static private int s_outputEventNameId = Shader.PropertyToID(s_outputEventName);
        static private int s_positionNameId = Shader.PropertyToID("position");
        static private int s_colorNameId = Shader.PropertyToID("color");
        static private int s_texIndexId = Shader.PropertyToID("texIndex");
        static private int s_customEmissionNameId = Shader.PropertyToID("custom_emission");

        public GameObject m_ObjectReference;

        private VisualEffect m_vfx;

        void Start()
        {
            m_vfx = GetComponent<VisualEffect>();

            if (!m_vfx)
            {
                Debug.LogError("Cannot find the VisualEffect component");
                return;
            }

            var outputEventNames = new List<string>();
            m_vfx.GetOutputEventNames(outputEventNames);
            if (outputEventNames.Count != 1)
            {
                Debug.LogErrorFormat("Unexpected output event count : {0} (expected : 1)", outputEventNames.Count);
                return;
            }

            if (outputEventNames[0] != s_outputEventName)
            {
                Debug.LogErrorFormat("Unexpected output event name : {0} (expected : {1})", outputEventNames.Count);
                return;
            }

            m_vfx.outputEventReceived += ProcessVisualEffectEvent;
        }

        static private float[] s_acceptableTexIndexValues = new [] { 10.0f, 20.0f, 30.0f };

        void ProcessVisualEffectEvent(VFXOutputEventArgs eventArgs)
        {
            if (eventArgs.nameId != s_outputEventNameId)
            {
                Debug.LogErrorFormat("Incorrect output event name : {0} (expected:{1})", eventArgs.nameId, s_outputEventNameId);
            }

            //texIndex isn't used in particle system but it should be reachable in these vfx event attribute.
            var texIndex = eventArgs.eventAttribute.GetFloat(s_texIndexId);
            bool correct = false;
            foreach (var value in s_acceptableTexIndexValues)
            {
                if (Mathf.Abs(value - texIndex) < 1e-5f)
                {
                    correct = true;
                    break;
                }
            }

            if (!correct)
            {
                Debug.LogError("Unable to retrieve texIndex, got : " + texIndex);
                return;
            }

            var newObject = GameObject.Instantiate(m_ObjectReference);
            newObject.GetComponent<Transform>().position = eventArgs.eventAttribute.GetVector3(s_positionNameId);
            var renderer = newObject.GetComponent<Renderer>();
            var currentMaterial = renderer.material;
            currentMaterial = new Material(currentMaterial);
            currentMaterial.SetVector(s_customEmissionNameId, eventArgs.eventAttribute.GetVector3(s_colorNameId));
            renderer.material = currentMaterial;
            newObject.SetActive(true);
        }
    }
}
