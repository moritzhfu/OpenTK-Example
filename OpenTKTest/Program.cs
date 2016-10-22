using System;

namespace OpenTKTest
{
    internal class Programm
    {
        [STAThread]
        public static void Main()
        {
            using (var game = new MainWindow())
            {
                game.Load += (sender, e) =>
                {
                   game.OnLoad();
                };
                
                game.Resize += (sender, e) =>
                {
                    game.OnResize();
                };

                game.UpdateFrame += (sender, e) =>
                {
                    game.OnUpdateFrame();
                };

                game.RenderFrame += (sender, e) =>
                {
                    game.OnRenderFrame();
                };

                // Run the game at 60 updates per second
                game.Run(60.0);
            }
        }
    }
}