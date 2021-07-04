using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KWEngine2.GameObjects.GameObject;
using static KWEngine2.KWEngine;

namespace KWEngine2.Helper
{
    /// <summary>
    /// Helferklasse für Rotationsberechnungen
    /// </summary>
    public static class HelperRotation
    {
        private static Matrix4 translationPointMatrix = Matrix4.Identity;
        private static Matrix4 rotationMatrix = Matrix4.Identity;
        private static Matrix4 translationMatrix = Matrix4.Identity;
        private static Matrix4 tempMatrix = Matrix4.Identity;
        private static Matrix4 spinMatrix = Matrix4.Identity;
        private static Vector3 finalTranslationPoint = Vector3.Zero;
        private static Vector3 zeroVector = Vector3.Zero;
        private static Quaternion Turn180 = Quaternion.FromAxisAngle(KWEngine.WorldUp, (float)Math.PI);

        internal static float CalculateRadiansFromDegrees(float degrees)
        {
            return (float)(Math.PI * degrees / 180.0);
        }

        internal static float CalculateDegreesFromRadians(float radiant)
        {
            return (float)((180.0 * radiant) / Math.PI);
        }

        /// <summary>
        /// Berechnet den Vektor, der entsteht, wenn der übergebene Vektor um die angegebenen Grad rotiert wird
        /// </summary>
        /// <param name="vector">zu rotierender Vektor</param>
        /// <param name="degrees">Rotation (in Grad)</param>
        /// <param name="plane">Einheitsvektor, um den rotiert wird</param>
        /// <returns>Rotierter Vektor</returns>
        public static Vector3 RotateVector(Vector3 vector, float degrees, Plane plane)
        {
            if (plane == Plane.X)
            {
                return RotateVector(vector, Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(degrees)));
            }
            else if (plane == Plane.Y)
            {
                return RotateVector(vector, Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(degrees)));

            }
            else if (plane == Plane.Z)
            {
                return RotateVector(vector, Quaternion.FromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(degrees)));
            }
            else
                return RotateVector(vector, Quaternion.FromAxisAngle(KWEngine.CurrentWorld.GetCameraLookAtVector(), MathHelper.DegreesToRadians(degrees)));
        }

        /// <summary>
        /// Konvertiert eine in Quaternion angegebene Rotation in eine XYZ-Rotation (in Grad)
        /// </summary>
        /// <param name="q">zu konvertierendes Quaternion</param>
        /// <returns>XYZ-Rotation als Vector3 (in Grad)</returns>
        public static Vector3 ConvertQuaternionToEulerAngles(Quaternion q)
        {
            Vector3 result = new Vector3(0, 0, 0);
            // roll (x-axis rotation)
            double sinr = +2.0 * (q.W * q.X + q.Y * q.Z);
            double cosr = +1.0 - 2.0 * (q.X * q.X + q.Y * q.Y);
            result.X = (float)Math.Atan2(sinr, cosr);

            // pitch (y-axis rotation)
            double sinp = +2.0 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
            {
                result.Y = sinp < 0 ? ((float)Math.PI / 2.0f) * -1.0f : (float)Math.PI / 2.0f;
            }
            else
                result.Y = (float)Math.Asin(sinp);

            // yaw (z-axis rotation)
            double siny = +2.0 * (q.W * q.Z + q.X * q.Y);
            double cosy = +1.0 - 2.0 * (q.Y * q.Y + q.Z * q.Z);
            result.Z = (float)Math.Atan2(siny, cosy);

            result.X = CalculateDegreesFromRadians(result.X);
            result.Y = CalculateDegreesFromRadians(result.Y);
            result.Z = CalculateDegreesFromRadians(result.Z);

            return result;
        }

        /// <summary>
        /// Berechnet die Position eines Punkts, der um einen angegeben Punkt entlang einer Achse rotiert wird
        /// </summary>
        /// <param name="point">Mittelpunkt der Rotation</param>
        /// <param name="distance">Distanz zum Mittelpunkt</param>
        /// <param name="degrees">Grad der Rotation</param>
        /// <param name="plane">Achse der Rotation (Standard: Y)</param>
        /// <returns>Position des rotierten Punkts</returns>
        public static Vector3 CalculateRotationAroundPointOnAxis(Vector3 point, float distance, float degrees, Plane plane = Plane.Y)
        {
            float radians = MathHelper.DegreesToRadians(degrees % 360);
            Matrix4.CreateTranslation(ref point, out translationPointMatrix);

            if (plane == Plane.X)
            {
                Matrix4.CreateRotationX(radians, out rotationMatrix);
                Matrix4.CreateTranslation(0, 0, distance, out translationMatrix);
            }
            else if (plane == Plane.Y)
            {
                Matrix4.CreateRotationY(radians, out rotationMatrix);
                Matrix4.CreateTranslation(0, 0, distance, out translationMatrix);
            }
            else if (plane == Plane.Z)
            {
                Matrix4.CreateRotationZ(radians, out rotationMatrix);
                Matrix4.CreateTranslation(distance, 0, 0, out translationMatrix);
            }
            else if(plane == Plane.Camera)
            {
                if(KWEngine.CurrentWorld != null)
                {
                    Vector3 camLookAt;
                    if (KWEngine.CurrentWorld.IsFirstPersonMode)
                    {
                        camLookAt = KWEngine.CurrentWorld.GetFirstPersonObject().GetLookAtVector();
                    }
                    else
                    {
                        camLookAt = KWEngine.CurrentWorld.GetCameraLookAtVector();
                    }
                    
                    rotationMatrix = HelperMatrix.CreateRotationMatrixForAxisAngle(ref camLookAt, ref radians);
                    Vector3 cross = Vector3.Cross(camLookAt, KWEngine.WorldUp);
                    Matrix4.CreateTranslation(-cross.X, -cross.Y, -cross.Z, out translationMatrix);
                }
                else
                {
                    HelperGL.ShowErrorAndQuit("HelperRotation::CalculateRotationAroundPointOnAxis()", "CurrentWorld is not set. Cannot rotate around a camera axis that does not exist.");
                    return Vector3.Zero;
                }
                
            }

            Matrix4.Mult(ref translationMatrix, ref rotationMatrix, out tempMatrix);
            Matrix4.Mult(ref tempMatrix, ref translationPointMatrix, out spinMatrix);
            Vector3.TransformPosition(ref zeroVector, ref spinMatrix, out finalTranslationPoint);

            return finalTranslationPoint;
        }

        /// <summary>
        /// Berechnet die Position eines Punkts, der um einen angegeben Punkt entlang einer Achse rotiert wird
        /// </summary>
        /// <param name="point">Mittelpunkt der Rotation</param>
        /// <param name="distance">Distanz zum Mittelpunkt</param>
        /// <param name="degrees">Grad der Rotation</param>
        /// <param name="axis">Achse der Rotation (normalisiert!)</param>
        /// <returns>Position des rotierten Punkts</returns>
        public static Vector3 CalculateRotationAroundPointOnAxis(Vector3 point, float distance, float degrees, Vector3 axis)
        {
            float radians = MathHelper.DegreesToRadians(degrees % 360);
            Matrix4.CreateTranslation(ref point, out translationPointMatrix);

            rotationMatrix = HelperMatrix.CreateRotationMatrixForAxisAngle(ref axis, ref radians);
            Vector3 cross = Vector3.Cross(axis, KWEngine.WorldUp);
            Matrix4.CreateTranslation(-cross.X, -cross.Y, -cross.Z, out translationMatrix);
            
            Matrix4.Mult(ref translationMatrix, ref rotationMatrix, out tempMatrix);
            Matrix4.Mult(ref tempMatrix, ref translationPointMatrix, out spinMatrix);
            Vector3.TransformPosition(ref zeroVector, ref spinMatrix, out finalTranslationPoint);

            return finalTranslationPoint;
        }

        /// <summary>
        /// Erfragt die Rotation, die nötig wäre, damit eine Quelle zu einem Ziel guckt
        /// </summary>
        /// <param name="source">Quellposition</param>
        /// <param name="target">Zielposition</param>
        /// <returns>Rotation</returns>
        public static Quaternion GetRotationForPoint(Vector3 source, Vector3 target)
        {
            target.X += 0.000001f;
            Matrix3 lookAt = new Matrix3(Matrix4.LookAt(target, source, KWEngine.WorldUp));
            lookAt.Transpose();
            lookAt.Invert();

            return Quaternion.FromMatrix(lookAt);
        }

        /// <summary>
        /// Gibt die Rotation für ein Objekt an, die es benötigt, um in Richtung der Kamera ausgerichtet zu sein (Billboarding)
        /// </summary>
        /// <returns>Rotation</returns>
        internal static Quaternion GetRotationTowardsCamera(GLWindow w)
        {
            return Quaternion.FromMatrix(w._viewMatrixTransposedInverted);
        }

        /// <summary>
        /// Rotiert einen Vektor mit Hilfe der angegebenen Quaternion (Hamilton-Produkt)
        /// </summary>
        /// <param name="source">zu rotierender Vektor</param>
        /// <param name="rotation">Rotation als Quaternion</param>
        /// <returns>rotierter Vektor</returns>
        private static Vector3 RotateVector(Vector3 source, Quaternion rotation)
        {
            Quaternion qri;
            Quaternion.Invert(ref rotation, out qri);
            Quaternion sourceQ = new Quaternion(source, 0);
            return (rotation * sourceQ * qri).Xyz;
        }

        /// <summary>
        /// Berechnet ein Quaternion aus den übergebenen Achsenrotationen (in Grad).
        /// (Die Rotationsreihenfolge ist Z -> Y -> X)
        /// </summary>
        /// <param name="x">x-Achsenrotation (in Grad)</param>
        /// <param name="y">y-Achsenrotation (in Grad)</param>
        /// <param name="z">z-Achsenrotation (in Grad)</param>
        /// <returns>Kombinierte Rotation als Quaternion-Instanz</returns>
        public static Quaternion GetQuaternionForEulerDegrees(float x, float y, float z)
        {
            Quaternion tmpRotateX = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(x));
            Quaternion tmpRotateY = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(y));
            Quaternion tmpRotateZ = Quaternion.FromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(z));
            return (tmpRotateZ * tmpRotateY * tmpRotateX);
        }
    }
}
