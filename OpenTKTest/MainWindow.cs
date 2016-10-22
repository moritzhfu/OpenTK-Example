using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace OpenTKTest
{
    public class MainWindow : GameWindow
    {

#region SHADER

        private const string VertexShader = @"
            #version 330

            layout (location = 0) in vec3 Position;

            uniform mat4 MVP;
            
            out vec4 Color;


            void main() 
            {
                Color = vec4(Position, 1.0);
                gl_Position = MVP * vec4(Position, 1.0);
             }

        ";

                private const string FragmentShader = @"
            #version 330
            #ifdef GL_ES
                precision highp float;
            #endif

            out vec4 FragColor;
            in vec4 Color;

            void main()
            {
                FragColor = Color;
            }";

#endregion

        // BUFFERS
        private uint _vbo;
        private uint _indexBo;

        // VERTICES
        private readonly Vector3[] _vertices = new[]
        {
                     // left, down, front vertex
                    new Vector3(-1, -1, -1), // 0  - belongs to left
                    new Vector3(-1, -1, -1), // 1  - belongs to down
                    new Vector3(-1, -1, -1), // 2  - belongs to front

                    // left, down, back vertex
                    new Vector3(-1, -1,  1),  // 3  - belongs to left
                    new Vector3(-1, -1,  1),  // 4  - belongs to down
                    new Vector3(-1, -1,  1),  // 5  - belongs to back

                    // left, up, front vertex
                    new Vector3(-1,  1, -1),  // 6  - belongs to left
                    new Vector3(-1,  1, -1),  // 7  - belongs to up
                    new Vector3(-1,  1, -1),  // 8  - belongs to front

                    // left, up, back vertex
                    new Vector3(-1,  1,  1),  // 9  - belongs to left
                    new Vector3(-1,  1,  1),  // 10 - belongs to up
                    new Vector3(-1,  1,  1),  // 11 - belongs to back

                    // right, down, front vertex
                    new Vector3( 1, -1, -1), // 12 - belongs to right
                    new Vector3( 1, -1, -1), // 13 - belongs to down
                    new Vector3( 1, -1, -1), // 14 - belongs to front

                    // right, down, back vertex
                    new Vector3( 1, -1,  1),  // 15 - belongs to right
                    new Vector3( 1, -1,  1),  // 16 - belongs to down
                    new Vector3( 1, -1,  1),  // 17 - belongs to back

                    // right, up, front vertex
                    new Vector3( 1,  1, -1),  // 18 - belongs to right
                    new Vector3( 1,  1, -1),  // 19 - belongs to up
                    new Vector3( 1,  1, -1),  // 20 - belongs to front

                    // right, up, back vertex
                    new Vector3( 1,  1,  1),  // 21 - belongs to right
                    new Vector3( 1,  1,  1),  // 22 - belongs to up
                    new Vector3( 1,  1,  1),  // 23 - belongs to back

        };

        private readonly int[] _indices =
            {
                     0,  6,  3,     3,  6,  9, // left
                   2, 14, 20,     2, 20,  8, // front
                  12, 15, 18,    15, 21, 18, // right
                   5, 11, 17,    17, 11, 23, // back
                   7, 22, 10,     7, 19, 22, // top
                   1,  4, 16,     1, 16, 13, // bottom 
        };


        // SHADER
        private int _shaderProgramm;
        private int _xform; // Matrixposition in Shader

        // Window
        public static int WindowWidth;
        public static int WindowHeight;

        // MATRIX

        public Matrix4 WorldMatrix { set; get; } = Matrix4.Identity;

        public Matrix4 ProjectionMatrix4 { set; get; }
        public Matrix4 ModelMatrix4 { set; get; }
        public Matrix4 ViewMatrix4 { set; get; }

        // CAMERA
        private readonly Camera _camera = new Camera();
        private Vector2 _centerMousePos;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
        }


        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow(int width, int height, GraphicsMode mode) : 
            base(width, height, mode)
        {
        }


        /// <summary>
        /// This method is called on load event.
        /// </summary>
        internal void OnLoad()
        {
            // setup settings, load textures, sounds
            VSync = VSyncMode.On;

            GL.FrontFace(FrontFaceDirection.Cw);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.CullFace);

            // Create VertexBuffer
            CreateVertexBuffer();
           CreateAndLinkShaders();
            // DEBUG:
            GetAllActiveUniforms();

           GL.ClearColor(0.1f, 0.1f, 0.1f, 0.1f);
            
           _xform = GetAndMapShaderValues("MVP");
          
            WindowWidth = Width;
            WindowHeight = Height;
        }

        #region GAMELOOP & RENDER
        
        /// <summary>
        /// OnUpdateFrame
        /// Add game logic, input handling, etc. here
        /// </summary>
        internal void OnUpdateFrame()
        {
            // Input region
            if (Keyboard[Key.Escape])
            {
                Exit();
            }
            if (Keyboard[Key.W])
            {
                MoveCamera(0f, 0.1f, 0f);
            }
            if (Keyboard[Key.A])
            {
                MoveCamera(-0.1f, 0f, 0f);
            }
            if (Keyboard[Key.S])
            {
                MoveCamera(0f, -0.1f, 0f);
            }
            if (Keyboard[Key.D])
            {
                MoveCamera(0.1f, 0f, 0f);
            }
            if (Keyboard[Key.Q])
            {
                MoveCamera(0f, 0f, 0.1f);
            }
            if (Keyboard[Key.E])
            {
                MoveCamera(0f, 0f, -0.1f);
            }
            
            if (Focused)
            {
                var delta = _centerMousePos - new Vector2(OpenTK.Input.Mouse.GetState().X, OpenTK.Input.Mouse.GetState().Y);

                AddRotation(delta.X, delta.Y);
                ResetCursor();
            }

        }


        /// <summary>
        //  OnRenderFrame
        /// </summary>
        internal void OnRenderFrame()
        {
            // render graphics
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Set MVP with CameraMovement
            // note: all calculations are the other way round due to row notation
            var aspectRatio = Width / (float)Height;
            ProjectionMatrix4 = Matrix4.CreatePerspectiveFieldOfView(1.3f, aspectRatio, 1.0f, 40.0f);
            ModelMatrix4 = CalculateModelMatrix(0.5f, new Vector3(0, 0, 0), new Vector3(0f, 0f, -3.0f));
            ViewMatrix4 = CalculateViewMatrix();
            var worldMatrix = ModelMatrix4 * ViewMatrix4 * ProjectionMatrix4;
            GL.UniformMatrix4(_xform, false, ref worldMatrix);

            Present();
        }


        #endregion

        #region MATRIXDEF


        public Matrix4 CalculateModelMatrix(float scale, Vector3 rotation, Vector3 position)
        {
            return Matrix4.CreateScale(scale) * Matrix4.CreateRotationX(rotation.X) * Matrix4.CreateRotationY(rotation.Y) * Matrix4.CreateRotationZ(rotation.Z) * Matrix4.CreateTranslation(position);
        }

        public Matrix4 CalculateViewMatrix()
        {
            var lookat = new Vector3
            {
                X = (float)(Math.Sin(_camera.Orientation.X) * Math.Cos(_camera.Orientation.Y)),
                Y = (float)(Math.Sin(_camera.Orientation.Y)),
                Z = (float)(Math.Cos(_camera.Orientation.X) * Math.Cos(_camera.Orientation.Y))
            };

            return Matrix4.LookAt(_camera.Position, _camera.Position + lookat, Vector3.UnitY);
        }
        #endregion

        #region CAMERAMethods
        public void MoveCamera(float x, float y, float z)
        {
            var offset = new Vector3();

            var forward = new Vector3((float)Math.Sin(_camera.Orientation.X), 0, (float)Math.Cos(_camera.Orientation.X));
            var right = new Vector3(-forward.Z, 0, forward.X);

            offset += x * right;
            offset += y * forward;
            offset.Y += z;

            offset.NormalizeFast();
            offset = Vector3.Multiply(offset, _camera.MoveSpeed);

            _camera.Position += offset;
        }

        public void AddRotation(float x, float y)
        {
            x = x * _camera.MouseSensitivity;
            y = y * _camera.MouseSensitivity;

            _camera.Orientation.X = (_camera.Orientation.X + x) % ((float)Math.PI * 2.0f);
            _camera.Orientation.Y = Math.Max(Math.Min(_camera.Orientation.Y + y, (float)Math.PI / 2.0f - 0.1f), (float)-Math.PI / 2.0f + 0.1f);
        }


        void ResetCursor()
        {
            OpenTK.Input.Mouse.SetPosition(Bounds.Left + Bounds.Width / 2, Bounds.Top + Bounds.Height / 2);
            _centerMousePos = new Vector2(OpenTK.Input.Mouse.GetState().X, OpenTK.Input.Mouse.GetState().Y);
        }

        // Centers mouse in the window
        protected override void OnFocusedChanged(EventArgs e)
        {
            base.OnFocusedChanged(e);

            if (Focused)
            {
                ResetCursor();
            }
        }
        #endregion


        private void GetAllActiveUniforms()
        {
            // Get number of active uniforms
            int numberOfUniforms;
            GL.GetProgram(_shaderProgramm, GetProgramParameterName.ActiveUniforms, out numberOfUniforms);
            Console.WriteLine($"Checking for active uniforms. Found {numberOfUniforms}");
            for (var i = 0; i < numberOfUniforms; i++)
            {
                ActiveUniformType uType;
                int size;
                var name = GL.GetActiveUniform(_shaderProgramm, i, out size, out uType);
                var position = GL.GetUniformLocation(_shaderProgramm, name);
                Console.WriteLine($"Uniform with name: {name} and position: {position} found.");
            }

        }

        private int GetAndMapShaderValues(string value)
        {
            var location = GL.GetUniformLocation(_shaderProgramm, value);
            Console.WriteLine($"Location found {location}");
            return location;
        }


        private unsafe void CreateVertexBuffer()
        {
            // Create one (1) Buffer and pass _vbo as our vertex buffer handle 
            GL.GenBuffers(1, out _vbo);
            // Bind the buffer as kind of "generic" buffer
            // ArrayBuffer means the buffer will contain an array of verticies
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            // After gen and bind we will fill the buffer with data now
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr) (_vertices.Length*sizeof(Vector3)), _vertices,
                BufferUsageHint.StaticDraw);
            // Bind index/indices data
            GL.GenBuffers(1, out _indexBo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBo);
            // Fill with data
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr) (_indices.Length*sizeof(int)), _indices,
                BufferUsageHint.StaticDraw);
        }

        private void CreateAndLinkShaders()
        {
            _shaderProgramm = GL.CreateProgram();
            var fragShader = GL.CreateShader(ShaderType.FragmentShader);
            var vertShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(fragShader, FragmentShader);
            GL.ShaderSource(vertShader, VertexShader);

            string info;
            int statusCode;

            // Compile fragShader & check for erros
            GL.CompileShader(fragShader);
            GL.GetShaderInfoLog(fragShader, out info);
            GL.GetShader(fragShader, ShaderParameter.CompileStatus, out statusCode);
            Console.WriteLine($"Compiled FragShader with StatusCode {statusCode} and info {info}");

            // Compile vertShader & check for errors
            GL.CompileShader(vertShader);
            GL.GetShaderInfoLog(fragShader, out info);
            GL.GetShader(fragShader, ShaderParameter.CompileStatus, out statusCode);
            Console.WriteLine($"Compiled VertShader with StatusCode {statusCode} and info {info}");


            // Attach both shaders to one program
            GL.AttachShader(_shaderProgramm, fragShader);
            GL.AttachShader(_shaderProgramm, vertShader);

            // Link Program and print error if something went wrong
            GL.LinkProgram(_shaderProgramm);
            Console.WriteLine($"ShaderProgrammInfo: {GL.GetProgramInfoLog(_shaderProgramm)}");
            Console.WriteLine($"Error: {GL.GetError()}");

            // Validate again
            GL.ValidateProgram(_shaderProgramm);

            // Use our program
            GL.UseProgram(_shaderProgramm);

        }
        
        internal void OnResize()
        {
            var aspectRatio = Width / (float)Height;
            ProjectionMatrix4 = Matrix4.CreatePerspectiveFieldOfView(1.3f, aspectRatio, 1.0f, 40.0f);
            GL.Viewport(0, 0, Width, Height);
          
        }

        private void Present()
        {
            // Enable Vertex Attribute data due to fixed pipeline when no shader is installed
            GL.EnableVertexAttribArray(0);
            // Bind Buffer again, we are doing the draw call
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            // This will tell the pipeline how to interpet the data coming in from the buffer
            // index == 0, size = 3 since Vector3 is used. Not normalized. 
            // stride is number of bytes between instances.
            // Struct with position and normals would be 6 *4 = 24 e.g.
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            // Bind index buffer
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBo);

            /* This method is no longer supported/needed with ElementArrayBuffer
            // Do the draw call.
            // First param is where to begin drawing, second how long
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);*/

            GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, IntPtr.Zero);

            // cleanup
            GL.DisableVertexAttribArray(0);

            SwapBuffers();
        }

    }

    internal class Camera
    {
        public Vector3 Orientation = new Vector3((float)Math.PI, 0f, 0f);
        public Vector3 Position = Vector3.Zero;
        public float MoveSpeed = 0.2f;
        public float MouseSensitivity = 0.01f;
    }
}