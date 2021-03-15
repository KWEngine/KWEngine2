using KWEngine2.GameObjects;
using OpenTK;
using System;

namespace KWEngine2.Helper
{
    internal static class HelperCamera
    {
        /// <summary>
        /// Initialorientierung
        /// </summary>
        private static Vector3 mOrientation = new Vector3(0f, 0f, 0f);

        private static GameObject mCurrentGameObject = null;

        /// <summary>
        /// Berechnet den aktuellen Richtungsvektor anhand der Kamerasicht
        /// </summary>
        /// <returns>Richtungsvektor</returns>
        public static Vector3 GetLookAtVector()
        {
            Vector3 lookat = new Vector3();
            lookat.X = (float)(Math.Sin((float)mOrientation.X) * Math.Cos((float)mOrientation.Y));
            lookat.Y = (float)Math.Sin((float)mOrientation.Y);
            lookat.Z = (float)(Math.Cos((float)mOrientation.X) * Math.Cos((float)mOrientation.Y));
            //lookat.NormalizeFast();
            return lookat;
        }

        /// <summary>
        /// Berechent die aktuelle View-Matrix
        /// </summary>
        /// <returns>View-Matrix (4x4)</returns>
        public static Matrix4 GetViewMatrix(Vector3 pos)
        {
            Vector3 lookat = new Vector3();

            lookat.X = (float)(Math.Sin((float)mOrientation.X) * Math.Cos((float)mOrientation.Y));
            lookat.Y = (float)Math.Sin((float)mOrientation.Y);
            lookat.Z = (float)(Math.Cos((float)mOrientation.X) * Math.Cos((float)mOrientation.Y));

            if (mCurrentGameObject != null)
                pos.Y += mCurrentGameObject.FPSEyeOffset;

            return Matrix4.LookAt(pos, pos + lookat, KWEngine.WorldUp);
        }

        public static Matrix3 GetViewMatrixInversed()
        {
            if(mCurrentGameObject != null)
            {
                Vector3 pos = mCurrentGameObject.Position;
                Vector3 lookat = new Vector3();
                lookat.X = (float)(Math.Sin((float)mOrientation.X) * Math.Cos((float)mOrientation.Y));
                lookat.Y = (float)Math.Sin((float)mOrientation.Y);
                lookat.Z = (float)(Math.Cos((float)mOrientation.X) * Math.Cos((float)mOrientation.Y));

                pos.Y = pos.Y + mCurrentGameObject.FPSEyeOffset;

                return new Matrix3(Matrix4.LookAt(pos, pos - lookat, KWEngine.WorldUp));
            }
            else
            {
                HelperGL.ShowErrorAndQuit("HelperCamera::GetViewMatrix()", "No first person object available.");
                return Matrix3.Identity;
            }
        }

        /// <summary>
        /// Berechnet die Positionsänderung der Kamera entlang der XZ-Achsen mit den angegeben Werten.
        /// </summary>
        /// <param name="forward">-1, 0 oder +1 für Vorwärtsbewegung entlang der Sicht</param>
        /// <param name="sides">-1, 0 oder +1 für Seitwärtsbewegung</param>
        /// <param name="speed">Geschwindigkeit</param>
        /// <returns>Der Bewegungsvektor, der anschließend zu gehen wäre</returns>
        public static Vector3 MoveXZ(float forward, float sides, float speed)
        {
            Matrix4 viewMatrix = GetViewMatrix(mCurrentGameObject.Position);

            Vector3 forwardVector = new Vector3(viewMatrix.M13, 0f, viewMatrix.M33);
            Vector3 strafeVector = new Vector3(viewMatrix.M11, 0f, viewMatrix.M31);
            Vector3 relativeChange = -forward * forwardVector + sides * strafeVector;
            relativeChange.NormalizeFast();
            return relativeChange * speed;
        }

        /// <summary>
        /// Bewegt die Kamera entlang der XZ-Achsen mit den angegeben Werten.
        /// </summary>
        /// <param name="forward">-1, 0 oder +1 für Vorwärtsbewegung entlang der Sicht</param>
        /// <param name="sides">-1, 0 oder +1 für Seitwärtsbewegung</param>
        /// <param name="speed">Geschwindigkeit</param>
        /// <returns>Der Bewegungsvektor, der anschließend zu gehen wäre</returns>
        public static Vector3 MoveXYZ(float forward, float sides, float speed)
        {
            Vector3 forwardVector = new Vector3();
            Vector3 strafeVector = new Vector3();
            Matrix4 viewMatrix = GetViewMatrix(mCurrentGameObject.Position);

            // In order to speed things up, we directly get the values from the view matrix' cells:
            forwardVector = new Vector3(viewMatrix.M13, viewMatrix.M23, viewMatrix.M33);
            strafeVector = new Vector3(viewMatrix.M11, viewMatrix.M21, viewMatrix.M31);

            Vector3 relativeChange = -forward * forwardVector + sides * strafeVector;
            relativeChange.NormalizeFast();
            return relativeChange * speed;
        }

        internal static void DeleteFirstPersonObject()
        {
            mCurrentGameObject = null;
        }

        internal static void SetStartRotationY(Quaternion rotation, GameObject fpsObject)
        {
            mCurrentGameObject = fpsObject;
            rotation.ToAxisAngle(out Vector3 yAxis, out float angle);
            mOrientation.X = angle % ((float)Math.PI * 2);
            mOrientation.Y = 0;
            mOrientation.Z = 0;

        }

        internal static void SetStartRotation(GameObject fpsObject)
        {
            mCurrentGameObject = fpsObject;
            fpsObject.GetRotationNoFPSMode().ToAxisAngle(out Vector3 yAxis, out float angle);
            mOrientation.X = angle % ((float)Math.PI * 2);
            mOrientation.Y = 0;
            mOrientation.Z = 0;

            mCurrentGameObject.SetRotation(Quaternion.FromAxisAngle(KWEngine.WorldUp, mOrientation.X));
        }

        /// <summary>
        /// Fügt der aktuellen Kamerarotation neue Rotation gemäß der relativen Mausbewegung (im Verhältnis zur Bildmitte) hinzu
        /// </summary>
        /// <param name="deltaX">x-Verschiebung</param>
        /// <param name="deltaY">y-Verschiebung</param>
        public static void AddRotation(float deltaX, float deltaY)
        {
            mCurrentGameObject.AddRotationY(MathHelper.RadiansToDegrees(deltaX));

            mOrientation.X = (mOrientation.X + deltaX) % ((float)Math.PI * 2.0f);
            mOrientation.Y = Math.Max(Math.Min(mOrientation.Y + deltaY, (float)Math.PI / 2.0f - 0.1f), (float)-Math.PI / 2.0f + 0.1f);
        }
    }
}
