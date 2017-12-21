using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    [ExecuteInEditMode]
    public class DebugMenuUpdater : MonoBehaviour
    {

        void Update()
        {
            DebugMenuManager.instance.Update();
            DebugActionManager.instance.Update();

            if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.EnableDebugMenu) != 0.0f)
            {
                DebugMenuManager.instance.menuUI.ToggleMenu();
            }

            if (DebugMenuManager.instance.menuUI.isEnabled)
            {
                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.PreviousDebugPanel) != 0.0f)
                {
                    DebugMenuManager.instance.menuUI.PreviousDebugPanel();
                }

                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.NextDebugPanel) != 0.0f)
                {
                    DebugMenuManager.instance.menuUI.NextDebugPanel();
                }

                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.Validate) != 0.0f)
                {
                    DebugMenuManager.instance.menuUI.OnValidate();
                }

                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.MakePersistent) != 0.0f)
                {
                    DebugMenuManager.instance.menuUI.OnMakePersistent();
                }

                float moveHorizontal = DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.MoveHorizontal);
                if (moveHorizontal != 0.0f)
                {
                    DebugMenuManager.instance.menuUI.OnMoveHorizontal(moveHorizontal);
                }

                float moveVertical = DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.MoveVertical);
                if (moveVertical != 0.0f)
                {
                    DebugMenuManager.instance.menuUI.OnMoveVertical(moveVertical);
                }
            }
        }
    }
}
