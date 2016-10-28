using System;
using OpenTK.Graphics.OpenGL;

namespace OpenTKTest
{
    // ReSharper disable once InconsistentNaming
    public class ShadowMapFBO
    {
        private int _fbo;
        private int _shadowMap;

        public ShadowMapFBO()
        {
            _fbo = 0;
            _shadowMap = 0;
        }

        public bool Init(int windowWidth, int windowHeight)
        {
            // Create the FBO
            GL.GenFramebuffers(1, out _fbo);

            // Create the depth buffer
            GL.GenTextures(1, out _shadowMap);
            GL.BindTexture(TextureTarget.Texture2D, _shadowMap);
            // Set Image
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, windowWidth, windowHeight, 0, PixelFormat.DepthComponent, PixelType.HalfFloat, IntPtr.Zero);
            // Set texParameter
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToEdge);

           
            // Bind
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _shadowMap, 0);

            // Disable writes to the color buffer
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception($"Framebuffer not ready! Status {status}");
            }

            return true;
      
      
        }

        public void BindForWriting()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);
        }

        public void BindForReading(TextureUnit textureUnit)
        {
            GL.ActiveTexture(textureUnit);
            GL.BindTexture(TextureTarget.Texture2D, _shadowMap);
        }
    }
}