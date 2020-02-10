﻿using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace KWEngine2.Helper
{
    /// <summary>
    /// Helferklasse für Mathefunktionen
    /// </summary>
    public static class HelperGL
    {
        /// <summary>
        /// Beschneidet Werte
        /// </summary>
        /// <param name="v">Wert</param>
        /// <param name="l">Untergrenze</param>
        /// <param name="u">Obergrenze</param>
        /// <returns></returns>
        public static float Clamp(float v, float l, float u)
        {
            if (v < l)
                return l;
            else if (v > u)
                return u;
            else
                return v;
        }

        /// <summary>
        /// Gibt die OpenGL-Fehlercodes des aktuellen Frames auf der Debugkonsole aus.
        /// </summary>
        /// <returns>true, wenn es Fehler gibt</returns>
        public static bool CheckGLErrors()
        {
            bool hasError = false;
            ErrorCode c;
            while ((c = GL.GetError()) != ErrorCode.NoError)
            {
                hasError = true;
                Debug.WriteLine(c.ToString());
            }
            return hasError;
        }

        internal static Vector3 UnProject(this Vector3 mouse, Matrix4 projection, Matrix4 view, int width, int height)
        {
            Vector4 vec;

            vec.X = 2.0f * mouse.X / (float)width - 1;
            vec.Y = -(2.0f * mouse.Y / (float)height - 1);
            vec.Z = mouse.Z;
            vec.W = 1.0f;
            Matrix4 viewInv;
            Matrix4 projInv;
            try
            {
                viewInv = Matrix4.Invert(view);
                projInv = Matrix4.Invert(projection);
            }
            catch (Exception)
            {
                return Vector3.Zero;
            }

            Vector4.Transform(ref vec, ref projInv, out vec);
            Vector4.Transform(ref vec, ref viewInv, out vec);

            if (vec.W > 0.000001f || vec.W < -0.000001f)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return vec.Xyz;
        }

        internal static Vector2 GetNormalizedMouseCoords(float mousex, float mousey, GLWindow window)
        {
            float x;
            float y;
            if (window.WindowState != WindowState.Fullscreen)
            {
                x = mousex - window.X - 8; // TODO: Find out why '8' ;-)
                y = mousey - window.Y - SystemInformation.CaptionHeight - 8;
            }
            else
            {
                x = mousex;
                y = mousey;
            }

            return new Vector2(x, y);
        }
    }
}
