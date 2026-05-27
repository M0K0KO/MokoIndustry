namespace MokoIndustry.Belt
{
    public struct MoveIntent
    {
        public int SourceIdx;
        public int DestIdx;
        public ItemId Item;

        public byte Priority;
    }
}