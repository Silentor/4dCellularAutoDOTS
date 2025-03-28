using Unity.Entities;
using Unity.Mathematics;

namespace Core
{
    public struct Input : IComponentData
    {
        public bool IsSelectedCell;
        public int2 SelectedCell;
        public float TemperatureDiff;
    }
}