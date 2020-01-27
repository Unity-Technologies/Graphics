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

        /// <summary>
        /// Add a camera to the end of the Entries.
        /// </summary>
        public void AddCamera(Camera camera)
        {
            if (camera != null)
            {
                CameraStackEntry cameraEntry = new CameraStackEntry { camera = camera, entryType = EntryType.Camera };
                m_Entries.Add(cameraEntry);
            }
        }

        /// <summary>
        /// Add a camera to the Entries at a certain index.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="index"></param>
        // MTT: Should we allow any camera in here or only Overlay Cameras?
        public void AddCameraAtIndex(Camera camera, int index)
        {
            // Checking to see that the index is lower than the total amount of entries.
            if (index <= m_Entries.Count)
            {
                // If we only allow Overlay Cameras
                if (IsOverlayCamera(camera))
                {
                    CameraStackEntry cameraEntry = new CameraStackEntry { camera = camera, entryType = EntryType.Camera };

                    // If index is the same amount as count we are adding it at the end of the list.
                    if (index == m_Entries.Count)
                    {
                        m_Entries.Add(cameraEntry);
                    }
                    else
                    {
                        m_Entries.Insert(index, cameraEntry);
                    }
                }
                else
                {
                    throw new ArgumentException($"{camera} is not an Overlay Camera");
                }
            }
        }

        /// <summary>
        /// Add a camera to the Entries at a certain index.
        /// </summary>
        /// <param name="cameras"></param>
        public void AddCameras(List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; ++i)
            {
                CameraStackEntry cameraEntry = new CameraStackEntry { camera = cameras[i], entryType = EntryType.Camera };
                m_Entries.Add(cameraEntry);
            }
        }

        /// <summary>
        /// Removes the parsed in camera.
        /// </summary>
        /// /// <param name="cameras"></param>
        public void RemoveCamera(Camera camera)
        {
            if (camera != null)
            {
                for (int i = 0; i < m_Entries.Count; ++i)
                {
                    if (camera == m_Entries[i].camera)
                    {
                        m_Entries.Remove(m_Entries[i]);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Camera can not be null.");
            }
        }

        /// <summary>
        /// Removes the camera entry at index.
        /// </summary>
        public void RemoveCameraAtIndex(int index)
        {
            if (index < m_Entries.Count)
            {
                m_Entries.RemoveAt(index);
            }
            else
            {
                throw new ArgumentException("Index is out of bounds.");
            }
        }

        bool IsOverlayCamera(Camera camera)
        {
            return camera.GetComponent<UniversalAdditionalCameraData>().renderType == CameraRenderType.Overlay;
        }

        #endregion


        #region Post Process

        /// <summary>
        /// Sets the index in the list of the Post Processing entry.
        /// </summary>
        public void SetPostProcessingEntryIndex(int index)
        {
            // Checking if Index is a valid entry
            if (index < m_Entries.Count)
            {
                // Checking if we have a PostProcessing Entry
                int prevIndex = GetPostProcessingEntryIndex();
                if (prevIndex != -1)
                {
                    // We only move it if it is not the same index
                    if (prevIndex != index)
                    {
                        // This shouldnt be null since we have already found an entry
                        CameraStackEntry postProcessingEntry = GetPostProcessingEntry();
                        if (postProcessingEntry != null)
                        {
                            // Removing old Entry and then insert it into the new index
                            m_Entries.RemoveAt(prevIndex);
                            m_Entries.Insert(index, postProcessingEntry);
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("There are no Post Processing Entry!");
                }
            }
            else
            {
                throw new ArgumentException("Index is out of bounds: " + index);
            }
        }

        /// <summary>
        /// Returns the index in the list of the Post Processing entry.
        /// </summary>
        public int GetPostProcessingEntryIndex()
        {
            for (int i = 0; i < m_Entries.Count; ++i)
            {
                if (m_Entries[i].entryType == EntryType.PostProcessing)
                {
                    return i;
                }
            }
            // If we do not have a PostProcessing Entry return -1
            // Maybe -1 is not the best thing...
            return -1;
        }

        // This is internal since we do not want users to have to deal with these Entries at the moment.
        CameraStackEntry GetPostProcessingEntry()
        {
            int index = GetPostProcessingEntryIndex();
            if (index != -1)
            {
                return m_Entries[index];
            }

            return null;
        }

        /// <summary>
        /// MTT
        /// Add a camera to the end of the Entries.
        /// This can only be added if Post Processing is turned on on the base camera.
        /// </summary>
        public void AddPostProcessing()
        {
            if (GetPostProcessingEntryIndex() == -1)
            {
                CameraStackEntry postEntry = new CameraStackEntry { camera = null, entryType = EntryType.PostProcessing };
                m_Entries.Add(postEntry);
            }
            else
            {
                throw new ArgumentException("There already exist a Post Processing entry.");
            }
        }

        /// <summary>
        /// Add a camera to the end of the Entries.
        /// </summary>
        public void AddPostProcessingAtIndex(int index)
        {
            if (GetPostProcessingEntryIndex() == -1)
            {
                CameraStackEntry postEntry = new CameraStackEntry { camera = null, entryType = EntryType.PostProcessing };
                // Adding it here so that we can use the other Function that already exist to move it to the right index.
                m_Entries.Add(postEntry);
                SetPostProcessingEntryIndex(index);
            }
            else
            {
                throw new ArgumentException("There already exist a Post Processing entry.");
            }
        }

        /// <summary>
        /// Removes the Post Processing entry.
        /// </summary>
        public void RemovePostProcessing()
        {
            int index = GetPostProcessingEntryIndex();
            if (index != -1)
            {
                m_Entries.RemoveAt(index);
            }
            else
            {
                throw new ArgumentException("No Post Processing entry exists.");
            }
        }


        #endregion

        // Maybe we want to use this to internally use for safety????
        // MTT
        internal void AddEntry(CameraStackEntry entry)
        {

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

        // This might be a bit confusing
        // MTT
        // public List<Camera> GetAllEntries()
        // {
        //     // Returning cameras from all entries.
        //     List<Camera> cameras = new List<Camera>();
        //     for (int i = 0; i < m_Entries.Count; ++i)
        //     {
        //         cameras.Add(m_Entries[i].camera);
        //     }
        //     return cameras;
        // }
    }
}
