using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;

namespace KWEngine2.Model
{
    /// <summary>
    /// Modellklasse
    /// </summary>
    public class GeoModel
    {

        internal bool IsKWCube6 { get; set; } = false;


        /// <summary>
        /// Wurzelknoten
        /// </summary>
        internal GeoNode Root { get; set; } = null;
        /// <summary>
        /// Pfad zur Modelldatei
        /// </summary>
        public string Path { get; internal set; }
        /// <summary>
        /// Absoluter Pfad zur Modelldatei
        /// </summary>
        public string PathAbsolute { get; internal set; }
        internal SceneImporter.AssemblyMode AssemblyMode { get; set; }
        /// <summary>
        /// Animationsliste
        /// </summary>
        public List<GeoAnimation> Animations { get; internal set; }
        /// <summary>
        /// Verfügt das Modell über Knochen für Animationen?
        /// </summary>
        public bool HasBones
        {
            get; internal set;

        } = false;
        
        /// <summary>
        /// Knoten des Skeletts
        /// </summary>
        internal GeoNode Armature { get; set; }

        /// <summary>
        /// Validität des Modells
        /// </summary>
        public bool IsValid { get; internal set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Dateiname
        /// </summary>
        public string Filename { get; internal set; }
        /// <summary>
        /// Globale Transformationsinvers-Matrix
        /// </summary>
        public Matrix4 TransformGlobalInverse { get; internal set; }
        internal Dictionary<string, GeoMesh> Meshes { get; set; }
        internal List<GeoMeshHitbox> MeshHitboxes { get; set; }
        /// <summary>
        /// Handelt es sich bei dem Modell um Terrain?
        /// </summary>
        public bool IsTerrain
        { 
            get
            {
                bool found = Meshes.TryGetValue("Terrain", out GeoMesh terrainMesh);
                if (found)
                {
                    return terrainMesh.Terrain != null;
                }
                else
                {
                    return false;
                }
            }
        }

        internal List<string> BoneNames { get; set; } = new List<string>();
        internal List<GeoNode> NodesWithoutHierarchy = new List<GeoNode>();
        internal Dictionary<string, GeoTexture> Textures { get; set; }

        internal void Dispose()
        {
            IsValid = false;
            if (Textures != null)
            {
                lock (Textures)
                {
                    foreach (GeoTexture t in Textures.Values)
                    {
                        GL.DeleteTexture(t.OpenGLID);
                    }
                    Textures.Clear();
                }
            }
            if (Meshes != null)
            {
                lock (Meshes)
                {
                    foreach (GeoMesh m in Meshes.Values)
                    {
                        m.Dispose();
                    }
                }
            }
        }

        internal void CalculatePath()
        {
            if (AssemblyMode == SceneImporter.AssemblyMode.File)
            {
                FileInfo fi = new FileInfo(Filename);
                if (fi.Exists)
                {
                    Path = fi.DirectoryName;
                }
                else
                {
                    throw new Exception("File " + Filename + " does not exist.");
                }
            }
        }

        internal int GLBOffset { get; set; } = -1;

        internal bool IsGLTF { get; set; } = false;
    }
}
