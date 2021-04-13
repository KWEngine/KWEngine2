using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2.Helper
{
    /// <summary>
    /// Hilfsklasse für das Erstellen eigener Skyboxes bzw. Environment Maps
    /// </summary>
    public static class HelperSkybox
    {
        internal static int _resolution = 4096;
        internal static int _fbFull = -1;
        internal static int _fbDownsample = -1;
        internal static int _tex0 = -1;
        internal static int _tex1 = -1;
        internal static int _tex0d = -1;
        internal static int _tex1d = -1;
        internal static int _texDepth = -1;

        internal static int _fbBloom1 = -1;
        internal static int _fbBloom2 = -1;
        internal static int _fbBloomTex0 = -1;
        internal static int _fbBloomTex1 = -1;

        internal static Matrix4[] _viewMatrixSkybox = new Matrix4[6];
        internal static Matrix4 _projectionMatrixSkybox = Matrix4.Identity;

        internal static void DeleteFramebuffer()
        {
            GL.DeleteFramebuffer(_fbFull);
            GL.DeleteFramebuffer(_fbDownsample);
            GL.DeleteFramebuffer(_fbBloom1);
            GL.DeleteFramebuffer(_fbBloom2);
            GL.Flush();
            GL.Finish();

            GL.DeleteTexture(_tex0);
            GL.DeleteTexture(_tex1);
            GL.DeleteTexture(_tex0d);
            GL.DeleteTexture(_tex1d);
            GL.DeleteTexture(_fbBloomTex0);
            GL.DeleteTexture(_fbBloomTex1);
            GL.DeleteTexture(_texDepth);
            GL.Flush();
            GL.Finish();
        }

        internal static void CreateFramebuffer()
        {
            _fbFull = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbFull);

            int renderedTexture = GL.GenTexture();
            int renderedTextureAttachment = GL.GenTexture();
            int depthTexId = GL.GenTexture();

            GL.DrawBuffers(2, new DrawBuffersEnum[2] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });

            GL.BindTexture(TextureTarget.Texture2D, renderedTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                _resolution, _resolution, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            //render buffer fsaa:
            int renderbufferFSAA = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbufferFSAA);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 8, RenderbufferStorage.Rgba8, _resolution, _resolution);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, renderbufferFSAA);

            GL.BindTexture(TextureTarget.Texture2D, renderedTextureAttachment);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                _resolution, _resolution, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            int renderbufferFSAA2 = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbufferFSAA2);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 8, RenderbufferStorage.Rgba8, _resolution, _resolution);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, RenderbufferTarget.Renderbuffer, renderbufferFSAA2);

            // depth buffer fsaa:
            int depthRenderBuffer = GL.GenRenderbuffer();
            GL.BindTexture(TextureTarget.Texture2D, depthTexId);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32,
                _resolution, _resolution, 0, OpenTK.Graphics.OpenGL4.PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, new int[] { (int)TextureCompareMode.CompareRefToTexture });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, new int[] { (int)DepthFunction.Lequal });
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthTexId, 0);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRenderBuffer);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 8, RenderbufferStorage.DepthComponent32, _resolution, _resolution);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthRenderBuffer);

            FramebufferErrorCode code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                HelperGL.ShowErrorAndQuit("HelperSkybox::CreateFramebuffer()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                return;
            }
            else
            {
                _tex0 = renderedTexture;
                _tex1 = renderedTextureAttachment;
                _texDepth = depthTexId;
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);


            // Downsample fb:
            _fbDownsample = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbDownsample);

            // Init der Textur auf die gerendet wird:
            renderedTexture = GL.GenTexture();
            renderedTextureAttachment = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, renderedTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                _resolution, _resolution, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);


            GL.BindTexture(TextureTarget.Texture2D, renderedTextureAttachment);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                _resolution, _resolution, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            //Konfig. des frame buffer:
            GL.DrawBuffers(2, new DrawBuffersEnum[2] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderedTexture, 0);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, renderedTextureAttachment, 0);

            code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                HelperGL.ShowErrorAndQuit("HelperSkybox::CreateFramebuffer()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                return;
            }
            else
            {
                _tex0d = renderedTexture;
                _tex1d = renderedTextureAttachment;
            }
        }

        internal static void InitFramebufferBloom()
        {
            int framebufferTempId;
            int renderedTextureTemp;

            // =========== TEMP ===========

            //Init des frame buffer:
            framebufferTempId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferTempId);

            // Init der Textur auf die gerendet wird:
            renderedTextureTemp = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, renderedTextureTemp);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                _resolution, _resolution, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            //Konfig. des frame buffer:
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderedTextureTemp, 0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            FramebufferErrorCode code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                HelperGL.ShowErrorAndQuit("HelperSkybox::InitFramebufferBloom()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                return;
            }
            else
            {
                _fbBloom1 = framebufferTempId;
                _fbBloomTex0 = renderedTextureTemp;
            }

            // =========== TEMP 2 ===========

            //Init des frame buffer:
            int framebufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            // Init der Textur auf die gerendet wird:
            int renderedTextureTemp2 = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, renderedTextureTemp2);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                _resolution, _resolution, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);
            //Konfig. des frame buffer:
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderedTextureTemp2, 0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                HelperGL.ShowErrorAndQuit("HelperSkybox::InitFramebufferBloom()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                return;
            }
            else
            {
                _fbBloom2 = framebufferId;
                _fbBloomTex1 = renderedTextureTemp2;
            }
        }

        /// <summary>
        /// Erstellt eine Skybox (oder Environment Map) für die aktuelle Szene als Bitmap
        /// </summary>
        /// <param name="resolution">Auflösung der Skybox (Standard: 4096)</param>
        public static void CreateSkyboxFromCurrentSceneAndExit(int resolution = 4096)
        {
            _resolution = HelperTexture.RoundDownToPowerOf2(resolution);
            KWEngine.CurrentWorld.AddRemoveObjects();

            CreateFramebuffer();
            InitFramebufferBloom();

            Matrix4 modelViewProjectionMatrixBloom = Matrix4.CreateScale(_resolution, _resolution, 1) * Matrix4.LookAt(0, 0, 1, 0, 0, 0, 0, 1, 0) * Matrix4.CreateOrthographic(_resolution, _resolution, 0.1f, 100f);

            Vector3 camPosition = KWEngine.CurrentWorld.GetCameraPositionEitherWay();
            _viewMatrixSkybox[0] = Matrix4.LookAt(camPosition, camPosition + new Vector3(1, 0, 0), new Vector3(0, -1, 0));  // right
            _viewMatrixSkybox[1] = Matrix4.LookAt(camPosition, camPosition + new Vector3(-1, 0, 0), new Vector3(0, -1, 0)); // left
            _viewMatrixSkybox[2] = Matrix4.LookAt(camPosition, camPosition + new Vector3(0, 1, 0), new Vector3(0, 0, 1));   // top
            _viewMatrixSkybox[3] = Matrix4.LookAt(camPosition, camPosition + new Vector3(0, -1, 0), new Vector3(0, 0, -1)); // bottom
            _viewMatrixSkybox[4] = Matrix4.LookAt(camPosition, camPosition + new Vector3(0, 0, 1), new Vector3(0, -1, 0));  // front
            _viewMatrixSkybox[5] = Matrix4.LookAt(camPosition, camPosition + new Vector3(0, 0, -1), new Vector3(0, -1, 0)); // back

            _projectionMatrixSkybox = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(90),
                1,
                0.1f,
                10000);
            Bitmap[] bitmaps = new Bitmap[6];

            for(int s = 0; s < _viewMatrixSkybox.Length; s++)
            {
                KWEngine.CurrentWindow.DrawSceneForSkybox(_viewMatrixSkybox[s], _projectionMatrixSkybox, s);

                // Downsample framebuffer:
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbFull);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbDownsample);

                GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                GL.BlitFramebuffer(0, 0, _resolution, _resolution, 0, 0, _resolution, _resolution, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);

                GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
                GL.DrawBuffer(DrawBufferMode.ColorAttachment1);
                GL.BlitFramebuffer(0, 0, _resolution, _resolution, 0, 0, _resolution, _resolution, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
                
                // Draw bloom:
                GL.UseProgram(KWEngine.RendererBloom.GetProgramId());
                GL.Viewport(0, 0, _resolution, _resolution);
                int sourceTex; // this is the texture that the bloom will be read from
                for (int i = 0; i < 4; i++)
                {
                    if (i % 2 == 0)
                    {
                        if (i == 0)
                            GLWindow.SwitchToBufferAndClear(_fbBloom1);
                        else
                            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbBloom1);
                        if (i == 0)
                            sourceTex = _tex1d;
                        else
                            sourceTex = _fbBloomTex1;
                    }
                    else
                    {
                        sourceTex = _fbBloomTex0;
                        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbBloom2);
                    }

                    KWEngine.RendererBloom.DrawBloom(
                        KWEngine.KWRect,
                        ref modelViewProjectionMatrixBloom,
                        i % 2 == 0,
                        sourceTex
                    );
                }

                GLWindow.SwitchToBufferAndClear(_fbDownsample);
                GL.UseProgram(KWEngine.RendererMerge.GetProgramId());
                GL.Viewport(0, 0, _resolution, _resolution);
                KWEngine.RendererMerge.DrawMerge(KWEngine.KWRect, ref modelViewProjectionMatrixBloom, _tex0d, _fbBloomTex1);
                GL.UseProgram(0); // unload bloom shader program
                

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbDownsample);
                GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
                bitmaps[s] = new Bitmap(_resolution, _resolution);
                BitmapData bd = bitmaps[s].LockBits(new Rectangle(0, 0, _resolution, _resolution), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                GL.ReadPixels(0, 0, _resolution, _resolution, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bd.Scan0);
                bitmaps[s].UnlockBits(bd);
            }

            // make 6img:
            Rectangle srcRect = new Rectangle(0, 0, _resolution, _resolution);
            Bitmap finalBitmap = new Bitmap(_resolution * 4, _resolution * 3);
            // left?:
            using (Graphics grD = Graphics.FromImage(finalBitmap))
            {
                grD.DrawImage(bitmaps[1], new Rectangle(0, _resolution, _resolution, _resolution), srcRect, GraphicsUnit.Pixel);
            }
            // front?:
            using (Graphics grD = Graphics.FromImage(finalBitmap))
            {
                grD.DrawImage(bitmaps[4], new Rectangle(_resolution, _resolution, _resolution, _resolution), srcRect, GraphicsUnit.Pixel);
            }
            // right?:
            using (Graphics grD = Graphics.FromImage(finalBitmap))
            {
                grD.DrawImage(bitmaps[0], new Rectangle(_resolution * 2, _resolution, _resolution, _resolution), srcRect, GraphicsUnit.Pixel);
            }
            // back?:
            using (Graphics grD = Graphics.FromImage(finalBitmap))
            {
                grD.DrawImage(bitmaps[5], new Rectangle(_resolution * 3, _resolution, _resolution, _resolution), srcRect, GraphicsUnit.Pixel);
            }
            // top?:
            using (Graphics grD = Graphics.FromImage(finalBitmap))
            {
                grD.DrawImage(bitmaps[2], new Rectangle(_resolution, 0, _resolution, _resolution), srcRect, GraphicsUnit.Pixel);
            }
            // bottom?:
            using (Graphics grD = Graphics.FromImage(finalBitmap))
            {
                grD.DrawImage(bitmaps[3], new Rectangle(_resolution, _resolution * 2, _resolution, _resolution), srcRect, GraphicsUnit.Pixel);
            }
            string fileaffix = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            finalBitmap.Save(fileaffix + "-environmentmap.png", ImageFormat.Png);
            finalBitmap.Dispose();


            DeleteFramebuffer();
            KWEngine.CurrentWindow.ForceClose();
        }
    }
}
