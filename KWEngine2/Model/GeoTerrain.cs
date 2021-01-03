using KWEngine2.Collision;
using KWEngine2.Helper;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace KWEngine2.Model
{
    internal class GeoTerrain
    {
        private float mWidth = 0;
        private float mDepth = 0;
        private float mScaleFactor;
        private int mDots = -1;
        private int mSectorSize = -1;
        private int mSectorSizeCoarse = -1;
        private float mSectorWidth = 0;
        private float mSectorDepth = 0;
        private float mSectorDiameter;
        private float mSectorWidthCoarse = 0;
        private float mSectorDepthCoarse = 0;

        internal int _texBlend = KWEngine.TextureBlack;
        internal int _texR = KWEngine.TextureAlpha;
        internal int _texG = KWEngine.TextureAlpha;
        internal int _texB = KWEngine.TextureAlpha;

        private List<Sector> mSectors = new List<Sector>();
        private List<Sector> mSectorsCoarse = new List<Sector>();
        private Dictionary<int, List<GeoTerrainTriangle>> mSectorTriangleMap = new Dictionary<int, List<GeoTerrainTriangle>>();
        private Dictionary<Sector, List<Sector>> mSectorCoarseMap = new Dictionary<Sector, List<Sector>>();

        private float mCompleteDiameter;
        internal float mTexX = 1;
        internal float mTexY = 1;

        public float GetScaleFactor()
        {
            return mScaleFactor;
        }

        public float GetWidth()
        {
            return mWidth;
        }

        public float GetDepth()
        {
            return mDepth;
        }

        internal GeoMesh BuildTerrain(Vector3 position, string heightMap, float width, float height, float depth, float texRepeatX = 1, float texRepeatY = 1, bool isFile = true)
        {
            GeoMesh mmp;
            try
            {
                Assembly a = Assembly.GetEntryAssembly();
                using (Stream s = isFile ? File.Open(heightMap, FileMode.Open) : a.GetManifestResourceStream(a.GetName().Name + "." + heightMap))
                {
                    using (Bitmap image = new Bitmap(s))
                    {

                        mDots = image.Width * image.Height;

                        mWidth = width;
                        mDepth = depth;
                        mScaleFactor = height > 0 ? height : 1;
                        mTexX = texRepeatX > 0 ? texRepeatX : 1;
                        mTexY = texRepeatY > 0 ? texRepeatY : 1;

                        if (image.Width < 4 || image.Height < 4 || image.Height > 256 || image.Width > 256)
                        {
                            throw new Exception("Image size too small or too big: width and height need to be >= 4 and <= 256 pixels.");
                        }

                        Debug.WriteLine("Generating terrain from height map: " + heightMap);
                        double mp = Math.Round(mDots / 1000000.0, 3);
                        if (mDots > 1000)
                        {
                            Debug.WriteLine("\tImage pixel count:\t\t" + mp + " megapixel");
                            if (mp >= 0.5)
                                Debug.WriteLine("(WARNING: pixel count > 0.5 megapixel! You will experience SERIOUS performance issues with this terrain mapping.)");
                        }
                        else
                        {
                            Debug.WriteLine("\tImage pixel count:\t\t" + mDots);
                        }
                        long start = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                        mSectorSize = image.Width < image.Height ? (int)Math.Sqrt(image.Width) : (int)Math.Sqrt(image.Height);
                        while (mSectorSize % 4 != 0)
                        {
                            mSectorSize++;
                        }
                        mSectorSizeCoarse = mSectorSize / 4;

                        if (mSectorSize > 144)
                        {
                            mSectorSize = 144;
                            mSectorSizeCoarse = 12;
                        }
                        mSectorWidth = mWidth / mSectorSize;
                        mSectorDepth = mDepth / mSectorSize;
                        mSectorDiameter = (float)Math.Sqrt(mSectorWidth * mSectorWidth + mSectorDepth * mSectorDepth);

                        mSectorWidthCoarse = mWidth / mSectorSizeCoarse;
                        mSectorDepthCoarse = mDepth / mSectorSizeCoarse;


                        for (int i = 0; i < mSectorSizeCoarse; i++)
                        {
                            for (int j = 0; j < mSectorSizeCoarse; j++)
                            {
                                Sector sec = new Sector(
                                    (position.X - mWidth / 2) + i * mSectorWidthCoarse,
                                    (position.X - mWidth / 2) + (i + 1) * mSectorWidthCoarse,

                                    (position.Z + mDepth / 2) - (j + 1) * mSectorDepthCoarse,
                                    (position.Z + mDepth / 2) - j * mSectorDepthCoarse
                                );
                                mSectorsCoarse.Add(sec);
                                mSectorCoarseMap.Add(sec, new List<Sector>());
                            }
                        }

                        for (int i = 0; i < mSectorSize; i++)
                        {
                            for (int j = 0; j < mSectorSize; j++)
                            {
                                Sector sec = new Sector(
                                    (position.X - mWidth / 2) + i * mSectorWidth,
                                    (position.X - mWidth / 2) + (i + 1) * mSectorWidth,

                                    (position.Z + mDepth / 2) - (j + 1) * mSectorDepth,
                                    (position.Z + mDepth / 2) - j * mSectorDepth
                                );
                                sec.ID = mSectors.Count;
                                mSectors.Add(sec);
                                Sector coarseSector = GetSectorCoarseForSector(sec);
                                mSectorCoarseMap[coarseSector].Add(sec);
                                mSectorTriangleMap.Add(mSectors.Count - 1, new List<GeoTerrainTriangle>());
                            }

                        }

                        float[,] mHeightMap = new float[image.Width, image.Height];
                        mCompleteDiameter = (float)Math.Sqrt(mWidth * mWidth + mDepth * mDepth + mScaleFactor * mScaleFactor);

                        float stepWidth = mWidth / (image.Width - 1);
                        float stepDepth = mDepth / (image.Height - 1);
                        Vector3[] points = new Vector3[mDots];
                        int c = 0;
                        int cFBuffer = 0;
                        int cFBufferUV = 0;
                        float[] VBOVerticesBuffer = new float[mDots * 3];
                        float[] VBONormalsBuffer = new float[mDots * 3];
                        float[] VBOUVBuffer = new float[mDots * 2];
                        float[] VBOTangentBuffer = new float[mDots * 3];
                        float[] VBOBiTangentBuffer = new float[mDots * 3];
                        Dictionary<int, Vector3> normalMapping = new Dictionary<int, Vector3>();
                        Dictionary<int, Vector3> tangentMapping = new Dictionary<int, Vector3>();
                        Dictionary<int, Vector3> bitangentMapping = new Dictionary<int, Vector3>();
                        Dictionary<int, int> normalMappingCount = new Dictionary<int, int>();

                        for (int i = 0; i < image.Width; i++)
                        {
                            for (int j = 0; j < image.Height; j++)
                            {
                                Color tmpColor = image.GetPixel(i, j);
                                float normalizedRGB = ((tmpColor.R + tmpColor.G + tmpColor.B) / 3f) / 255f;
                                mHeightMap[i, j] = normalizedRGB;

                                Vector3 tmp = new Vector3(
                                    position.X + i * stepWidth - mWidth / 2,
                                    position.Y + mScaleFactor * normalizedRGB,
                                    position.Z - mDepth / 2 + j * stepDepth
                                    );
                                points[c] = tmp;
                                VBOVerticesBuffer[cFBuffer + 0] = points[c].X;
                                VBOVerticesBuffer[cFBuffer + 1] = points[c].Y;
                                VBOVerticesBuffer[cFBuffer + 2] = points[c].Z;

                                VBOUVBuffer[cFBufferUV + 0] = i * (1f / (image.Width - 1));  //* mTexX;
                                VBOUVBuffer[cFBufferUV + 1] = j * (1f / (image.Height - 1)); // * mTexY;

                                normalMapping.Add(c, new Vector3(0, 0, 0));
                                tangentMapping.Add(c, new Vector3(0, 0, 0));
                                bitangentMapping.Add(c, new Vector3(0, 0, 0));
                                normalMappingCount.Add(c, 0);

                                //increase counter:
                                c++;
                                cFBuffer += 3;
                                cFBufferUV += 2;
                            }
                        }

                        mmp = new GeoMesh();
                        mmp.Name = heightMap;

                        // Build indices and triangles:
                        mmp.VAO = GL.GenVertexArray();
                        GL.BindVertexArray(mmp.VAO);

                        int triangles = 0;
                        List<uint> mIndices = new List<uint>();
                        int imageHeight = image.Height;
                        Vector3 normalT1 = new Vector3(0, 0, 0);
                        Vector3 normalT2 = new Vector3(0, 0, 0);

                        float deltaU1 = 0;
                        float deltaV1 = 0;
                        float deltaU2 = 0;
                        float deltaV2 = 0;
                        float f = 1.0f;
                        Vector3 tangent = new Vector3(0, 0, 0);
                        Vector3 bitangent = new Vector3(0, 0, 0);
                        for (int i = 0; i < points.Length - imageHeight - 1; i++)
                        {
                            Vector3 tmp;

                            if ((i + 1) % imageHeight == 0)
                            {
                                continue;
                            }

                            // Generate Indices:
                            mIndices.Add((uint)(i + imageHeight + 1));
                            mIndices.Add((uint)(i + imageHeight));
                            mIndices.Add((uint)(i));


                            mIndices.Add((uint)(i));
                            mIndices.Add((uint)(i + 1));
                            mIndices.Add((uint)(i + imageHeight + 1));

                            // Generate Triangle objects:

                            // T1:
                            Vector3 v1 = new Vector3(points[i + imageHeight + 1]);
                            Vector3 v2 = new Vector3(points[i + imageHeight]);
                            Vector3 v3 = new Vector3(points[i]);
                            GeoTerrainTriangle t123 = new GeoTerrainTriangle(v1, v2, v3);
                            normalT1 = t123.Normal;
                            List<int> sectorIds = GetSectorsForGeoTriangle(t123);
                            foreach (int sID in sectorIds)
                            {
                                mSectorTriangleMap[sID].Add(t123);
                            }

                            // tangents and bitangent generation
                            deltaU1 = VBOUVBuffer[(i + imageHeight) * 2] - VBOUVBuffer[(i + imageHeight + 1) * 2];
                            deltaV1 = VBOUVBuffer[(i + imageHeight) * 2 + 1] - VBOUVBuffer[(i + imageHeight + 1) * 2 + 1];
                            deltaU2 = VBOUVBuffer[(i + 0) * 2] - VBOUVBuffer[(i + imageHeight + 1) * 2];
                            deltaV2 = VBOUVBuffer[(i + 0) * 2 + 1] - VBOUVBuffer[(i + imageHeight + 1) * 2 + 1];
                            f = 1.0f / (deltaU1 * deltaV2 - deltaU2 * deltaV1);

                            tangent.X = f * (deltaV2 * t123.edge1.X - deltaV1 * t123.edge2.X);
                            tangent.Y = f * (deltaV2 * t123.edge1.Y - deltaV1 * t123.edge2.Y);
                            tangent.Z = f * (deltaV2 * t123.edge1.Z - deltaV1 * t123.edge2.Z);

                            bitangent.X = f * (-deltaU2 * t123.edge1.X - deltaU1 * t123.edge2.X);
                            bitangent.Y = f * (-deltaU2 * t123.edge1.Y - deltaU1 * t123.edge2.Y);
                            bitangent.Z = f * (-deltaU2 * t123.edge1.Z - deltaU1 * t123.edge2.Z);

                            // Generate their normals for VBO:
                            normalMapping.TryGetValue(i, out tmp);
                            tmp += normalT1;
                            normalMapping[i] = tmp;
                            normalMappingCount[i]++;

                            normalMapping.TryGetValue(i + imageHeight, out tmp);
                            tmp += normalT1;
                            normalMapping[i + imageHeight] = tmp;
                            normalMappingCount[i + imageHeight]++;

                            normalMapping.TryGetValue(i + imageHeight + 1, out tmp);
                            tmp += normalT1;
                            normalMapping[i + imageHeight + 1] = tmp;
                            normalMappingCount[i + imageHeight + 1]++;

                            // map tangents & bitangent here:
                            tangentMapping.TryGetValue(i, out tmp);
                            tmp += tangent;
                            tangentMapping[i] = tmp;
                            tangentMapping.TryGetValue(i + imageHeight, out tmp);
                            tmp += tangent;
                            tangentMapping[i + imageHeight] = tmp;
                            tangentMapping.TryGetValue(i + imageHeight + 1, out tmp);
                            tmp += tangent;
                            tangentMapping[i + imageHeight + 1] = tmp;

                            bitangentMapping.TryGetValue(i, out tmp);
                            tmp += bitangent;
                            bitangentMapping[i] = tmp;
                            bitangentMapping.TryGetValue(i + imageHeight, out tmp);
                            tmp += bitangent;
                            bitangentMapping[i + imageHeight] = tmp;
                            bitangentMapping.TryGetValue(i + imageHeight + 1, out tmp);
                            tmp += bitangent;
                            bitangentMapping[i + imageHeight + 1] = tmp;



                            // ============================ T2 ======================

                            Vector3 v4 = new Vector3(points[i]);
                            Vector3 v5 = new Vector3(points[i + 1]);
                            Vector3 v6 = new Vector3(points[i + imageHeight + 1]);
                            GeoTerrainTriangle t456 = new GeoTerrainTriangle(v4, v5, v6);
                            normalT2 = t456.Normal;
                            sectorIds.Clear();
                            sectorIds = GetSectorsForGeoTriangle(t456);
                            foreach (int sID in sectorIds)
                            {
                                mSectorTriangleMap[sID].Add(t456);
                            }

                            // tangents and bitangent generation
                            deltaU1 = VBOUVBuffer[(i + 1) * 2] - VBOUVBuffer[(i) * 2];
                            deltaV1 = VBOUVBuffer[(i + 1) * 2 + 1] - VBOUVBuffer[(i) * 2 + 1];
                            deltaU2 = VBOUVBuffer[(i + imageHeight + 1) * 2] - VBOUVBuffer[(i) * 2];
                            deltaV2 = VBOUVBuffer[(i + imageHeight + 1) * 2 + 1] - VBOUVBuffer[(i) * 2 + 1];
                            f = 1.0f / (deltaU1 * deltaV2 - deltaU2 * deltaV1);

                            tangent.X = f * (deltaV2 * t456.edge1.X - deltaV1 * t456.edge2.X);
                            tangent.Y = f * (deltaV2 * t456.edge1.Y - deltaV1 * t456.edge2.Y);
                            tangent.Z = f * (deltaV2 * t456.edge1.Z - deltaV1 * t456.edge2.Z);

                            bitangent.X = f * (-deltaU2 * t456.edge1.X - deltaU1 * t456.edge2.X);
                            bitangent.Y = f * (-deltaU2 * t456.edge1.Y - deltaU1 * t456.edge2.Y);
                            bitangent.Z = f * (-deltaU2 * t456.edge1.Z - deltaU1 * t456.edge2.Z);

                            normalMapping.TryGetValue(i + imageHeight + 1, out tmp);
                            tmp += normalT2;
                            normalMapping[i + imageHeight + 1] = tmp;
                            normalMappingCount[i + imageHeight + 1]++;

                            normalMapping.TryGetValue(i + 1, out tmp);
                            tmp += normalT2;
                            normalMapping[i + 1] = tmp;
                            normalMappingCount[i + 1]++;

                            normalMapping.TryGetValue(i, out tmp);
                            tmp += normalT2;
                            normalMapping[i] = tmp;
                            normalMappingCount[i]++;

                            // map tangents & bitangent here:
                            tangentMapping.TryGetValue(i + imageHeight + 1, out tmp);
                            tmp += tangent;
                            tangentMapping[i + imageHeight + 1] = tmp;
                            tangentMapping.TryGetValue(i + 1, out tmp);
                            tmp += tangent;
                            tangentMapping[i + 1] = tmp;
                            tangentMapping.TryGetValue(i, out tmp);
                            tmp += tangent;
                            tangentMapping[i] = tmp;

                            bitangentMapping.TryGetValue(i + imageHeight + 1, out tmp);
                            tmp += bitangent;
                            bitangentMapping[i + imageHeight + 1] = tmp;
                            bitangentMapping.TryGetValue(i + 1, out tmp);
                            tmp += bitangent;
                            bitangentMapping[i + 1] = tmp;
                            bitangentMapping.TryGetValue(i, out tmp);
                            tmp += bitangent;
                            bitangentMapping[i] = tmp;



                            triangles += 2;
                        }
                        Debug.WriteLine("\tGenerated triangles:\t" + triangles);
                        cFBuffer = 0;


                        for (int i = 0; i < points.Length; i++)
                        {
                            // Interpolate normals:
                            Vector3 tmp = new Vector3(normalMapping[i].X / normalMappingCount[i],
                                normalMapping[i].Y / normalMappingCount[i],
                                normalMapping[i].Z / normalMappingCount[i]);
                            tmp.Normalize();

                            Vector3 tTemp = new Vector3(tangentMapping[i].X / normalMappingCount[i],
                                tangentMapping[i].Y / normalMappingCount[i],
                                tangentMapping[i].Z / normalMappingCount[i]);
                            tTemp.Normalize();

                            Vector3 btTemp = new Vector3(bitangentMapping[i].X / normalMappingCount[i],
                                bitangentMapping[i].Y / normalMappingCount[i],
                                bitangentMapping[i].Z / normalMappingCount[i]);
                            btTemp.Normalize();

                            VBONormalsBuffer[cFBuffer + 0] = tmp.X;
                            VBONormalsBuffer[cFBuffer + 1] = tmp.Y;
                            VBONormalsBuffer[cFBuffer + 2] = tmp.Z;

                            // tangents and bitangents:
                            VBOTangentBuffer[cFBuffer + 0] = tTemp.X;
                            VBOTangentBuffer[cFBuffer + 1] = tTemp.Y;
                            VBOTangentBuffer[cFBuffer + 2] = tTemp.Z;

                            // tangents and bitangents:
                            VBOBiTangentBuffer[cFBuffer + 0] = btTemp.X;
                            VBOBiTangentBuffer[cFBuffer + 1] = btTemp.Y;
                            VBOBiTangentBuffer[cFBuffer + 2] = btTemp.Z;

                            cFBuffer += 3;
                        }

                        // Generate VBOs:
                        mmp.VBOPosition = GL.GenBuffer();
                        mmp.VBONormal = GL.GenBuffer();
                        mmp.VBOTexture1 = GL.GenBuffer();
                        mmp.VBOTangent = GL.GenBuffer();
                        mmp.VBOBiTangent = GL.GenBuffer();
                        mmp.VBOIndex = GL.GenBuffer();

                        // Vertices:
                        GL.BindBuffer(BufferTarget.ArrayBuffer, mmp.VBOPosition);
                        GL.BufferData(BufferTarget.ArrayBuffer, VBOVerticesBuffer.Length * 4, VBOVerticesBuffer, BufferUsageHint.StaticDraw);
                        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
                        GL.EnableVertexAttribArray(0);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                        // Normals
                        GL.BindBuffer(BufferTarget.ArrayBuffer, mmp.VBONormal);
                        GL.BufferData(BufferTarget.ArrayBuffer, VBONormalsBuffer.Length * 4, VBONormalsBuffer, BufferUsageHint.StaticDraw);
                        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
                        GL.EnableVertexAttribArray(1);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                        // UVs
                        GL.BindBuffer(BufferTarget.ArrayBuffer, mmp.VBOTexture1);
                        GL.BufferData(BufferTarget.ArrayBuffer, VBOUVBuffer.Length * 4, VBOUVBuffer, BufferUsageHint.StaticDraw);
                        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 0);
                        GL.EnableVertexAttribArray(2);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                        // Tangents
                        GL.BindBuffer(BufferTarget.ArrayBuffer, mmp.VBOTangent);
                        GL.BufferData(BufferTarget.ArrayBuffer, VBOTangentBuffer.Length * 4, VBOTangentBuffer, BufferUsageHint.StaticDraw);
                        GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, 0, 0);
                        GL.EnableVertexAttribArray(4);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                        // Bitangents
                        GL.BindBuffer(BufferTarget.ArrayBuffer, mmp.VBOBiTangent);
                        GL.BufferData(BufferTarget.ArrayBuffer, VBOBiTangentBuffer.Length * 4, VBOBiTangentBuffer, BufferUsageHint.StaticDraw);
                        GL.VertexAttribPointer(5, 3, VertexAttribPointerType.Float, false, 0, 0);
                        GL.EnableVertexAttribArray(5);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                        uint[] indices = mIndices.ToArray();
                        mmp.IndexCount = indices.Length;
                        // Indices:
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, mmp.VBOIndex);
                        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);


                        mmp.Transform = Matrix4.Identity;

                        GL.BindVertexArray(0);

                        long diff = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - start;
                        Debug.WriteLine("\t...done (" + Math.Round(diff / 1000f, 2) + " seconds)");

                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Terrain could not be created: " + ex.Message);
            }
            finally
            {
                GLWindow.StartGarbageCollection();
            }
            mmp.Primitive = PrimitiveType.Triangles;

            return mmp;
        }


        private List<GeoTerrainTriangle> resultlist = new List<GeoTerrainTriangle>();
        private List<Sector> collisionSectors = new List<Sector>();
        private List<Sector> fineSectors = new List<Sector>();
        private List<Sector> coarse = new List<Sector>();
        private Vector3 hbTestVector = Vector3.Zero;


        internal List<GeoTerrainTriangle> GetTrianglesForHitbox(Hitbox hb, Vector3 offset, Vector3 collisionOffset)
        {
            resultlist.Clear();
            coarse.Clear();

            hbTestVector.X = hb.GetCenter().X + collisionOffset.X;
            hbTestVector.Y = 0;
            hbTestVector.Z = hb.GetCenter().Z + collisionOffset.Z;
            collisionSectors.Clear();
            fineSectors.Clear();

            foreach (Sector coarseSector in mSectorsCoarse)
            {
                if (hbTestVector.X >= coarseSector.Left + offset.X
                    && hbTestVector.X <= coarseSector.Right + offset.X
                    && hbTestVector.Z >= coarseSector.Back + offset.Z
                    && hbTestVector.Z <= coarseSector.Front + offset.Z)
                {
                    if (!coarse.Contains(coarseSector))
                    {
                        coarse.Add(coarseSector);
                    }
                }
            }

            if (coarse.Count > 0)
            {

                foreach (Sector cs in coarse)
                {
                    fineSectors.AddRange(mSectorCoarseMap[cs]);
                    foreach (Sector fineSector in fineSectors)
                    {
                        if (hbTestVector.X >= fineSector.Left + offset.X
                            && hbTestVector.X <= fineSector.Right + offset.X
                            && hbTestVector.Z >= fineSector.Back + offset.Z
                            && hbTestVector.Z <= fineSector.Front + offset.Z)
                        {
                            foreach (GeoTerrainTriangle t in mSectorTriangleMap[fineSector.ID])
                            {
                                if (hbTestVector.X >= t.boundLeft + offset.X && hbTestVector.X <= t.boundRight + offset.X
                                    && hbTestVector.Z >= t.boundBack + offset.Z && hbTestVector.Z <= t.boundFront + offset.Z)
                                    resultlist.Add(t);
                            }
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No coarse sector found for hitbox of " + hb.Owner.Name);
            }
            return resultlist;
        }

        private Sector GetSectorCoarseForSector(Sector s)
        {
            foreach (Sector sc in mSectorsCoarse)
            {
                if (s.Center.X >= sc.Left
                    && s.Center.X <= sc.Right
                    && s.Center.Y <= sc.Front
                    && s.Center.Y >= sc.Back
                    )
                {
                    return sc;
                }
            }
            throw new Exception("Could not calculate coarse sector for " + s.ToString());
        }

        private List<int> GetSectorsForGeoTriangle(GeoTerrainTriangle triangle)
        {
            List<int> sectorlist = new List<int>();
            for (int i = 0; i < mSectors.Count; i++)
            {
                foreach (Vector3 vertex in triangle.Vertices)
                {
                    if (vertex.X >= mSectors[i].Left
                   && vertex.X <= mSectors[i].Right
                   && vertex.Z <= mSectors[i].Front
                   && vertex.Z >= mSectors[i].Back
                   )
                    {
                        if (!sectorlist.Contains(i))
                            sectorlist.Add(i);
                    }
                }
            }

            return sectorlist;
        }
    }
}
