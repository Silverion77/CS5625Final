using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes
{
    public class MeshGroup
    {
        List<TriMesh> meshes;

        public MeshGroup()
        {
            meshes = new List<TriMesh>();
        }

        public MeshGroup(List<TriMesh> ms)
        {
            meshes = new List<TriMesh>();
            this.meshes.AddRange(ms);
        }

        public void renderMesh(Camera camera, Matrix4 toWorldMatrix)
        {
            foreach (TriMesh m in meshes) {
                m.renderMesh(camera, toWorldMatrix);
            }
        }

        public void addMesh(TriMesh m)
        {
            meshes.Add(m);
        }

        public void removeMesh(TriMesh m)
        {
            meshes.Remove(m);
        }
    }
}
