using Unity.Burst;
using Unity.Entities;

namespace Core
{
    [BurstCompile]
    public struct SimulationState : IComponentData
    {
        public readonly Entity CellsBuffer;            //DynamicBuffer<CellState>

        public bool ProcessSimulation;


        public SimulationState(Entity cellsBuffer ) : this()
        {
            CellsBuffer = cellsBuffer;
        }
    }
}