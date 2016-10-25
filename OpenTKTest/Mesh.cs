using System;
using System.Collections.Generic;
using Assimp;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace OpenTKTest
{
    public class Mesh
    {
      
        public struct Vertex
        {
            public Vector3 Vertices;
            public Vector2 Uv;
            public Vector3 Normal;
        }

        private readonly List<Texture> _textures = new List<Texture>();
        private readonly List<int> _materialTextureIndex = new List<int>();


        private readonly List<Tuple<Vertex[], int[]>> _completeScene = new List<Tuple<Vertex[], int[]>>();
        private readonly List<Tuple<uint, uint>> _bufferList = new List<Tuple<uint, uint>>();

        public unsafe void InitGlBuffer(Vertex[] vertices, int[] indices)
        {

            uint vertexBuffer;
            uint indexBuffer;

            GL.GenBuffers(1, out vertexBuffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr) (sizeof(Vertex) * vertices.Length), ref vertices[0], BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out indexBuffer);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr) (sizeof(int) * indices.Length), ref indices[0], BufferUsageHint.StaticDraw);
           
            // Save Buffer
            _bufferList.Add(new Tuple<uint, uint>(vertexBuffer, indexBuffer));

        }

        public void LoadMesh(string filename)
        {
            Clear();
          
                var fileName = Environment.CurrentDirectory + "\\" + filename;
                var assimpImporter = new AssimpContext();           
                    var scene = assimpImporter.ImportFile(fileName,
                    PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs |
                    PostProcessSteps.JoinIdenticalVertices);

                InitMaterials(scene);

                // for every mesh
                var allMeshesInScene = scene.Meshes;
                foreach (var mesh in allMeshesInScene)
                {
                    // init and add to vertices
                    InitMesh(mesh);
                
                }
        }

        private void InitMesh(Assimp.Mesh mesh)
        {
              _materialTextureIndex.Add(mesh.MaterialIndex);

              var vertices = new Vertex[mesh.Vertices.Count];
              var indices = new int[mesh.FaceCount * 3];

              var meshVertices = mesh.Vertices;
              var normals = mesh.Normals;
              var texCords = mesh.TextureCoordinateChannels;
              var faces = mesh.Faces;           

            for (var i = 0; i < meshVertices.Count; i++)
            {
                var vertex = new Vector3(meshVertices[i].X, meshVertices[i].Y, meshVertices[i].Z);
                var normal = new Vector3(normals[i].X, normals[i].Y, normals[i].Z);
                var texCord = new Vector2(texCords[0][i].X, texCords[0][i].Y);

                var compiledVertex = new Vertex
                {
                    Vertices = vertex,
                    Normal = normal,
                    Uv = texCord
                };

                // Add vertex to list
                vertices[i] = compiledVertex;
            }

            var count = 0;

            foreach (var face in faces)
            {
                indices[count] = face.Indices[0];
                indices[++count] = face.Indices[1];
                indices[++count] = face.Indices[2];
                ++count;
            }
             
          
            // add all to tuple:
            _completeScene.Add(new Tuple<Vertex[], int[]>(vertices, indices));

            // init buffer
            InitGlBuffer(vertices, indices);
        }

        private void Clear()
        {
            
        }


        private void InitMaterials(Scene scene)
        {
            for (var i = 0; i < scene.MaterialCount; i++)
            {
                var material = scene.Materials[i];

                if (material.GetMaterialTextureCount(TextureType.Diffuse) > 0)
                {
                    TextureSlot foundTexture;
                    if (material.GetMaterialTexture(TextureType.Diffuse, 0, out foundTexture))
                    {
                        _textures.Add(new Texture(TextureTarget.Texture2D, foundTexture.FilePath));
                        if (!_textures[i].Load())
                        {
                            Console.WriteLine("Error Loading texture!");
                        }
                    }
                }
                else
                {
                   _textures.Add(new Texture(TextureTarget.Texture2D, "white.png"));
                   _textures[i].Load();
                  }
                }
            }

        public unsafe void Render()
        {
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            for (var i = 0; i < _completeScene.Count; i++)
            {
                var vertexBuffer = _bufferList[i].Item1;
                var indexBuffer = _bufferList[i].Item2;
                var indicesCount = _completeScene[i].Item2.Length;

                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), IntPtr.Zero);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(Vertex), (IntPtr) 12);
                GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), (IntPtr) 20);
                
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);

                _textures[_materialTextureIndex[i]].Bind(TextureUnit.Texture0);
               


                GL.DrawElements(BeginMode.Triangles, indicesCount, DrawElementsType.UnsignedInt ,0);
            }

            // cleanup
            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);
        }
    }
}
