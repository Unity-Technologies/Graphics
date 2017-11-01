using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Testing.LWtesting
{
    public class LWpipeSwitcher : MonoBehaviour
    {

        public MaterialSwitch[] materialSwitchList;
        public RenderPipelineAsset pipelineAsset;

        public Text ui;

        private bool state = false;

		void Update()
		{
			if(Input.GetKeyDown(KeyCode.Space))
			{
                Switch();
            }
		}

        [ContextMenu("SwitchPipeline")]
        void Switch()
        {
            if (!state)
            {
                ui.text = "Lightweight";
                SetPipeline(pipelineAsset);
				foreach(MaterialSwitch matSwitch in materialSwitchList)
				{
                    SwitchMaterials(matSwitch.LWMat, matSwitch.renderers);
                }
                state = !state;
            }
            else
            {
                ui.text = "Legacy";
                SetPipeline(null);
				foreach (MaterialSwitch matSwitch in materialSwitchList)
                {
                    SwitchMaterials(matSwitch.legacyMat, matSwitch.renderers);
                }
				state = !state;
            }
        }

        void SetPipeline(RenderPipelineAsset pipe)
        {
            GraphicsSettings.renderPipelineAsset = pipe;
        }


		void SwitchMaterials(Material mat, Renderer[] renderers){
			foreach(Renderer rend in renderers)
			{
                rend.material = mat;
            }
		}

		[System.Serializable]
        public class MaterialSwitch
        {
            public Material LWMat;
            public Material legacyMat;
            public Renderer[] renderers;
        }

    }
}
