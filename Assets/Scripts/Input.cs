using Unity.Entities;
using Unity.Mathematics;

namespace Core
{
    public struct Input : IComponentData
    {
        public bool IsSelectedCell;
        public bool Clicked;
        public int3 SelectedCell;
        public float TemperatureDiff;
        public float HeightDiff;
        public int WCoord;
    }
}