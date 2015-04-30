using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Materials;
using Chireiden.Scenes;

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
        int[] indexBuffer;

        /// <summary>
        /// Set of vertex normals in this object. Stored in the same order as vertices[].
        /// </summary>
        Vector3[] normals;

        /// <summary>
        /// Set of UV texture coordinates in this object, in the same order as vertices[].
        /// </summary>
        Vector2[] texCoords;

        /// <summary>
        /// Set of vertex tangents, in vertex order. The first three coordinates give the 
        /// actual (x,y,z) values, while the fourth coordinate is the handedness.
        /// </summary>
        Vector4[] tangents;

        Material material;

        int positionVboHandle;
        int normalVboHandle;
        int texCoordVboHandle;
        int tangentVboHandle;
        int eboHandle;

        int vaoHandle;

        public TriMesh(Vector3[] vs, int[] fs, Vector3[] ns, Vector2[] tcs, Vector4[] tans, Material mat)
            : base()
        {
            vertices = vs;
            indexBuffer = fs;
            normals = ns;
            texCoords = tcs;
            tangents = tans;
            material = mat;

            CreateVBOs();
            CreateVAOs();
        }

        public bool hasNormals()
        {
            return normals.Length > 0;
        }

        public bool hasTexCoords()
        {
            return texCoords.Length > 0;
        }

        public bool hasTangentFrame()
        {
            return tangents.Length > 0;
        }
        
        void CreateVBOs()
        {
            // Create the VBO for vertex positions
            positionVboHandle = GL.GenBuffer();
            // Bind the VBO we just created so that we can upload things to it
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            // Upload the actual positions to it
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(vertices.Length * Vector3.SizeInBytes),
                vertices, BufferUsageHint.StaticDraw);

            // Create the VBO for vertex normals
            normalVboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(normals.Length * Vector3.SizeInBytes),
                normals, BufferUsageHint.StaticDraw);

            // Create VBO for texture coordinates
            texCoordVboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordVboHandle);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer,
                new IntPtr(texCoords.Length * Vector2.SizeInBytes),
                texCoords, BufferUsageHint.StaticDraw);

            // Create VBO for tangents
            tangentVboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, tangentVboHandle);
            GL.BufferData<Vector4>(BufferTarget.ArrayBuffer,
                new IntPtr(tangents.Length * Vector4.SizeInBytes),
                tangents, BufferUsageHint.StaticDraw);

            // Create the index buffer
            eboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                new IntPtr(sizeof(uint) * indexBuffer.Length),
                indexBuffer, BufferUsageHint.StaticDraw);

            // Unbind our stuff to clean up
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        /// <summary>
        /// BE AWARE: Because of the indices we've chosen here for our attributes,
        /// ALL VERTEX SHADERS for triangle meshes must contain the attributes in the
        /// following order:
        /// 
        /// in vec3 vert_position;
        /// in vec3 vert_normal;
        /// in vec2 vert_texCoord;
        /// in vec4 vert_tangent;
        /// </summary>
        void CreateVAOs()
        {
            // GL3 allows us to store the vertex layout in a "vertex array object" (VAO).
            // This means we do not have to re-issue VertexAttribPointer calls
            // every time we try to use a different vertex layout - these calls are
            // stored in the VAO so we simply need to bind the correct VAO.
            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            // We're going to use index 0 in the VAO to refer to the vertex positions.
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            // Each vertex position is a 3-vector of floats.
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            // Use index 1 to refer to the vertex normals.
            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            // Use index 2 to refer to texture coordinates.
            GL.EnableVertexAttribArray(2);
            GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordVboHandle);
            // Texture coordinates are 2-vectors of floats, so each attribute has 2 components,
            // the type is float, and the stride is the size of a vector2.
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);

            // Use index 3 to refer to vertex tangents.
            GL.EnableVertexAttribArray(3);
            GL.BindBuffer(BufferTarget.ArrayBuffer, tangentVboHandle);
            // Tangents are 4-vectors, so each attribute has 4 components,
            // of type float, with the stride as the size of a vector4.
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, true, Vector4.SizeInBytes, 0);

            // Bind the index buffer so that we know what faces exist.
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

            // Unbind the VAO to clean up.
            GL.BindVertexArray(0);
        }

        public override void render(Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            // Compute transformation matrices
            // TODO: why is toWorldMatrix the left operand...
            Matrix4 modelView = Matrix4.Mult(toWorldMatrix, viewMatrix);

            // Bind the stuff we need for this object (VAO, index buffer, program)
            GL.BindVertexArray(vaoHandle);
            Shaders.BlenderShader.use();
            material.useMaterialParameters(Shaders.BlenderShader);

            // TODO: set shader uniforms, incl. modelview and projection matrices
            Shaders.BlenderShader.setUniformMatrix4("projectionMatrix", projectionMatrix);
            Shaders.BlenderShader.setUniformMatrix4("modelViewMatrix", modelView);
            Shaders.BlenderShader.setUniformMatrix4("viewMatrix", viewMatrix);

            GL.DrawElements(PrimitiveType.Triangles, indexBuffer.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);

            // Clean up
            material.unuseMaterialParameters(Shaders.BlenderShader);
            Shaders.BlenderShader.unuse();
            GL.BindVertexArray(0);

            // TODO: get the material properties, and render that way
            foreach (SceneTreeNode c in children)
            {
                render(viewMatrix, projectionMatrix);
            }
        }
    }
}
