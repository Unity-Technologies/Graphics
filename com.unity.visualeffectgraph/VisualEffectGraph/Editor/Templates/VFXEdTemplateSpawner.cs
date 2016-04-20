using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    // TODO Refactor
  /*  internal class VFXEdTemplateSpawner : VFXEdSpawner
    {
        private string m_Path;
        private VFXEdDataSource m_Datasource;
        private VFXEdCanvas m_Canvas;

        public VFXEdTemplateSpawner(string path, VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 canvasPosition ) : base(canvasPosition)
        {
            m_Path = path;
            m_Datasource = datasource;
            m_Canvas = canvas;
        }

        public override void Spawn()
        {
            VFXEdSpawnTemplate template = VFXEditor.SpawnTemplates.GetTemplate(m_Path);
            template.Spawn(m_Datasource, m_Canvas, m_canvasPosition);
        }
    }*/
}
