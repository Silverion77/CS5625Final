using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Materials;

namespace Chireiden.Meshes
{
    /// <summary>
    /// A mesh composed entirely of triangles.
    /// 
    /// I'm enforcing that meshes by themselves should not move. If you
    /// want them to move, then you have to set some mobile object as their parent
    /// (e.g. a skeleton, or an empty).
    /// </summary>
    public class TriMesh : PlaceableObject
    {
        /// <summary>
        /// The set of vertices in this object, in local space points.
        /// </summary>
        Vector3[] vertices;

        /// <summary>
        /// The set of faces, stored as flattened triples of indices.
        /// </summary>
        int[] faceIndices;

        /// <summary>
        /// Set of vertex normals in this object. Stored in the same order as vertices[].
        /// </summary>
        Vector3[] normals;

        /// <summary>
        /// Set of UV texture coordinates in this object, in the same order as vertices[].
        /// </summary>
        Vector2[] texCoords;

        Material material;

        public TriMesh(Vector3[] vs, int[] fs, Vector3[] ns, Vector2[] tcs, Material mat)
            : base()
        {
            vertices = vs;
            faceIndices = fs;
            normals = ns;
            texCoords = tcs;
            material = mat;
        }

        public bool hasNormals()
        {
            return normals.Length > 0;
        }

        public bool hasTexCoords()
        {
            return texCoords.Length > 0;
        }

        public void printAllVertices()
        {
            if (hasNormals() & hasTexCoords())
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Console.WriteLine("Vertex {0} has position {1}, normal {2}, texCoord {3}", i, vertices[i], normals[i], texCoords[i]);
                }
            }
            else if (hasNormals())
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Console.WriteLine("Vertex {0} has position {1}, normal {2}", i, vertices[i], normals[i]);
                }
            }
            else if (hasTexCoords())
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Console.WriteLine("Vertex {0} has position {1}, texCoord {2}", i, vertices[i], texCoords[i]);
                }
            }
            else
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Console.WriteLine("Vertex {0} has position {1}", i, vertices[i]);
                }
            }
        }

        public override void render(Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            // TODO: get the material properties, and render that way
            foreach (SceneTreeNode c in children)
            {
                render(viewMatrix, projectionMatrix);
            }
        }
    }
}
