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
            fixedGroup.Timestep = 1.0f / 30.0f;
        }
    }
}