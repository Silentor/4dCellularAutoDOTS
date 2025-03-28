using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;


namespace Core
{
    partial struct Generate2DSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            // Get the entity with the Config component
            var config = SystemAPI.GetSingleton<Config>();
            // Get the CellPrefab from the Config component
            var cellPrefab = config.CellPrefab;

            var rnd = new Random( config.Seed );

            // Create a new render entity for each cell in the grid
            for (int x = 0; x < Config.GridSize; x++)
            {
                for (int y = 0; y < Config.GridSize; y++)
                {
                    var cellEntity = state.EntityManager.Instantiate(cellPrefab);
                }
            }

            var cellsCount = Config.GridSize * Config.GridSize;
            {
                //Create simulation grid state buffer
                var gridEntity = state.EntityManager.CreateSingletonBuffer<CellState>( "Grid" );
                var buffer     = state.EntityManager.GetBuffer<CellState>( gridEntity );
                buffer.Length = cellsCount;
                for ( int i = 0; i < buffer.Length; i++ )
                {
                    buffer[i] = new CellState()
                    {
                            Temperature = rnd.NextFloat( -1, 1 ),
                    };
                }
            }

            {
                //Init cells
                var x = 0;
                var z = 0;

                foreach ( var (trans, cell) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Cell>>() )
                {
                    trans.ValueRW.Position.x = x + 0.5f;
                    trans.ValueRW.Position.z = z + 0.5f;
                    cell.ValueRW.position = new int2( x, z );

                    x++;
                    if ( x >= Config.GridSize )
                    {
                        x = 0;
                        z++;
                    }
                }
            }

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
