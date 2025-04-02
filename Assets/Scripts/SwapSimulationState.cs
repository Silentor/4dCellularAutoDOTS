using Unity.Burst;
using Unity.Entities;

namespace Core
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(SimulateSystem2d))]
    [UpdateBefore(typeof(SimulateSystem3d))]
    [UpdateBefore(typeof(SimulateSystem4d))]
    public partial struct SwapSimulationState : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulState = SystemAPI.GetSingletonRW<SimulationState>();
            simulState.ValueRW.SwapBuffers();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}