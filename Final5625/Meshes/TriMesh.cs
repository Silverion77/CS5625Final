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
    /// Notice that this class does not go in the scene tree, and indeed, it has no
    /// information about locations or transformations at all. This class must be wrapped
    /// in a MeshNode to be placed in the world. This is because we will want to have 
    /// multiple copies of the same mesh in different places in the world, and we don't want
    /// to have to make an entirely new copy of the mesh data each time.
    /// </summary>
    public class TriMesh
    {
        /// <summary>
        /// The set of vertices in this object, in local space points.
        /// </summary>
        protected Vector3[] vertices;

        /// <summary>
        /// The set of faces, stored as flattened triples of indices.
        /// </summary>
        protected int[] indexBuffer;

        /// <summary>
        /// Set of vertex normals in this object. Stored in the same order as vertices[].
        /// </summary>
        protected Vector3[] normals;

        /// <summary>
        /// Set of UV texture coordinates in this object, in the same order as vertices[].
        /// </summary>
        protected Vector2[] texCoords;

        /// <summary>
        /// Set of vertex tangents, in vertex order. The first three coordinates give the 
        /// actual (x,y,z) values, while the fourth coordinate is the handedness.
        /// </summary>
        protected Vector4[] tangents;

        protected Material material;

        protected int positionVboHandle;
        protected int normalVboHandle;
        protected int texCoordVboHandle;
        protected int tangentVboHandle;
        protected int eboHandle;

        protected int vaoHandle;

        /// <summary>
        /// Construct a TriMesh. The boolean flag is there because, if some subclasses have additional
        /// vertex attributes, we'll need to set those fields before we call CreateVBOs / VAOs.
        /// </summary>
        /// <param name="vs"></param>
        /// <param name="fs"></param>
        /// <param name="ns"></param>
        /// <param name="tcs"></param>
        /// <param name="tans"></param>
        /// <param name="mat"></param>
        /// <param name="initVBOAndVAO"></param>
        public TriMesh(Vector3[] vs, int[] fs, Vector3[] ns, Vector2[] tcs, Vector4[] tans, Material mat, bool initVBOAndVAO)
        {
            vertices = vs;
            indexBuffer = fs;
            normals = ns;
            texCoords = tcs;
            tangents = tans;
            material = mat;

            if (initVBOAndVAO)
            {
                CreateVBOs();
                CreateVAOs();
            }
        }

        public TriMesh(Vector3[] vs, int[] fs, Vector3[] ns, Vector2[] tcs, Vector4[] tans, Material mat)
            : this(vs, fs, ns, tcs, tans, mat, true)
        { }

        protected virtual void initializeAllFields()
        {

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
        
        protected virtual void CreateVBOs()
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
        protected virtual void CreateVAOs()
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
        /// <summary>
        /// Renders this particular section of the mesh, binding the first texture to startTexUnit.
        /// 
        /// We can assume that global uniforms such as transformation matrices, which don't
        /// change when we render different parts of the same object, have already been bound.
        /// So all we need to do here is use the VAO that is specific to this mesh,
        /// and bind the material properties for this mesh part.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="toWorldMatrix"></param>
        /// <param name="program"></param>
        public virtual void renderMesh(Camera camera, Matrix4 toWorldMatrix, ShaderProgram program, int startTexUnit)
        {
            // Bind the stuff we need for this object (VAO, index buffer, program)
            GL.BindVertexArray(vaoHandle);
            material.useMaterialParameters(program, startTexUnit);

            GL.DrawElements(PrimitiveType.Triangles, indexBuffer.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);

            // Clean up
            material.unuseMaterialParameters(program, startTexUnit);
            GL.BindVertexArray(0);
        }
    }
}
