using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VoxelFieldIO
{
    public static bool Read(string assetPath, out VoxelField voxelField)
    {
        voxelField = null;

        if (!File.Exists(assetPath))
            return false;

        voxelField = new VoxelField();

        System.IO.FileStream fs = new System.IO.FileStream(assetPath, System.IO.FileMode.Open);
        System.IO.BinaryReader br = new System.IO.BinaryReader(fs);

        // Read the asset name
        System.UInt32 assetNameLength = br.ReadUInt32();
        br.ReadBytes(1);
        voxelField.m_Name = System.Text.Encoding.Default.GetString(br.ReadBytes((int)(assetNameLength)));

        // Read the Voxel ID
        voxelField.m_Id = br.ReadInt32();

        // Read the voxel dimensions for each axis
        voxelField.m_VoxelCountX = br.ReadInt32();
        voxelField.m_VoxelCountY = br.ReadInt32();
        voxelField.m_VoxelCountZ = br.ReadInt32();

        // Read the voxel size
        voxelField.m_VoxelSize = br.ReadSingle();

        // Read the mesh bounds
        // center
        Vector3 center = new Vector3();
        center.x = br.ReadSingle();
        center.y = br.ReadSingle();
        center.z = br.ReadSingle();
        // size
        Vector3 size = new Vector3();
        size.x = br.ReadSingle();
        size.y = br.ReadSingle();
        size.z = br.ReadSingle();

        voxelField.m_MeshBounds = new Bounds(center, size);

        // Write the voxel field values
        System.UInt32 voxelFieldCount = br.ReadUInt32();
        voxelField.m_Field = new float[voxelFieldCount];
        for (int i = 0; i < voxelField.m_Field.Length; ++i)
        {
            voxelField.m_Field[i] = br.ReadSingle();
        }

        br.Close();
        fs.Close();

        return true;
    }

    public static bool Write(MeshToSDFProcessorInternalSettings settings, VoxelField voxelField)
    {
        if (voxelField == null)
            return false;

        System.IO.FileStream fs = new System.IO.FileStream(settings.inputSettings.outputFilePath, System.IO.FileMode.OpenOrCreate);
        System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);

        // Write the asset name
        bw.Write((System.UInt32)(settings.inputSettings.assetName.Length));
        bw.Write(settings.inputSettings.assetName);

        // Read the Voxel ID
        bw.Write(voxelField.m_Id);

        // Write the voxel dimensions for each axis
        bw.Write(voxelField.m_VoxelCountX);
        bw.Write(voxelField.m_VoxelCountY);
        bw.Write(voxelField.m_VoxelCountZ);

        // Write the voxel size
        bw.Write(voxelField.VoxelSize);

        // Write the mesh bounds
        // center
        bw.Write(voxelField.MeshBounds.center.x);
        bw.Write(voxelField.MeshBounds.center.y);
        bw.Write(voxelField.MeshBounds.center.z);
        // size
        bw.Write(voxelField.MeshBounds.size.x);
        bw.Write(voxelField.MeshBounds.size.y);
        bw.Write(voxelField.MeshBounds.size.z);

        // Write the voxel field values
        bw.Write((System.UInt32)(voxelField.m_Field.Length));
        for (int i = 0; i < voxelField.m_Field.Length; ++i)
        {
            bw.Write(voxelField.m_Field[i]);
        }

        bw.Close();
        fs.Close();

        return true;
    }
}

public class VoxelField : ScriptableObject
{
    [SerializeField]
    public int m_Id;

    [SerializeField]
    public string m_Name;

    [SerializeField]
    public float[] m_Field;

    [SerializeField]
    public int m_VoxelCountX;
    [SerializeField]
    public int m_VoxelCountY;
    [SerializeField]
    public int m_VoxelCountZ;

    [SerializeField]
    public float m_VoxelSize;

    [SerializeField]
    public Bounds m_MeshBounds;

    public float VoxelSize
    {
        get
        {
            return m_VoxelSize;
        }
    }

    public Bounds MeshBounds
    {
        get
        {
            return m_MeshBounds;
        }
    }

    static int s_Ids = 0;
    public void Initialize(int voxelCountX, int voxelCountY, int voxelCountZ, float voxelSize, Bounds meshBounds)
    {
        m_Id = s_Ids++;

        m_VoxelCountX = voxelCountX;
        m_VoxelCountY = voxelCountY;
        m_VoxelCountZ = voxelCountZ;

        m_VoxelSize = voxelSize;
        m_MeshBounds = meshBounds;

        m_Field = new float[m_VoxelCountX * m_VoxelCountY * m_VoxelCountZ];
    }

    public float Get(int x, int y, int z)
    {
        int index = GetIndex(x, y, z);
        return m_Field[index];
    }

    public void Set(int x, int y, int z, float value)
    {
        int index = GetIndex(x, y, z);
        m_Field[index] = value;
    }

    int GetIndex(int x, int y, int z)
    {
        int index = x + m_VoxelCountX * (y + (m_VoxelCountY * z));
        //int index = (z * m_VoxelFieldDimensions.x * m_VoxelFieldDimensions.y) + (y * m_VoxelFieldDimensions.x) + x;
        return index;
    }

    public void Fill(float value)
    {
        for (int z = 0; z < m_VoxelCountZ; ++z)
        {
            for (int y = 0; y < m_VoxelCountY; ++y)
            {
                for (int x = 0; x < m_VoxelCountX; ++x)
                {
                    Set(x, y, z, value);
                }
            }
        }
    }
}
