using Unity.Entities;
using Unity.Mathematics;

namespace Core
{
    public struct Cell : IComponentData
    {
        public int2 position;
    }

    public struct CellState : IBufferElementData
    {
        public float Temperature;
    }


}