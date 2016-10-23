using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
                var bitmap = new Bitmap(Environment.CurrentDirectory + "\\" + _filename);
               
                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
                _textureObject = GL.GenTexture();
                GL.BindTexture(_textureTarget, _textureObject);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                  PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                bitmap.UnlockBits(data);

                GL.TexParameter(_textureTarget, TextureParameterName.TextureMinFilter, (float) TextureMinFilter.Linear);
                GL.TexParameter(_textureTarget, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);
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
/*
        public static TextureImage LoadImage(Stream file)
        {
            Bitmap bmp = BitmapFactory.DecodeStream(file, null, new BitmapFactory.Options { InPremultiplied = false });

            int nPixels = bmp.Width * bmp.Height;
            int nBytes = nPixels * 4;
            int[] pxls = new int[nPixels];
            bmp.GetPixels(pxls, 0, bmp.Width, 0, 0, bmp.Width, bmp.Height);

            var ret = new ImageData
            {
                PixelData = new byte[nBytes],
                Height = bmp.Height,
                Width = bmp.Width,
                PixelFormat = ImagePixelFormat.RGBA,
                Stride = bmp.Width
            };

            // Flip upside down
            for (int iLine = 0; iLine < ret.Height; iLine++)
            {
                Buffer.BlockCopy(pxls, (bmp.Height - 1 - iLine) * bmp.Width * 4, ret.PixelData, iLine * bmp.Width * 4, bmp.Width * 4);
            }

            // As a whole... Buffer.BlockCopy(pxls, 0, ret.PixelData, 0, nBytes);
            return ret;
        } */
    }

    // TODO: Fix this

    internal static class ImageHelper
    {    
        // extension method
        public static byte[] ImageToByteArray(this Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, image.RawFormat);
                return ms.ToArray();
            }
        }
    }

 
}