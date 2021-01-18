using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Assimp;
using Assimp.Configs;
using KWEngine2.Collision;
using KWEngine2.Helper;
using OpenTK;

namespace KWEngine2.Model
{
    internal static class SceneImporter
    {
        private enum FileType { DirectX, Filmbox, Wavefront, GLTF, Collada, Blender, Invalid }
        private static FileType CheckFileEnding(string filename)
        {
            string ending = filename.Trim().ToLower().Substring(filename.LastIndexOf('.') + 1);
            switch (ending)
            {
                case "x":
                    return FileType.DirectX;
                case "dae":
                    return FileType.Collada;
                case "glb":
                    return FileType.GLTF;
                case "gltf":
                    return FileType.GLTF;
                case "obj":
                    return FileType.Wavefront;
                case "fbx":
                    return FileType.Filmbox;
                //case "blend":
                //    return FileType.Blender;
                default:
                    return FileType.Invalid;
            }
        }

        internal enum AssemblyMode { File, Internal, User }

        internal static GeoModel LoadModel(string filename, bool flipTextureCoordinates = false, AssemblyMode am = AssemblyMode.Internal)
        {
            AssimpContext importer = new AssimpContext();
            importer.SetConfig(new VertexBoneWeightLimitConfig(KWEngine.MAX_BONE_WEIGHTS));
            importer.SetConfig(new MaxBoneCountConfig(KWEngine.MAX_BONES));
            FileType type = CheckFileEnding(filename);
            if(type == FileType.GLTF)
            {
                GeoModel model = SceneImporterGLTF.LoadModel(filename, flipTextureCoordinates);


                return model;
            }
            else
            {
                Scene scene = null;
                if (am != AssemblyMode.File)
                {
                    if (type == FileType.Invalid)
                    {
                        throw new Exception("Model file has invalid type.");
                    }
                    string resourceName;
                    Assembly assembly;

                    if (am == AssemblyMode.Internal)
                    {
                        assembly = Assembly.GetExecutingAssembly();
                        resourceName = "KWEngine2.Assets.Models." + filename;
                    }
                    else
                    {
                        assembly = Assembly.GetEntryAssembly();
                        resourceName = assembly.GetName().Name + "." + filename;
                    }

                    using (Stream s = assembly.GetManifestResourceStream(resourceName))
                    {
                        PostProcessSteps steps =
                              PostProcessSteps.LimitBoneWeights
                            | PostProcessSteps.Triangulate
                            | PostProcessSteps.ValidateDataStructure
                            | PostProcessSteps.GenerateUVCoords
                            | PostProcessSteps.CalculateTangentSpace;
                            
                        if (filename != "kwcube.obj" && filename != "kwcube6.obj")
                            steps |= PostProcessSteps.JoinIdenticalVertices;
                        if(filename == "kwsphere.obj")
                            steps |= PostProcessSteps.GenerateSmoothNormals;
                        if (type == FileType.DirectX)
                            steps |= PostProcessSteps.FlipWindingOrder;
                        if (flipTextureCoordinates)
                            steps |= PostProcessSteps.FlipUVs;
                        scene = importer.ImportFileFromStream(s, steps);
                    }
                }
                else
                {
                    if (type != FileType.Invalid)
                    {
                        PostProcessSteps steps =
                                  PostProcessSteps.LimitBoneWeights
                                | PostProcessSteps.Triangulate
                                //| PostProcessSteps.FixInFacingNormals
                                | PostProcessSteps.ValidateDataStructure
                                | PostProcessSteps.GenerateUVCoords
                                | PostProcessSteps.CalculateTangentSpace
                                | PostProcessSteps.JoinIdenticalVertices
                                ;
                        if (type == FileType.DirectX)
                            steps |= PostProcessSteps.FlipWindingOrder;
                        if (flipTextureCoordinates)
                            steps |= PostProcessSteps.FlipUVs;

                        scene = importer.ImportFile(filename, steps);
                    }
                    else
                    {
                        throw new Exception("Could not load model: only OBJ, DAE, FBX and X are supported (GLTF support coming soon).");
                    }
                }
                if (scene == null)
                    throw new Exception("Could not load or find model: " + filename);

                GeoModel model = ProcessScene(scene, am == AssemblyMode.File ? filename.ToLower().Trim() : filename, am);
                return model;
            }
        }

