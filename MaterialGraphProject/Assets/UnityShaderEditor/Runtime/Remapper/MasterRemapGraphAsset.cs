namespace UnityEngine.MaterialGraph
{
     public class MasterRemapGraphAsset : ScriptableObject
     {
         [SerializeField]
         private MasterRemapGraph m_RemapGraph = new MasterRemapGraph();

         public MasterRemapGraph remapGraph
         {
             get { return m_RemapGraph; }
             set { m_RemapGraph = value; }
         }
     }

}
