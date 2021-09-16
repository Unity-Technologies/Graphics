using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Path2D.GUIFramework
{
    internal class GenericControl : Control
    {
        public Func<IGUIState, LayoutData> onBeginLayout = null;
        public Action<IGUIState> onEndLayout = null;
        public Action<IGUIState, Control, int> onRepaint;
        public Func<int> count;
        public Func<int, Vector3> position;
        public Func<IGUIState, int, float> distance;
        public Func<int, Vector3> forward;
        public Func<int, Vector3> up;
        public Func<int, Vector3> right;
        public Func<int, object> userData = null;

        public GenericControl(string name) : base(name)
        {
        }

        protected override int GetCount()
        {
            if (count != null)
                return count();

            return base.GetCount();
        }

        protected override void OnEndLayout(IGUIState guiState)
        {
            if (onEndLayout != null)
                onEndLayout(guiState);
        }

        protected override void OnRepaint(IGUIState guiState, int index)
        {
            if (onRepaint != null)
                onRepaint(guiState, this, index);
        }

        protected override LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            if (onBeginLayout != null)
                return onBeginLayout(guiState);

            return data;
        }

        protected override Vector3 GetPosition(IGUIState guiState, int index)
        {
            if (position != null)
                return position(index);

            return base.GetPosition(guiState, index);
        }

        protected override float GetDistance(IGUIState guiState, int index)
        {
            if (distance != null)
                return distance(guiState, index);

            return base.GetDistance(guiState, index);
        }

        protected override Vector3 GetForward(IGUIState guiState, int index)
        {
            if (forward != null)
                return forward(index);

            return base.GetForward(guiState, index);
        }

        protected override Vector3 GetUp(IGUIState guiState, int index)
        {
            if (up != null)
                return up(index);

            return base.GetUp(guiState, index);
        }

        protected override Vector3 GetRight(IGUIState guiState, int index)
        {
            if (right != null)
                return right(index);

            return base.GetRight(guiState, index);
        }

        protected override object GetUserData(IGUIState guiState, int index)
        {
            if (userData != null)
                return userData(index);

            return base.GetUserData(guiState, index);
        }
    }
}
