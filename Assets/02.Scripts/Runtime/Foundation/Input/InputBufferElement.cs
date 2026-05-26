using Unity.Entities;

namespace MokoIndustry.Foundation.Input
{
    // initial 16 commands are stored inside the chunk.
    // if it exceeds, it will be automatically allocated in heap
    [InternalBufferCapacity(16)]
    public struct InputBufferElement : IBufferElementData
    {
        public InputCommand Command;
    }
}