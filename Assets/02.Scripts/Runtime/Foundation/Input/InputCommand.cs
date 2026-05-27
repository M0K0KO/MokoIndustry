using MokoIndustry.Foundation.Common;
using Unity.Mathematics;

namespace MokoIndustry.Foundation.Input
{
    public enum CommandType : byte
    {
        None,
        Build,
        Demolish,
        DebugInject
        // There will be more of these CommandTypes (ex. Upgrade, PlayerMove etc...)
    }

    public struct InputCommand
    {
        public CommandType Type;
        public BuildingType Building;
        public Direction4 Direction;
        public byte PlayerId;       // For Multiplayer Feature
        public int TargetTick;      
        public int2 Cell;           // TargetCell
        public int BuildableId;     // What Buildable Object this is
    }
}