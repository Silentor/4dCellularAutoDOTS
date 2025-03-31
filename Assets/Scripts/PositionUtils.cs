using Unity.Burst;
using Unity.Mathematics;

namespace Core
{
    [BurstCompile]
    public static class PositionUtils
    {
        public  static int PositionToIndex( int x, int y )
        {
            return x * Config.GridSize + y;
        }

        public  static int PositionToIndex( int2 position )
        {
            return position.x * Config.GridSize + position.y;
        }

        public  static int2 IndexToPosition2( int index )
        {
            var x = index / Config.GridSize;
            var y = index % Config.GridSize;
            return new int2( x, y );
        }

    }
}