using glTFLoader;
using glTFLoader.Schema;
using KWEngine2.Helper;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using static KWEngine2.Model.SceneImporter;

namespace KWEngine2.Model
{
    internal static class SceneImporterGLTF
    {
        internal static GeoModel LoadModel(string filename, bool flipTextureCoordinates = false)
        {
            Gltf scene = Interface.LoadModel(filename);
            if(scene.Scenes.Length != 1)
            {
                throw new Exception("Cannot load gltf files with less or more than one scene. Please remodel!");
            }
            if (scene.Skins != null && scene.Skins.Length > 1)
            {
                throw new Exception("Cannot load gltf files with more than one skeletal armature. Please remodel!");
            }

            GeoModel model = ProcessScene(scene, filename.ToLower().Trim());
            return model;
        }

        private static GeoModel ProcessScene(Gltf scene, string filename)
        {
            GeoModel returnModel = new GeoModel();
            returnModel.Filename = filename;
            returnModel.Name = StripPathFromFile(filename);
            
            string p = Assembly.GetExecutingAssembly().Location;
            string pA = new DirectoryInfo(StripFileNameFromPath(p)).FullName;
            if (!Path.IsPathRooted(filename))
            {
                returnModel.PathAbsolute = Path.Combine(pA, filename);
            }
            else
            {
                returnModel.PathAbsolute = filename;
            }

            bool success = File.Exists(returnModel.PathAbsolute);
                
            returnModel.AssemblyMode = AssemblyMode.File;
            returnModel.CalculatePath();
            returnModel.Meshes = new Dictionary<string, GeoMesh>();
            returnModel.TransformGlobalInverse = Matrix4.Identity;
            returnModel.Textures = new Dictionary<string, GeoTexture>();
            returnModel.IsValid = false;

            GenerateNodeHierarchy(scene, ref returnModel);
            FindRootBone(scene.Scenes[0], ref scene, ref returnModel);
            ProcessBones(scene, ref returnModel);
            ProcessMeshes(scene, ref returnModel);
            ProcessAnimations(scene, ref returnModel);

            returnModel.IsValid = true;
            GLWindow.StartGarbageCollection();
            return returnModel;
        }

        private static Node GetNode(Gltf scene, int index)
        {
            return null;
        }

        private static Node GetRootNode()
        {
            return null;
        }


        private static void GenerateNodeHierarchy(Gltf gltf, ref GeoModel model)
        {
            Scene scene = gltf.Scenes[0];
            GeoNode root = new GeoNode();
            root.Name = "KWRootGLTF";
            root.Transform = Matrix4.Identity;
            root.Parent = null;
            model.Root = root;
            model.NodesWithoutHierarchy.Add(root);
            foreach(int childIndex in scene.Nodes)
            {
                Node child = gltf.Nodes[childIndex];
                root.Children.Add(MapNodeToNode(child, ref gltf, ref model, ref root));

            }
        }

        private static GeoNode MapNodeToNode(Node n, ref Gltf scene, ref GeoModel model, ref GeoNode callingNode)
        {
            
            GeoNode gNode = new GeoNode();
            gNode.Parent = callingNode;
            gNode.Transform = HelperMatrix.ConvertGLTFTRSToOpenTKMatrix(n.Scale, n.Rotation, n.Translation);
            gNode.Name = n.Name;
            model.NodesWithoutHierarchy.Add(gNode);
            if (n.Children != null)
            {
                foreach (int childIndex in n.Children)
                {
                    Node child = scene.Nodes[childIndex];
                    gNode.Children.Add(MapNodeToNode(child, ref scene, ref model, ref gNode));
                }
            }
            return gNode;
        }

        //private static Node GetNodeForBone(Node node, Bone b)
        private static Node GetNodeForBone(Node node, Skin b, ref Gltf gltf)
        {
            foreach(int nIndex in node.Children)
            {
                Node n = gltf.Nodes[nIndex];
                if(n.Name == b.Name)
                {
                    return n;
                }
                else
                {
                    Node nodeCandidate = GetNodeForBone(n, b, ref gltf);
                    if (nodeCandidate != null)
                        return nodeCandidate;
                }
            }
            return null;
        }

        private static void FindRootBone(Scene scene, ref Gltf gltf, ref GeoModel model)
        {
            if (gltf.Skins != null && gltf.Skins.Length > 0 && gltf.Skins[0].Joints.Length > 0)
            {
                string armatureNodeName = gltf.Skins[0].Name;
                foreach (GeoNode n in model.NodesWithoutHierarchy)
                {
                    if (n.Name == armatureNodeName)
                    {
                        model.Armature = n;
                        return;
                    }
                }
            }
        }

        private static void ProcessBoneStructure(Gltf scene, ref GeoModel model, ref Node currentNode)
        {
            if (currentNode.Skin == null)
            {
                // this is a bone
                GeoBone geoBone = new GeoBone();
                geoBone.Name = currentNode.Name;
                //geoBone.Index = localBoneIndex++;
                //geoBone.Offset = FindInverseBindMatrixForBone(scene, childNode, ref model);
                if (!model.BoneNames.Contains(geoBone.Name))
                {
                    model.BoneNames.Add(geoBone.Name);
                }

            }
            if (currentNode.Children != null)
            {
                foreach (int childIndex in currentNode.Children)
                    ProcessBoneStructure(scene, ref model, ref scene.Nodes[childIndex]);
            }
        }

        private static void ProcessBones(Gltf scene, ref GeoModel model)
        {
            // TODO: Check if this is even needed anymore...
            if(model.Armature == null)
            {
                return;
            }

            Node armature = null;
            foreach(Node n in scene.Nodes)
            {
                if(n.Name == model.Armature.Name)
                {
                    // found armature node
                    armature = n;
                    break;
                }
            }
            if(armature != null)
            {
                model.HasBones = true;
                int localBoneIndex = 0;
                foreach(int armatureChildNodeId in armature.Children)
                {
                    Node childNode = scene.Nodes[armatureChildNodeId];
                    if(childNode.Skin == null)
                    {
                        // this is a bone
                        GeoBone geoBone = new GeoBone();
                        geoBone.Name = childNode.Name;
                        geoBone.Index = localBoneIndex++;
                        geoBone.Offset = FindInverseBindMatrixForBone(scene, childNode, ref model);
                        if(!model.BoneNames.Contains(geoBone.Name))
                        {
                            model.BoneNames.Add(geoBone.Name);
                        }
                        
                    }
                    ProcessBoneStructure(scene, ref model, ref childNode);

                }
            }
            
            if (model.BoneNames.Count > KWEngine.MAX_BONES)
            {
                throw new Exception("Model has more than " + KWEngine.MAX_BONES + " bones. Cannot import model.");
            }

        }

