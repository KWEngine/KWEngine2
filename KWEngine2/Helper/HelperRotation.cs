﻿using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KWEngine2.GameObjects.GameObject;

namespace KWEngine2.Helper
{
    public static class HelperRotation
    {
        private static Matrix4 translationPointMatrix = Matrix4.Identity;
        private static Matrix4 rotationMatrix = Matrix4.Identity;
        private static Matrix4 translationMatrix = Matrix4.Identity;
        private static Matrix4 tempMatrix = Matrix4.Identity;
        private static Matrix4 spinMatrix = Matrix4.Identity;
        private static Vector3 finalTranslationPoint = Vector3.Zero;
        private static Vector3 zeroVector = Vector3.Zero;

        public static float CalculateRadiansFromDegrees(float degrees)
        {
            return (float)Math.PI * degrees / 180f;
        }

        public static float CalculateDegreesFromRadians(float radiant)
        {
            return (180f * radiant) / (float)Math.PI;
        }

        public static Vector3 RotateVector(Vector3 vector, float degrees)
        {
            return Vector3.TransformNormal(vector, Matrix4.CreateRotationY(CalculateRadiansFromDegrees(degrees)));
        }

        /// <summary>
        /// Berechnet den Vektor, der entsteht, wenn der übergebene Vektor um die angegebenen Grad rotiert wird
        /// </summary>
        /// <param name="vector">zu rotierender Vektor</param>
        /// <param name="degrees">Rotation (in Grad)</param>
        /// <param name="unitVector">Einheitsvektor, um den rotiert wird</param>
        /// <returns>Rotierter Vektor</returns>
        public static Vector3 RotateVector(Vector3 vector, float degrees, Plane plane)
        {
            if (plane == Plane.X)
            {
                return Vector3.TransformNormal(vector, Matrix4.CreateRotationX(CalculateRadiansFromDegrees(degrees)));
            }
            else if (plane == Plane.Y)
            {
                return Vector3.TransformNormal(vector, Matrix4.CreateRotationZ(CalculateRadiansFromDegrees(degrees)));
            }
            else if (plane == Plane.Z)
            {
                return Vector3.TransformNormal(vector, Matrix4.CreateRotationY(CalculateRadiansFromDegrees(degrees)));
            }
            else
                throw new Exception("Only planes X, Y and Z are allowed for vector rotation.");
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

        public static Vector3 CalculateRotationAroundPointOnAxis(Vector3 point, float distance, float degrees, Plane plane)
        {
            float radians = MathHelper.DegreesToRadians(degrees % 360);
            Matrix4.CreateTranslation(ref point, out translationPointMatrix);

            if (plane == Plane.X)
            {
                Matrix4.CreateRotationX(radians, out rotationMatrix);
                Matrix4.CreateTranslation(distance, 0, 0, out translationMatrix);
            }
            else if (plane == Plane.Y)
            {
                Matrix4.CreateRotationY(radians, out rotationMatrix);
                Matrix4.CreateTranslation(0, 0, distance, out translationMatrix);
            }
            else if (plane == Plane.Z)
            {
                Matrix4.CreateRotationZ(radians, out rotationMatrix);
                Matrix4.CreateTranslation(0, distance, 0, out translationMatrix);
            }
            else
            {
                throw new Exception("Only Rotations around X, Y or Z axis are allowed.");
            }

            Matrix4.Mult(ref translationMatrix, ref rotationMatrix, out tempMatrix);
            Matrix4.Mult(ref tempMatrix, ref translationPointMatrix, out spinMatrix);


            Vector3.TransformPosition(ref zeroVector, ref spinMatrix, out finalTranslationPoint);

            return finalTranslationPoint;
        }
/*
        public static Quaternion GetRotationForPoint(Vector3 source, Vector3 target)
        {
            Matrix4 lookat = Matrix4.LookAt(source, target, KWEngine.WorldUp);
            lookat.Transpose();
            lookat.Invert();
            //Quaternion rotation = Quaternion.FromAxisAngle(KWEngine.WorldUp, (float)-Math.PI) * Quaternion.FromMatrix(new Matrix3(lookat));
            return Quaternion.FromAxisAngle(KWEngine.WorldUp, (float)Math.PI * 1.5f) * Quaternion.FromMatrix(new Matrix3(lookat));
        }
  */      
        public static Matrix4 GetRotationForPoint(Vector3 source, Vector3 target)
        {
            Matrix4 lookAt = Matrix4.LookAt(source, target, KWEngine.WorldUp);
            lookAt.Transpose();
            lookAt.Invert();

            Quaternion newRotation = Quaternion.FromMatrix(new Matrix3(lookAt));
            //Quaternion quarterRotation = Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI / -2.0f);
            //newRotation = newRotation * quarterRotation;
            return Matrix4.CreateFromQuaternion(newRotation);

            //return mOldRotationMatrix;
        }


    }
}
