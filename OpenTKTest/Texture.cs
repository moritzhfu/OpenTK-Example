using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageMagick;
using OpenTK.Graphics.OpenGL;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace OpenTKTest
{
    public class Texture
    {
        private int _textureObject;
        private readonly string _filename;
        private readonly TextureTarget _textureTarget;

        public Texture(TextureTarget textureTarget, string filename)
        {
            _filename = filename;
            _textureTarget = textureTarget;
        }

        public bool Load()
        {
            try
            {
                var bitmap = new MagickImage(Environment.CurrentDirectory + "\\" + _filename);

                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
                var data = bitmap;
              //  var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
               //     ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                _textureObject = GL.GenTexture();
                GL.BindTexture(_textureTarget, _textureObject);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                    PixelFormat.Bgra, PixelType.UnsignedByte, data.GetPixels().GetValues());

              //  bitmap.UnlockBits(data);

                GL.TexParameter(_textureTarget, TextureParameterName.TextureMinFilter, (float) TextureMinFilter.Linear);
                GL.TexParameter(_textureTarget, TextureParameterName.TextureMagFilter, (float) TextureMinFilter.Linear);
                GL.BindTexture(_textureTarget, 0);

            }
            catch (FileNotFoundException)
            {
                return false;
            }

            return true;
        }


        public void Bind(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(_textureTarget, _textureObject);
        }
    }

}