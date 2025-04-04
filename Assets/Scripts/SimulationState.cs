using Unity.Burst;
using Unity.Entities;

namespace Core
{
    [BurstCompile]
    public struct SimulationState : IComponentData
    {
        public readonly Entity CellsBuffer1;            //DynamicBuffer<CellState>
        public readonly Entity CellsBuffer2;            //DynamicBuffer<CellState>

        public int  CurrentBufferIndex;
        public bool ProcessSimulation;


        public SimulationState(Entity cellsBuffer1, Entity cellsBuffer2 ) : this()
        {
            CellsBuffer1 = cellsBuffer1;
            CellsBuffer2 = cellsBuffer2;
        }

        public void SwapBuffers()
        {
            CurrentBufferIndex = (CurrentBufferIndex + 1) % 2;
        }

        public Entity GetCurrentBuffer()
        {
            return CurrentBufferIndex == 0 ? CellsBuffer1 : CellsBuffer2;
        }

        public Entity GetPreviousBuffer()
        {
            return CurrentBufferIndex == 0 ? CellsBuffer2 : CellsBuffer1;
        }
    }
}