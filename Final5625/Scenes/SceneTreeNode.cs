using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Scenes
{
    /// <summary>
    /// All objects in the game will be organized into a scene tree, where each node
    /// represents some object in the scene (whether visible or not).
    /// Children inherit the transformations of their parents.
    /// 
    /// Intended workflow:
    /// 
    ///     Each timestep, we call update on the root node, which will recursively update
    ///     everything in the world. Along the way it should compute toParentMatrix and
    ///     toWorldMatrix. The former can be computed by just looking at its local transformations,
    ///     while the latter is computed by recursively multiplying down the chain.
    ///     
    ///     Once we have that, we should check game logic using the new positions.
    ///     
    ///     After this, we should render everything by calling render on the root node,
    ///     which will display everything in the world. (Obviously we should have some
    ///     sort of frustum culling here.) We pass in the view and projection matrices,
    ///     which should be passed unchanged during all the recursive calls. Each
    ///     element can just pass these as uniforms to whichever program it uses.
    ///     
    /// </summary>
    public abstract class SceneTreeNode
    {
        public SceneTreeNode()
        {
            toWorldMatrix = Matrix4.Identity;
            toParentMatrix = Matrix4.Identity;
            children = new List<SceneTreeNode>();
        }

        /// <summary>
        /// Returns the matrix that transforms points from this object's local space
        /// to world space. If this object has no parent (i.e. it is the world),
        /// then this is the identity matrix.
        /// </summary>
        protected Matrix4 toWorldMatrix;

        /// <summary>
        /// Returns the matrix that transforms points from this object's local space
        /// to its parent's space. If this object has no parent (i.e. it is the world),
        /// then this is the identity matrix.
        /// </summary>
        protected Matrix4 toParentMatrix;

        /// <summary>
        /// The list of children of this scene tree node.
        /// </summary>
        protected List<SceneTreeNode> children;

        /// <summary>
        /// Adds a new node as the child of this node.
        /// Be aware that the child retains its current modeling transformation.
        /// </summary>
        /// <param name="c">The child node to be added.</param>
        public void addChild(SceneTreeNode c)
        {
            children.Add(c);
        }

        /// <summary>
        /// Removes the given node from the children of this node.
        /// </summary>
        /// <param name="c"></param>
        public void removeChild(SceneTreeNode c)
        {
            children.Remove(c);
        }

        /// <summary>
        /// Updates the modeling transformations of this object to reflect the changes.
        /// </summary>
        /// <param name="e">Data describing what's happened between now and the previous frame.</param>
        /// <param name="parentToWorldMatrix">The modeling transformation of the parent.</param>
        abstract public void update(FrameEventArgs e, Matrix4 parentToWorldMatrix);

        /// <summary>
        /// Renders the contents of this scene tree node and all its children from the
        /// viewpoint of the camera, using the modeling transformation that was computed
        /// previously by update().
        /// </summary>
        /// <param name="camera"></param>
        abstract public void render(Camera camera);

        /// <summary>
        /// Updates every child of this scene tree node, using our toWorldMatrix.
        /// Every implementation of update should call this at the end.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="toWorldMatrix"></param>
        public void updateChildren(FrameEventArgs e, Matrix4 toWorldMatrix)
        {
            foreach (SceneTreeNode c in children)
            {
                c.update(e, toWorldMatrix);
            }
        }

        /// <summary>
        /// Renders every child of this scene tree node.
        /// Every implementation of render should call this at the end.
        /// </summary>
        /// <param name="viewMatrix"></param>
        /// <param name="projectionMatrix"></param>
        public void renderChildren(Camera camera)
        {
            foreach (SceneTreeNode c in children)
            {
                c.render(camera);
            }
        }
    }
}
