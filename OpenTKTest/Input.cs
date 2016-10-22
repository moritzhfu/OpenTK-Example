using OpenTK;
using OpenTK.Input;

namespace OpenTKTest
{
    public class Input
    {
        private static bool _mouseDown;

        /*public void ParseMovement()
        {
            MouseDown += MouseWithButtonDown;
            MouseMove += MouseMoveWithButtonDown;

            MouseUp += (sender, args) =>
            {
                _mouseDown = false;
            };
        }

        private static void MouseWithButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDown = true;
        }

        public static void MouseMoveWithButtonDown(object sender, MouseMoveEventArgs e)
        {
            if (!_mouseDown) return;

            _alpha -= e.XDelta * 0.000000001f;
            M.WorldMatrix *= Matrix4.CreateRotationY(_alpha);

        }*/
    }
}