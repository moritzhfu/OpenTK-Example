﻿using System;
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
          out vec3 WorldPos0;

            void main() 
            {
                gl_Position = MVP * vec4(Position, 1.0);
                TexCoord0 = TexCoord;
                Normal0 = (M * vec4(Normal, 0.0)).xyz;
                WorldPos0 = (M * vec4(Position, 1.0)).xyz;
             }

        ";

                private const string FragmentShader = @"
            #version 330
            #ifdef GL_ES
                precision highp float;
            #endif
            in vec2 TexCoord0;
            in vec3 Normal0;
            in vec3 WorldPos0;

            out vec4 FragColor;

            uniform sampler2D gSampler;

            // Directional Light
            struct DirectionalLight
            {
   	             vec3 Color;
                 vec3 Position;
                 float AmbientIntensity;
                 vec3 Direction;
                 float DiffuseIntensity;
                 float Attenuation;
            };

            uniform DirectionalLight gDirectionalLight;

            void main()
            {
                vec3 lightResult = vec3(0);
                    
                    vec3 LightToPixel = normalize(WorldPos0 - gDirectionalLight.Position.xyz);
                    float SpotFactor = dot(LightToPixel, gDirectionalLight.Direction);

                    if (degrees(acos(SpotFactor)) < 15.0) {                        
                        lightResult = vec3(1.0,1.0,1.0);
                    }                 

                

                 FragColor = texture2D(gSampler, TexCoord0.xy) * vec4(lightResult,1.0);
            }";


        private const string shadowMapVS = @"#version 330

                layout (location = 0) in vec3 Position;
                layout (location = 1) in vec2 TexCoord;
                layout (location = 2) in vec3 Normal;

                uniform mat4 MVP;

                out vec2 TexCoordOut;

                void main()
                {
                    gl_Position = MVP * vec4(Position, 1.0);
                    TexCoordOut = TexCoord;
                }";

        private const string shadowMapFS = @"#version 330

            in vec2 TexCoordOut;
            uniform sampler2D gShadowMap;

            out vec4 FragColor;

            void main()
            {
                float Depth = texture(gShadowMap, TexCoordOut).x;
                Depth = 1.0 - (1.0 - Depth) * 25.0;
                FragColor = vec4(Depth);
            }";

        #endregion

        // BUFFERS
        private uint _vbo;
        private uint _indexBo;

        // SHADER
        private int _shaderProgramm;
        private int _xform; // Matrixposition in Shader
        private int _modelMatrixPosition; // Matrixposition in Shader
        private int _gSamplerPosition;

        private int _shadowShaderProgramm;

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

       
        // Lightning
        private int _directionalLightColor;
        private int _directionalAmbientIntensity;
        private int _directionalDirection;
        private int _directionalIntensity;
        private int _directionalAttenuation;
        private int _directionalPosition;

        private struct Light
        {

            public Vector3 Color;
            public Vector3 Position;
            public float AmbientIntensity;
            public Vector3 Direction;
            public float DiffuseIntensity;
            public float Attenuation;
        }

        private Light _light = new Light
        {
            Color = Vector3.One,
            AmbientIntensity = 1f,
            Direction = Vector3.UnitX,
            DiffuseIntensity = 2f,
            Position = new Vector3(0f,0f,10f)
        };


        // ShadowMap
        private ShadowMapFBO _shadowMapFbo;

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
            GL.Enable(EnableCap.DepthTest);

            // Create ShadowMap
            // TODO: FIX Exception!
            _shadowMapFbo = new ShadowMapFBO();
            _shadowMapFbo.Init(Width, Height);

            // Create and link shader
            CreateAndLinkShaders();
            // DEBUG:
            GetAllActiveUniforms();

           GL.ClearColor(0.1f, 0.1f, 0.1f, 0.1f);
            
            _xform = GetAndMapShaderValues("MVP");
            _modelMatrixPosition = GetAndMapShaderValues("M");
            WindowWidth = Width;
            WindowHeight = Height;

            // Get Textureposition in Shader
            _gSamplerPosition = GetAndMapShaderValues("gSampler");


            // Get Lightposition in Shader
            _directionalLightColor = GetAndMapShaderValues("gDirectionalLight.Color");
            _directionalAmbientIntensity = GetAndMapShaderValues("gDirectionalLight.AmbientIntensity");
            _directionalDirection = GetAndMapShaderValues("gDirectionalLight.Direction");
            _directionalIntensity = GetAndMapShaderValues("gDirectionalLight.DiffuseIntensity");
            _directionalPosition = GetAndMapShaderValues("gDirectionalLight.Position");

            _scene = new Mesh();
            _scene.LoadMesh("phoenix_ugv.md2");

            _quad = new Mesh();
            _quad.LoadMesh("quad.obj");
        }

        private Mesh _scene;
        private Mesh _quad;
        

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
                _light.Position += new Vector3(0f,0.1f,0f);
            }
            if (Keyboard[Key.Down])
            {
                _light.Position -= new Vector3(0f, 0.1f, 0f);
            }
            if (Keyboard[Key.Left])
            {
                _light.Position += new Vector3(0f, 0.0f, 0.1f);
            }
            if (Keyboard[Key.Right])
            {
                _light.Position -= new Vector3(0f, 0.0f, 0.1f);
            }

            if (Focused)
            {
                var delta = _centerMousePos - new Vector2(OpenTK.Input.Mouse.GetState().X, OpenTK.Input.Mouse.GetState().Y);

                AddRotation(delta.X, delta.Y);
                ResetCursor();
            }

        }


        private void RenderPass()
        {
            // render graphics
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
           

            // Use our program
            GL.UseProgram(_shaderProgramm);

            GL.Uniform1(_gSamplerPosition, 0);
            _shadowMapFbo.BindForReading(TextureUnit.Texture0);
           
            //  m_pShadowMapTech->SetTextureUnit(0);
            //   m_shadowMapFBO.BindForReading(GL_TEXTURE0);


            // Bind normal framebuffer for diagnostics of light
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // Set MVP with CameraMovement
            // note: all calculations are the other way round due to row notation
            var aspectRatio = Width / (float)Height;
            ProjectionMatrix4 = Matrix4.CreatePerspectiveFieldOfView(1.3f, aspectRatio, 1.0f, 40.0f);
            ModelMatrix4 = CalculateModelMatrix(5000f, new Vector3(0, 0, 0), new Vector3(0f, 0f, -3f));
            ViewMatrix4 = CalculateViewMatrix();
            var worldMatrix = ModelMatrix4 * ViewMatrix4 * ProjectionMatrix4;
            // Set MVP Matrix
            GL.UniformMatrix4(_xform, false, ref worldMatrix);
            // Set M Matrix
            var modelMatrix4 = ModelMatrix4;
            GL.UniformMatrix4(_modelMatrixPosition, false, ref modelMatrix4);
            // Set texture
            //GL.Uniform1(_gSamplerPosition, 0);


            // Set Directional Lightning
            SetLight();

            _scene.Render();
            //_quad.Render();

        }

        private void ShadowPass()
        {
            _shadowMapFbo.BindForWriting();

            GL.Clear(ClearBufferMask.DepthBufferBit);
            
            // Use our program
            GL.UseProgram(_shadowShaderProgramm);

            // Set Directional Lightning
            SetLight();


            var aspectRatio = Width / (float)Height;
            ProjectionMatrix4 = Matrix4.CreatePerspectiveFieldOfView(1.3f, aspectRatio, 1.0f, 40.0f);
            ModelMatrix4 = CalculateModelMatrix(0, new Vector3(0, 0, 0), new Vector3(0f, 0f, 0f));
            ViewMatrix4 = CalculateViewMatrix();
            var worldMatrix = ModelMatrix4 * ViewMatrix4 * ProjectionMatrix4;
            // Set MVP Matrix
            GL.UniformMatrix4(_xform, false, ref worldMatrix);
            _scene.Render();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        /// <summary>
        //  OnRenderFrame
        /// </summary>
        internal void OnRenderFrame()
        {
            ShadowPass();
            RenderPass();
            SwapBuffers();
        }

        private void SetLight()
        {
            GL.Uniform3(_directionalLightColor, _light.Color);
            GL.Uniform1(_directionalAmbientIntensity, _light.AmbientIntensity);
            var direction = _light.Direction;
            direction.Normalize();
            GL.Uniform3(_directionalDirection, _camera.Orientation.Normalized());
            GL.Uniform1(_directionalIntensity, _light.DiffuseIntensity);
            GL.Uniform1(_directionalAttenuation, _light.Attenuation);
            GL.Uniform3(_directionalPosition, _camera.Position);
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
        private static void CreateNormals(IReadOnlyList<int> indices, ref Mesh.Vertex[] vertex)
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


        }

        private void CreateAndLinkShadowShaders()
        {
            _shadowShaderProgramm = GL.CreateProgram();
            var fragShader = GL.CreateShader(ShaderType.FragmentShader);
            var vertShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(fragShader, shadowMapFS);
            GL.ShaderSource(vertShader, shadowMapVS);

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
            GL.AttachShader(_shadowShaderProgramm, fragShader);
            GL.AttachShader(_shadowShaderProgramm, vertShader);

            // Link Program and print error if something went wrong
            GL.LinkProgram(_shadowShaderProgramm);
            Console.WriteLine($"ShaderProgrammInfo: {GL.GetProgramInfoLog(_shaderProgramm)}");
            Console.WriteLine($"Error: {GL.GetError()}");

            // Validate again
            GL.ValidateProgram(_shadowShaderProgramm);

          

        }

        internal void OnResize()
        {
            var aspectRatio = Width / (float)Height;
            ProjectionMatrix4 = Matrix4.CreatePerspectiveFieldOfView(1.3f, aspectRatio, 1.0f, 40.0f);
            GL.Viewport(0, 0, Width, Height);
          
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