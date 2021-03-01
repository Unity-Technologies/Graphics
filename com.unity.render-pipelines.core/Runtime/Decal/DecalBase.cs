using System;
using UnityEngine;

namespace UnityEngine.Rendering
{
    public class DecalBase : MonoBehaviour
    {
        internal static readonly Quaternion k_MinusYtoZRotation = Quaternion.Euler(-90, 0, 0);

        [SerializeField]
        private Vector2 m_UVScale = new Vector2(1, 1);
        /// <summary>
        /// Tilling of the UV of the projected texture.
        /// </summary>
        public Vector2 uvScale
        {
            get
            {
                return m_UVScale;
            }
            set
            {
                m_UVScale = value;
                OnValidate();
            }
        }

        [SerializeField]
        private Vector2 m_UVBias = new Vector2(0, 0);
        /// <summary>
        /// Offset of the UV of the projected texture.
        /// </summary>
        public Vector2 uvBias
        {
            get
            {
                return m_UVBias;
            }
            set
            {
                m_UVBias = value;
                OnValidate();
            }
        }

        [SerializeField]
        protected Vector3 m_Offset = new Vector3(0, 0, 0.5f);
        /// <summary>
        /// Change the pivot position.
        /// It is an offset between the center of the projection and the transform position.
        /// </summary>
        public Vector3 pivot
        {
            get
            {
                return m_Offset;
            }
            set
            {
                m_Offset = value;
                OnValidate();
            }
        }

        [SerializeField]
        protected Vector3 m_Size = new Vector3(1, 1, 1);
        /// <summary>
        /// The size of the projection volume.
        /// See also <seealso cref="ResizeAroundPivot"/> to rescale relatively to the pivot position.
        /// </summary>
        public Vector3 size
        {
            get => m_Size;
            set
            {
                m_Size = value;
                OnValidate();
            }
        }

        /// <summary>
        /// Update the pivot to resize centered on the pivot position.
        /// </summary>
        /// <param name="newSize">The new size.</param>
        public void ResizeAroundPivot(Vector3 newSize)
        {
            for (int axis = 0; axis < 3; ++axis)
                if (m_Size[axis] > Mathf.Epsilon)
                    m_Offset[axis] *= newSize[axis] / m_Size[axis];
            size = newSize;
        }

        protected Material m_OldMaterial = null;


        /// <summary>current rotation in a way the DecalSystem will be able to use it</summary>
        protected Quaternion rotation => transform.rotation * k_MinusYtoZRotation;
        /// <summary>current position in a way the DecalSystem will be able to use it</summary>
        protected Vector3 position => transform.position;
        /// <summary>current size in a way the DecalSystem will be able to use it</summary>
        protected Vector3 decalSize => new Vector3(m_Size.x, m_Size.z, m_Size.y);
        /// <summary>current size in a way the DecalSystem will be able to use it</summary>
        protected Vector3 decalOffset => new Vector3(m_Offset.x, -m_Offset.z, m_Offset.y);
        /// <summary>current uv parameters in a way the DecalSystem will be able to use it</summary>
        protected Vector4 uvScaleBias => new Vector4(m_UVScale.x, m_UVScale.y, m_UVBias.x, m_UVBias.y);

        [SerializeField]
        // TOUPDATE was private before
        protected Material m_Material = null;
        /// <summary>
        /// The material used by the decal. It should be of type HDRP/Decal if you want to have transparency.
        /// </summary>
        public Material material
        {
            get
            {
                return m_Material;
            }
            set
            {
                m_Material = value;
                // TOUPDATE
                // OnValidate();
            }
        }

        /// <summary>
        /// Event called each time the used material change.
        /// </summary>
        public event Action OnMaterialChange;

        protected void RaiseOnMaterialChange()
        {
            if (OnMaterialChange != null)
            {
                OnMaterialChange();
            }
        }

        protected void OnValidate()
        {

        }
    }
}
