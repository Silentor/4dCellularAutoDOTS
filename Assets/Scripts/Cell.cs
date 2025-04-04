using Unity.Entities;
using Unity.Mathematics;

namespace Core
{
    public struct Cell : IComponentData
    {
    }

    public struct CellState : IBufferElementData
    {
        public float Temperature;
        public float Illness;
        public float Height;
        public float HeightDiff;            //Height change against the previous frame, need for proper wave simulation
    }
}