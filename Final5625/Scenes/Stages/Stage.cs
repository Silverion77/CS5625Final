using System;
using System.Collections.Generic;

using OpenTK;

using Chireiden.Materials;
using Chireiden.Meshes;

namespace Chireiden.Scenes.Stages
{
    public partial class Stage : SceneTreeNode
    {
        int[,] tiles;
        int width;
        int height;

        List<StageTilesNode> stageElements;

        public Stage(int[,] tiles, int tileSideLength, int wallHeight)
        {
            this.tiles = tiles;
            width = tiles.GetUpperBound(0) + 1;
            height = tiles.GetUpperBound(1) + 1;

            stageElements = new List<StageTilesNode>();

            setUpStage(tiles, tileSideLength, wallHeight);

            toWorldMatrix = Matrix4.Identity;
            toParentMatrix = Matrix4.Identity;
        }

        static StageTilesNode stageMaterialOfID(int id, MaterialTileCollection matTiles)
        {
            switch (id)
            {
                default:
                    Texture lambertTex = TextureManager.getTexture("data/texture/edv.png");
                    Material m = new LambertianMaterial(new Vector4(1, 1, 1, 1), lambertTex, 10, 10);
                    return new StageTilesNode(matTiles.Vertices.ToArray(), matTiles.Faces.ToArray(),
                        matTiles.Normals.ToArray(), matTiles.TexCoords.ToArray(), matTiles.Tangents.ToArray(), m);
            }
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            // Nothing needs to be done here
        }

        public override void render(Camera camera)
        {
            // Just render each piece of the scene
            foreach (StageTilesNode stageElement in stageElements)
            {
                stageElement.render(camera);
            }
        }
    }
}
