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
        MeshContainer meshes;

        public MeshNode(MeshContainer m)
            : base()
        {
            meshes = m;
        }

        public MeshNode(MeshContainer m, Vector3 loc)
            : base(loc)
        {
            meshes = m;
        }

        public override void render(Camera camera)
        {
            meshes.renderMeshes(camera, toWorldMatrix);
            renderChildren(camera);
        }
    }
}
