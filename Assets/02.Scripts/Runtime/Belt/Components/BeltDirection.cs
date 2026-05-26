using Unity.Mathematics;

namespace MokoIndustry.Belt
{
    public enum BeltDirection : byte
    {
        East  = 0,
        North = 1,
        West  = 2,
        South = 3,
    }

    public static class BeltDirectionExtensions
    {
        /// <summary> Convert BeltDirection to GridCell Offset </summary>
        public static int2 ToOffset(this BeltDirection dir) => dir switch
        {
            BeltDirection.East  => new int2( 1, 0),
            BeltDirection.North => new int2( 0, 1),
            BeltDirection.West  => new int2(-1, 0),
            BeltDirection.South => new int2( 0,-1),
            _                   => int2.zero,
        };

        /// <summary> Convert BeltDirection to Radian (for Rendering / Rotation) </summary>
        public static float ToRadians(this BeltDirection dir) => dir switch
        {
            BeltDirection.East  => 0f,
            BeltDirection.North => math.PI * 0.5f,
            BeltDirection.West  => math.PI,
            BeltDirection.South => math.PI * 1.5f,
            _                   => 0f,
        };
    }
}