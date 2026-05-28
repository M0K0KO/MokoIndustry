namespace MokoIndustry.Belt
{
    public static class BeltConstants
    {
        public const int Capacity = 3;

        public const byte ItemSpace = 102;

        public const byte MaxPosition = 255;

        public const byte SpeedPerTick = 32;

        public const byte RouterOutputInterval = MaxPosition / SpeedPerTick;
    }
}