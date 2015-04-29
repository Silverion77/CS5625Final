using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using OpenTK;

using Assimp;
using Assimp.Configs;

using Chireiden.Materials;

namespace Chireiden.Meshes
{
    public class MeshImporter
    {
        /// <summary>
        /// Reads all the meshes contained in the given file and returns them in a list,
        /// having also created the appropriate instances of materials, and loaded the
        /// necessary textures for these meshes.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static List<TriMesh> importFromFile(string filename)
        {
            // Create a new importer
            AssimpContext importer = new AssimpContext();

            //This is how we add a configuration (each config is its own class)
            NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
            importer.SetConfig(config);

            //This is how we add a logging callback 
            LogStream logstream = new LogStream(delegate(String msg, String userData)
            {
                Console.WriteLine(msg);
            });
            logstream.Attach();

            //Import the model. All configs are set. The model
            //is imported, loaded into managed memory. Then the unmanaged memory is released, and everything is reset.
            Scene model = importer.ImportFile(filename, PostProcessPreset.TargetRealTimeMaximumQuality);

            Console.WriteLine("number of meshes = {0}", model.Meshes.Count);
            Console.WriteLine("number of animations = {0}", model.AnimationCount);

            // Get the direction where the textures will be
            string directory = System.IO.Path.GetDirectoryName(filename);
            Console.WriteLine("Looking in directory {0}", directory);

            List<TriMesh> outMeshes = new List<TriMesh>();

            foreach (Mesh m in model.Meshes)
            {
                // We should only be interested in meshes with vertices
                if (!m.HasVertices) continue;
                var verts = m.Vertices;
                int numVerts = m.VertexCount;

                Vector3[] vertArr = new Vector3[numVerts];

                int i = 0;
                foreach (Vector3D v in verts)
                {
                    vertArr[i] = new Vector3(v.X, v.Y, v.Z);
                    i++;
                }

                // Import the faces
                // Not really sure what to do if there aren't faces, will think about it later
                // if it's actually a problem
                var faces = m.Faces;
                int numFaces = m.FaceCount;
                int[] faceArr = new int[numFaces * 3];
                i = 0;
                foreach (Face f in faces)
                {
                    var indices = f.Indices;
                    if (indices.Count != 3)
                    {
                        Console.WriteLine("ERROR LOADING MESH: THIS AIN'T NO TRIANGLE");
                    }
                    faceArr[i] = indices[0];
                    faceArr[i + 1] = indices[1];
                    faceArr[i + 2] = indices[2];
                    i += 3;
                }

                // Import the normals
                Vector3[] normalArr;
                if (m.HasNormals)
                {
                    var normals = m.Normals;
                    // One normal per vertex
                    normalArr = new Vector3[numVerts];
                    i = 0;
                    foreach (Vector3D n in normals)
                    {
                        normalArr[i] = new Vector3(n.X, n.Y, n.Z);
                        i++;
                    }
                }
                else
                {
                    normalArr = new Vector3[0];
                }

                // This importer supports multiple texture coordinates per vertex,
                // but to keep things simple, we're only going to support one texture
                // coordinate per vertex.
                Vector2[] texCoordArr;
                if (m.TextureCoordinateChannelCount > 0)
                {
                    if (m.UVComponentCount[0] != 2)
                    {
                        Console.WriteLine("ERROR LOADING MESH: UV COORDINATES DON'T HAVE 2 COMPONENTS");
                    }
                    // Just grab the first UV coordinate list
                    var texCoords = m.TextureCoordinateChannels[0];
                    // There should be 1 UV coordinate per vertex
                    texCoordArr = new Vector2[numVerts];
                    i = 0;
                    foreach (Vector3D texC in texCoords)
                    {
                        // Since we're assuming UV coords, the Z component is unused
                        texCoordArr[i] = new Vector2(texC.X, texC.Y);
                        i++;
                    }
                }
                else
                {
                    texCoordArr = new Vector2[0];
                }

                // Now manufacture the associated material with this mesh
                int matID = m.MaterialIndex;
                Assimp.Material mat = model.Materials[matID];
                BlenderMaterial ourMat = new BlenderMaterial(mat, directory);

                TriMesh outMesh = new TriMesh(vertArr, faceArr, normalArr, texCoordArr, ourMat);
                outMeshes.Add(outMesh);
            }

            //End of example
            importer.Dispose();

            return outMeshes;
        }
    }
}
