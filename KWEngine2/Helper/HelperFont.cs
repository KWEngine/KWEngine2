using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace KWEngine2.Helper
{
    internal static class HelperFont
    {
        public static string LETTERS = "";
        public const int LETTERSIZE = 64;
        public static int[] TEXTURES = null;
        internal static int LetterOffsetX = 0;
        internal static int LetterOffsetY = 0;
        internal static int LetterSizeOffset = 0;

        public static void AddFontFromResource(PrivateFontCollection privateFontCollection, Assembly a, string fontResourceName)
        {
            var fontBytes = GetFontResourceBytes(a, fontResourceName);
            var fontData = Marshal.AllocCoTaskMem(fontBytes.Length);
            Marshal.Copy(fontBytes, 0, fontData, fontBytes.Length);
            privateFontCollection.AddMemoryFont(fontData, fontBytes.Length);
            // Marshal.FreeCoTaskMem(fontData); // bug?
        }

        public static void AddFontFromFile(PrivateFontCollection privateFontCollection, string filename)
        {
            if (File.Exists(filename))
            {
               
                privateFontCollection.AddFontFile(filename);
            }
            else
            {
                throw new Exception("Invalid font file: " + filename);
            }
        }

        private static byte[] GetFontResourceBytes(Assembly assembly, string fontResourceName)
        {
            var resourceStream = assembly.GetManifestResourceStream(fontResourceName);
            if (resourceStream == null)
                throw new Exception(string.Format("Unable to find font '{0}' in embedded resources.", fontResourceName));
            var fontBytes = new byte[resourceStream.Length];
            resourceStream.Read(fontBytes, 0, (int)resourceStream.Length);
            resourceStream.Close();
            return fontBytes;
        }

        public static int GenerateTexture(string resourceName, Assembly a)
        {
            Stream resourceStream = a.GetManifestResourceStream(resourceName);
            return HelperTexture.LoadTextureCompressedNoMipMap(resourceStream);
        }

        public static void GenerateTextures(int sizeOffset = 0, int offsetX = 0, int offsetY = 0)
        {
            for (byte i = 32; i < 255; i++)
            {
                LETTERS += Convert.ToChar(i);
            }
            TEXTURES = new int[LETTERS.Length];

            LetterOffsetX = offsetX;
            LetterOffsetY = offsetY;
            LetterSizeOffset = sizeOffset;

            GL.DeleteTextures(TEXTURES.Length, TEXTURES);
            Thread.Sleep(32);

            for (int i = 0; i < LETTERS.Length; i++)
            {
                TEXTURES[i] = LoadCharacter(LETTERS[i]);
            }
        }

        private static int LoadCharacter(char c)
        {
            Bitmap b = new Bitmap(LETTERSIZE + LetterSizeOffset, LETTERSIZE + LetterSizeOffset, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            FontFamily family = KWEngine.Collection.Families[0];
            Font f = new Font(family, LETTERSIZE + LetterSizeOffset, FontStyle.Regular, GraphicsUnit.Pixel);
            Graphics g = Graphics.FromImage(b);
            SolidBrush alpha = new SolidBrush(Color.FromArgb(0, 0, 0, 0));
            g.FillRectangle(alpha, 0, 0, LETTERSIZE + LetterSizeOffset, LETTERSIZE + LetterSizeOffset);
            SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
            string tmpString = c.ToString();
            g.DrawString(tmpString, f, brush, 0 + LetterOffsetX, -15 - LetterOffsetY);
            f.Dispose();
            brush.Dispose();
            alpha.Dispose();
            g.Dispose();
            int texID = LoadTexture(b);
            b.Dispose();
            return texID;
        }

        private static int LoadTexture(Bitmap image)
        {
            int texID = GL.GenTexture();
           
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.BindTexture(TextureTarget.Texture2D, texID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            image.UnlockBits(data);
            return texID;
        }
    }
}
