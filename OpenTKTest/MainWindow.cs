using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
          layout (location = 1) in vec2 TexCoord;
          layout (location = 2) in vec3 Normal;

          uniform mat4 MVP;
          uniform mat4 M;           

          out vec2 TexCoord0;           
          out vec3 Normal0;

            void main() 
            {
                gl_Position = MVP * vec4(Position, 1.0);
                TexCoord0 = TexCoord;
                Normal0 = (M * vec4(Normal, 0.0)).xyz;
             }

        ";

                private const string FragmentShader = @"
            #version 330
            #ifdef GL_ES
                precision highp float;
            #endif
            in vec2 TexCoord0;
            in vec3 Normal0;
            
            out vec4 FragColor;

            uniform sampler2D gSampler;

            // Directional Light
            struct DirectionalLight
            {
   	             vec3 Color;
                 float AmbientIntensity;
                 vec3 Direction;
                 float DiffuseIntensity;
            };

            uniform DirectionalLight gDirectionalLight;

            void main()
            {
                vec4 AmbientColor = vec4(gDirectionalLight.Color * gDirectionalLight.AmbientIntensity, 1.0f);
                float DiffuseFactor = dot(normalize(Normal0), -gDirectionalLight.Direction);

                 vec4 DiffuseColor;

                if (DiffuseFactor > 0) {
                    DiffuseColor = vec4(gDirectionalLight.Color * gDirectionalLight.DiffuseIntensity * DiffuseFactor, 1.0f);
                }
                else {
                    DiffuseColor = vec4(0, 0, 0, 0);
                }

                 FragColor = texture2D(gSampler, TexCoord0.xy) * (AmbientColor + DiffuseColor);
            }";

#endregion

        // BUFFERS
        private uint _vbo;
        private uint _indexBo;
#region Vertices
        // VERTICES
        public struct Vertex
        {
            public Vector3 Vertices;
            public Vector2 Uv;
            public Vector3 Normal;
        }

        private Vertex[] _vertex = {
            new Vertex {
                Vertices = new Vector3(-1.0f, -1.0f, 0.5773f),
                Uv = new Vector2(0.0f, 0.0f),
                Normal = Vector3.Zero
            },
             new Vertex {
                Vertices = new Vector3(0.0f, -1.0f, -1.15475f),
                Uv = new Vector2(0.5f, 0.0f),
                Normal = Vector3.Zero
            },
              new Vertex {
                Vertices = new Vector3(1.0f, -1.0f, 0.5773f),
                Uv = new Vector2(1.0f, 0.0f),
                Normal = Vector3.Zero
            },
               new Vertex {
                Vertices = new Vector3(0.0f, 1.0f, 0.0f),
                Uv = new Vector2(0.5f, 1.0f),
                Normal = Vector3.Zero
            },
        };

        private readonly int[] _indices =
            {
                  0, 3, 1,
                  1, 3, 2,
                  2, 3, 0,
                  0, 1, 2
            };
