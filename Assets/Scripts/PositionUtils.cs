using Unity.Burst;
using Unity.Mathematics;

namespace Core
{
    [BurstCompile]
    public static class PositionUtils
    {
        public  static int PositionToIndex( int x, int y )
        {
            return x + y * Config.GridSize;
        }

        public  static int PositionToIndex( int2 position )
        {
            return position.x + position.y * Config.GridSize;
        }

        public  static int2 IndexToPosition2( int index )
        {
            var x = index % Config.GridSize;
            var y = index / Config.GridSize;
            return new int2( x, y );
        }

        public  static int PositionToIndex( int x, int y, int z )
        {
            return x + y * Config.GridSize + z * Config.GridSize * Config.GridSize;
        }

        public  static int PositionToIndex( int3 position )
        {
            return position.x + position.y * Config.GridSize + position.z * Config.GridSize * Config.GridSize;
        }

        public  static int3 IndexToPosition3( int index )
        {
            var x = index % Config.GridSize;
            var y = (index / Config.GridSize) % Config.GridSize;
            var z = index  / (Config.GridSize * Config.GridSize);
            
            return new int3( x, y, z );
        }

        public static int PositionToIndex(int x, int y, int z, int w)
        {
            return x + y * Config.GridSize + z * Config.GridSize * Config.GridSize + w * Config.GridSize * Config.GridSize * Config.GridSize;
        }

        public static int PositionToIndex(int4 position)
        {
            return position.x + position.y * Config.GridSize + position.z * Config.GridSize * Config.GridSize + position.w * Config.GridSize * Config.GridSize * Config.GridSize;
        }

        public static int4 IndexToPosition4(int index)
        {
            var x = index  % Config.GridSize;
            var y = (index / Config.GridSize)                     % Config.GridSize;
            var z = (index / (Config.GridSize * Config.GridSize)) % Config.GridSize;
            var w = index  / (Config.GridSize * Config.GridSize * Config.GridSize);

            return new int4(x, y, z, w);
        }

    }
}