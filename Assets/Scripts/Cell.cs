using Unity.Entities;

namespace Core
{
    public partial struct Cell : IComponentData
    {
        public int X;
        public int Y;
        public bool IsAlive;
    }

    public partial struct CellState : IBufferElementData
    {
        public float Temperature;
    }


}