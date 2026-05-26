using Unity.Mathematics;

namespace MokoIndustry.Foundation.Grid
{
    public static class GridUtility
    {
        public static float3 CellToWorld(int2 cell, in GridConfigSingleton config)
        {
            return config.Origin + new float3(
                cell.x * config.CellSize + config.CellSize * 0.5f,
                0f,
                cell.y * config.CellSize + config.CellSize * 0.5f
            );
        }

        public static int2 WorldToCell(float3 world, in GridConfigSingleton config)
        {
            float3 local = world - config.Origin;
            return new int2(
                (int)math.floor(local.x / config.CellSize),
                (int)math.floor(local.z / config.CellSize)
            );
        }

        public static bool IsInBounds(int2 cell, in GridConfigSingleton config)
        {
            return cell.x >= 0 && cell.x < config.Size.x
                && cell.y >= 0 && cell.y < config.Size.y;
        }
    }
}