using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Meshes;

namespace Chireiden.Scenes
{
    /// <summary>
    /// A scene tree node that wraps data for triangle meshes. If there are multiple meshes,
    /// they will move and deform together, and effectively be treated as the same object.
    /// This is because, if one 3D model contains multiple materials, each material needs
    /// to be treated as a separate mesh.
    /// </summary>
    public class MeshNode : MobileObject
    {
        List<TriMesh> meshes;

        public MeshNode()
            : base()
        {
            meshes = new List<TriMesh>();
        }

        public MeshNode(List<TriMesh> ms)
            : base()
        {
            meshes = new List<TriMesh>();
            meshes.AddRange(ms);
        }

        public MeshNode(List<TriMesh> ms, Vector3 loc)
            : base(loc)
        {
            meshes = new List<TriMesh>();
            meshes.AddRange(ms);
        }

        public override void render(Camera camera)
        {
            foreach (TriMesh m in meshes) {
                m.renderMesh(camera, toWorldMatrix);
            }
            renderChildren(camera);
        }
    }
}
