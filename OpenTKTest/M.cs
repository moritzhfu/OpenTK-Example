using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace OpenTKTest
{
    public static class M
    {
        public static float WorldScaleFactor = 0f;
        private static Matrix4 _worldMatrix = Matrix4.Identity;


        public static Matrix4 WorldMatrix
        {
            set { _worldMatrix = value; }
            get
            {
                _worldMatrix[0, 3] = (float) Math.Sin(WorldScaleFactor);
                return _worldMatrix;
            }
         
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            return val.CompareTo(max) > 0 ? max : val;
        }
    }
}