using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Logistics
{
    // pure FIFO Buffer, Round Robin Output
    public struct RouterSegment : IComponentData
    {
        public const int Capacity = 8;

        // ItemId FIFO, [0]이 다음 출력 대상, Count - 1이 tail
        public FixedList32Bytes<byte> Buffer;

        // 다음 출력 후보 인덱스
        public byte RoundRobinPtr;

        public byte OutputCooldown;

        public bool HasSpace => Buffer.Length < Capacity;
        public bool IsEmpty => Buffer.Length == 0;
    }
}