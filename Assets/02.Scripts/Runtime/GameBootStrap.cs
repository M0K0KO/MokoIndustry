using MokoIndustry.Foundation;
using Unity.Entities;
using UnityEngine;

namespace MokoIndustry
{
    public class GameBootStrap : MonoBehaviour
    {
        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var fixedGroup = world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            fixedGroup.Timestep = (float)FoundationConstants.TickDuration;
        }
    }
}