        private static Matrix4 FindInverseBindMatrixForBone(Gltf scene, Node boneNode, ref GeoModel model)
        {
            int accessorId = scene.Skins[0].InverseBindMatrices == null ? -1 : (int)scene.Skins[0].InverseBindMatrices;
            if (accessorId < 0)
                return Matrix4.Identity;
            
            Accessor a = scene.Accessors[accessorId];
            if(a.Type == Accessor.TypeEnum.MAT4)
            {
                Matrix4[] matrices = GetInverseBindMatricesFromAccessor(a, scene, ref model);
                // find the index of the given boneNode in the joints list
                for (int i = 0; i < scene.Skins[0].Joints.Length; i++)
                {
                    if (scene.Nodes[scene.Skins[0].Joints[i]].Name == boneNode.Name)
                    {
                        return matrices[i];
                    }
                }
                return Matrix4.Identity;
            }
            else
            {
                return Matrix4.Identity;
            }
        }

        private static GeoBone CreateBone(Gltf scene, ref GeoModel model)
        {
            GeoBone bone = new GeoBone();
            //
            return bone;
        }

        private static Matrix4[] GetInverseBindMatricesFromAccessor(Accessor a, Gltf scene, ref GeoModel model)
        {
            Matrix4[] matrices = new Matrix4[a.Count];
            byte[] data = GetByteDataFromAccessor(scene, a, ref model);

            int offset = 0;
            // analyse data array:
            for(int i = 0; i < matrices.Length; i++)
            {
                float[] tmpArray = new float[16];
                for (int j = 0; j < tmpArray.Length; j++)
                {
                    float f = BitConverter.ToSingle(data, offset);
                    tmpArray[j] = f;
                    offset += 4;
                }
                matrices[i] = HelperMatrix.ConvertGLTFFloatArraytoOpenTKMatrix(tmpArray);
            }

            // return final matrices
            return matrices;
        }

        private static bool FindTransformForMesh(Gltf scene, Node currentNode, Mesh mesh, ref Matrix4 transform, out string nodeName, out Node node, ref Matrix4 parentTransform)
        {
            
            Matrix4 currentNodeTransform = parentTransform * HelperMatrix.ConvertGLTFTRSToOpenTKMatrix(currentNode.Scale, currentNode.Rotation, currentNode.Translation);
            if(currentNode.Mesh != null)
            {
                Mesh tmpMesh = scene.Meshes[(int)currentNode.Mesh];
                if (tmpMesh.Name == mesh.Name)
                {
                    transform = currentNodeTransform;
                    nodeName = currentNode.Name;
                    node = currentNode;
                    return true;
                }
            }
            if (currentNode.Children != null)
            {
                for (int i = 0; i < currentNode.Children.Length; i++)
                {
                    Node child = scene.Nodes[currentNode.Children[i]];
                    bool found = FindTransformForMesh(scene, child, mesh, ref transform, out string nName, out Node node2, ref currentNodeTransform);
                    if (found)
                    {
                        nodeName = nName;
                        node = node2;
                        return true;
                    }
                }
            }

            transform = Matrix4.Identity;
            nodeName = null;
            node = null;
            return false;
        }

        internal static string StripFileNameFromPath(string path)
        {
            int index = path.LastIndexOf('\\');
            if (index < 0)
            {
                return path;
            }
            else
            {
                return path.Substring(0, index + 1).ToLower();
            }

        }

        internal static string StripFileNameFromAssemblyPath(string path)
        {
            int index = path.LastIndexOf('.');
            if (index < 0)
            {
                return path;
            }
            else
            {
                index = path.LastIndexOf('.', index - 1);
                if (index < 0)
                {
                    return path;
                }
                else
                {
                    return path.Substring(0, index + 1);
                }
            }
        }

        internal static string StripPathFromFile(string fileWithPath)
        {
            int index = fileWithPath.LastIndexOf('\\');
            if (index < 0)
            {
                return fileWithPath;
            }
            else
            {
                return fileWithPath.Substring(index + 1).ToLower();
            }
        }

        internal static string FindTextureInSubs(string filename, string path = null)
        {
            DirectoryInfo currentDir;
            if (path == null)
            {
                string p = Assembly.GetExecutingAssembly().Location;
                currentDir = new DirectoryInfo(StripFileNameFromPath(p));
            }
            else
            {
                currentDir = new DirectoryInfo(StripFileNameFromPath(path));
            }

            foreach (FileInfo fi in currentDir.GetFiles())
            {
                if (fi.Name.ToLower() == StripPathFromFile(filename).ToLower())
                {
                    // file found:
                    return fi.FullName;
                }
            }

            if (currentDir.GetDirectories().Length == 0)
            {
                Debug.WriteLine("File " + filename + " not found anywhere.");
            }
            else
            {
                foreach (DirectoryInfo di in currentDir.GetDirectories())
                {
                    return FindTextureInSubs(filename, di.FullName);
                }
            }

            return "";
        }

