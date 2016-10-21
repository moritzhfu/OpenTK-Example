using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace OpenTKTest
{
    public class MainWindow : GameWindow
    {
        private const string VertexShader = @"
            #version 330

            layout (location = 0) in vec3 Position;

            uniform mat4 gWorld;

            out vec4 Color;


            void main() 
            {
                Color = vec4(clamp(Position, 0.0, 1.0), 1.0);
                gl_Position = gWorld * vec4(Position.x, Position.y, Position.z, 1.0);
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

        private Matrix4 _worldMatrix = Matrix4.Identity;
        private uint _vbo;
        private uint _indexBo;

        public MainWindow()
        {
        }

        public MainWindow(int width, int height, GraphicsMode mode) : 
            base(width, height, mode)
        {

        }
        private readonly Vector3[] _vertices = new[]
        {
             new Vector3(-0.8165f, -0.3333f, -0.4714f), // Vertex 0
            new Vector3(0.8165f, -0.3333f, -0.4714f),  // Vertex 1
            new Vector3(0, -0.3333f, 0.9428f),         // Vertex 2
            new Vector3(0, 1, 0),                      // Vertex 3
        };

        private readonly int[] _indices = 
            {
               0, 2, 1,  // Triangle 0 "Bottom" facing towards negative y axis
            0, 1, 3,  // Triangle 1 "Back side" facing towards negative z axis
            1, 2, 3,  // Triangle 2 "Right side" facing towards positive x axis
            2, 0, 3,  // Triangle 3 "Left side" facing towrads negative x axis
        };


        private int _shaderProgramm;
        private int _worldMatrixLocation;

        internal void OnLoad()
        {
            // setup settings, load textures, sounds
            VSync = VSyncMode.On;

            // Create VertexBuffer
           CreateVertexBuffer();
           CreateAndLinkShaders();
            // DEBUG:
            GetAllActiveUniforms();

           GL.ClearColor(0.1f, 0.1f, 0.1f, 0.1f);


            _worldMatrixLocation = GetAndMapShaderValues("gWorld");
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
            GL.Viewport(0, 0, Width, Height);
        }

        internal void OnUpdateFrame()
        {
            // add game logic, input handling
            if (Keyboard[Key.Escape])
            {
                Exit();
            }
            if (Keyboard[Key.W])
            {
                _worldMatrix *= Matrix4.CreateRotationX(0.1f);
            }
            if (Keyboard[Key.S])
            {
                _worldMatrix *= Matrix4.CreateRotationY(0.1f);
            }
            if (Keyboard[Key.D])
            {
                _worldMatrix *= Matrix4.CreateRotationZ(0.1f);
            }

        }




        internal void OnRenderFrame()
        {
            RenderWithAShader();
        }



        private void RenderWithAShader()
        {
            // render graphics
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Set WorldMatrix
            GL.UniformMatrix4(_worldMatrixLocation, false, ref _worldMatrix);

            Present();
        }

        private void Present()
        {
            // Enable Vertex Attribute data due to fixed pipeline when no shader is installed
            GL.EnableVertexAttribArray(0);
            // Bind Buffer again, we are doing the draw call
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            // This will tell the pipeline how to interpet the data coming in from the buffer
            // index == 0, size = 3 since float3 is used. Not normalized. 
            // stride is number of bytes between instances.
            // Struct with position and normals would be 6 *4 = 24 e.g.
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            // Bind index buffer
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBo);

            /* This method is no longer supported/needed with ElementArrayBuffer
            // Do the draw call.
            // First param is where to begin drawing, second how long
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);*/

            GL.DrawElements(PrimitiveType.Triangles, 12, DrawElementsType.UnsignedInt, 0);

            // cleanup
            GL.DisableVertexAttribArray(0);

            SwapBuffers();
        }
    }
}