using Unity.Mathematics;

namespace MokoIndustry.Foundation.Common
{
    public enum Direction4 : byte
    {
        East  = 0,
        North = 1,
        West  = 2,
        South = 3,
    }

    public static class Direction4Extensions
    {
        /// <summary> Convert Direction4 to GridCell Offset </summary>
        public static int2 ToOffset(this Direction4 dir) => dir switch
        {
            Direction4.East => new int2(1, 0),
            Direction4.West => new int2(-1, 0),
            Direction4.North => new int2(0, 1),
            Direction4.South => new int2(0, -1),
            _                   => int2.zero,
        };

        /// <summary> Convert Direction4 to Radian (for Rendering / Rotation) </summary>
        public static float ToRadians(this Direction4 dir) => dir switch
        {
            Direction4.North => 0f,
            Direction4.West  => math.PI * 1.5f,
            Direction4.South => math.PI,
            Direction4.East  => math.PI * 0.5f,
            _                => 0f,
        };

        public static byte Bit(Direction4 d) => (byte)(1 << (int)d);
        public static byte Bit(int d) => (byte)(1 << d);

        public static bool Has(byte mask, Direction4 d) => (mask & Bit(d)) != 0;
        public static bool Has(byte mask, int d) => (mask & Bit(d)) != 0;

        public static Direction4 Opposite(Direction4 d) => (Direction4)(((int)d + 2) & 3);
        public static Direction4 Opposite(int d) => (Direction4)((d + 2) & 3);
    }
}