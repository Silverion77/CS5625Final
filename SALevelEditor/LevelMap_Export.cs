using System;
using System.Collections.Generic;

using System.IO;

using OpenTK;

namespace SALevelEditor
{
    public partial class LevelMap
    {
        public void exportToFile(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                // Write dimensions
                sw.WriteLine("width {0}", dimensions.X);
                sw.WriteLine("height {0}", dimensions.Y);

                sw.WriteLine("tileSideLength {0}", tileSideLength);
                sw.WriteLine("wallHeight {0}", wallHeight);

                // Write all the tiles in sequence
                for (int x = 0; x < dimensions.X; x++)
                {
                    for (int y = 0; y < dimensions.Y; y++)
                    {
                        sw.WriteLine("tile {0}", tiles[x, y]);
                    }
                }

                // Write Okuu's starting position
                sw.WriteLine("okuu {0} {1}", okuuPosition.X, okuuPosition.Y);

                // Write finish position
                sw.WriteLine("goal {0} {1}", goalPosition.X, goalPosition.Y);

                // Write all zombie fairy positions
                foreach (Vector2 loc in zombieFairies)
                {
                    sw.WriteLine("zombie {0} {1}", loc.X, loc.Y);
                }
            }
        }
    }
}
