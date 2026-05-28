namespace MokoIndustry.Belt
{
    public struct MoveIntent
    {
        public int SourceIdx;
        public int DestIdx;
        public ItemId Item;

        public byte Priority;

        // round-robin
        public byte SourceChosenDir;

        public byte CarryY;
    }
}