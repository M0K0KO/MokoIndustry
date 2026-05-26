using Unity.Entities;

namespace MokoIndustry.Foundation.Input
{
    [UpdateInGroup(typeof(FoundationSystemGroup))]
    [UpdateAfter(typeof(MokoIndustry.Foundation.Tick.TickAdvanceSystem))]
    public partial class CommandApplySystemGroup : ComponentSystemGroup
    {

    }
}