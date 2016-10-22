using System;
using System.Drawing;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace OpenTKTest
{
    public class Texture
    {
        private struct TextureImage
        {
            public byte[] ImageByteData;
            public int Width;
            public int Height;
        }

        private TextureImage _textureImage;
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
                    var retrivedImage = Image.FromFile(Environment.CurrentDirectory + "\\" + _filename);

                     _textureImage = new TextureImage
                        {
                            ImageByteData = retrivedImage.ImageToByteArray(),
                            Height = retrivedImage.Height,
                            Width = retrivedImage.Width
                        };


                _textureObject = GL.GenTexture();
                GL.BindTexture(_textureTarget, _textureObject);
                GL.TexImage2D(_textureTarget, 0, PixelInternalFormat.Rgba16f, _textureImage.Width, _textureImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, _textureImage.ImageByteData);

                GL.TexParameter(_textureTarget, TextureParameterName.TextureMinFilter, (float) TextureMinFilter.Linear);
                GL.TexParameter(_textureTarget, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);
                GL.BindTexture(_textureTarget, 0);

            }
            catch (FileNotFoundException e)
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