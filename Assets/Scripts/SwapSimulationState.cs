using Unity.Burst;
using Unity.Entities;

namespace Core
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
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
            var simulStateEntity = SystemAPI.GetSingletonEntity<SimulationState>();
            var simulState = state.EntityManager.GetComponentData<SimulationState>( simulStateEntity );
            simulState.SwapBuffers();
            state.EntityManager.SetComponentData( simulStateEntity, simulState );
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}