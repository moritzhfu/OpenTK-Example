using OpenTK;

namespace OpenTKTest
{
    public class FirstPersonCamera
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Quaternion Orientation;

        public Matrix4 Matrix;
        public Matrix4 Model;
        public Matrix4 Projection;

        public FirstPersonCamera()
        {
            Matrix = Matrix4.Identity;
            Projection = Matrix4.Identity;
            Orientation = Quaternion.Identity;
        }

        public void Update()
        {
            Orientation =
                Quaternion.FromAxisAngle(Vector3.UnitY, Rotation.Y) *
                Quaternion.FromAxisAngle(Vector3.UnitX, Rotation.X);

            var forward = Vector3.Transform(Vector3.UnitZ, Orientation);
            Model = Matrix4.LookAt(Position, Position + forward, Vector3.UnitY);
            Matrix = Model * Projection;
        }

        public void Resize(int width, int height)
        {
            Projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, (float)width / height, 0.1f, 1000f
                );
        }

        public void TurnX(float a)
        {
            Rotation.X += a;
            Rotation.X = MathHelper.Clamp(Rotation.X, -1.57f, 1.57f);
        }

        public void TurnY(float a)
        {
            Rotation.Y += a;
            Rotation.Y = ClampCircular(Rotation.Y, 0, MathHelper.TwoPi);
        }

        public void MoveX(float a)
        {
            Position += Vector3.Transform(Vector3.UnitX * a, Quaternion.FromAxisAngle(Vector3.UnitY, Rotation.Y));
        }

        public void MoveY(float a)
        {
            Position += Vector3.Transform(Vector3.UnitY * a, Quaternion.FromAxisAngle(Vector3.UnitY, Rotation.Y));
        }

        public void MoveZ(float a)
        {
            Position += Vector3.Transform(Vector3.UnitZ * a, Quaternion.FromAxisAngle(Vector3.UnitY, Rotation.Y));
        }

        public void MoveYLocal(float a)
        {
            Position += Vector3.Transform(Vector3.UnitY * a, Orientation);
        }

        public void MoveZLocal(float a)
        {
            Position += Vector3.Transform(Vector3.UnitZ * a, Orientation);
        }

        public static float ClampCircular(float n, float min, float max)
        {
            if (n >= max) n -= max;
            if (n < min) n += max;
            return n;
        }
    }
}
