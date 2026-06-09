using BlockHaven.Blocks;
using UnityEngine;

namespace BlockHaven.World
{
    public static class CityGenerator
    {
        public static void BuildSpawnCity(WorldManager world)
        {
            BuildRoads(world);
            BuildHouse(world, new Vector3Int(-26, 7, -22), "starter");
            BuildHouse(world, new Vector3Int(28, 7, -26), "family");
            BuildHouse(world, new Vector3Int(-48, 7, 24), "villa");
            BuildPublicBuilding(world, new Vector3Int(18, 7, 18), 16, 11, BlockType.Brick, "Hospital", BlockType.Glass);
            BuildPublicBuilding(world, new Vector3Int(-18, 7, 25), 14, 10, BlockType.Metal, "Police", BlockType.Glass);
            BuildPublicBuilding(world, new Vector3Int(0, 7, -46), 20, 9, BlockType.Planks, "Market", BlockType.Glass);
            PlantForestRing(world);
        }

        private static void BuildRoads(WorldManager world)
        {
            for (int x = -72; x <= 72; x++)
            {
                for (int w = -3; w <= 3; w++)
                {
                    world.SetBlock(new Vector3Int(x, 7, w), BlockType.Road, false);
                }
            }

            for (int z = -72; z <= 72; z++)
            {
                for (int w = -3; w <= 3; w++)
                {
                    world.SetBlock(new Vector3Int(w, 7, z), BlockType.Road, false);
                }
            }

            for (int i = -64; i <= 64; i += 16)
            {
                BuildLamp(world, new Vector3Int(i, 8, 5));
                BuildLamp(world, new Vector3Int(5, 8, i));
            }
        }

        private static void BuildHouse(WorldManager world, Vector3Int origin, string id)
        {
            BuildBox(world, origin, 12, 7, 10, BlockType.Planks, BlockType.Air);
            FillFloor(world, origin, 12, 10, BlockType.Wood);
            Roof(world, origin, 12, 10, 7);
            Door(world, origin + new Vector3Int(5, 1, 0));
            Window(world, origin + new Vector3Int(2, 3, 0));
            Window(world, origin + new Vector3Int(8, 3, 0));
            BuildFurniture(world, origin);
        }

        private static void BuildPublicBuilding(WorldManager world, Vector3Int origin, int width, int height, BlockType wall, string label, BlockType window)
        {
            BuildBox(world, origin, width, height, 14, wall, BlockType.Air);
            FillFloor(world, origin, width, 14, BlockType.Stone);
            for (int x = 2; x < width - 2; x += 4)
            {
                Window(world, origin + new Vector3Int(x, 4, 0), window);
            }
            Door(world, origin + new Vector3Int(width / 2, 1, 0));
            Sign(world, origin + new Vector3Int(1, height - 1, -1), label);
        }

        private static void BuildBox(WorldManager world, Vector3Int o, int sx, int sy, int sz, BlockType wall, BlockType inside)
        {
            for (int x = 0; x < sx; x++)
            for (int y = 0; y < sy; y++)
            for (int z = 0; z < sz; z++)
            {
                bool shell = x == 0 || z == 0 || x == sx - 1 || z == sz - 1 || y == sy - 1;
                world.SetBlock(o + new Vector3Int(x, y, z), shell ? wall : inside, false);
            }
        }

        private static void FillFloor(WorldManager world, Vector3Int o, int sx, int sz, BlockType type)
        {
            for (int x = 0; x < sx; x++)
            for (int z = 0; z < sz; z++)
            {
                world.SetBlock(o + new Vector3Int(x, 0, z), type, false);
            }
        }

        private static void Roof(WorldManager world, Vector3Int o, int sx, int sz, int y)
        {
            for (int x = -1; x <= sx; x++)
            for (int z = -1; z <= sz; z++)
            {
                world.SetBlock(o + new Vector3Int(x, y, z), BlockType.Brick, false);
            }
        }

        private static void Door(WorldManager world, Vector3Int p)
        {
            world.SetBlock(p, BlockType.Air, false);
            world.SetBlock(p + Vector3Int.up, BlockType.Air, false);
        }

        private static void Window(WorldManager world, Vector3Int p, BlockType type = BlockType.Glass)
        {
            world.SetBlock(p, type, false);
            world.SetBlock(p + Vector3Int.right, type, false);
        }

        private static void Sign(WorldManager world, Vector3Int p, string label)
        {
            for (int i = 0; i < Mathf.Min(label.Length, 8); i++)
            {
                world.SetBlock(p + new Vector3Int(i, 0, 0), BlockType.Lamp, false);
            }
        }

        private static void BuildFurniture(WorldManager world, Vector3Int o)
        {
            world.SetBlock(o + new Vector3Int(2, 1, 7), BlockType.Wood, false);
            world.SetBlock(o + new Vector3Int(3, 1, 7), BlockType.Wood, false);
            world.SetBlock(o + new Vector3Int(8, 1, 6), BlockType.Planks, false);
        }

        private static void BuildLamp(WorldManager world, Vector3Int p)
        {
            for (int y = 0; y < 4; y++)
            {
                world.SetBlock(p + new Vector3Int(0, y, 0), BlockType.Metal, false);
            }
            world.SetBlock(p + new Vector3Int(0, 4, 0), BlockType.Lamp, false);
        }

        private static void PlantForestRing(WorldManager world)
        {
            for (int x = -105; x <= 105; x += 15)
            for (int z = -105; z <= 105; z += 15)
            {
                if (Mathf.Abs(x) < 80 && Mathf.Abs(z) < 80) continue;
                for (int y = 7; y < 12; y++) world.SetBlock(new Vector3Int(x, y, z), BlockType.Wood, false);
                for (int dx = -2; dx <= 2; dx++)
                for (int dz = -2; dz <= 2; dz++)
                for (int dy = 10; dy <= 13; dy++) world.SetBlock(new Vector3Int(x + dx, dy, z + dz), BlockType.Grass, false);
            }
        }
    }
}
