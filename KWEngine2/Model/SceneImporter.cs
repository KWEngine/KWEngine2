﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Assimp;
using Assimp.Configs;
using KWEngine2.Collision;
using KWEngine2.Engine;
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
                case "blend":
                    return FileType.Blender;
                default:
                    return FileType.Invalid;
            }
        }

        internal static GeoModel LoadModel(string filename, bool isInAssembly = false)
        {
            AssimpContext importer = new AssimpContext();
            importer.SetConfig(new VertexBoneWeightLimitConfig(KWEngine.MAX_BONE_WEIGHTS));
            importer.SetConfig(new MaxBoneCountConfig(KWEngine.MAX_BONES));

            Scene scene = null;
            if (isInAssembly)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "KWEngine2.Assets.Models." + filename;

                using (Stream s = assembly.GetManifestResourceStream(resourceName))
                {
                    PostProcessSteps steps =
                          PostProcessSteps.LimitBoneWeights
                        | PostProcessSteps.Triangulate
                        | PostProcessSteps.ValidateDataStructure
                        | PostProcessSteps.GenerateUVCoords
                        | PostProcessSteps.CalculateTangentSpace
                        | PostProcessSteps.JoinIdenticalVertices;
                    scene = importer.ImportFileFromStream(s, steps);
                }
            }
            else
            {
                FileType type = CheckFileEnding(filename);
                if (type != FileType.Invalid)
                {
                    PostProcessSteps steps =
                              PostProcessSteps.LimitBoneWeights
                            | PostProcessSteps.Triangulate
                            | PostProcessSteps.FixInFacingNormals
                            | PostProcessSteps.ValidateDataStructure
                            | PostProcessSteps.GenerateUVCoords
                            | PostProcessSteps.CalculateTangentSpace
                            | PostProcessSteps.JoinIdenticalVertices;

                    if (type == FileType.DirectX)
                        steps |= PostProcessSteps.FlipWindingOrder;

                    scene = importer.ImportFile(filename, steps);
                }
            }
            GeoModel model = ProcessScene(scene, filename.ToLower().Trim(), isInAssembly);
            return model;
        }

        private static GeoModel ProcessScene(Scene scene, string filename, bool isInAssembly)
        {
            GeoModel returnModel = new GeoModel();
            returnModel.Filename = filename;
            returnModel.Name = StripPathFromFile(filename);
            if (isInAssembly)
            {
                returnModel.PathAbsolute = "";
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


            returnModel.IsInAssembly = isInAssembly;
            returnModel.CalculatePath();
            //returnModel.Bones = new Dictionary<int, GeoBone>();
            returnModel.Meshes = new Dictionary<string, GeoMesh>();
            returnModel.TransformGlobalInverse = Matrix4.Invert(HelperMatrix.ConvertAssimpToOpenTKMatrix(scene.RootNode.Transform));
            returnModel.Textures = new Dictionary<string, GeoTexture>();
            returnModel.IsValid = false;

            GenerateNodeHierarchy(scene.RootNode, ref returnModel);
            ProcessBones(scene, ref returnModel);
            ProcessMeshes(scene, ref returnModel);
            ProcessAnimations(scene, ref returnModel);

            returnModel.IsValid = true;
            GC.Collect(GC.MaxGeneration);
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

        private static void FindRootBone(Scene scene, ref GeoModel model, string boneName)
        {
            foreach (Node child in scene.RootNode.Children)
            {
                if (child.Name == boneName) // found the anchor
                {
                    Node armature = ScanForParent(scene, child);
                    if (armature != null)
                    {
                        foreach (GeoNode n in model.NodesWithoutHierarchy)
                        {
                            if (armature.Name == n.Name)
                                model.Armature = n;
                            return;
                        }
                    }
                }
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
        }

        private static bool FindTransformForMesh(Scene scene, Node currentNode, Mesh mesh, out Matrix4 transform, out string nodeName)
        {
            for (int i = 0; i < currentNode.MeshIndices.Count; i++)
            {
                Mesh tmpMesh = scene.Meshes[currentNode.MeshIndices[i]];
                if (tmpMesh.Name == mesh.Name)
                {
                    transform = HelperMatrix.ConvertAssimpToOpenTKMatrix(currentNode.Transform);
                    nodeName = currentNode.Name;
                    return true;
                }
            }

            for (int i = 0; i < currentNode.ChildCount; i++)
            {
                Node child = currentNode.Children[i];
                bool found = FindTransformForMesh(scene, child, mesh, out Matrix4 t, out string nName);
                if (found)
                {
                    transform = t;
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
                if (fi.Name.ToLower() == StripPathFromFile(filename))
                {
                    // file found:
                    return fi.FullName;
                }
            }

            if (currentDir.GetDirectories().Length == 0)
            {
                throw new Exception("File " + filename + " not found anywhere. Aborting import.");
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

        private static void ProcessMaterialsForMesh(Scene scene, Mesh mesh, ref GeoModel model, ref GeoMesh geoMesh)
        {
            GeoMaterial geoMaterial = new GeoMaterial();
            Material material = null;
            if (mesh.MaterialIndex >= 0)
            {
                material = scene.Materials[mesh.MaterialIndex];
                geoMaterial.Name = material.Name;
                geoMaterial.BlendMode = material.BlendMode == BlendMode.Default ? OpenTK.Graphics.OpenGL4.BlendingFactor.OneMinusSrcAlpha : OpenTK.Graphics.OpenGL4.BlendingFactor.One; // TODO: Check if this is correct!
                geoMaterial.ColorDiffuse = material.HasColorDiffuse ? new Vector4(material.ColorDiffuse.R, material.ColorDiffuse.G, material.ColorDiffuse.B, material.ColorDiffuse.A) : new Vector4(1, 1, 1, 1);
                geoMaterial.ColorEmissive = material.HasColorEmissive ? new Vector4(material.ColorEmissive.R, material.ColorEmissive.G, material.ColorEmissive.B, material.ColorEmissive.A) : new Vector4(0, 0, 0, 1);
            }
            else
            {
                geoMaterial.Name = "kw-undefined.";
                geoMaterial.BlendMode = OpenTK.Graphics.OpenGL4.BlendingFactor.OneMinusSrcAlpha;
                geoMaterial.ColorDiffuse = new Vector4(1, 1, 1, 1);
                geoMaterial.ColorEmissive = new Vector4(0, 0, 0, 1);
            }

            // Process Textures:
            if (material != null)
            {
                // Diffuse texture
                if (material.HasTextureDiffuse)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = material.TextureDiffuse.FilePath;
                    tex.UVMapIndex = material.TextureDiffuse.UVIndex;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                    }
                    else
                    {
                        tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                            );
                        model.Textures.Add(tex.Filename, tex);
                    }
                    tex.Type = GeoTexture.TexType.Diffuse;
                    geoMaterial.TextureDiffuse = tex;
                }

                // Normal map texture
                if (material.HasTextureNormal)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = material.TextureNormal.FilePath;
                    tex.UVMapIndex = material.TextureNormal.UVIndex;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                    }
                    else
                    {
                        tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                            );
                        model.Textures.Add(tex.Filename, tex);
                    }
                    tex.Type = GeoTexture.TexType.Normal;
                }

                // Specular map texture
                if (material.HasTextureSpecular)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = material.TextureSpecular.FilePath;
                    tex.UVMapIndex = material.TextureSpecular.UVIndex;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                    }
                    else
                    {
                        tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                            );
                        model.Textures.Add(tex.Filename, tex);
                    }
                    tex.Type = GeoTexture.TexType.Specular;
                }

                // Emissive map texture
                if (material.HasTextureEmissive)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = material.TextureEmissive.FilePath;
                    tex.UVMapIndex = material.TextureEmissive.UVIndex;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                    }
                    else
                    {
                        tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                            );
                        model.Textures.Add(tex.Filename, tex);
                    }
                    tex.Type = GeoTexture.TexType.Emissive;
                }

                // Light map texture
                if (material.HasTextureLightMap)
                {
                    GeoTexture tex = new GeoTexture();
                    tex.UVTransform = new OpenTK.Vector2(1, 1);
                    tex.Filename = material.TextureLightMap.FilePath;
                    tex.UVMapIndex = material.TextureLightMap.UVIndex;
                    if (model.Textures.ContainsKey(tex.Filename))
                    {
                        tex.OpenGLID = model.Textures[tex.Filename].OpenGLID;
                    }
                    else
                    {
                        tex.OpenGLID = HelperTexture.LoadTextureForModelExternal(
                                FindTextureInSubs(StripPathFromFile(tex.Filename), model.PathAbsolute)
                            );
                        model.Textures.Add(tex.Filename, tex);
                    }
                    tex.Type = GeoTexture.TexType.Light;
                }

            }

            geoMesh.Material = geoMaterial;
        }

        private static void ProcessMeshes(Scene scene, ref GeoModel model)
        {
            model.MeshHitboxes = new List<GeoMeshHitbox>();

            string currentMeshName = null;
            GeoMeshHitbox meshHitBox = null;
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            Matrix4 nodeTransform = Matrix4.Identity;

            for (int m = 0; m < scene.MeshCount; m++)
            {
                Mesh mesh = scene.Meshes[m];
                bool isNewMesh = currentMeshName != null && mesh.Name != currentMeshName && model.Filename != "kwcube6.obj";

                if (mesh.PrimitiveType != PrimitiveType.Triangle)
                {
                    throw new Exception("Model's primitive type is not set to 'triangles'. Cannot import model.");
                }

                if (isNewMesh)
                {
                    // Generate hitbox for the previous mesh:
                    meshHitBox = new GeoMeshHitbox(maxX, maxY, maxZ, minX, minY, minZ);
                    meshHitBox.Model = model;
                    meshHitBox.Name = currentMeshName;
                    meshHitBox.Transform = nodeTransform;
                    meshHitBox.HasPCA = false;
                    model.MeshHitboxes.Add(meshHitBox);

                    minX = float.MaxValue;
                    minY = float.MaxValue;
                    minZ = float.MaxValue;

                    maxX = float.MinValue;
                    maxY = float.MinValue;
                    maxZ = float.MinValue;
                }

                currentMeshName = mesh.Name;

                GeoMesh geoMesh = new GeoMesh();
                bool transformFound = FindTransformForMesh(scene, scene.RootNode, mesh, out nodeTransform, out string nodeName);
                geoMesh.Transform = nodeTransform;
                geoMesh.Bones = new Dictionary<string, GeoBone>();
                geoMesh.Terrain = null;
                geoMesh.BoneTranslationMatrixCount = mesh.BoneCount;
                geoMesh.HasPCAHitbox = false; // TODO: Calculate PCA Hitbox
                geoMesh.Name = mesh.Name + " #" + m.ToString().PadLeft(4, '0') + " (Node: " + nodeName + ")";
                geoMesh.Vertices = new GeoVertex[mesh.VertexCount];
                geoMesh.Primitive = OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles;
                geoMesh.BoneOrder = new List<string>();
                geoMesh.VAOGenerateAndBind();

                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    //TODO: Find max/min for x/y/z (for hitboxing)
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
                geoMesh.Indices = mesh.GetUnsignedIndices();

                if (model.HasBones)
                {
                    for (int i = 0; i < mesh.BoneCount; i++)
                    {
                        Bone bone = mesh.Bones[i];
                        geoMesh.BoneOrder.Add(bone.Name);

                        GeoBone gBone = new GeoBone();
                        gBone.Offset = HelperMatrix.ConvertAssimpToOpenTKMatrix(bone.OffsetMatrix);
                        gBone.Index = i;
                        gBone.Name = bone.Name;
                        geoMesh.Bones.Add(bone.Name, gBone);

                        foreach (VertexWeight vw in bone.VertexWeights)
                        {

                            int weightIndexToBeSet = geoMesh.Vertices[vw.VertexID].WeightSet;
                            if (weightIndexToBeSet >= KWEngine.MAX_BONE_WEIGHTS)
                            {
                                throw new Exception("Model's bones have more than three weights per vertex. Cannot import model.");
                            }

                            //Debug.WriteLine("Setting Vertex " + vw.VertexID + " with BoneID " + i + " and Weight: " + vw.Weight + " to Slot #" + weightIndexToBeSet);
                            geoMesh.Vertices[vw.VertexID].Weights[weightIndexToBeSet] = vw.Weight;
                            geoMesh.Vertices[vw.VertexID].BoneIDs[weightIndexToBeSet] = i;
                            geoMesh.Vertices[vw.VertexID].WeightSet++;
                        }
                    }
                }

                geoMesh.VBOGenerateIndices();
                geoMesh.VBOGenerateVerticesAndBones(model.HasBones);
                geoMesh.VBOGenerateNormals(mesh);
                geoMesh.VBOGenerateTangents(mesh);
                geoMesh.VBOGenerateTextureCoords1(mesh);
                geoMesh.VBOGenerateTextureCoords2(mesh);

                ProcessMaterialsForMesh(scene, mesh, ref model, ref geoMesh);



                geoMesh.VAOUnbind();

                model.Meshes.Add(geoMesh.Name, geoMesh);
            }

            // Generate hitbox for the last mesh:
            meshHitBox = new GeoMeshHitbox(maxX, maxY, maxZ, minX, minY, minZ);
            meshHitBox.Model = model;
            meshHitBox.Name = model.Filename == "kwcube6.obj" ? "KWCube6" : currentMeshName;
            meshHitBox.Transform = nodeTransform;
            meshHitBox.HasPCA = false;
            model.MeshHitboxes.Add(meshHitBox);

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