        private static void ProcessMaterialsForMesh(Gltf scene, Mesh mesh, MeshPrimitive currentPrimitive, ref GeoModel model, ref GeoMesh geoMesh)
        {
            GeoMaterial geoMaterial = new GeoMaterial();
            int materialId = currentPrimitive.Material == null ? -1 : (int)currentPrimitive.Material;

            if (materialId < 0)
            {
                geoMaterial.BlendMode = OpenTK.Graphics.OpenGL4.BlendingFactor.OneMinusSrcAlpha;
                geoMaterial.ColorAlbedo = new Vector4(1, 1, 1, 1);
                geoMaterial.ColorEmissive = new Vector4(0, 0, 0, 1);
                geoMaterial.TextureRoughnessIsSpecular = false;
                geoMaterial.Roughness = 1;
                geoMaterial.Metalness = 0;
                geoMaterial.Opacity = 1;
                if (mesh.Name != null && mesh.Name.ToLower().Contains("_invisible"))
                {
                    geoMaterial.Opacity = 0;
                }
            }
            else
            {
                Material material = scene.Materials[materialId];
                geoMaterial.Name = material.Name;
                geoMaterial.BlendMode = OpenTK.Graphics.OpenGL4.BlendingFactor.OneMinusSrcAlpha;
                geoMaterial.ColorAlbedo = new Vector4(material.PbrMetallicRoughness.BaseColorFactor[0], material.PbrMetallicRoughness.BaseColorFactor[1], material.PbrMetallicRoughness.BaseColorFactor[2], material.PbrMetallicRoughness.BaseColorFactor[3]);
                geoMaterial.ColorEmissive = new Vector4(material.EmissiveFactor[0], material.EmissiveFactor[1], material.EmissiveFactor[2], 1f);
                geoMaterial.Metalness = material.PbrMetallicRoughness.MetallicFactor;
                geoMaterial.Roughness = material.PbrMetallicRoughness.RoughnessFactor;
                if (mesh.Name != null && mesh.Name.ToLower().Contains("_invisible"))
                {
                    geoMaterial.Opacity = 0;
                }
                else
                {
                    geoMaterial.Opacity = 1;
                }
                


                // Color/Diffuse texture:
                if(material.PbrMetallicRoughness.BaseColorTexture != null)
                {
                    Texture tinfo = scene.Textures[material.PbrMetallicRoughness.BaseColorTexture.Index];
                    int sourceId = tinfo.Source != null ? (int)tinfo.Source : -1;
                    int sampleId = tinfo.Sampler != null ? (int)tinfo.Sampler : -1;
                    if (sourceId >= 0)
                    {
                        glTFLoader.Schema.Image i = scene.Images[sourceId];

                        GeoTexture tex = new GeoTexture();
                        bool duplicateFound = CheckIfOtherModelsShareTexture(i.Uri, model.Path, out tex);
                        if (!duplicateFound)
                        {
                            byte[] rawTextureData = GetTextureDataFromAccessor(scene, i, ref model, out string filename);
                            int glTextureId = HelperTexture.LoadTextureForModelGLB(rawTextureData);
                            tex.UVTransform = new OpenTK.Vector2(1, 1); // TODO: find a way to lookup uv transform in glTF
                            tex.Filename = filename;
                            tex.UVMapIndex = material.PbrMetallicRoughness.BaseColorTexture.TexCoord;
                            tex.Type = TextureType.Albedo;
                            tex.OpenGLID = glTextureId;
                        }
                        geoMaterial.TextureAlbedo = tex;
                    }
                }

                // Normal map:
                if (material.NormalTexture != null)
                {
                    MaterialNormalTextureInfo tinfo = material.NormalTexture;
                    int sourceId = tinfo.Index;
                    glTFLoader.Schema.Image i = scene.Images[sourceId];

                    GeoTexture tex = new GeoTexture();
                    bool duplicateFound = CheckIfOtherModelsShareTexture(i.Uri, model.Path, out tex);
                    if (!duplicateFound)
                    {
                        byte[] rawTextureData = GetTextureDataFromAccessor(scene, i, ref model, out string filename);
                        int glTextureId = HelperTexture.LoadTextureForModelGLB(rawTextureData);
                        tex.UVTransform = new OpenTK.Vector2(material.NormalTexture.Scale, material.NormalTexture.Scale);
                        tex.Filename = filename;
                        tex.UVMapIndex = material.NormalTexture.TexCoord;
                        tex.Type = TextureType.Normal;
                        tex.OpenGLID = glTextureId;
                    }
                    geoMaterial.TextureNormal = tex;
                }

                // Metalness/roughness texture:
                if (material.PbrMetallicRoughness.MetallicRoughnessTexture != null)
                {
                    geoMaterial.TextureRoughnessInMetalness = true;
                    Texture tinfo = scene.Textures[material.PbrMetallicRoughness.MetallicRoughnessTexture.Index];
                    int sourceId = tinfo.Source != null ? (int)tinfo.Source : -1;
                    int sampleId = tinfo.Sampler != null ? (int)tinfo.Sampler : -1;
                    if (sourceId >= 0)
                    {
                        glTFLoader.Schema.Image i = scene.Images[sourceId];

                        GeoTexture tex = new GeoTexture();
                        bool duplicateFound = CheckIfOtherModelsShareTexture(i.Uri, model.Path, out tex);
                        if (!duplicateFound)
                        {
                            byte[] rawTextureData = GetTextureDataFromAccessor(scene, i, ref model, out string filename);
                            int glTextureId = HelperTexture.LoadTextureForModelGLB(rawTextureData);
                            tex.UVTransform = new OpenTK.Vector2(1, 1);
                            tex.Filename = filename;
                            tex.UVMapIndex = material.PbrMetallicRoughness.BaseColorTexture.TexCoord;
                            tex.Type = TextureType.Metalness;
                            tex.OpenGLID = glTextureId;
                        }
                        geoMaterial.TextureMetalness = tex;
                        geoMaterial.TextureRoughnessIsSpecular = true;
                    }
                }

                // Emissive texture
                if (material.EmissiveTexture != null)
                {
                    TextureInfo tinfo = material.EmissiveTexture;
                    int sourceId = tinfo.Index;
                    glTFLoader.Schema.Image i = scene.Images[sourceId];

                    GeoTexture tex = new GeoTexture();
                    bool duplicateFound = CheckIfOtherModelsShareTexture(i.Uri, model.Path, out tex);
                    if (!duplicateFound)
                    {
                        byte[] rawTextureData = GetTextureDataFromAccessor(scene, i, ref model, out string filename);
                        int glTextureId = HelperTexture.LoadTextureForModelGLB(rawTextureData);
                        tex.UVTransform = new OpenTK.Vector2(1, 1);
                        tex.Filename = filename;
                        tex.UVMapIndex = material.PbrMetallicRoughness.BaseColorTexture.TexCoord;
                        tex.Type = TextureType.Emissive;
                        tex.OpenGLID = glTextureId;
                    }
                    geoMaterial.TextureEmissive = tex;
                }

                // TODO: implement metalness texture & lightmap texture on second UV-Index as soon as gltf supports lightmaps
            }
            geoMesh.Material = geoMaterial;
        }

        private static bool CheckIfOtherModelsShareTexture(string texture, string path, out GeoTexture sharedTex)
        {
            sharedTex = new GeoTexture();
            foreach (string key in KWEngine.Models.Keys)
            {
                GeoModel m = KWEngine.Models[key];
                if (m.Path == path)
                {
                    foreach (string texKey in m.Textures.Keys)
                    {
                        if (texKey == texture)
                        {
                            sharedTex = m.Textures[texKey];
                            return true;
                        }
                    }
                }

            }
            return false;
        }

        private static int GetGlbOffset(string pathAbsolute, ref GeoModel model)
        {
            if (model.GLBOffset >= 0)
            {
                return model.GLBOffset;
            }
            else
            {
                int glbOffset = 0;
                using (FileStream stream = File.Open(pathAbsolute, FileMode.Open, FileAccess.Read))
                {
                    byte[] data = new byte[4];
                    for (int i = 0; i < stream.Length - 4; i++)
                    {
                        stream.Position = i;
                        stream.Read(data, 0, 4);

                        if (data[3] == 0x00 && data[2] == 0x4E && data[1] == 0x49 && data[0] == 0x42)
                        {
                            glbOffset = i + 4;
                            break;
                        }
                    }
                }
                return glbOffset;
            }
        }

