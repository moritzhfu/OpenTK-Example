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
                var image = new MagickImage(Environment.CurrentDirectory + "\\" + _filename);
                image.Write(_filename + ".bmp");
                // Write to stream
                var settings = new MagickReadSettings
                {
                    Width = 800,
                    Height = 600
                };

                var memStream = new MemoryStream();
                // Create image that is completely purple and 800x600
                var imageTmp = new MagickImage("xc:purple", settings);
                // Sets the output format to png
                imageTmp.Format = MagickFormat.Bgra;
                // Write the image to the memorystream
                imageTmp.Write(memStream);
                    
                
                var bitmap = new Bitmap(Environment.CurrentDirectory + "\\" + _filename + ".bmp");

                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
   
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                   ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                _textureObject = GL.GenTexture();
                GL.BindTexture(_textureTarget, _textureObject);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                    PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                bitmap.UnlockBits(data);

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