        private static GeoModel ProcessScene(Scene scene, string filename, AssemblyMode am)
        {
            GeoModel returnModel = new GeoModel();
            if (filename.Contains("kwcube6.obj"))
                returnModel.IsKWCube6 = true;
            returnModel.Filename = filename;
            returnModel.Name = StripPathFromFile(filename);
            if (am == AssemblyMode.Internal)
            {
                returnModel.PathAbsolute = "";
            }
            else
            {
                if (am == AssemblyMode.User)
                {
                    returnModel.PathAbsolute = Assembly.GetEntryAssembly().GetName().Name + "." + filename;
                }
                else
                {


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
                }
            }


            returnModel.AssemblyMode = am;
            returnModel.CalculatePath();
            returnModel.Meshes = new Dictionary<string, GeoMesh>();
            returnModel.TransformGlobalInverse = Matrix4.Invert(HelperMatrix.ConvertAssimpToOpenTKMatrix(scene.RootNode.Transform));
            returnModel.Textures = new Dictionary<string, GeoTexture>();
            returnModel.IsValid = false;

            GenerateNodeHierarchy(scene.RootNode, ref returnModel);
            ProcessBones(scene, ref returnModel);
            ProcessMeshes(scene, ref returnModel);
            ProcessAnimations(scene, ref returnModel);

            returnModel.IsValid = true;
            GLWindow.StartGarbageCollection();
            return returnModel;
        }

        private static void GenerateNodeHierarchy(Node node, ref GeoModel model)
        {
            GeoNode root = new GeoNode();
            root.Name = node.Name;
            root.Transform = HelperMatrix.ConvertAssimpToOpenTKMatrix(node.Transform);
            root.Parent = null;
            model.Root = root;
            model.NodesWithoutHierarchy.Add(root);
            foreach (Node child in node.Children)
            {
                root.Children.Add(MapNodeToNode(child, ref model, ref root));
            }
        }

        private static GeoNode MapNodeToNode(Node n, ref GeoModel model, ref GeoNode callingNode)
        {
            GeoNode gNode = new GeoNode();
            gNode.Parent = callingNode;
            gNode.Transform = HelperMatrix.ConvertAssimpToOpenTKMatrix(n.Transform);
            gNode.Name = n.Name;
            model.NodesWithoutHierarchy.Add(gNode);
            foreach (Node child in n.Children)
            {
                gNode.Children.Add(MapNodeToNode(child, ref model, ref gNode));
            }

            return gNode;
        }

        private static Node GetNodeForBone(Node node, Bone b)
        {
            foreach(Node n in node.Children)
            {
                if(n.Name == b.Name)
                {
                    return n;
                }
                else
                {
                    Node nodeCandidate = GetNodeForBone(n, b);
                    if (nodeCandidate != null)
                        return nodeCandidate;
                }
            }
            return null;
        }

        private static void FindRootBone(Scene scene, ref GeoModel model, string boneName)
        {
            int c = 0;
            while(c < scene.Meshes.Count)
            {
                Mesh m = scene.Meshes[c];
                if(m.BoneCount > 0)
                {
                    Node boneNode = GetNodeForBone(scene.RootNode, m.Bones[0]);
                    if(boneNode != null)
                    {
                        while(boneNode.Parent != scene.RootNode)
                        {
                            boneNode = boneNode.Parent;
                        }
                        foreach(GeoNode n in model.NodesWithoutHierarchy)
                        {
                            if(n.Name == boneNode.Name)
                            {
                                model.Armature = n;
                                return;
                            }
                        }

                    }
                }
                c++;
            }
        }

        private static Node ScanForParent(Scene scene, Node node)
        {
            if (node.Parent != null && node.Parent.Parent == null)
            {
                return node.Parent;
            }
            else
            {
                return ScanForParent(scene, node.Parent);
            }
        }

