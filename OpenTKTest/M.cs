using System;
using OpenTK;

namespace OpenTKTest
{
    public static class M
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            return val.CompareTo(max) > 0 ? max : val;
        }

        public static void CreateTranslation(float x, float y, float z, out Matrix4 result)
        {
            result = Matrix4.Identity;

            // Row order notation
            // result.Row3 = new double4(x, y, z, 1);

            // Column order notation
            result.M14 = x;
            result.M24 = y;
            result.M34 = z;

        }
        public static void CreateScale(float scale, out Matrix4 result)
        {
            result = Matrix4.Identity;
            result.M11 = scale;
            result.M22 = scale;
            result.M33 = scale;
        }
    }
}