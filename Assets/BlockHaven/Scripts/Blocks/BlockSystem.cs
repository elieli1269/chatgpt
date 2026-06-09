using System;
using UnityEngine;

namespace BlockHaven.Blocks
{
    public enum BlockType
    {
        Air = 0,
        Grass = 1,
        Dirt = 2,
        Stone = 3,
        Wood = 4,
        Planks = 5,
        Road = 6,
        Glass = 7,
        Water = 8,
        Brick = 9,
        Lamp = 10,
        Metal = 11
    }

    [Serializable]
    public struct BlockRecord
    {
        public int x;
        public int y;
        public int z;
        public BlockType type;

        public BlockRecord(Vector3Int position, BlockType blockType)
        {
            x = position.x;
            y = position.y;
            z = position.z;
            type = blockType;
        }

        public Vector3Int Position => new Vector3Int(x, y, z);
    }

    public static class BlockSystem
    {
        public const int ChunkSize = 16;
        public const int ChunkHeight = 48;
        public const int ViewDistanceInChunks = 5;

        public static bool IsSolid(BlockType type) => type != BlockType.Air && type != BlockType.Water;
        public static bool IsTransparent(BlockType type) => type == BlockType.Air || type == BlockType.Water || type == BlockType.Glass || type == BlockType.Lamp;

        public static Color ColorFor(BlockType type)
        {
            switch (type)
            {
                case BlockType.Grass: return new Color(0.23f, 0.68f, 0.20f);
                case BlockType.Dirt: return new Color(0.45f, 0.28f, 0.13f);
                case BlockType.Stone: return new Color(0.48f, 0.48f, 0.50f);
                case BlockType.Wood: return new Color(0.43f, 0.24f, 0.10f);
                case BlockType.Planks: return new Color(0.72f, 0.48f, 0.25f);
                case BlockType.Road: return new Color(0.08f, 0.08f, 0.09f);
                case BlockType.Glass: return new Color(0.62f, 0.9f, 1f, 0.55f);
                case BlockType.Water: return new Color(0.1f, 0.35f, 0.9f, 0.62f);
                case BlockType.Brick: return new Color(0.58f, 0.16f, 0.12f);
                case BlockType.Lamp: return new Color(1f, 0.88f, 0.38f);
                case BlockType.Metal: return new Color(0.30f, 0.33f, 0.36f);
                default: return Color.clear;
            }
        }
    }
}
