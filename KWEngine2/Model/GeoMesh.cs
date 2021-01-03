using Assimp;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2.Model
{
    internal class GeoMesh
    {
        public override string ToString()
        {
            return Name;
        }

        public List<string> BoneNames = new List<string>();
        public List<int> BoneIndices = new List<int>();
        public List<Matrix4> BoneOffset = new List<Matrix4>();

        internal int BoneTranslationMatrixCount;

        public int VAO { get; internal set; }
        public int VBOPosition { get; internal set; }
        public int VBONormal { get; internal set; }
        public int VBOTexture1 { get; internal set; }
        public int VBOTexture2 { get; internal set; }
        public int VBOBoneIDs { get; internal set; }
        public int VBOBoneWeights { get; internal set; }
        public int VBOTangent { get; internal set; }
        public int VBOBiTangent { get; internal set; }
        public int VBOIndex { get; internal set; }
        public string Name { get; internal set; }
        public string NameOrg { get; internal set; }
        internal Matrix4 Transform;
        public GeoVertex[] Vertices { get; internal set; }
        public OpenTK.Graphics.OpenGL4.PrimitiveType Primitive;
        public int IndexCount
        {
            get;
            internal set;
        }

        //public uint[] Indices { get; internal set; }
        public GeoMaterial Material { get; internal set; }

        internal void VAOGenerateAndBind()
        {
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);
        }

        internal void VAOUnbind()
        {
            GL.BindVertexArray(0);
        }

        internal void VBOGenerateVerticesAndBones(bool hasBones)
        {
            float[] verticesF = new float[Vertices.Length * 3];
            uint[] boneIds = new uint[Vertices.Length * 3];
            float[] boneWeights = new float[Vertices.Length * 3];

            for (int i = 0, arrayIndex = 0; i < Vertices.Length; i++, arrayIndex += 3)
            {
                verticesF[arrayIndex] = Vertices[i].X;
                verticesF[arrayIndex + 1] = Vertices[i].Y;
                verticesF[arrayIndex + 2] = Vertices[i].Z;

                if (hasBones)
                {
                    boneIds[arrayIndex] = Vertices[i].BoneIDs[0];
                    boneIds[arrayIndex + 1] = Vertices[i].BoneIDs[1];
                    boneIds[arrayIndex + 2] = Vertices[i].BoneIDs[2];

                    boneWeights[arrayIndex] = Vertices[i].Weights[0];
                    boneWeights[arrayIndex + 1] = Vertices[i].Weights[1];
                    boneWeights[arrayIndex + 2] = Vertices[i].Weights[2];
                }
            }
            VBOPosition = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOPosition);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * 3 * 4, verticesF, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            Vertices = null; // Not needed anymore. Let the GC clear it...

            if (hasBones)
            {
                VBOBoneIDs = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOBoneIDs);
                GL.BufferData(BufferTarget.ArrayBuffer, boneIds.Length * 4, boneIds, BufferUsageHint.StaticDraw);
                GL.VertexAttribIPointer(6, 3, VertexAttribIntegerType.UnsignedInt, 0, IntPtr.Zero);
                GL.EnableVertexAttribArray(6);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                VBOBoneWeights = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOBoneWeights);
                GL.BufferData(BufferTarget.ArrayBuffer, boneWeights.Length * 4, boneWeights, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(7, 3, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(7);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        internal void VBOGenerateNormals(Mesh mesh)
        {
            if (mesh.HasNormals)
            {
                float[] values = new float[mesh.Normals.Count * 3];

                for (int i = 0, arrayIndex = 0; i < mesh.Normals.Count; i++, arrayIndex += 3)
                {
                    values[arrayIndex] = mesh.Normals[i].X;
                    values[arrayIndex + 1] = mesh.Normals[i].Y;
                    values[arrayIndex + 2] = mesh.Normals[i].Z;
                }
                VBONormal = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBONormal);
                GL.BufferData(BufferTarget.ArrayBuffer, mesh.Normals.Count * 3 * 4, values, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(1);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        internal void VBOGenerateNormals(float[] normals)
        {
            if (normals.Length > 0)
            {
                VBONormal = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBONormal);
                GL.BufferData(BufferTarget.ArrayBuffer, normals.Length * 4, normals, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(1);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        internal void VBOGenerateTextureCoords1(Mesh mesh, Scene scene, int isKWCube = 0)
        {
            if (mesh.HasTextureCoords(0))
            {
                float[] values = new float[mesh.TextureCoordinateChannels[0].Count * 2];

                for (int i = 0, arrayIndex = 0; i < mesh.TextureCoordinateChannels[0].Count; i++, arrayIndex += 2)
                {
                    if (isKWCube == 2)
                    {
                        values[arrayIndex] = mesh.TextureCoordinateChannels[0][i].X;
                        values[arrayIndex + 1] = mesh.TextureCoordinateChannels[0][i].Y;
                    }
                    else if (isKWCube == 6)
                    {
                        values[arrayIndex] = mesh.TextureCoordinateChannels[0][i].X;
                        values[arrayIndex + 1] = 1 - mesh.TextureCoordinateChannels[0][i].Y;
                    }
                    else
                    {
                        values[arrayIndex] = mesh.TextureCoordinateChannels[0][i].X;
                        values[arrayIndex + 1] = mesh.TextureCoordinateChannels[0][i].Y;
                    }
                }

                VBOTexture1 = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOTexture1);
                GL.BufferData(BufferTarget.ArrayBuffer, values.Length * 4, values, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(2);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }

        }

        internal void VBOGenerateTextureCoords1(float[] uvs)
        {
            if (uvs != null && uvs.Length > 0)
            {
                VBOTexture1 = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOTexture1);
                GL.BufferData(BufferTarget.ArrayBuffer, uvs.Length * 4, uvs, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(2);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        internal void VBOGenerateTextureCoords2(float[] uvs)
        {
            if (uvs != null && uvs.Length > 0)
            {
                VBOTexture1 = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOTexture2);
                GL.BufferData(BufferTarget.ArrayBuffer, uvs.Length * 4, uvs, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(3);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        internal void VBOGenerateTextureCoords2(Mesh mesh)
        {
            if (mesh.HasTextureCoords(1))
            {
                float[] values = new float[mesh.TextureCoordinateChannels[1].Count * 2];

                for (int i = 0, arrayIndex = 0; i < mesh.TextureCoordinateChannels[1].Count; i++, arrayIndex += 2)
                {
                    values[arrayIndex] = mesh.TextureCoordinateChannels[1][i].X;
                    values[arrayIndex + 1] = mesh.TextureCoordinateChannels[1][i].Y;
                }
                VBOTexture2 = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOTexture2);
                GL.BufferData(BufferTarget.ArrayBuffer, mesh.TextureCoordinateChannels[1].Count * 2 * 4, values, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(3);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        internal void VBOGenerateTangents(Mesh mesh)
        {
            if (mesh.HasTangentBasis)
            {
                //Tangents
                float[] values = new float[mesh.Tangents.Count * 3];

                for (int i = 0, arrayIndex = 0; i < mesh.Tangents.Count; i++, arrayIndex += 3)
                {
                    values[arrayIndex] = mesh.Tangents[i].X;
                    values[arrayIndex + 1] = mesh.Tangents[i].Y;
                    values[arrayIndex + 2] = mesh.Tangents[i].Z;
                }
                VBOTangent = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOTangent);
                GL.BufferData(BufferTarget.ArrayBuffer, mesh.Tangents.Count * 3 * 4, values, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(4);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                //BiTangents
                values = new float[mesh.BiTangents.Count * 3];

                for (int i = 0, arrayIndex = 0; i < mesh.BiTangents.Count; i++, arrayIndex += 3)
                {
                    values[arrayIndex] = mesh.BiTangents[i].X;
                    values[arrayIndex + 1] = mesh.BiTangents[i].Y;
                    values[arrayIndex + 2] = mesh.BiTangents[i].Z;
                }
                VBOBiTangent = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOBiTangent);
                GL.BufferData(BufferTarget.ArrayBuffer, mesh.BiTangents.Count * 3 * 4, values, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(5, 3, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(5);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        internal void VBOGenerateTangents(float[] normals, float[] tangents)
        {
            if (normals.Length > 0)
            {
                VBOTangent = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOTangent);
                GL.BufferData(BufferTarget.ArrayBuffer, tangents.Length * 4, tangents, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(4);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                //BiTangents
                float[] bitangents = new float[tangents.Length];
                for (int i = 0; i < normals.Length; i += 3)
                {
                    Vector3 n = new Vector3(normals[i], normals[i + 1], normals[i + 2]);
                    Vector3 t = new Vector3(tangents[i], tangents[i + 1], tangents[i + 2]);
                    Vector3 bt = Vector3.Cross(n, t);
                    bitangents[i] = bt.X;
                    bitangents[i+1] = bt.Y;
                    bitangents[i+2] = bt.Z;
                }

                VBOBiTangent = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOBiTangent);
                GL.BufferData(BufferTarget.ArrayBuffer, bitangents.Length * 4, bitangents, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(5, 3, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(5);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        internal void VBOGenerateIndices(uint[] indices)
        {
            VBOIndex = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, VBOIndex);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        internal void Dispose()
        {
            // Dispose VAO and VBOS:
            if (VBOIndex >= 0)
                GL.DeleteBuffer(VBOIndex);
            if (VBOPosition >= 0)
                GL.DeleteBuffer(VBOPosition);
            if (VBONormal >= 0)
                GL.DeleteBuffer(VBONormal);
            if (VBOTangent >= 0)
                GL.DeleteBuffer(VBOTangent);
            if (VBOBiTangent >= 0)
                GL.DeleteBuffer(VBOBiTangent);
            if (VBOTexture1 >= 0)
                GL.DeleteBuffer(VBOTexture1);
            if (VBOTexture2 >= 0)
                GL.DeleteBuffer(VBOTexture2);
            if (VBOBoneIDs >= 0)
                GL.DeleteBuffer(VBOBoneIDs);
            if (VBOBoneWeights >= 0)
                GL.DeleteBuffer(VBOBoneWeights);

            if (VAO >= 0)
                GL.DeleteVertexArray(VAO);
        }

        public GeoTerrain Terrain { get; internal set; }
    }
}
