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
            children = new LinkedList<SceneTreeNode>();
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
        protected LinkedList<SceneTreeNode> children;

        /// <summary>
        /// Adds a new node as the child of this node.
        /// Be aware that the child retains its current modeling transformation.
        /// </summary>
        /// <param name="c">The child node to be added.</param>
        public void addChild(SceneTreeNode c)
        {
            children.AddFirst(c);
        }

        /// <summary>
        /// Timestep all of the contents of this scene tree node, updating
        /// position, rotation, scale, and any other attributes.
        /// Updates the modeling transformations of this object to reflect the changes.
        /// </summary>
        /// <param name="e">Data describing what's happened between now and the previous frame.</param>
        /// <param name="parentModelMatrix">The modeling transformation of the parent.</param>
        abstract public void update(FrameEventArgs e, Matrix4 parentToWorldMatrix);

        /// <summary>
        /// Renders all of the contents of this scene tree node, using the
        /// modeling transformation that was computed during the previous call to update.
        /// </summary>
        /// <param name="viewMatrix">The camera's view matrix.</param>
        /// <param name="projectionMatrix">The camera's projection matrix.</param>
        abstract public void render(Matrix4 viewMatrix, Matrix4 projectionMatrix);
    }
}