        private static void ProcessBones(Scene scene, ref GeoModel model)
        {
            foreach (Mesh mesh in scene.Meshes)
            {
                int boneIndexLocal = 0;
                foreach (Bone bone in mesh.Bones)
                {
                    model.HasBones = true;
                    if (model.Armature == null)
                        FindRootBone(scene, ref model, bone.Name);

                    if (!model.BoneNames.Contains(bone.Name))
                        model.BoneNames.Add(bone.Name);

                    GeoBone geoBone = new GeoBone();
                    geoBone.Name = bone.Name;
                    geoBone.Index = boneIndexLocal;
                    geoBone.Offset = HelperMatrix.ConvertAssimpToOpenTKMatrix(bone.OffsetMatrix);
                    boneIndexLocal++;

                    
                }
            }
            if(model.BoneNames.Count > KWEngine.MAX_BONES)
            {
                throw new Exception("Model has more than " + KWEngine.MAX_BONES + " bones. Cannot import model.");
            }
        }

        private static bool FindTransformForMesh(Scene scene, Node currentNode, Mesh mesh, ref Matrix4 transform, out string nodeName, ref Matrix4 parentTransform)
        {
            Matrix4 currentNodeTransform = parentTransform * HelperMatrix.ConvertAssimpToOpenTKMatrix(currentNode.Transform);
            for (int i = 0; i < currentNode.MeshIndices.Count; i++)
            {
                Mesh tmpMesh = scene.Meshes[currentNode.MeshIndices[i]];
                if (tmpMesh.Name == mesh.Name)
                {
                    transform = currentNodeTransform;
                    nodeName = currentNode.Name;
                    return true;
                }
            }

            for (int i = 0; i < currentNode.ChildCount; i++)
            {
                Node child = currentNode.Children[i];
                bool found = FindTransformForMesh(scene, child, mesh, ref transform, out string nName, ref currentNodeTransform);
                if (found)
                {
                    nodeName = nName;
                    return true;
                }
            }

            transform = Matrix4.Identity;
            nodeName = null;
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

        private static void ProcessMaterialsForMesh(Scene scene, Mesh mesh, ref GeoModel model, ref GeoMesh geoMesh, bool isKWCube = false)
        {
            GeoMaterial geoMaterial = new GeoMaterial();
            Material material = null;
            if (isKWCube)
            {
                if (mesh.MaterialIndex >= 0)
                {
                    material = scene.Materials[mesh.MaterialIndex];
                    geoMaterial.Name = model.Filename == "kwcube.obj" ? "KWCube" : material.Name;
                    geoMaterial.BlendMode = material.BlendMode == BlendMode.Default ? OpenTK.Graphics.OpenGL4.BlendingFactor.OneMinusSrcAlpha : OpenTK.Graphics.OpenGL4.BlendingFactor.One; // TODO: Check if this is correct!
                    geoMaterial.ColorAlbedo = new Vector4(1, 1, 1, 1);
                    geoMaterial.ColorEmissive = new Vector4(0, 0, 0, 1);

                }
                else
                {
                    geoMaterial.Name = "kw-undefined.";
                    geoMaterial.BlendMode = OpenTK.Graphics.OpenGL4.BlendingFactor.OneMinusSrcAlpha;
                    geoMaterial.ColorAlbedo = new Vector4(1, 1, 1, 1);
                    geoMaterial.ColorEmissive = new Vector4(0, 0, 0, 1);
                }
                geoMaterial.Roughness = 1;
                geoMaterial.Metalness = 0;
            }
            else
            {
                

                if (mesh.MaterialIndex >= 0)
                {
                    material = scene.Materials[mesh.MaterialIndex];
                    geoMaterial.Name = material.Name;

                    if (material.Name == "DefaultMaterial")
                    {
                        geoMaterial.BlendMode = OpenTK.Graphics.OpenGL4.BlendingFactor.OneMinusSrcAlpha;
                        geoMaterial.ColorAlbedo = new Vector4(1, 1, 1, 1);
                        geoMaterial.ColorEmissive = new Vector4(0, 0, 0, 1);
                        geoMaterial.Roughness = 1;
                        geoMaterial.Metalness = 0;
                        geoMaterial.TextureRoughnessIsSpecular = false;
                        if (mesh.Name != null && mesh.Name.ToLower().Contains("_invisible"))
                        {
                            geoMaterial.Opacity = 0;
                        }
                    }
                    else
                    {
                        geoMaterial.Roughness = material.HasShininess ? (100f - material.Shininess) / 100f : 1;
                        geoMaterial.Metalness = material.HasReflectivity ? material.Reflectivity : 0;
                        geoMaterial.BlendMode = material.BlendMode == BlendMode.Default ? OpenTK.Graphics.OpenGL4.BlendingFactor.OneMinusSrcAlpha : OpenTK.Graphics.OpenGL4.BlendingFactor.One; // TODO: Check if this is correct!
                        if (model.AssemblyMode == AssemblyMode.Internal && material.Name == "System")
                        {
                            geoMaterial.ColorAlbedo = new Vector4(1, 1, 1, 1);
                        }
                        else if (model.AssemblyMode == AssemblyMode.Internal && material.Name == "X")
                        {
                            geoMaterial.ColorAlbedo = new Vector4(1, 0, 0, 1);
                        }
                        else if (model.AssemblyMode == AssemblyMode.Internal && material.Name == "Y")
                        {
                            geoMaterial.ColorAlbedo = new Vector4(0, 1, 0, 1);
                        }
                        else if (model.AssemblyMode == AssemblyMode.Internal && material.Name == "Z")
                        {
                            geoMaterial.ColorAlbedo = new Vector4(0, 0, 1, 1);
                        }
                        else
                        {
                            geoMaterial.ColorAlbedo = material.HasColorDiffuse ? new Vector4(material.ColorDiffuse.R, material.ColorDiffuse.G, material.ColorDiffuse.B, material.ColorDiffuse.A) : new Vector4(1, 1, 1, 1);
                        }
                        geoMaterial.ColorEmissive = material.HasColorEmissive ? new Vector4(material.ColorEmissive.R, material.ColorEmissive.G, material.ColorEmissive.B, material.ColorEmissive.A) : new Vector4(0, 0, 0, 1);
                        geoMaterial.TextureRoughnessIsSpecular = false;
                        geoMaterial.Opacity = material.HasOpacity ? material.Opacity : 1;
                        if(mesh.Name != null && mesh.Name.ToLower().Contains("_invisible"))
                        {
                            geoMaterial.Opacity = 0;
                        }
                    }

                    
                }
                else
                {
                    geoMaterial.Name = "kw-undefined.";
                    geoMaterial.Metalness = 0;
                    geoMaterial.Roughness = 1;
                    geoMaterial.BlendMode = OpenTK.Graphics.OpenGL4.BlendingFactor.OneMinusSrcAlpha;
                    geoMaterial.ColorAlbedo = new Vector4(1, 1, 1, 1);
                    geoMaterial.ColorEmissive = new Vector4(0, 0, 0, 1);
                    geoMaterial.TextureRoughnessIsSpecular = false;
                    if (mesh.Name != null && mesh.Name.ToLower().Contains("_invisible"))
                    {
                        geoMaterial.Opacity = 0;
                    }
                }
            }

            // Process Textures:
            if (material != null)
            {

                // TODO: Metalness texture missing with assimp

                bool specularUsed = false;
                int roughnessTextureIndex = -1;
                TextureSlot[] texturesOfMaterial = material.GetAllMaterialTextures();
                for(int i = 0; i < texturesOfMaterial.Length; i++)
                {
                    TextureSlot slot = texturesOfMaterial[i];
                    if (roughnessTextureIndex < 0 && slot.TextureType == Assimp.TextureType.Shininess)
                    {
                        roughnessTextureIndex = i;
                    }


                    if(slot.TextureType == Assimp.TextureType.Specular)
                    {
                        GeoTexture tex = new GeoTexture();
                        tex.UVTransform = new OpenTK.Vector2(1, 1);
                        tex.Filename = slot.FilePath;
                        tex.UVMapIndex = slot.UVIndex;
                        if (model.Textures.ContainsKey(tex.Filename))
                        {
                            tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                        }
                        else
                        {
                            if (model.AssemblyMode == AssemblyMode.File)
                            {
                                tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                        FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                                    );
                            }
                            else
                            {
                                string path = StripFileNameFromAssemblyPath(model.PathAbsolute).Substring(model.PathAbsolute.IndexOf('.') + 1) + StripPathFromFile(tex.Filename);
                                tex.OpenGLID = HelperTexture.LoadTextureForModelInternal(path, true);
                            }
                            if (tex.OpenGLID > 0)
                            {
                                tex.Type = TextureType.Roughness;
                                model.Textures.Add(tex.Filename, tex);
                                geoMaterial.TextureRoughness = tex;
                                geoMaterial.TextureRoughnessIsSpecular = true;
                                specularUsed = true;
                            }
                            else
                            {
                                geoMaterial.TextureRoughness = tex;
                                geoMaterial.TextureRoughnessIsSpecular = false;
                                tex.OpenGLID = KWEngine.TextureBlack;
                            }
                        }
                    }
                }

                
                // Diffuse texture
                if (material.HasTextureDiffuse)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = material.TextureDiffuse.FilePath;
                    tex.UVMapIndex = material.TextureDiffuse.UVIndex;
                    tex.Type = TextureType.Albedo;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                        geoMaterial.TextureAlbedo = tex;
                    }
                    else if (CheckIfOtherModelsShareTexture(tex.Filename, model.Path, out GeoTexture sharedTexture))
                    {
                        geoMaterial.TextureAlbedo = sharedTexture;
                    }
                    else
                    {
                        if (model.AssemblyMode == AssemblyMode.File)
                        {
                            tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                    FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                                );
                        }
                        else
                        {
                            string path = StripFileNameFromAssemblyPath(model.PathAbsolute).Substring(model.PathAbsolute.IndexOf('.') + 1) + StripPathFromFile(tex.Filename);
                            tex.OpenGLID = HelperTexture.LoadTextureForModelInternal(path, true);
                        }
                        if (tex.OpenGLID > 0)
                        {
                            geoMaterial.TextureAlbedo = tex;
                            model.Textures.Add(tex.Filename, tex);
                        }
                        else
                        {
                            tex.OpenGLID = KWEngine.TextureDefault;
                            tex.UVTransform = new Vector2(100, 100);
                            geoMaterial.TextureAlbedo = tex;
                        }
                    }
                }