#endregion

        // SHADER
        private int _shaderProgramm;
        private int _xform; // Matrixposition in Shader
        private int _modelMatrixPosition; // Matrixposition in Shader
        private int _gSamplerPosition;

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

        // TEXTURE
        private TextureTarget _textureTarget;
        private Texture _texture;

        // Lightning
        private int _directionalLightColor;
        private int _directionalAmbientIntensity;
        private int _directionalDirection;
        private int _directionalIntensity;

        private struct Light
        {

            public Vector3 Color;
            public float AmbientIntensity;
            public Vector3 Direction;
            public float DiffuseIntensity;
        }

        private Light _light = new Light
        {
            Color = Vector3.One,
            AmbientIntensity = -0.9f,
            Direction = Vector3.UnitX,
            DiffuseIntensity = 2f
        };

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

            // FrontFace is counter clockwise
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.CullFace);

            // Create normals
            CreateNormals(_indices, ref _vertex);

            // Create VertexBuffer
            CreateVertexBuffer();
            CreateAndLinkShaders();
            // DEBUG:
            GetAllActiveUniforms();

           GL.ClearColor(0.1f, 0.1f, 0.1f, 0.1f);
            
           _xform = GetAndMapShaderValues("MVP");
           _modelMatrixPosition = GetAndMapShaderValues("M");
            WindowWidth = Width;
            WindowHeight = Height;


            _textureTarget = TextureTarget.Texture2D;
            // Get Texture
            _texture = new Texture(_textureTarget, "test.png");
            if(!_texture.Load())
                throw new Exception("Texture file not found!");
            // Get Textureposition in Shader
            _gSamplerPosition = GetAndMapShaderValues("gSampler");


            // Get Lightposition in Shader
            _directionalLightColor = GetAndMapShaderValues("gDirectionalLight.Color");
            _directionalAmbientIntensity = GetAndMapShaderValues("gDirectionalLight.AmbientIntensity");
            _directionalDirection = GetAndMapShaderValues("gDirectionalLight.Direction");
            _directionalIntensity = GetAndMapShaderValues("gDirectionalLight.DiffuseIntensity");

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
            if (Keyboard[Key.Up])
            {
                _light.Direction += new Vector3(0f,0.1f,0f);
                _light.Direction.Y.Clamp(0.0f, 1.0f);
            }
            if (Keyboard[Key.Down])
            {
                _light.Direction -= new Vector3(0f, 0.1f, 0f);
                _light.Direction.Y.Clamp(0.0f, 1.0f);
            }
            if (Keyboard[Key.Left])
            {
                _light.DiffuseIntensity -= 0.1f;
            }
            if (Keyboard[Key.Right])
            {
                _light.DiffuseIntensity += 0.1f;
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
            ModelMatrix4 = CalculateModelMatrix(2f, new Vector3(0, 0, 0), new Vector3(0f, 0f, -3.0f));
            ViewMatrix4 = CalculateViewMatrix();
            var worldMatrix = ModelMatrix4 * ViewMatrix4 * ProjectionMatrix4;
            // Set MVP Matrix
            GL.UniformMatrix4(_xform, false, ref worldMatrix);
            // Set M Matrix
            var modelMatrix4 = ModelMatrix4;
            GL.UniformMatrix4(_modelMatrixPosition, false, ref modelMatrix4);
            // Set texture
            GL.Uniform1(_gSamplerPosition, 0);


            // Set Directional Lightning
            SetLight();

            Present();
        }

        private void SetLight()
        {
            GL.Uniform3(_directionalLightColor, _light.Color);
            GL.Uniform1(_directionalAmbientIntensity, _light.AmbientIntensity);
            var direction = _light.Direction;
            direction.Normalize();
            GL.Uniform3(_directionalDirection, direction);
            GL.Uniform1(_directionalIntensity, _light.DiffuseIntensity);
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


        private void ResetCursor()
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

        /// <summary>
        /// This function takes an array of vertices and indices, fetches the vertices of each triangle according to the indices and calculates its normal.
        /// In the first loop we only accumulate the normals into each of the three triangle vertices.
        /// For each triangle the normal is calculated as a cross product between the two edges that are coming out of the first vertex.
        ///  Before accumulating the normal in the vertex we make sure we normalize it.
        /// The reaons is that the result of the cross product is not guaranteed to be of unit length.
        /// In the second loop we scan the array of vertices directly (since we don't care about the indices any more) and normalize the normal of each vertex.
        /// This operation is equivalent to averaging out the accumulated sum of normals and leaves us with a vertex normal that is of a unit length. 
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="vertex"></param>
        private static void CreateNormals(IReadOnlyList<int> indices, ref Vertex[] vertex)
        {
            for (var i = 0; i < indices.Count; i += 3)
            {
                var index0 = indices[i];
                var index1 = indices[i + 1];
                var index2 = indices[i + 2];
                var v1 = vertex[index1].Vertices - vertex[index0].Vertices;
                var v2 = vertex[index2].Vertices - vertex[index0].Vertices;
                Vector3 normal;
                Vector3.Cross(ref v1, ref v2, out normal);
                normal.Normalize();

               vertex[index0].Normal += normal;
               vertex[index1].Normal += normal;
               vertex[index2].Normal += normal;
            }

            for (var j = 0; j < vertex.Length; j++)
            {
                vertex[j].Normal.Normalize();
            }
            
        }


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
            // Size = Vertexelements * vec3 Vertex * vec2 Uv * vec3 normal 
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr) (_vertex.Length * sizeof(Vector3) * sizeof(Vector2) * sizeof(Vector3)), _vertex,
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
            GL.EnableVertexAttribArray(0); // vertex
            GL.EnableVertexAttribArray(1); // uv
            GL.EnableVertexAttribArray(2); // normal

            // Bind Buffer again, we are doing the draw call
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            // This will tell the pipeline how to interpet the data coming in from the buffer
            // index == 0, size = 3 since Vector3 is used. Not normalized. 
            // stride is number of bytes between instances.
            // Struct with position and normals would be 6 *4 = 24 e.g.
            unsafe
            {
                // This is the vertex data
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), IntPtr.Zero);
                // This is the uv map data
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(Vertex), (IntPtr) 12);
                // This is the normal data
                GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), (IntPtr) 20);
            }
            
            // Bind index buffer
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBo);

            /* This method is no longer supported/needed with ElementArrayBuffer
            // Do the draw call.
            // First param is where to begin drawing, second how long
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);*/

            // Bind Texture
            _texture.Bind(TextureUnit.Texture0);

            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);

            // cleanup
            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);

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