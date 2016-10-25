using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Assimp;
using Assimp.Configs;
using Assimp.Unmanaged;

namespace OpenTKTest
{
    public class Mesh
    {
        public Mesh()
        {
            
        }

         struct MeshEntry
        {
            public bool Init()
            {
                throw new NotImplementedException();
            }
             public uint VB;
             public uint IB;
             public uint numIndices;
             public uint materialIndex;
        }

        public List<Mesh> Meshes;
        public List<Texture> Textures;

        public bool LoadMesh(string filename)
        {
            Clear();
          
                var ret = false;
                var fileName = Environment.CurrentDirectory + "\\" + filename;
                var assimpImporter = new AssimpContext();
                Scene model;
            try
            {
                model = assimpImporter.ImportFile(fileName,
                    PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs |
                    PostProcessSteps.JoinIdenticalVertices);
            }
            catch (Exception e)
            {
                throw new Exception($"Error importing Mesh: {e}");
            }

            ret = InitFromScene(model, fileName);
          

            return ret;
        }

        private bool InitFromScene(Scene scene, string filename)
        {
            return false;
        }

        private void InitMesh()
        {
            
        }

        private bool InitMaterials()
        {
            return false;
        }

        private void Clear()
        {
            
        }
        
            
    }
}