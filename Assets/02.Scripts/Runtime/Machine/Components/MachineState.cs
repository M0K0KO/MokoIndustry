using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Machine
{
    public struct MachineState : IComponentData
    {
        public ushort ProgressTicks;
        public FixedList64Bytes<byte> InputBuffer;
        public FixedList64Bytes<byte> OutputBuffer;
    }
}