        private static byte[] GetTextureDataFromAccessor(Gltf scene, glTFLoader.Schema.Image i, ref GeoModel model, out string filename)
        {
            filename = null;
            if(i.MimeType != glTFLoader.Schema.Image.MimeTypeEnum.image_jpeg && i.MimeType != glTFLoader.Schema.Image.MimeTypeEnum.image_png)
            {
                throw new Exception("Invalid texture format detected. Only JPG and PNG are allowed.");
            }
            byte[] data = null;
            if (i.BufferView != null)
            {
                BufferView bvTexture = scene.BufferViews[(int)i.BufferView];
                int bufferViewStride = bvTexture.ByteStride == null ? 0 : (int)bvTexture.ByteStride;
                int bufferViewLength = bvTexture.ByteLength;
                int bufferViewOffset = bvTexture.ByteOffset;
                data = new byte[bufferViewLength];
                if (model.PathAbsolute.ToLower().EndsWith(".glb"))
                {
                    int glbOffset = GetGlbOffset(model.PathAbsolute, ref model);
                    using (FileStream stream = File.Open(model.PathAbsolute, FileMode.Open, FileAccess.Read))
                    {
                        stream.Position = glbOffset + bufferViewOffset;
                        if (bufferViewStride == 0)
                        {
                            stream.Read(data, 0, bufferViewLength);
                        }
                        else
                        {
                            throw new Exception("GLTF byte stride attribute not supported yet.");
                        }

                    }
                }
                else
                {
                    if (i.Uri.StartsWith("data:application/octet-stream;base64"))
                    {
                        data = GetDataFromBase64Stream(scene, i, i.Uri);
                    }
                    else
                    {
                        using(FileStream stream = File.Open(model.Path + @"\\" + i.Uri, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            stream.Position = bufferViewOffset;
                            stream.Read(data, 0, bufferViewLength);
                        }
                        filename = i.Uri;
                    }
                }
            }
            else
            {
                using (FileStream stream = File.Open(model.Path + @"\\" + i.Uri, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    data = new byte[stream.Length];
                    stream.Position = 0;
                    stream.Read(data, 0, (int)stream.Length);
                }
                filename = i.Uri;
            }
            return data;
        }

        private static byte[] GetByteDataFromAccessor(Gltf scene, Accessor a, ref GeoModel model)
        {
            if (a == null)
                return null;

            int accessorOffset = a.ByteOffset;
            int accessorCount = a.Count;
            int bufferViewId = (int)a.BufferView;

            BufferView bufferView = scene.BufferViews[bufferViewId];
            int bufferViewStride = bufferView.ByteStride == null ? 0 : (int)bufferView.ByteStride;
            int bufferViewLength = bufferView.ByteLength;
            int bufferViewOffset = bufferView.ByteOffset;
            byte[] data = new byte[bufferViewLength];
            if (model.PathAbsolute.ToLower().EndsWith(".glb"))
            {
                
                int glbOffset = 0;
                using (FileStream stream = File.Open(model.PathAbsolute, FileMode.Open, FileAccess.Read))
                {
                    if (model.GLBOffset < 0)
                    {
                        for (int i = 0; i < stream.Length - 4; i++)
                        {
                            stream.Position = i;
                            stream.Read(data, 0, 4);

                            if (data[3] == 0x00 && data[2] == 0x4E && data[1] == 0x49 && data[0] == 0x42)
                            {
                                glbOffset = i + 4;
                                break;
                            }
                        }
                    }
                    else
                        glbOffset = model.GLBOffset;
                    stream.Position = glbOffset + accessorOffset + bufferViewOffset;
                    if (bufferViewStride == 0)
                    {
                        stream.Read(data, 0, bufferViewLength);
                    }
                    else
                    {
                        throw new Exception("GLTF byte stride attribute not supported yet.");
                    }

                }
            }
            else
            {
                glTFLoader.Schema.Buffer buffer = scene.Buffers[bufferView.Buffer];
                if (buffer.Uri.StartsWith("data:application/octet-stream;base64"))
                {
                    if (bufferViewStride == 0)
                        data = GetDataFromBase64Stream(a, bufferViewLength, accessorOffset + bufferViewOffset, buffer.Uri);
                    else
                        throw new Exception("GLTF byte stride attribute not supported yet.");
                }
                else
                {

                    using (FileStream stream = File.Open(model.Path + @"\\" + buffer.Uri, FileMode.Open, FileAccess.Read))
                    {
                        stream.Position = accessorOffset + bufferViewOffset;
                        if (bufferViewStride == 0)
                        {
                            stream.Read(data, 0, bufferViewLength);
                        }
                        else
                        {
                            throw new Exception("GLTF byte stride attribute not supported yet.");
                        }
                    }
                }
            }
            return data;
        }

        private static byte[] GetDataFromBase64Stream(Accessor accessor, int bytesToCopy, int offset, string uri)
        {
            int commapos = uri.IndexOf(',');
            byte[] data = null;
            if (commapos >= 0)
            {
                byte[] chunk = Convert.FromBase64String(uri.Substring(commapos + 1));
                data = new byte[bytesToCopy];
                Array.Copy(chunk, offset, data, 0, data.Length);
            }
            else
            {
                throw new Exception("no valid base64 string found in gltf file. Please remodel.");
            }
            return data;
        }

        private static byte[] GetDataFromBase64Stream(Gltf scene, glTFLoader.Schema.Image accessor, string uri)
        {
            int commapos = uri.IndexOf(',');
            int bufferViewId = accessor.BufferView != null ? (int)accessor.BufferView : -1;
            byte[] data = null;
            if (commapos >= 0 && bufferViewId >= 0)
            {
                byte[] chunk = Convert.FromBase64String(uri.Substring(commapos + 1));
                data = new byte[scene.BufferViews[bufferViewId].ByteLength];
                Array.Copy(chunk, scene.BufferViews[bufferViewId].ByteOffset, data, 0, data.Length);
            }
            else
            {
                throw new Exception("no valid base64 string found in gltf file. Please remodel.");
            }
            return data;
        }


        private static GeoVertex[] GetVertexDataForMeshPrimitive(Gltf scene, MeshPrimitive mprim, ref GeoModel model, Node node, out float xmin, out float xmax, out float ymin, out float ymax, out float zmin, out float zmax, out List<Vector3> uniqueVertices, out List<int> uniqueBoneIds)
        {
            xmin = float.MaxValue;
            xmax = float.MinValue;
            ymin = float.MaxValue;
            ymax = float.MinValue;
            zmin = float.MaxValue;
            zmax = float.MinValue;
            uniqueVertices = new List<Vector3>();

            int vertexIndex = (int)mprim.Attributes["POSITION"];
            int bonesIndex = mprim.Attributes.ContainsKey("JOINTS_0") ? mprim.Attributes["JOINTS_0"] : -1;
            int weightsIndex = mprim.Attributes.ContainsKey("WEIGHTS_0") ? mprim.Attributes["WEIGHTS_0"] : -1;
            Accessor indexAccessor = scene.Accessors[vertexIndex];
            Accessor bonesAccessor = null;
            Accessor weightsAccessor = null;
            if (bonesIndex >= 0 && weightsIndex >= 0)
            {
                bonesAccessor = scene.Accessors[bonesIndex];
                weightsAccessor = scene.Accessors[weightsIndex];
            }

            byte[] data = GetByteDataFromAccessor(scene, indexAccessor, ref model);
            byte[] dataBones = GetByteDataFromAccessor(scene, bonesAccessor, ref model);
            byte[] dataWeights = GetByteDataFromAccessor(scene, weightsAccessor, ref model);

            int bytesPerData = GetBytesPerData(indexAccessor);
            int bytesPerDataBones = GetBytesPerData(bonesAccessor);
            int bytesPerDataWeights = GetBytesPerData(weightsAccessor);
            uint[] bonesData = null;
            float[] weightsData = null;

            float[] verticesData = new float[data.Length / bytesPerData];
            if (bytesPerDataBones > 0 && bytesPerDataWeights > 0)
            {
                bonesData = new uint[dataBones.Length / bytesPerDataBones];
                weightsData = new float[dataWeights.Length / bytesPerDataWeights];
            }
            List<GeoVertex> verticesArray = new List<GeoVertex>();
            for (int i = 0, j = 0, cswitch = 0; i < data.Length; i += bytesPerData)
            {
                if (bytesPerData == 1)
                {
                    verticesData[j++] = data[i];
                }
                else if (bytesPerData == 2)
                {
                    verticesData[j++] = BitConverter.ToUInt16(data, i);
                }
                else
                {
                    verticesData[j] = BitConverter.ToSingle(data, i);

                    if (cswitch == 0)
                    {
                        if (verticesData[j] < xmin)
                            xmin = verticesData[j];
                        if (verticesData[j] > xmax)
                            xmax = verticesData[j];
                    }
                    else if (cswitch == 1)
                    {
                        if (verticesData[j] < ymin)
                            ymin = verticesData[j];
                        if (verticesData[j] > ymax)
                            ymax = verticesData[j];
                    }
                    else
                    {
                        if (verticesData[j] < zmin)
                            zmin = verticesData[j];
                        if (verticesData[j] > zmax)
                            zmax = verticesData[j];

                        Vector3 tmpVertex = new Vector3(verticesData[j - 2], verticesData[j - 1], verticesData[j]);
                        if (node.Name.ToLower().Contains("_fullhitbox"))
                        {
                            bool add = true;
                            for (int k = 0; k < uniqueVertices.Count; k++)
                            {
                                if (uniqueVertices[k].X == tmpVertex.X && uniqueVertices[k].Y == tmpVertex.Y && uniqueVertices[k].Z == tmpVertex.Z)
                                {
                                    add = false;
                                    break;
                                }
                            }
                            if (add)
                            {
                                uniqueVertices.Add(tmpVertex);
                            }
                        }
                        verticesArray.Add(new GeoVertex(j, tmpVertex.X, tmpVertex.Y, tmpVertex.Z));
                    }

                    cswitch = (cswitch + 1) % 3;
                    j++;
                }
            }

            uniqueBoneIds = new List<int>();
            int numberOfWeightsActuallySet = 0;
            if (bytesPerDataBones > 0 && bytesPerDataWeights > 0)
            {
                for (int i = 0; i < verticesArray.Count * 4; i++)
                {
                    // if four values have been collected, update the GeoVertex instance appropriately:
                    if (i > 0 && i % 4 == 0)
                    {
                        GeoVertex vertex = verticesArray[(i - 1) / 4];
                        int tmpWeightsSet = FillGeoVertexWithBoneIdsAndWeights(bonesData[i - 4], bonesData[i - 3], bonesData[i - 2], bonesData[i - 1], weightsData[i - 4], weightsData[i - 3], weightsData[i - 2], weightsData[i - 1], ref vertex, out List<int> bIds);
                        if(tmpWeightsSet > numberOfWeightsActuallySet)
                        {
                            numberOfWeightsActuallySet = tmpWeightsSet;
                        }
                        foreach(int bId in bIds)
                        {
                            if (!uniqueBoneIds.Contains(bId))
                                uniqueBoneIds.Add(bId);
                        }
                    }

                    if (bytesPerDataBones == 1)
                        bonesData[i] = dataBones[i];
                    else if (bytesPerDataBones == 2)
                    {
                        bonesData[i] = BitConverter.ToUInt16(dataBones, i * bytesPerDataBones);
                    }
                    else
                    {
                        bonesData[i] = BitConverter.ToUInt32(dataBones, i * bytesPerDataBones);
                    }

                    if (bytesPerDataWeights == 1)
                        weightsData[i] = dataWeights[i];
                    else if (bytesPerDataWeights == 2)
                    {
                        weightsData[i] = BitConverter.ToInt16(dataWeights, i * bytesPerDataWeights);

                    }
                    else
                    {
                        weightsData[i] = BitConverter.ToSingle(dataWeights, i * bytesPerDataWeights);
                    }
                }

            }
            //numberOfWeightsSet = numberOfWeightsActuallySet;
            return verticesArray.ToArray();
        }

        private static int FillGeoVertexWithBoneIdsAndWeights(uint b0, uint b1, uint b2, uint b3, float w0, float w1, float w2, float w3, ref GeoVertex vertex, out List<int> boneIds)
        {
            Vector3 weight = new Vector3(w0, w1, w2);
            weight.Normalize();
            boneIds = new List<int>();

            vertex.BoneIDs[0] = b0;
            vertex.Weights[0] = weight.X;
            vertex.BoneIDs[1] = b1;
            vertex.Weights[1] = weight.Y;
            vertex.BoneIDs[2] = b2;
            vertex.Weights[2] = weight.Z;
            if (weight.X > 0)
            {
                vertex.WeightSet++;
                boneIds.Add((int)b0);
            }
            if (weight.Y > 0)
            {
                vertex.WeightSet++;
                boneIds.Add((int)b1);
            }
            if (weight.Z > 0)
            {
                vertex.WeightSet++;
                boneIds.Add((int)b2);
            }

            return vertex.WeightSet;
        }

        private static float[] GetNormalDataForMeshPrimitive(Gltf scene, MeshPrimitive mprim, ref GeoModel model, Node node, out List<Vector3> normalDataUnique)
        {
            int vertexIndex = (int)mprim.Attributes["NORMAL"];
            Accessor indexAccessor = scene.Accessors[vertexIndex];

            byte[] data = GetByteDataFromAccessor(scene, indexAccessor, ref model);
            int bytesPerData = GetBytesPerData(indexAccessor);
            float[] normalData = new float[data.Length / bytesPerData];
            normalDataUnique = new List<Vector3>();
            for (int i = 0, j = 0; i < data.Length; i += bytesPerData)
            {
                if (bytesPerData == 1)
                {
                    normalData[j++] = data[i];
                }
                else if (bytesPerData == 2)
                {
                    normalData[j++] = BitConverter.ToUInt16(data, i);
                }
                else
                {
                    normalData[j++] = BitConverter.ToSingle(data, i);
                }
            }

            if(node.Name.ToLower().Contains("_fullhitbox"))
            {
                for(int i = 0; i < normalData.Length; i+=3)
                {
                    Vector3 n = new Vector3(normalData[i], normalData[i+1], normalData[i+2]);
                    bool add = true;
                    for(int j = 0; j < normalDataUnique.Count; j++)
                    {
                        if ((normalDataUnique[j].X == n.X && normalDataUnique[j].Y == n.Y && normalDataUnique[j].Z == n.Z) ||
                                (normalDataUnique[j].X == -n.X && normalDataUnique[j].Y == -n.Y && normalDataUnique[j].Z == -n.Z))
                        {
                            add = false;
                            break;
                        }
                    }
                    if(add)
                    {
                        normalDataUnique.Add(n);
                    }
                }
            }

            return normalData;
        }

        private static float[] GetTangentDataForMeshPrimitive(Gltf scene, MeshPrimitive mprim, ref GeoModel model)
        {
            if (mprim.Attributes.ContainsKey("TANGENT"))
            {
                int vertexIndex = (int)mprim.Attributes["TANGENT"];
                Accessor indexAccessor = scene.Accessors[vertexIndex];

                byte[] data = GetByteDataFromAccessor(scene, indexAccessor, ref model);

                int bytesPerData = GetBytesPerData(indexAccessor);
                float[] tangentData = new float[data.Length / bytesPerData];
                for (int i = 0, j = 0; i < data.Length; i += bytesPerData)
                {
                    if (bytesPerData == 1)
                    {
                        tangentData[j++] = data[i];
                    }
                    else if (bytesPerData == 2)
                    {
                        tangentData[j++] = BitConverter.ToUInt16(data, i);
                    }
                    else
                    {
                        tangentData[j++] = BitConverter.ToSingle(data, i);
                    }
                }
                return tangentData;
            }
            else
                return null;
            
        }

        private static float[] GetUVDataForMeshPrimitive(Gltf scene, MeshPrimitive mprim, ref GeoModel model, int index = 0)
        {
            if (mprim.Attributes.ContainsKey("TEXCOORD_" + index))
            {
                int vertexIndex = (int)mprim.Attributes["TEXCOORD_" + index];
                Accessor indexAccessor = scene.Accessors[vertexIndex];

                byte[] data = GetByteDataFromAccessor(scene, indexAccessor, ref model);

                int bytesPerData = GetBytesPerData(indexAccessor);
                float[] uvData = new float[data.Length / bytesPerData];
                for (int i = 0, j = 0; i < data.Length; i += bytesPerData)
                {
                    if (bytesPerData == 1)
                    {
                        uvData[j++] = data[i];
                    }
                    else if (bytesPerData == 2)
                    {
                        uvData[j++] = BitConverter.ToUInt16(data, i);
                    }
                    else
                    {
                        uvData[j++] = BitConverter.ToSingle(data, i);
                    }
                }
                return uvData;
            }
            else
                return null;
        }


        private static uint[] GetIndicesForMeshPrimitive(Gltf scene, MeshPrimitive mprim, ref GeoModel model)
        {
            int indicesIndex = (int)mprim.Indices;
            Accessor indexAccessor = scene.Accessors[indicesIndex];
            
            int accessorOffset = indexAccessor.ByteOffset;
            int accessorCount = indexAccessor.Count;
            int bufferViewId = (int)indexAccessor.BufferView;

            BufferView bufferViewIndices = scene.BufferViews[bufferViewId];
            int bufferViewStride = bufferViewIndices.ByteStride == null ? 0 : (int)bufferViewIndices.ByteStride;
            int bufferViewLength = bufferViewIndices.ByteLength;
            int bufferViewOffset = bufferViewIndices.ByteOffset;
            glTFLoader.Schema.Buffer indicesBuffer = scene.Buffers[bufferViewIndices.Buffer];
            byte[] data = new byte[bufferViewLength];
            if (model.PathAbsolute.ToLower().EndsWith(".glb"))
            {

                int glbOffset = 0;
                using (FileStream stream = File.Open(model.PathAbsolute, FileMode.Open, FileAccess.Read))
                {
                    for(int i = 0; i < stream.Length - 4; i++)
                    {
                        stream.Position = i;
                        stream.Read(data, 0, 4);

                        if (data[3] == 0x00 && data[2] == 0x4E && data[1] == 0x49 && data[0] == 0x42)
                        {
                            glbOffset = i + 4;
                            break;
                        }
                    }
                    model.GLBOffset = glbOffset;
                    stream.Position = glbOffset + accessorOffset + bufferViewOffset;
                    if(bufferViewStride == 0)
                    {
                        stream.Read(data, 0, bufferViewLength);
                    }
                    else
                    {
                        throw new Exception("GLTF byte stride attribute not supported yet.");
                    }
                    
                }
            }
            else
            {
                if(indicesBuffer.Uri != null)
                {
                    if(indicesBuffer.Uri.StartsWith("data:application/octet-stream;base64"))
                    {
                        data = GetDataFromBase64Stream(indexAccessor, bufferViewIndices.ByteLength, accessorOffset + bufferViewOffset, indicesBuffer.Uri);
                    }
                    else
                    {
                        
                        using (FileStream file = File.Open(model.Path + @"\\" + indicesBuffer.Uri, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            file.Position = accessorOffset + bufferViewOffset;
                            file.Read(data, 0, bufferViewLength);
                        }
                    }
                }
                else
                {
                    throw new Exception("Unable to generate model vertex indices. Please remodel.");
                }
            }

            int bytesPerData = GetBytesPerData(indexAccessor);
            uint[] indicesData = new uint[data.Length / bytesPerData];
            for(int i = 0, j = 0;  i < data.Length; i += bytesPerData)
            {
                if(bytesPerData == 1)
                {
                    indicesData[j++] = data[i];
                }
                else if(bytesPerData == 2)
                {
                    indicesData[j++] = BitConverter.ToUInt16(data, i);
                }
                else
                {
                    indicesData[j++] = BitConverter.ToUInt32(data, i);
                }
            }
            return indicesData;
        }

        private static int GetBytesPerData(Accessor a)
        {
            if (a != null)
            {
                if (a.ComponentType == Accessor.ComponentTypeEnum.BYTE)
                    return 1;
                else if (a.ComponentType == Accessor.ComponentTypeEnum.SHORT || a.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                    return 2;
                else if (a.ComponentType == Accessor.ComponentTypeEnum.FLOAT || a.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_INT)
                    return 4;
                else
                    return 1;
            }
            else
                return -1;
        }

        private static void ProcessMeshes(Gltf scene, ref GeoModel model)
        {
            model.MeshHitboxes = new List<GeoMeshHitbox>();
            string currentNodeName = null;
            Matrix4 currentNodeTransform = Matrix4.Identity;
            GeoMeshHitbox meshHitBox = null;
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            Matrix4 nodeTransform = Matrix4.Identity;
            Mesh mesh = null;
            

            for (int m = 0; m < scene.Meshes.Length; m++)
            {
                Node currentNode = null;
                mesh = scene.Meshes[m];
                if (mesh.Primitives[0].Mode != MeshPrimitive.ModeEnum.TRIANGLES)
                {
                    throw new Exception("Model's primitive type is not set to 'triangles'. Cannot import model.");
                }
                Matrix4 parentTransform = Matrix4.Identity;
                bool transformFound = false;
                string nodeName = "";
                while(transformFound != true)
                {
                    foreach(Node node in scene.Nodes)
                    {
                        transformFound = FindTransformForMesh(scene, node, mesh, ref nodeTransform, out nodeName, out Node targetNode, ref parentTransform);
                        if(transformFound)
                        {
                            currentNode = targetNode;
                            break;
                        }
                    }
                }
                
                minX = float.MaxValue;
                minY = float.MaxValue;
                minZ = float.MaxValue;

                maxX = float.MinValue;
                maxY = float.MinValue;
                maxZ = float.MinValue;

                //currentMeshName = mesh.Name;
                List<Vector3> uniqueVerticesForWholeMesh = new List<Vector3>();
                List<Vector3> uniqueNormalsForWholeMesh = new List<Vector3>();
                for(int m2 = 0; m2 < mesh.Primitives.Length; m2++)
                {
                    MeshPrimitive mprim = mesh.Primitives[m2];
                    GeoMesh geoMesh = new GeoMesh();

                    uint[] indices = GetIndicesForMeshPrimitive(scene, mprim, ref model);
                    geoMesh.Vertices = GetVertexDataForMeshPrimitive(scene, mprim, ref model, currentNode, out float xmin, out float xmax, out float ymin, out float ymax, out float zmin, out float zmax, out List<Vector3> uniqueVertices, out List<int> boneIds);
                    float[] normals = GetNormalDataForMeshPrimitive(scene, mprim, ref model, currentNode, out List<Vector3> uniqueNormals);
                    float[] tangents = GetTangentDataForMeshPrimitive(scene, mprim, ref model);
                    float[] uvs = GetUVDataForMeshPrimitive(scene, mprim, ref model, 0);
                    float[] uvs2 = GetUVDataForMeshPrimitive(scene, mprim, ref model, 1);

                    if (xmin < minX)
                        minX = xmin;
                    if (xmax > maxX)
                        maxX = xmax;
                    if (ymin < minY)
                        minY = ymin;
                    if (ymax > maxY)
                        maxY = ymax;
                    if (zmin < minZ)
                        minZ = zmin;
                    if (zmax > maxZ)
                        maxZ = zmax;

                   

                    geoMesh.Transform = nodeTransform;
                    geoMesh.Terrain = null;
                    
                    geoMesh.Name = mesh.Name + " #" + m2.ToString().PadLeft(4, '0') + " (Node: " + nodeName + ")";
                    currentNodeName = nodeName;
                    currentNodeTransform = nodeTransform;
                    geoMesh.NameOrg = mesh.Name;
                    geoMesh.Primitive = OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles;
                    geoMesh.IndexCount = indices.Length;

                    if (model.HasBones)
                    {
                        geoMesh.BoneTranslationMatrixCount = boneIds.Count;
                        ProcessMeshBones(scene, currentNode, ref model, ref geoMesh, boneIds);
                        
                    }

                    geoMesh.VAOGenerateAndBind();

                    geoMesh.VBOGenerateIndices(indices);
                    geoMesh.VBOGenerateVerticesAndBones(model.HasBones);
                    geoMesh.VBOGenerateNormals(normals);
                    geoMesh.VBOGenerateTangents(normals, tangents);
                    geoMesh.VBOGenerateTextureCoords1(uvs);
                    geoMesh.VBOGenerateTextureCoords2(uvs2);

                    

                    ProcessMaterialsForMesh(scene, mesh, mprim, ref model, ref geoMesh);

                    geoMesh.VAOUnbind();
                    model.Meshes.Add(geoMesh.Name, geoMesh);

                    if(currentNodeName.ToLower().Contains("_fullhitbox"))
                    {
                        foreach(Vector3 normal in uniqueNormals)
                        {
                            if(!uniqueNormalsForWholeMesh.Contains(normal))
                            {
                                uniqueNormalsForWholeMesh.Add(normal);
                            }
                        }
                        foreach (Vector3 vertex in uniqueVertices)
                        {
                            if (!uniqueVerticesForWholeMesh.Contains(vertex))
                            {
                                uniqueVerticesForWholeMesh.Add(vertex);
                            }
                        }
                    }
                }

                // Generate hitbox for the previous mesh:
                meshHitBox = new GeoMeshHitbox(maxX, maxY, maxZ, minX, minY, minZ, currentNodeName.ToLower().Contains("_fullhitbox") ? uniqueNormalsForWholeMesh : null, currentNodeName.ToLower().Contains("_fullhitbox") ? uniqueVerticesForWholeMesh : null);
                meshHitBox.Model = model;
                meshHitBox.Name = mesh.Name;
                meshHitBox.Transform = currentNodeTransform;
                meshHitBox.IsActive = !currentNodeName.ToLower().Contains("_nohitbox");
                model.MeshHitboxes.Add(meshHitBox);

            }
        }

        private static void ProcessMeshBones(Gltf scene, Node n, ref GeoModel model, ref GeoMesh geoMesh, List<int> uniqueBoneIds)
        {
            
                if(n.Mesh != null && n.Skin != null && scene.Meshes[(int)n.Mesh].Name == geoMesh.NameOrg)
                {
                    Skin s = scene.Skins[(int)n.Skin];
                    if (s.Joints != null)
                    {
                        foreach (int index in s.Joints)
                        {
                            geoMesh.BoneNames.Add(scene.Nodes[index].Name);
                            geoMesh.BoneIndices.Add(index);
                            geoMesh.BoneOffset.Add(FindInverseBindMatrixForBone(scene, scene.Nodes[index], ref model));
                        }
                    }
                }
            
        }

        private static Vector3[] GetTranslationOrScaleValues(Gltf scene, Accessor a, ref GeoModel model)
        {
            List<Vector3> validValues = new List<Vector3>();
            byte[] data = GetByteDataFromAccessor(scene, a, ref model);

            for(int i = 0; i < data.Length; i += 12)
            {
                float tx = BitConverter.ToSingle(data, i);
                float ty = BitConverter.ToSingle(data, i + 4);
                float tz = BitConverter.ToSingle(data, i + 8);
                Vector3 t = new Vector3(tx, ty, tz);
                validValues.Add(t);
            }

            return validValues.ToArray();
        }

        private static Quaternion[] GetRotationValues(Gltf scene, Accessor a, ref GeoModel model)
        {
            List<Quaternion> validQuaternions = new List<Quaternion>();
            //Quaternion[] values = new Quaternion[a.Count];
            byte[] data = GetByteDataFromAccessor(scene, a, ref model);

            for (int i = 0; i < data.Length; i += 16)
            {
                float rx = BitConverter.ToSingle(data, i);
                float ry = BitConverter.ToSingle(data, i + 4);
                float rz = BitConverter.ToSingle(data, i + 8);
                float rw = BitConverter.ToSingle(data, i + 12);
                Quaternion r = new Quaternion(rx, ry, rz, rw);
                if (r.LengthSquared > 0)
                    validQuaternions.Add(r);
            }

            return validQuaternions.ToArray();
        }

        private static float[] GetTimestampValues(Gltf scene, Accessor a, ref GeoModel model, out float duration)
        {
            float[] values = new float[a.Count];
            byte[] data = GetByteDataFromAccessor(scene, a, ref model);
            duration = BitConverter.ToSingle(data, data.Length - 4);
            for (int i = 0, counter = 0; i < data.Length; i += 4)
            {
                float ts = BitConverter.ToSingle(data, i);
                values[counter++] = ts;
            }
            return values;
        }

        private static void ProcessAnimations(Gltf scene, ref GeoModel model)
        {
            if(scene.Animations != null)
            {
                model.Animations = new List<GeoAnimation>();

                foreach(Animation a in scene.Animations)
                {
                    GeoAnimation ga = new GeoAnimation();
                    ga.Name = a.Name;
                    ga.AnimationChannels = new Dictionary<string, GeoNodeAnimationChannel>();
                    if (a.Channels != null)
                    {
                        List<GeoNodeAnimationChannel> channels = new List<GeoNodeAnimationChannel>();
                        List<GeoAnimationKeyframe> rotationKeys = new List<GeoAnimationKeyframe>();
                        List<GeoAnimationKeyframe> scaleKeys = new List<GeoAnimationKeyframe>();
                        List<GeoAnimationKeyframe> translationKeys = new List<GeoAnimationKeyframe>();

                        foreach (AnimationChannel nac in a.Channels)
                        {
                            string targetBoneName = scene.Nodes[(int)nac.Target.Node].Name;
                            GeoNodeAnimationChannel gnac = null;
                            // Translation:
                            if (nac.Target.Path == AnimationChannelTarget.PathEnum.translation)
                            {
                                AnimationSampler sampler = a.Samplers[nac.Sampler];
                                Accessor accInputTimestamps = scene.Accessors[sampler.Input];
                                Accessor accOutputValues = scene.Accessors[sampler.Output];
                                Vector3[] translationKeyValues = GetTranslationOrScaleValues(scene, accOutputValues, ref model);
                                float[] translationKeyTimestamps = GetTimestampValues(scene, accInputTimestamps, ref model, out float duration);
                                if(ga.DurationInTicks == 0)
                                    ga.DurationInTicks = duration;
                                List<GeoAnimationKeyframe> kframes = new List<GeoAnimationKeyframe>();

                                for (int i = 0; i < translationKeyTimestamps.Length; i++)
                                {
                                    GeoAnimationKeyframe keyframe = new GeoAnimationKeyframe();
                                    keyframe.Time = translationKeyTimestamps[i];
                                    keyframe.Translation = translationKeyValues[i];
                                    keyframe.Type = GeoKeyframeType.Translation;
                                    kframes.Add(keyframe);
                                }

                                for (int i = 0; i < channels.Count; i++)
                                {
                                    if(channels[i].NodeName == targetBoneName)
                                    {
                                        gnac = channels[i];
                                        break;
                                    }
                                }
                                if (gnac == null)
                                {
                                    gnac = new GeoNodeAnimationChannel();
                                    gnac.NodeName = targetBoneName;
                                    channels.Add(gnac);
                                }
                                gnac.TranslationKeys = kframes;
                            }

                            // Rotation:
                            if (nac.Target.Path == AnimationChannelTarget.PathEnum.rotation)
                            {
                                AnimationSampler sampler = a.Samplers[nac.Sampler];
                                Accessor accInputTimestamps = scene.Accessors[sampler.Input];
                                Accessor accOutputValues = scene.Accessors[sampler.Output];
                                Quaternion[] rotationKeyValues = GetRotationValues(scene, accOutputValues, ref model);
                                float[] rotationKeyTimestamps = GetTimestampValues(scene, accInputTimestamps, ref model, out float duration);
                                if (ga.DurationInTicks == 0)
                                    ga.DurationInTicks = duration;
                                List<GeoAnimationKeyframe> kframes = new List<GeoAnimationKeyframe>();

                                for (int i = 0; i < rotationKeyTimestamps.Length; i++)
                                {
                                    GeoAnimationKeyframe keyframe = new GeoAnimationKeyframe();
                                    keyframe.Time = rotationKeyTimestamps[i];
                                    keyframe.Rotation = rotationKeyValues[i];
                                    keyframe.Type = GeoKeyframeType.Rotation;
                                    kframes.Add(keyframe);
                                }

                                for (int i = 0; i < channels.Count; i++)
                                {
                                    if (channels[i].NodeName == targetBoneName)
                                    {
                                        gnac = channels[i];
                                        break;
                                    }
                                }
                                if (gnac == null)
                                {
                                    gnac = new GeoNodeAnimationChannel();
                                    gnac.NodeName = targetBoneName;
                                    channels.Add(gnac);
                                }
                                gnac.RotationKeys = kframes;
                            }

                            // Scale:
                            if (nac.Target.Path == AnimationChannelTarget.PathEnum.scale)
                            {
                                AnimationSampler sampler = a.Samplers[nac.Sampler];
                                Accessor accInputTimestamps = scene.Accessors[sampler.Input];
                                Accessor accOutputValues = scene.Accessors[sampler.Output];
                                Vector3[] scaleKeyValues = GetTranslationOrScaleValues(scene, accOutputValues, ref model);
                                float[] scaleKeyTimestamps = GetTimestampValues(scene, accInputTimestamps, ref model, out float duration);
                                if (ga.DurationInTicks == 0)
                                    ga.DurationInTicks = duration;
                                List<GeoAnimationKeyframe> kframes = new List<GeoAnimationKeyframe>();

                                for (int i = 0; i < scaleKeyTimestamps.Length; i++)
                                {
                                    GeoAnimationKeyframe keyframe = new GeoAnimationKeyframe();
                                    keyframe.Time = scaleKeyTimestamps[i];
                                    keyframe.Scale = scaleKeyValues[i];
                                    keyframe.Type = GeoKeyframeType.Scale;
                                    kframes.Add(keyframe);
                                }

                                for (int i = 0; i < channels.Count; i++)
                                {
                                    if (channels[i].NodeName == targetBoneName)
                                    {
                                        gnac = channels[i];
                                        break;
                                    }
                                }
                                if (gnac == null)
                                {
                                    gnac = new GeoNodeAnimationChannel();
                                    gnac.NodeName = targetBoneName;
                                    channels.Add(gnac);
                                }
                                gnac.ScaleKeys = kframes;
                            }
                        }
                        foreach(GeoNodeAnimationChannel channel in channels)
                        {
                            ga.AnimationChannels.Add(channel.NodeName, channel);
                        }
                        
                    }
                    model.Animations.Add(ga);
                }
            }
        }
    }
}