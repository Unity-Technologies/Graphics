using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    internal enum EntryType
    {
        Camera,
        PostProcessing,
    }

    [Serializable]
    internal class CameraStackEntry
    {
        public Camera camera;
        public EntryType entryType;
    }
    /// <summary>
    /// Holds the Camera Stack with all the entries.
    /// </summary>
    [Serializable]
    public class CameraStack : System.Object
    {
        #region Properties
        [SerializeField]
        List<CameraStackEntry> m_Entries = new List<CameraStackEntry>();
        Camera m_BaseCamera = null;
        UniversalAdditionalCameraData m_BaseCameraAdditionalData;

        #endregion

        #region Cameras

        /// <summary>
        /// Returns a list of all cameras.
        /// </summary>
        public List<Camera> GetAllCameras()
        {
            // Returning cameras from all entries.
            List<Camera> cameras = new List<Camera>();
            for (int i = 0; i < m_Entries.Count; ++i)
            {
                if(m_Entries[i].entryType == EntryType.Camera)
                {
                    cameras.Add(m_Entries[i].camera);
                }
            }
            return cameras;
        }

        // public CameraStack()
        // {
        //     //var camera = this.GetComponent<Camera>();
        // }
        /// <summary>
        /// Add a camera to the end of the entries.
        /// </summary>
        public bool AddCamera(Camera camera)
        {
            return AddCameraAtIndex(camera, -1);
        }

        /// <summary>
        /// Add a camera to the Entries at a certain index.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="index"></param>
        // MTT: Should we allow any camera in here or only Overlay Cameras?
        public bool AddCameraAtIndex(Camera camera, int index)
        {
            // Checking to see that the index is lower than the total amount of entries.
            if (index > m_Entries.Count)
            {
                throw new ArgumentException($"{index} is out of bounds.");
            }

            // If we only allow Overlay Cameras
            if (IsOverlayCamera(camera))
            {
                CameraStackEntry cameraEntry = new CameraStackEntry { camera = camera, entryType = EntryType.Camera };
                AddEntryAtIndex(cameraEntry, index == -1 ? m_Entries.Count - 1: index);

                return true;
            }
            throw new ArgumentException($"{camera} is not an Overlay Camera");
        }

        /// <summary>
        /// Add cameras to the end of the entries.
        /// </summary>
        /// <param name="cameras"></param>
        public void AddCameras(List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; ++i)
            {
                CameraStackEntry cameraEntry = new CameraStackEntry { camera = cameras[i], entryType = EntryType.Camera };
                AddEntry(cameraEntry);
            }
        }

        /// <summary>
        /// Add a cameras to the Entries at a certain index.
        /// </summary>
        /// <param name="cameras"></param>
        /// <param name="index"></param>
        public bool AddCamerasAtIndex(List<Camera> cameras, int index)
        {
            if (index >= Count())
            {
                throw new ArgumentException("Index out of bounds.");
            }
            bool success = true;
            for (int i = cameras.Count - 1; i >= 0; i--)
            {
                success &= AddCameraAtIndex(cameras[i], index);
            }

            return success;
        }

        /// <summary>
        /// Removes the parsed in camera.
        /// </summary>
        /// <param name="camera"></param>
        public bool RemoveCamera(Camera camera)
        {
            if (camera == null)
            {
                throw new ArgumentException("Camera can not be null.");
            }

            bool success = false;
            for (int i = 0; i < m_Entries.Count; ++i)
            {
                if (camera == m_Entries[i].camera)
                {
                    RemoveEntry(m_Entries[i]);
                    success = true;
                }
            }

            return success;
        }

        /// <summary>
        /// Removes the entry at index.
        /// </summary>
        public bool RemoveAtIndex(int index)
        {
            if (index >= Count())
            {
                throw new ArgumentException($"{index} is out of bounds. There are only {Count()} entries.");
            }

            RemoveEntryAtIndex(index);
            return true;
        }

        bool IsOverlayCamera(Camera camera)
        {
            return camera.GetComponent<UniversalAdditionalCameraData>().renderType == CameraRenderType.Overlay;
        }

        #endregion


        #region Post Process

        /// <summary>
        /// Move an entry to another position.
        /// </summary>
        public bool MoveEntryToIndex(int fromIndex, int toIndex)
        {
            if (Count() == 0)
            {
                throw new ArgumentException("There are no entries.");
            }

            if (fromIndex < 0 || fromIndex >= Count())
            {
                throw new ArgumentException($"{fromIndex} is out of bounds.");
            }

            if (toIndex < 0 || toIndex >= Count())
            {
                throw new ArgumentException($"{toIndex} is out of bounds.");
            }

            CameraStackEntry entry = GetEntry(fromIndex);
            RemoveEntryAtIndex(fromIndex);
            AddEntryAtIndex(entry, toIndex);

            return true;
        }

        /// <summary>
        /// Returns the index in the list of the Post Processing entry.
        /// </summary>
        public void GetPostProcessingEntries(ref List<int> postProcessingIndices)
        {
            postProcessingIndices.Clear();
            for (int i = 0; i < m_Entries.Count; ++i)
            {
                if (m_Entries[i].entryType == EntryType.PostProcessing)
                {
                    postProcessingIndices.Add(i);
                }
            }
        }

        /// <summary>
        /// MTT
        /// Add a camera to the end of the Entries.
        /// This can only be added if Post Processing is turned on on the base camera.
        /// </summary>
        public void AddPostProcessing()
        {
            // Only add Post Processing if the base camera has Post Processing turned on
            if (!m_BaseCameraAdditionalData.renderPostProcessing)
            {
                throw new ArgumentException($"The base camera {m_BaseCamera.name} has no Post Processing turned on.");
            }
            AddPostProcessingAtIndex(-1);
        }

        public bool HasPostProcessingEntry()
        {
            for (int i = 0; i < m_Entries.Count; ++i)
            {
                if (m_Entries[i].entryType == EntryType.PostProcessing)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Add a post processing to the end of the Entries.
        /// </summary>
        public void AddPostProcessingAtIndex(int index)
        {
            if (HasPostProcessingEntry())
            {
                throw new ArgumentException("There already exist a Post Processing entry.");
            }

            if (index > Count())
            {
                throw new ArgumentException($"{index} is out of bounds.");
            }

            CameraStackEntry postEntry = new CameraStackEntry { camera = null, entryType = EntryType.PostProcessing };
            AddEntryAtIndex(postEntry, index == -1 ? Count() : index);
        }

        /// <summary>
        /// Removes the Post Processing entry.
        /// </summary>
        public bool RemoveAllPostProcessing()
        {
            if (!HasPostProcessingEntry())
            {
                throw new ArgumentException("There are no Post Processing entries.");
            }

            bool success = false;
            for (int i = m_Entries.Count - 1; i >= 0; --i)
            {
                if (m_Entries[i].entryType == EntryType.PostProcessing)
                {
                    success &= RemoveAtIndex(i);
                }
            }

            return success;
        }

        #endregion


        // Maybe we want to use this to internally use for safety????
        // MTT
        internal void AddEntry(CameraStackEntry entry)
        {
            m_Entries.Add(entry);
        }

        // Maybe we want to use this to internally use for safety????
        // MTT
        internal void AddEntryAtIndex(CameraStackEntry entry, int index)
        {
            m_Entries.Insert(index, entry);
        }

        internal void RemoveEntry(CameraStackEntry entry)
        {
            m_Entries.Remove(entry);
        }

        internal void RemoveEntryAtIndex(int index)
        {
            m_Entries.RemoveAt(index);
        }

        // Maybe we want to use this to internally use for safety????
        // MTT
        internal CameraStackEntry GetEntry(int index)
        {
            return m_Entries[index];
        }

        /// <summary>
        /// Clears the stack of all entries.
        /// </summary>
        public void ClearEntries()
        {
            m_Entries.Clear();
        }

        /// <summary>
        /// Returns the amount of entries in the stack.
        /// </summary>
        public int Count()
        {
            return m_Entries.Count;
        }

        internal void AddBaseCamera(Camera baseCamera)
        {
            m_BaseCamera = baseCamera;
            m_BaseCameraAdditionalData = baseCamera.GetComponent<UniversalAdditionalCameraData>();
            Debug.Log("Muppets");
        }
    }
}
