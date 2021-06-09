using Assimp;
using KWEngine2.GameObjects;
using KWEngine2.Model;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KWEngine2.Collision
{
    internal class Hitbox
    {
        private Vector3[] mVerticesSpheroid = new Vector3[] { 
                    Vector3.UnitX / 2, 
                    Vector3.UnitY / 2, 
                    Vector3.UnitZ / 2, 
                    -Vector3.UnitX / 2, 
                    -Vector3.UnitY / 2, 
                    -Vector3.UnitZ / 2,
                    Vector3.NormalizeFast(Vector3.UnitX + Vector3.UnitY - Vector3.UnitZ) / 2,
                    Vector3.NormalizeFast(Vector3.UnitX + Vector3.UnitY + Vector3.UnitZ) / 2,
                    Vector3.NormalizeFast(Vector3.UnitX - Vector3.UnitY - Vector3.UnitZ) / 2,
                     Vector3.NormalizeFast(Vector3.UnitX - Vector3.UnitY + Vector3.UnitZ) / 2,

                    Vector3.NormalizeFast(-Vector3.UnitX + Vector3.UnitY - Vector3.UnitZ) / 2,
                    Vector3.NormalizeFast(-Vector3.UnitX + Vector3.UnitY + Vector3.UnitZ) / 2,
                    Vector3.NormalizeFast(-Vector3.UnitX - Vector3.UnitY - Vector3.UnitZ) / 2,
                     Vector3.NormalizeFast(-Vector3.UnitX - Vector3.UnitY + Vector3.UnitZ) / 2,

                };
        private Vector3[] mNormalsSpheroid = new Vector3[] {
                    Vector3.NormalizeFast(Vector3.UnitX + Vector3.UnitY - Vector3.UnitZ),
                    Vector3.NormalizeFast(Vector3.UnitX + Vector3.UnitY + Vector3.UnitZ),
                    Vector3.NormalizeFast(Vector3.UnitX - Vector3.UnitY - Vector3.UnitZ),
                     Vector3.NormalizeFast(Vector3.UnitX - Vector3.UnitY + Vector3.UnitZ),

                    Vector3.NormalizeFast(-Vector3.UnitX + Vector3.UnitY - Vector3.UnitZ),
                    Vector3.NormalizeFast(-Vector3.UnitX + Vector3.UnitY + Vector3.UnitZ),
                    Vector3.NormalizeFast(-Vector3.UnitX - Vector3.UnitY - Vector3.UnitZ),
                     Vector3.NormalizeFast(-Vector3.UnitX - Vector3.UnitY + Vector3.UnitZ),

                    };

        private Vector3[] mVertices = new Vector3[8];
        private Vector3[] mNormals = new Vector3[3];
        private Vector3 mCenter = new Vector3(0, 0, 0);
        private Vector3 mDimensions = new Vector3(0, 0, 0);
        private float mLow = 0;
        private float mHigh = 0;
        private float mAverageDiameter = 0;
        private float mFullDiameter = 0;
        private static Vector3 tmp = new Vector3(0, 0, 0);
        private static Vector3 tmpMap = Vector3.Zero;
        private static List<GeoTerrainTriangle> triangles = new List<GeoTerrainTriangle>();
        private static List<GeoTerrainTriangle> trianglesMTV = new List<GeoTerrainTriangle>();

        public float DiameterAveraged
        {
            get
            {
                return mAverageDiameter;
            }
        }

        public float DiameterFull
        {
            get
            {
                return mFullDiameter;
            }

        }

        internal float[] GetVertices()
        {
            float[] vertices = new float[mVertices.Length * 3];
            for(int i = 0, j = 0; i < mVertices.Length; i++)
            {
                vertices[j + 0] = mVertices[i].X;
                vertices[j + 1] = mVertices[i].Y;
                vertices[j + 2] = mVertices[i].Z;
                j += 3;
            }
            return vertices;
        }

        public bool IsActive { get; internal set; } = true;

        private static Vector3 MTVTemp = new Vector3(0, 0, 0);
        private static Vector3 MTVTempUp = new Vector3(0, 0, 0);
        private Matrix4 mModelMatrixFinal = Matrix4.Identity;

        public GameObject Owner { get; private set; }
        private GeoMeshHitbox mMesh;

        internal bool IsExtended
        {
            get
            {
                return mMesh != null ? mMesh.IsExtended : false;
            }
        }

        public Hitbox(GameObject owner, GeoMeshHitbox mesh)
        {
            Owner = owner;
            mMesh = mesh;
            if (mesh.IsExtended)
            {
                mVertices = new Vector3[mesh.Vertices.Length];
                mNormals = new Vector3[mesh.Normals.Length];
            }
            else if (mMesh.Model.Filename == "kwsphere.obj")
            {
                mVertices = new Vector3[mVerticesSpheroid.Length];
                mNormals = new Vector3[mNormalsSpheroid.Length];
            }
            if (mMesh.IsActive)
            { 
                Vector3 sceneCenter = Update(ref owner._sceneDimensions);
                Owner._sceneCenter = sceneCenter;
            }
            else
            {
                IsActive = false;
            }
        }

        public Vector3 Update(ref Vector3 dims)
        {
            Matrix4.Mult(ref mMesh.Transform, ref Owner._modelMatrix, out mModelMatrixFinal);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < mVertices.Length; i++)
            {
                if (i < mNormals.Length)
                {
                    if (mMesh.Model.Filename == "kwsphere.obj")
                    {
                        if (Owner.IsSpheroid())
                        {
                            Vector3.TransformNormal(ref mNormalsSpheroid[i], ref mModelMatrixFinal, out mNormals[i]);
                            mNormals[i].NormalizeFast();
                        }
                    }
                    else
                    {
                        Vector3.TransformNormal(ref mMesh.Normals[i], ref mModelMatrixFinal, out mNormals[i]);
                        mNormals[i].NormalizeFast();
                    }
                }

                if (mMesh.Model.Filename == "kwsphere.obj")
                {
                    Vector3.TransformPosition(ref mVerticesSpheroid[i], ref mModelMatrixFinal, out mVertices[i]);
                }
                else
                {
                    Vector3.TransformPosition(ref mMesh.Vertices[i], ref mModelMatrixFinal, out mVertices[i]);
                }

                if (mVertices[i].X > maxX)
                    maxX = mVertices[i].X;
                if (mVertices[i].X < minX)
                    minX = mVertices[i].X;
                if (mVertices[i].Y > maxY)
                    maxY = mVertices[i].Y;
                if (mVertices[i].Y < minY)
                    minY = mVertices[i].Y;
                if (mVertices[i].Z > maxZ)
                    maxZ = mVertices[i].Z;
                if (mVertices[i].Z < minZ)
                    minZ = mVertices[i].Z;   
            }

            Vector3.TransformPosition(ref mMesh.Center, ref mModelMatrixFinal, out mCenter);

            float xWidth = maxX - minX;
            float yWidth = maxY - minY;
            float zWidth = maxZ - minZ;
            dims.X = xWidth;
            dims.Y = yWidth;
            dims.Z = zWidth;

            mLow = minY;
            mHigh = maxY;

            mAverageDiameter = (xWidth + yWidth + zWidth) / 3;
            mFullDiameter = -1;
            if (xWidth > mFullDiameter)
                mFullDiameter = xWidth;
            if (yWidth > mFullDiameter)
                mFullDiameter = yWidth;
            if (zWidth > mFullDiameter)
                mFullDiameter = zWidth;

            mDimensions.X = xWidth;
            mDimensions.Y = yWidth;
            mDimensions.Z = zWidth;

            return mCenter;
        }

        public Vector3 GetDimensions()
        {
            return mDimensions;
        }

        public Vector3 GetCenter()
        {
            return mCenter;
        }

        internal float GetLowestVertexHeight()
        {
            return mLow;
        }

        internal float GetHighestVertexHeight()
        {
            return mHigh;
        }

        private static Vector3 ZeroVector = Vector3.Zero;

        private static Intersection TestIntersectionSphereConvexHull(Hitbox caller, Hitbox collider, Vector3 offsetCaller)
        {
            float mtvDistance = float.MaxValue;
            float mtvDirection = 1;
            float mtvDistanceUp = float.MaxValue;
            float mtvDirectionUp = 1;

            MTVTemp = Vector3.Zero;
            MTVTempUp = Vector3.Zero;

            float sphereRadius = caller.Owner.Scale.X / 2;
            int bestCollisionIndex = 0;
            for (int i = 0; i < collider.mNormals.Length; i++)
            {
                float shape1Min, shape1Max, shape2Min, shape2Max;

                shape1Min = Vector3.Dot((caller.GetCenter() + offsetCaller) - collider.mNormals[i] * sphereRadius, collider.mNormals[i]);
                shape1Max = Vector3.Dot((caller.GetCenter() + offsetCaller) + collider.mNormals[i] * sphereRadius, collider.mNormals[i]);
                SatTest(ref collider.mNormals[i], ref collider.mVertices, out shape2Min, out shape2Max, ref ZeroVector);

                if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max)) 
                {
                    return null;
                }
                else
                {
                    bool m = CalculateOverlap(ref collider.mNormals[i], ref shape1Min, ref shape1Max, ref shape2Min, ref shape2Max,
                        ref mtvDistance, ref mtvDistanceUp, ref MTVTemp, ref MTVTempUp, ref mtvDirection, ref mtvDirectionUp, ref caller.mCenter, ref collider.mCenter, ref offsetCaller);
                    if (m)
                        bestCollisionIndex = i;
                }
            }

            if (MTVTemp == Vector3.Zero)
                return null;

            Intersection o = new Intersection(collider.Owner, MTVTemp, MTVTempUp, collider.mMesh.Name, collider.mNormals[bestCollisionIndex]);
            return o;
        }

        private static Intersection TestIntersectionConvexHullSphere(Hitbox caller, Hitbox collider, Vector3 offsetCaller)
        {
            float mtvDistance = float.MaxValue;
            float mtvDirection = 1;
            float mtvDistanceUp = float.MaxValue;
            float mtvDirectionUp = 1;

            MTVTemp = Vector3.Zero;
            MTVTempUp = Vector3.Zero;

            float sphereRadius = collider.Owner.Scale.X / 2;
            Vector3 collisionSurfaceNormal = new Vector3(0, 0, 0);
            for (int i = 0; i < caller.mNormals.Length; i++)
            {
                float shape1Min, shape1Max, shape2Min, shape2Max;

                SatTest(ref caller.mNormals[i], ref caller.mVertices, out shape1Min, out shape1Max, ref ZeroVector);
                shape2Min = Vector3.Dot(collider.GetCenter() - caller.mNormals[i] * sphereRadius, caller.mNormals[i]);
                shape2Max = Vector3.Dot(collider.GetCenter() + caller.mNormals[i] * sphereRadius, caller.mNormals[i]);
                

                if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                {
                    return null;
                }
                else
                {
                    bool m = CalculateOverlap(ref caller.mNormals[i], ref shape1Min, ref shape1Max, ref shape2Min, ref shape2Max,
                        ref mtvDistance, ref mtvDistanceUp, ref MTVTemp, ref MTVTempUp, ref mtvDirection, ref mtvDirectionUp, ref caller.mCenter, ref collider.mCenter, ref offsetCaller);
                    if (m)
                        collisionSurfaceNormal = Vector3.NormalizeFast((caller.mCenter + offsetCaller) - collider.mCenter);
                }
            }

            if (MTVTemp == Vector3.Zero)
                return null;

            Intersection o = new Intersection(collider.Owner, MTVTemp, MTVTempUp, collider.mMesh.Name, collisionSurfaceNormal);
            return o;
        }

        private static Intersection TestIntersectionSphereSphere(Hitbox caller, Hitbox collider, Vector3 offsetCaller)
        {
            Intersection i = null;
            Vector3 diff = collider.GetCenter() - (caller.GetCenter() + offsetCaller);

            float diffLength = diff.LengthFast;
            float radiusCaller = caller.Owner.Scale.X / 2;
            float radiusCollider = collider.Owner.Scale.X / 2;

            float diffCollision = diffLength - (radiusCollider + radiusCaller);

            if(diffCollision < 0)
            {
                // collision detected!
                diff.NormalizeFast();

                Vector3 mtv = diff * diffCollision;
                Vector3 mtvUp = new Vector3(0, (caller.GetCenter() + offsetCaller).Y >= collider.GetCenter().Y ? -diffCollision : diffCollision, 0); // approximated!
                Vector3 collisionSurfaceNormal = Vector3.NormalizeFast((caller.GetCenter() + offsetCaller) - collider.GetCenter());
                i = new Intersection(collider.Owner, mtv , mtvUp, "KWSphere", collisionSurfaceNormal);
                return i;
            }

            return i;
        }

        public static Intersection TestIntersection(Hitbox caller, Hitbox collider, Vector3 offsetCaller)
        {
            if(caller.Owner.IsSpherePerfect() && !collider.Owner.IsSpherePerfect())
            {
                return TestIntersectionSphereConvexHull(caller, collider, offsetCaller);
            }
            else if (caller.Owner.IsSpherePerfect() && collider.Owner.IsSpherePerfect())
            {
                return TestIntersectionSphereSphere(caller, collider, offsetCaller);
            }
            else if (!caller.Owner.IsSpherePerfect() && collider.Owner.IsSpherePerfect())
            {
                return TestIntersectionConvexHullSphere(caller, collider, offsetCaller);
            }

            float mtvDistance = float.MaxValue;
            float mtvDirection = 1;
            float mtvDistanceUp = float.MaxValue;
            float mtvDirectionUp = 1;

            MTVTemp = Vector3.Zero;
            MTVTempUp = Vector3.Zero;
            int collisionNormalIndex = 0;
            for (int i = 0; i < caller.mNormals.Length; i++)
            {
                float shape1Min, shape1Max, shape2Min, shape2Max;
                SatTest(ref caller.mNormals[i], ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                SatTest(ref caller.mNormals[i], ref collider.mVertices, out shape2Min, out shape2Max, ref ZeroVector);
                if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                {
                    return null;
                }
                else
                {
                    bool m = CalculateOverlap(ref caller.mNormals[i], ref shape1Min, ref shape1Max, ref shape2Min, ref shape2Max,
                        ref mtvDistance, ref mtvDistanceUp, ref MTVTemp, ref MTVTempUp, ref mtvDirection, ref mtvDirectionUp, ref caller.mCenter, ref collider.mCenter, ref offsetCaller);
                    if (m)
                        collisionNormalIndex = i;
                }
            }

            for (int i = 0; i < collider.mNormals.Length; i++)
            {
                float shape1Min, shape1Max, shape2Min, shape2Max;
                SatTest(ref collider.mNormals[i], ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                SatTest(ref collider.mNormals[i], ref collider.mVertices, out shape2Min, out shape2Max, ref ZeroVector);
                if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                {
                    return null;
                }
                else
                {
                    bool m = CalculateOverlap(ref collider.mNormals[i], ref shape1Min, ref shape1Max, ref shape2Min, ref shape2Max,
                        ref mtvDistance, ref mtvDistanceUp, ref MTVTemp, ref MTVTempUp, ref mtvDirection, ref mtvDirectionUp, ref caller.mCenter, ref collider.mCenter, ref offsetCaller);
                    if (m)
                        collisionNormalIndex = caller.mNormals.Length + i;
                }
            }

            if (MTVTemp == Vector3.Zero)
            return null;

            Vector3 collisionSurfaceNormal;
            if (collisionNormalIndex < caller.mNormals.Length)
            {
                collisionSurfaceNormal = caller.mNormals[collisionNormalIndex];
            }
            else
            {
                collisionSurfaceNormal = collider.mNormals[collisionNormalIndex - caller.mNormals.Length];
            }

            Intersection o = new Intersection(collider.Owner, MTVTemp, MTVTempUp, collider.mMesh.Name, collisionSurfaceNormal);
            return o;
        }

        internal static Intersection TestIntersectionTerrain(Hitbox caller, Hitbox collider, Vector3 offset)
        {
            float heightOnMap;
            triangles.Clear();
            GeoModel model = collider.Owner.Model;
            triangles.AddRange(model.Meshes.Values.ElementAt(0).Terrain.GetTrianglesForHitbox(caller, collider.Owner.Position, offset));
            float a = (caller.Owner.Position.Y - caller.GetLowestVertexHeight());
            TestIntersectionSATForTerrain(ref triangles, caller, collider, offset);
            Vector3 mobbPosition = new Vector3();
            int lowestTriangle = -1;
            float lowestIntersectionHeight = float.MaxValue;
            int c = 0;
            foreach (GeoTerrainTriangle triangle in trianglesMTV)
            {
                mobbPosition.X = caller.Owner.GetLargestHitbox().mCenter.X + offset.X;
                mobbPosition.Y = caller.Owner.GetLargestHitbox().mCenter.Y + caller.Owner.CurrentWorld.WorldCenter.Y + caller.Owner.CurrentWorld.WorldDistance;
                mobbPosition.Z = caller.Owner.GetLargestHitbox().mCenter.Z + offset.Z;

                int rayResult = triangle.Intersect3D_RayTriangle(ref mobbPosition, ref tmpMap, collider.Owner.Position);
                float lowestVertexHeight = caller.GetLowestVertexHeight();

                if (rayResult > 0)
                {
                    if (tmpMap.Y < lowestIntersectionHeight)
                    {
                        lowestIntersectionHeight = tmpMap.Y;
                        lowestTriangle = c;
                    }
                }
                else
                {
                    if (rayResult == 0)
                    {
                        if (mobbPosition.X == tmpMap.X && mobbPosition.Z == tmpMap.Z)
                        {
                            if (tmpMap.Y < lowestIntersectionHeight)
                            {
                                lowestIntersectionHeight = tmpMap.Y;
                                lowestTriangle = c;
                            }
                        }
                    }
                }
                c++;
            }
            if (lowestTriangle >= 0)
            {
                heightOnMap = lowestIntersectionHeight + a - offset.Y;
                return new Intersection(collider.Owner, Vector3.Zero, Vector3.Zero, collider.Owner.Name, trianglesMTV[lowestTriangle].Normal, heightOnMap, lowestIntersectionHeight, true);
            }
            if (trianglesMTV.Count > 0)
            {
                return new Intersection(collider.Owner, Vector3.Zero, Vector3.Zero, collider.Owner.Name, KWEngine.WorldUp, collider.mCenter.Y, collider.mCenter.Y, true);
            }
            return null;
        }

        internal static void TestIntersectionSATForTerrain(ref List<GeoTerrainTriangle> tris, Hitbox caller, Hitbox collider, Vector3 offsetCaller)
        {
            trianglesMTV.Clear();
            float shape1Min, shape1Max, shape2Min, shape2Max;
            Vector3 o = collider.Owner.Position;
            
            for (int i = 0; i < tris.Count; i++)
            {
                GeoTerrainTriangle triangle = tris[i];

                // Test #1
                SatTest(ref triangle.Normal, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                SatTestOffset(ref triangle.Normal, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);

                if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                {
                    // Test #2:
                    SatTest(ref caller.mNormals[0], ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                    SatTestOffset(ref caller.mNormals[0], ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                    if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                    {
                        // Test #3:
                        SatTest(ref caller.mNormals[1], ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                        SatTestOffset(ref caller.mNormals[1], ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                        if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                        {
                            // Test #4:
                            SatTest(ref caller.mNormals[2], ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                            SatTestOffset(ref caller.mNormals[2], ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                            if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                            {
                                // Test #5: B-A x Hitbox-X-Axis
                                Vector3 subVector = triangle.Vertices[1] - triangle.Vertices[0];
                                //Vector3.Subtract(ref triangle.Vertices[1], ref triangle.Vertices[0], out Vector3 subVector);
                                Vector3.Cross(ref subVector, ref caller.mNormals[0], out Vector3 axisFive);
                                axisFive.NormalizeFast();

                                SatTest(ref axisFive, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                                SatTestOffset(ref axisFive, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                                if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                                {
                                    // Test #6: B-A x Hitbox-Y-Axis
                                    //Vector3.Subtract(ref triangle.Vertices[1], ref triangle.Vertices[0], out subVector);
                                    Vector3.Cross(ref subVector, ref caller.mNormals[1], out Vector3 axisSix);
                                    axisSix.NormalizeFast();


                                    SatTest(ref axisSix, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                                    SatTestOffset(ref axisSix, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                                    if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                                    {
                                        // Test #7: B-A x Hitbox-Z-Axis
                                        //Vector3.Subtract(ref triangle.Vertices[1], ref triangle.Vertices[0], out subVector);
                                        Vector3.Cross(ref subVector, ref caller.mNormals[2], out Vector3 axisSeven);
                                        axisSeven.NormalizeFast();

                                        SatTest(ref axisSeven, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                                        SatTestOffset(ref axisSeven, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                                        if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                                        {
                                            // Test #8: C-B x Hitbox-X-Axis
                                            //Vector3.Subtract(ref triangle.Vertices[2], ref triangle.Vertices[1], out subVector);
                                            subVector = triangle.Vertices[2] - triangle.Vertices[1];
                                            Vector3.Cross(ref subVector, ref caller.mNormals[0], out Vector3 axisEight);
                                            axisEight.NormalizeFast();

                                            SatTest(ref axisEight, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                                            SatTestOffset(ref axisEight, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                                            if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                                            {
                                                // Test #9: C-B x Hitbox-Y-Axis
                                                //Vector3.Subtract(ref triangle.Vertices[2], ref triangle.Vertices[1], out subVector);
                                                Vector3.Cross(ref subVector, ref caller.mNormals[1], out Vector3 axisNine);
                                                axisNine.NormalizeFast();

                                                SatTest(ref axisNine, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                                                SatTestOffset(ref axisNine, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                                                if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                                                {
                                                    // Test #10: C-B x Hitbox-Z-Axis
                                                    //Vector3.Subtract(ref triangle.Vertices[2], ref triangle.Vertices[1], out subVector);
                                                    Vector3.Cross(ref subVector, ref caller.mNormals[2], out Vector3 axisTen);
                                                    axisTen.NormalizeFast();

                                                    SatTest(ref axisTen, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                                                    SatTestOffset(ref axisTen, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                                                    if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                                                    {
                                                        // Test #11: A-C x Hitbox-X-Axis
                                                        //Vector3.Subtract(ref triangle.Vertices[0], ref triangle.Vertices[2], out subVector);
                                                        subVector = triangle.Vertices[0] - triangle.Vertices[2];
                                                        Vector3.Cross(ref subVector, ref caller.mNormals[0], out Vector3 axisEleven);
                                                        axisEleven.NormalizeFast();
                                                        SatTest(ref axisEleven, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                                                        SatTestOffset(ref axisEleven, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                                                        if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                                                        {
                                                            // Test #12: A-C x Hitbox-Y-Axis
                                                            //Vector3.Subtract(ref triangle.Vertices[0], ref triangle.Vertices[2], out subVector);
                                                            Vector3.Cross(ref subVector, ref caller.mNormals[1], out Vector3 axisTwelve);
                                                            axisTwelve.NormalizeFast();
                                                            SatTest(ref axisTwelve, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                                                            SatTestOffset(ref axisTwelve, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                                                            if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                                                            {
                                                                // Test #13: A-C x Hitbox-Z-Axis
                                                                //Vector3.Subtract(ref triangle.Vertices[0], ref triangle.Vertices[2], out subVector);
                                                                Vector3.Cross(ref subVector, ref caller.mNormals[2], out Vector3 axisThirteen);
                                                                axisThirteen.NormalizeFast();
                                                                SatTest(ref axisThirteen, ref caller.mVertices, out shape1Min, out shape1Max, ref offsetCaller);
                                                                SatTestOffset(ref axisThirteen, ref triangle.Vertices, out shape2Min, out shape2Max, ref o);
                                                                if (Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
                                                                {
                                                                    trianglesMTV.Add(triangle);
                                                                }
                                                            }
                                                            else
                                                                continue;
                                                        }
                                                        else
                                                            continue;
                                                    }
                                                    else
                                                        continue;
                                                }
                                                else
                                                    continue;
                                            }
                                            else
                                                continue;
                                        }
                                        else
                                            continue;
                                    }
                                    else
                                        continue;
                                }
                                else
                                    continue;
                            }
                            else
                                continue;
                        }
                        else
                            continue;
                    }
                    else
                        continue;
                }
                else
                    continue;
            }
        }


        private static bool CalculateOverlap(ref Vector3 axis, ref float shape1Min, ref float shape1Max, ref float shape2Min, ref float shape2Max,
            ref float mtvDistance, ref float mtvDistanceUp, ref Vector3 mtv, ref Vector3 mtvUp, ref float mtvDirection, ref float mtvDirectionUp, ref Vector3 posA, ref Vector3 posB, ref Vector3 callerOffset)
        {
            float intersectionDepthScaled;
            if (shape1Min < shape2Min)
            {
                if (shape1Max > shape2Max)
                {
                    float diff1 = shape1Max - shape2Max;
                    float diff2 = shape2Min - shape1Min;
                    if(diff1 > diff2)
                    {
                        intersectionDepthScaled = shape2Max - shape1Min;
                    }
                    else
                    {
                        intersectionDepthScaled = shape2Min - shape1Max;
                    }

                }
                else
                {
                    intersectionDepthScaled = shape1Max - shape2Min; // default
                }

            }
            else
            {
                if(shape1Max < shape2Max)
                {
                    float diff1 = shape2Max - shape1Max;
                    float diff2 = shape1Min - shape2Min;
                    if (diff1 > diff2)
                    {
                        intersectionDepthScaled = shape1Max - shape2Min;
                    }
                    else
                    {
                        intersectionDepthScaled = shape1Min - shape2Max;
                    }
                }
                else
                {
                    intersectionDepthScaled = shape1Min - shape2Max; // default
                }

            }

            float axisLengthSquared = Vector3.Dot(axis, axis);
            float intersectionDepthSquared = (intersectionDepthScaled * intersectionDepthScaled) / axisLengthSquared;

            if(Math.Abs(axis.Y) > Math.Abs(axis.X) && Math.Abs(axis.Y) > Math.Abs(axis.Z) && intersectionDepthSquared < mtvDistanceUp)
            {
                mtvDistanceUp = intersectionDepthSquared;
                mtvUp = axis * (intersectionDepthScaled / axisLengthSquared);
                float notSameDirection = Vector3.Dot(posA + callerOffset - posB, mtvUp);
                mtvDirectionUp = notSameDirection < 0 ? -1.0f : 1.0f;
                mtvUp = mtvUp * mtvDirectionUp;
            }
            if (intersectionDepthSquared < mtvDistance || mtvDistance < 0)
            {
                mtvDistance = intersectionDepthSquared;
                mtv = axis * (intersectionDepthScaled / axisLengthSquared);
                float notSameDirection = Vector3.Dot(posA + callerOffset - posB, mtv);
                mtvDirection = notSameDirection < 0 ? -1.0f : 1.0f;
                mtv = mtv * mtvDirection;

                return true;
            }
            return false;
        }

        private static bool Overlaps(float min1, float max1, float min2, float max2)
        {
            return IsBetweenOrdered(min2, min1, max1) || IsBetweenOrdered(min1, min2, max2);
        }

        private static bool IsBetweenOrdered(float val, float lowerBound, float upperBound)
        {
            return lowerBound <= val && val <= upperBound;
        }

        private static void SatTest(ref Vector3 axis, ref Vector3[] ptSet, out float minAlong, out float maxAlong, ref Vector3 offset)
        {
            minAlong = float.MaxValue;
            maxAlong = float.MinValue;
            for (int i = 0; i < ptSet.Length; i++)
            {
                float dotVal = Vector3.Dot(ptSet[i] + offset, axis);
                if (dotVal < minAlong) minAlong = dotVal;
                if (dotVal > maxAlong) maxAlong = dotVal;
            }
        }

        private static void SatTestOffset(ref Vector3 axis, ref Vector3[] ptSet, out float minAlong, out float maxAlong, ref Vector3 offset)
        {
            minAlong = float.MaxValue;
            maxAlong = float.MinValue;
            for (int i = 0; i < ptSet.Length; i++)
            {
                float dotVal = Vector3.Dot(ptSet[i] + offset, axis);
                if (dotVal < minAlong) minAlong = dotVal;
                if (dotVal > maxAlong) maxAlong = dotVal;
            }
        }
    }
}
