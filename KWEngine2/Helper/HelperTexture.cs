using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using OpenTK.Graphics.OpenGL4;


namespace KWEngine2.Helper
{
    internal static class HelperTexture
    {

        public static int RoundToPowerOf2(int value)
        {
            if (value < 0)
            {
                throw new Exception("Negative values are not allowed.");
            }

            uint v = (uint)value;

            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;

            return (int)v;
        }

        internal static int LoadTextureFromAssembly(string resourceName, Assembly assembly)
        {
            int texID = -1;
            using (Stream s = assembly.GetManifestResourceStream(resourceName))
            {
                Bitmap image = new Bitmap(s);
                if (image == null)
                {
                    throw new Exception("File " + resourceName + " is not a valid image file.");
                }
                texID = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texID);
                BitmapData data = null;

                if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                {
                    data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                     OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                }
                else
                {
                    data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, data.Width, data.Height, 0,
                     OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
                }
                
                GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, GLWindow.CurrentWindow.AnisotropicFiltering);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                image.Dispose();
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
            return texID;
        }

        internal static int LoadTextureInternal(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "KWEngine2.Assets.Textures." + filename;
            return LoadTextureFromAssembly(resourceName, assembly);
        }

        public static int LoadTextureForModelExternal(string filename, bool convertRoughnessToSpecular = false)
        {
            if (!File.Exists(filename))
            {
                return -1;
            }

            int texID;
            try
            {
                Bitmap image = new Bitmap(filename);
                if (image == null)
                {
                    throw new Exception("File " + filename + " is not a valid image file.");
                }
                texID =  GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texID);
                BitmapData data = null;

                if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                {
                    data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                     OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                }
                else
                {
                    data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, data.Width, data.Height, 0,
                     OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
                }

                GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, GLWindow.CurrentWindow.AnisotropicFiltering);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                image.Dispose();
                GL.BindTexture(TextureTarget.Texture2D, 0);

            }
            catch (Exception ex)
            {
                throw new Exception("Could not load image file " + filename + "! Make sure to copy it to the correct output directory. " + "[" + ex.Message + "]");
            }
            return texID;
        }

        public static int LoadTextureForModelGLB(byte[] rawTextureData, bool convertRoughnessToSpecular = false)
        {
            int texID;
            try
            {
                using (MemoryStream ms = new MemoryStream(rawTextureData))
                {
                    Bitmap image = new Bitmap(ms);
                    if (image == null)
                    {
                        throw new Exception("Image data inside GLB file corrupted. No valid image data found.");
                    }
                    texID = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, texID);
                    BitmapData data = null;

                    if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                    {
                        data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                         OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    }
                    else
                    {
                        data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, data.Width, data.Height, 0,
                         OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
                    }

                    GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, GLWindow.CurrentWindow.AnisotropicFiltering);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                    image.Dispose();
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not load image file inside GLB. [" + ex.Message + "]");
            }
            return texID;
        }

        public static int LoadTextureForModelInternal(string filename, bool convertRoughnessToSpecular = false)
        {
            Assembly a = Assembly.GetEntryAssembly();
            int texID;
            try
            {
                string assPath = a.GetName().Name + "." + filename;
                using (Stream s = a.GetManifestResourceStream(assPath))
                {
                    Bitmap image = new Bitmap(s);
                    if (image == null)
                    {
                        throw new Exception("File " + filename + " is not a valid image file.");
                    }
                    texID = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, texID);
                    BitmapData data = null;

                    if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                    {
                        data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                         OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    }
                    else
                    {
                        data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, data.Width, data.Height, 0,
                         OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
                    }

                    GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, GLWindow.CurrentWindow.AnisotropicFiltering);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                    image.Dispose();
                    GL.BindTexture(TextureTarget.Texture2D, 0);

                }
            }
            catch (Exception)
            {
                throw new Exception("Could not load image file from assembly: " + filename);
            }
            return texID;
        }

        public static int LoadTextureForBackgroundInternal(string assemblyPathAndName)
        {
            Assembly a = Assembly.GetEntryAssembly();
            int texID;
            try
            {
                using (Stream s = a.GetManifestResourceStream(a.GetName().Name + "." + assemblyPathAndName))
                {
                    
                    Bitmap image = new Bitmap(s);
                    if (image == null)
                    {
                        throw new Exception("File " + assemblyPathAndName + " not found or not a valid image file.");
                    }
                    texID = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, texID);
                    BitmapData data = null;

                    if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                    {
                        data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                         OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    }
                    else
                    {
                        data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, data.Width, data.Height, 0,
                         OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
                    }

                    //GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, GLWindow.CurrentWindow.AnisotropicFiltering);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                    //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                    image.Dispose();
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }
            }
            catch (Exception)
            {
                throw new Exception("Could not load image file " + assemblyPathAndName + "!");
            }
            return texID;
        }

        public static int LoadTextureForBackgroundExternal(string filename)
        {
            if (!File.Exists(filename))
            {
                Debug.WriteLine("File " + filename + " for setting background image not found.");
                return -1;
            }
            int texID;
            try
            {
                Bitmap image = new Bitmap(filename);
                if (image == null)
                {
                    throw new Exception("File " + filename + " is not a valid image file.");
                }
                texID = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texID);
                BitmapData data = null;

                if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                {
                    data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                     OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                }
                else
                {
                    data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, data.Width, data.Height, 0,
                     OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
                }

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                image.Dispose();
                GL.BindTexture(TextureTarget.Texture2D, 0);

            }
            catch (Exception ex)
            {
                throw new Exception("Could not load image file " + filename + "! Make sure to copy it to the correct output directory. " + "[" + ex.Message + "]");
            }
            return texID;
        }

        internal static int LoadTextureSkybox(string filename, bool isInAssembly = false)
        {
            Assembly a = Assembly.GetEntryAssembly();
            if (!filename.ToLower().EndsWith("jpg") && !filename.ToLower().EndsWith("jpeg") && !filename.ToLower().EndsWith("png"))
                throw new Exception("Only JPG and PNG files are supported.");

            if (!KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(filename))
            {
                try
                {
                    using (Stream s = isInAssembly ? a.GetManifestResourceStream(a.GetName().Name + "." + filename) : File.Open(filename, FileMode.Open))
                    {
                        Bitmap image = new Bitmap(s);
                        int width = image.Width;
                        int height = image.Height;
                        int height_onethird = height / 3;
                        int width_onequarter = width / 4;

                        Bitmap image_front = new Bitmap(width_onequarter, height_onethird, image.PixelFormat);
                        Bitmap image_back = new Bitmap(width_onequarter, height_onethird, image.PixelFormat);
                        Bitmap image_up = new Bitmap(width_onequarter, height_onethird, image.PixelFormat);
                        Bitmap image_down = new Bitmap(width_onequarter, height_onethird, image.PixelFormat);
                        Bitmap image_left = new Bitmap(width_onequarter, height_onethird, image.PixelFormat);
                        Bitmap image_right = new Bitmap(width_onequarter, height_onethird, image.PixelFormat);

                        Graphics g = null;
                        //front
                        g = Graphics.FromImage(image_front);
                        g.DrawImage(image,
                            new Rectangle(0, 0, width_onequarter, height_onethird),
                            new Rectangle(2 * width_onequarter, height_onethird, width_onequarter, height_onethird),
                            GraphicsUnit.Pixel
                            );
                        g.Dispose();

                        //back
                        g = Graphics.FromImage(image_back);
                        g.DrawImage(image,
                            new Rectangle(0, 0, width_onequarter, height_onethird),
                            new Rectangle(0, height_onethird, width_onequarter, height_onethird),
                            GraphicsUnit.Pixel
                            );
                        g.Dispose();

                        //up
                        g = Graphics.FromImage(image_up);
                        g.DrawImage(image,
                            new Rectangle(0, 0, width_onequarter, height_onethird),
                            new Rectangle(width_onequarter, 0, width_onequarter, height_onethird),
                            GraphicsUnit.Pixel
                            );
                        g.Dispose();

                        //down
                        g = Graphics.FromImage(image_down);
                        g.DrawImage(image,
                            new Rectangle(0, 0, width_onequarter, height_onethird),
                            new Rectangle(width_onequarter, 2 * height_onethird, width_onequarter, height_onethird),
                            GraphicsUnit.Pixel
                            );
                        g.Dispose();

                        //left
                        g = Graphics.FromImage(image_left);
                        g.DrawImage(image,
                            new Rectangle(0, 0, width_onequarter, height_onethird),
                            new Rectangle(width_onequarter, height_onethird, width_onequarter, height_onethird),
                            GraphicsUnit.Pixel
                            );
                        g.Dispose();

                        //right
                        g = Graphics.FromImage(image_right);
                        g.DrawImage(image,
                            new Rectangle(0, 0, width_onequarter, height_onethird),
                            new Rectangle(3 * width_onequarter, height_onethird, width_onequarter, height_onethird),
                            GraphicsUnit.Pixel
                            );
                        g.Dispose();

                        int newTexture = GL.GenTexture();
                        GL.BindTexture(TextureTarget.TextureCubeMap, newTexture);
                        BitmapData data = null;

                        PixelInternalFormat iFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? PixelInternalFormat.Rgb : PixelInternalFormat.Rgba;
                        OpenTK.Graphics.OpenGL4.PixelFormat pxFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? OpenTK.Graphics.OpenGL4.PixelFormat.Bgr : OpenTK.Graphics.OpenGL4.PixelFormat.Bgra;

                        // front
                        data = image_front.LockBits(new Rectangle(0, 0, image_front.Width, image_front.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX, 0, iFormat, data.Width, data.Height, 0, pxFormat, PixelType.UnsignedByte, data.Scan0);
                        image_front.UnlockBits(data);

                        // back
                        data = image_back.LockBits(new Rectangle(0, 0, image_back.Width, image_back.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                        GL.TexImage2D(TextureTarget.TextureCubeMapNegativeX, 0, iFormat, data.Width, data.Height, 0, pxFormat, PixelType.UnsignedByte, data.Scan0);
                        image_back.UnlockBits(data);

                        // up
                        data = image_up.LockBits(new Rectangle(0, 0, image_up.Width, image_up.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveY, 0, iFormat, data.Width, data.Height, 0, pxFormat, PixelType.UnsignedByte, data.Scan0);
                        image_up.UnlockBits(data);

                        // down
                        data = image_down.LockBits(new Rectangle(0, 0, image_down.Width, image_down.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                        GL.TexImage2D(TextureTarget.TextureCubeMapNegativeY, 0, iFormat, data.Width, data.Height, 0, pxFormat, PixelType.UnsignedByte, data.Scan0);
                        image_down.UnlockBits(data);

                        // left
                        data = image_left.LockBits(new Rectangle(0, 0, image_left.Width, image_left.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveZ, 0, iFormat, data.Width, data.Height, 0, pxFormat, PixelType.UnsignedByte, data.Scan0);
                        image_left.UnlockBits(data);

                        // right
                        data = image_right.LockBits(new Rectangle(0, 0, image_right.Width, image_right.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                        GL.TexImage2D(TextureTarget.TextureCubeMapNegativeZ, 0, iFormat, data.Width, data.Height, 0, pxFormat, PixelType.UnsignedByte, data.Scan0);
                        image_right.UnlockBits(data);

                        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

                        KWEngine.CustomTextures[KWEngine.CurrentWorld].Add(filename, newTexture);

                        image.Dispose();
                        image_front.Dispose();
                        image_back.Dispose();
                        image_up.Dispose();
                        image_down.Dispose();
                        image_left.Dispose();
                        image_right.Dispose();
                        GL.BindTexture(TextureTarget.TextureCubeMap, 0);

                        return newTexture;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error loading skybox texture: " + filename + " (" + ex.Message + ")");
                    return -1;
                }
            }
            else
            {
                int id = -1;
                KWEngine.CustomTextures[KWEngine.CurrentWorld].TryGetValue(filename, out id);
                return id;
            }
        }
    }
}