                // Normal map texture
                if (material.HasTextureNormal)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = material.TextureNormal.FilePath;
                    tex.UVMapIndex = material.TextureNormal.UVIndex;
                    tex.Type = TextureType.Normal;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                        geoMaterial.TextureNormal = tex;
                    }
                    else if (CheckIfOtherModelsShareTexture(tex.Filename, model.Path, out GeoTexture sharedTexture))
                    {
                        geoMaterial.TextureNormal = sharedTexture;
                    }
                    else
                    {
                        if (model.AssemblyMode == AssemblyMode.File)
                        {
                            tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                    FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                                );
                        }
                        else
                        {
                            string path = StripFileNameFromAssemblyPath(model.PathAbsolute).Substring(model.PathAbsolute.IndexOf('.') + 1) + StripPathFromFile(tex.Filename);
                            tex.OpenGLID = HelperTexture.LoadTextureForModelInternal(path, true);
                        }
                        if (tex.OpenGLID > 0)
                        {
                            model.Textures.Add(tex.Filename, tex);
                            geoMaterial.TextureNormal = tex;
                        }
                    }
                }

                // Roughness map texture
                if (roughnessTextureIndex >= 0 && specularUsed == false)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = texturesOfMaterial[roughnessTextureIndex].FilePath;
                    tex.UVMapIndex = texturesOfMaterial[roughnessTextureIndex].UVIndex;
                    tex.Type = TextureType.Roughness;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                        geoMaterial.TextureRoughness = tex;
                    }
                    else if (CheckIfOtherModelsShareTexture(tex.Filename, model.Path, out GeoTexture sharedTexture))
                    {
                        geoMaterial.TextureRoughness = sharedTexture;
                    }
                    else
                    {
                        if (model.AssemblyMode == AssemblyMode.File)
                        {
                            tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                    FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                                );
                        }
                        else
                        {
                            string path = StripFileNameFromAssemblyPath(model.PathAbsolute).Substring(model.PathAbsolute.IndexOf('.') + 1) + StripPathFromFile(tex.Filename);
                            tex.OpenGLID = HelperTexture.LoadTextureForModelInternal(path, true);
                        }
                        if (tex.OpenGLID > 0)
                        {
                            geoMaterial.TextureRoughness = tex;
                            model.Textures.Add(tex.Filename, tex);
                        }
                    }
                }
                else
                {
                    if(specularUsed)
                    {
                        Debug.WriteLine("Skipping roughness texture for " + model.Filename + " because old style specular texture was found.");
                    }
                }

                // Emissive map texture
                if (material.HasTextureEmissive)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = material.TextureEmissive.FilePath;
                    tex.UVMapIndex = material.TextureEmissive.UVIndex;
                    tex.Type = TextureType.Emissive;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                        geoMaterial.TextureEmissive = tex;
                    }
                    else if (CheckIfOtherModelsShareTexture(tex.Filename, model.Path, out GeoTexture sharedTexture))
                    {
                        geoMaterial.TextureEmissive = sharedTexture;
                    }
                    else
                    {
                        if (model.AssemblyMode == AssemblyMode.File)
                        {
                            tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                    FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                                );
                        }
                        else
                        {
                            string path = StripFileNameFromAssemblyPath(model.PathAbsolute).Substring(model.PathAbsolute.IndexOf('.') + 1) + StripPathFromFile(tex.Filename);
                            tex.OpenGLID = HelperTexture.LoadTextureForModelInternal(path, true);
                        }
                        if (tex.OpenGLID > 0)
                        {
                            geoMaterial.TextureEmissive = tex;

                            model.Textures.Add(tex.Filename, tex);

                        }
                        else
                        {
                            tex.OpenGLID = KWEngine.TextureBlack;
                            geoMaterial.TextureEmissive = tex;
                        }
                    }
                    
                }

                // Light map texture
                if (material.HasTextureLightMap)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = material.TextureLightMap.FilePath;
                    tex.UVMapIndex = material.TextureLightMap.UVIndex;
                    tex.Type = TextureType.Light;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                        geoMaterial.TextureLight = tex;
                    }
                    else if (CheckIfOtherModelsShareTexture(tex.Filename, model.Path, out GeoTexture sharedTexture))
                    {
                        geoMaterial.TextureLight = sharedTexture;
                    }
                    else
                    {
                        if (model.AssemblyMode == AssemblyMode.File)
                        {
                            tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                    FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                                );
                        }
                        else
                        {
                            string path = StripFileNameFromAssemblyPath(model.PathAbsolute).Substring(model.PathAbsolute.IndexOf('.') + 1) + StripPathFromFile(tex.Filename);
                            tex.OpenGLID = HelperTexture.LoadTextureForModelInternal(path, true);
                        }
                        if (tex.OpenGLID > 0)
                        {
                            model.Textures.Add(tex.Filename, tex);
                            geoMaterial.TextureLight = tex;
                        }
                    }                    
                }

            }

            geoMesh.Material = geoMaterial;
        }

        private static bool CheckIfOtherModelsShareTexture(string texture, string path, out GeoTexture sharedTex)
        {
            sharedTex = new GeoTexture();
            foreach(string key in KWEngine.Models.Keys)
            {
                GeoModel m = KWEngine.Models[key];
                if(m.Path == path)
                {
                    foreach (string texKey in m.Textures.Keys)
                    {
                        if(texKey == texture)
                        {

                            sharedTex = m.Textures[texKey];
                            return true;
                        }
                    }
                }
                
            }

            return false;
        }

        private static void ProcessMeshes(Scene scene, ref GeoModel model)
        {
            model.MeshHitboxes = new List<GeoMeshHitbox>();

            string currentMeshName = null;
            string currentNodeName = null;
            Matrix4 currentNodeTransform = Matrix4.Identity;
            Mesh currentMesh = null;
            GeoMeshHitbox meshHitBox = null;
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            Matrix4 nodeTransform = Matrix4.Identity;
            Mesh mesh = null;
            for (int m = 0; m < scene.MeshCount; m++)
            {
                mesh = scene.Meshes[m];

                Matrix4 parentTransform = Matrix4.Identity;
                bool transformFound = FindTransformForMesh(scene, scene.RootNode, mesh, ref nodeTransform, out string nodeName, ref parentTransform);

                bool isNewMesh = currentMeshName != null && mesh.Name != currentMeshName && currentNodeName != null && currentNodeName != nodeName && model.Filename != "kwcube6.obj";

                if (mesh.PrimitiveType != PrimitiveType.Triangle)
                {
                    throw new Exception("Model's primitive type is not set to 'triangles'. Cannot import model.");
                }
                
                if (isNewMesh)
                {
                    if (currentMeshName != null)
                    {
                        // Generate hitbox for the previous mesh:
                        meshHitBox = new GeoMeshHitbox(maxX, maxY, maxZ, minX, minY, minZ, currentNodeName.ToLower().Contains("_fullhitbox") ? currentMesh : null);
                        meshHitBox.Model = model;
                        meshHitBox.Name = currentMeshName;
                        meshHitBox.Transform = currentNodeTransform;
                        meshHitBox.IsActive = !currentNodeName.ToLower().Contains("_nohitbox");
                        model.MeshHitboxes.Add(meshHitBox);

                    }
                    minX = float.MaxValue;
                    minY = float.MaxValue;
                    minZ = float.MaxValue;

                    maxX = float.MinValue;
                    maxY = float.MinValue;
                    maxZ = float.MinValue;
                }

                currentMeshName = mesh.Name;
                
                GeoMesh geoMesh = new GeoMesh();
                //Matrix4 parentTransform = Matrix4.Identity;
                //bool transformFound = FindTransformForMesh(scene, scene.RootNode, mesh, ref nodeTransform, out string nodeName, ref parentTransform);
                geoMesh.Transform = nodeTransform;
                geoMesh.Terrain = null;
                geoMesh.BoneTranslationMatrixCount = mesh.BoneCount;
                geoMesh.Name = mesh.Name + " #" + m.ToString().PadLeft(4, '0') + " (Node: " + nodeName + ")";
                currentNodeName = nodeName;
                currentMesh = mesh;
                currentNodeTransform = nodeTransform;
                geoMesh.NameOrg = mesh.Name;
                geoMesh.Vertices = new GeoVertex[mesh.VertexCount];
                geoMesh.Primitive = OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles;
                geoMesh.VAOGenerateAndBind();

                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    Vector3D vertex = mesh.Vertices[i];
                    if (vertex.X > maxX)
                        maxX = vertex.X;
                    if (vertex.Y > maxY)
                        maxY = vertex.Y;
                    if (vertex.Z > maxZ)
                        maxZ = vertex.Z;
                    if (vertex.X < minX)
                        minX = vertex.X;
                    if (vertex.Y < minY)
                        minY = vertex.Y;
                    if (vertex.Z < minZ)
                        minZ = vertex.Z;

                    GeoVertex geoVertex = new GeoVertex(i, vertex.X, vertex.Y, vertex.Z);
                    geoMesh.Vertices[i] = geoVertex;
                }
                uint[] indices = mesh.GetUnsignedIndices();
                geoMesh.IndexCount = indices.Length;

                if (model.HasBones)
                {
                    for (int i = 0; i < mesh.BoneCount; i++)
                    {
                        Bone bone = mesh.Bones[i];

                        geoMesh.BoneNames.Add(bone.Name);
                        geoMesh.BoneIndices.Add(i);
                        geoMesh.BoneOffset.Add(HelperMatrix.ConvertAssimpToOpenTKMatrix(bone.OffsetMatrix));

                        foreach (VertexWeight vw in bone.VertexWeights)
                        {

                            int weightIndexToBeSet = geoMesh.Vertices[vw.VertexID].WeightSet;
                            if (weightIndexToBeSet >= KWEngine.MAX_BONE_WEIGHTS)
                            {
                                throw new Exception("Model's bones have more than three weights per vertex. Cannot import model.");
                            }

                            //Debug.WriteLine("Setting Vertex " + vw.VertexID + " with BoneID " + i + " and Weight: " + vw.Weight + " to Slot #" + weightIndexToBeSet);
                            geoMesh.Vertices[vw.VertexID].Weights[weightIndexToBeSet] = vw.Weight;
                            geoMesh.Vertices[vw.VertexID].BoneIDs[weightIndexToBeSet] = (uint)i;
                            geoMesh.Vertices[vw.VertexID].WeightSet++;
                        }
                    }
                }

                geoMesh.VBOGenerateIndices(indices);
                geoMesh.VBOGenerateVerticesAndBones(model.HasBones);
                geoMesh.VBOGenerateNormals(mesh);
                geoMesh.VBOGenerateTangents(mesh);
                if(model.Filename == "kwcube.obj")
                    geoMesh.VBOGenerateTextureCoords1(mesh, scene, 1);
                else if(model.Filename == "kwcube6.obj")
                    geoMesh.VBOGenerateTextureCoords1(mesh, scene, 6);
                else if (model.Filename == "kwsphere.obj")
                    geoMesh.VBOGenerateTextureCoords1(mesh, scene, 2);
                else
                    geoMesh.VBOGenerateTextureCoords1(mesh, scene);
                geoMesh.VBOGenerateTextureCoords2(mesh);

                ProcessMaterialsForMesh(scene, mesh, ref model, ref geoMesh, model.Filename == "kwcube.obj" || model.Filename == "kwcube6.obj");

                geoMesh.VAOUnbind();
                model.Meshes.Add(geoMesh.Name, geoMesh);
            }

            // Generate hitbox for the last mesh:
            if (currentMeshName != null)
            {
                meshHitBox = new GeoMeshHitbox(maxX, maxY, maxZ, minX, minY, minZ, currentNodeName.ToLower().Contains("_fullhitbox") ? mesh : null);
                meshHitBox.Model = model;
                meshHitBox.Name = model.Filename == "kwcube6.obj" ? "KWCube6" : currentMeshName;
                meshHitBox.Transform = nodeTransform;
                meshHitBox.IsActive = !currentNodeName.ToLower().Contains("_nohitbox");
                model.MeshHitboxes.Add(meshHitBox);
            }
        }

        private static void ProcessAnimations(Scene scene, ref GeoModel model)
        {

            if (scene.HasAnimations)
            {
                model.Animations = new List<GeoAnimation>();
                foreach (Animation a in scene.Animations)
                {
                    GeoAnimation ga = new GeoAnimation();
                    ga.DurationInTicks = (float)a.DurationInTicks;
                    ga.TicksPerSecond = (float)a.TicksPerSecond;
                    ga.Name = a.Name;
                    ga.AnimationChannels = new Dictionary<string, GeoNodeAnimationChannel>();
                    foreach (NodeAnimationChannel nac in a.NodeAnimationChannels)
                    {
                        GeoNodeAnimationChannel ganc = new GeoNodeAnimationChannel();

                        // Rotation:
                        ganc.RotationKeys = new List<GeoAnimationKeyframe>();
                        foreach (QuaternionKey key in nac.RotationKeys)
                        {
                            GeoAnimationKeyframe akf = new GeoAnimationKeyframe();
                            akf.Time = (float)key.Time;
                            akf.Rotation = new OpenTK.Quaternion(key.Value.X, key.Value.Y, key.Value.Z, key.Value.W);
                            akf.Translation = new Vector3(0, 0, 0);
                            akf.Scale = new Vector3(1, 1, 1);
                            akf.Type = GeoKeyframeType.Rotation;
                            ganc.RotationKeys.Add(akf);
                        }

                        // Scale:
                        ganc.ScaleKeys = new List<GeoAnimationKeyframe>();
                        foreach (VectorKey key in nac.ScalingKeys)
                        {
                            GeoAnimationKeyframe akf = new GeoAnimationKeyframe();
                            akf.Time = (float)key.Time;
                            akf.Rotation = new OpenTK.Quaternion(0, 0, 0, 1);
                            akf.Translation = new Vector3(0, 0, 0);
                            akf.Scale = new Vector3(key.Value.X, key.Value.Y, key.Value.Z);
                            akf.Type = GeoKeyframeType.Scale;
                            ganc.ScaleKeys.Add(akf);
                        }

                        // Translation:
                        ganc.TranslationKeys = new List<GeoAnimationKeyframe>();
                        foreach (VectorKey key in nac.PositionKeys)
                        {
                            GeoAnimationKeyframe akf = new GeoAnimationKeyframe();
                            akf.Time = (float)key.Time;
                            akf.Rotation = new OpenTK.Quaternion(0, 0, 0, 1);
                            akf.Translation = new Vector3(key.Value.X, key.Value.Y, key.Value.Z);
                            akf.Scale = new Vector3(1, 1, 1);
                            akf.Type = GeoKeyframeType.Translation;
                            ganc.TranslationKeys.Add(akf);
                        }

                        //if(model.BoneNames.Contains(nac.NodeName))
                        ga.AnimationChannels.Add(nac.NodeName, ganc);
                    }
                    model.Animations.Add(ga);
                }
            }
        }
    }
}