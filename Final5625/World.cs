using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden
{
    class World : SceneTreeNode
    {
        public World() : base()
        { }

        public void update(FrameEventArgs e)
        {
            foreach (SceneTreeNode c in children)
            {
                c.update(e, Matrix4.Identity);
            }

        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            update(e);
        }

        public override void render(Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            foreach (SceneTreeNode c in children)
            {
                c.render(viewMatrix, projectionMatrix);
            }
        }

    }
}
