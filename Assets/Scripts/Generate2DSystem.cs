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

            {
                //Create simulation grid state buffer
                var simulStateEntity = state.EntityManager.CreateSingleton<SimulationState>( "SimulState" );
                var gridEntity1 = state.EntityManager.CreateEntity( );
                var gridEntity2 = state.EntityManager.CreateEntity( );
                state.EntityManager.AddBuffer<CellState>( gridEntity1 );
                state.EntityManager.AddBuffer<CellState>( gridEntity2 );
                var buffer1     = state.EntityManager.GetBuffer<CellState>( gridEntity1 );
                var buffer2     = state.EntityManager.GetBuffer<CellState>( gridEntity2 );
                buffer1.Length = Config.GridTotalCount;
                buffer2.Length = Config.GridTotalCount;
                for ( int i = 0; i < Config.GridTotalCount; i++ )
                {
                    var initState = new CellState()
                                 {
                                         Temperature = rnd.NextFloat( -1, 1 ),
                                 };
                    buffer1[ i ] = initState;
                    buffer2[ i ] = initState;
                }

                
                state.EntityManager.SetComponentData( simulStateEntity, new SimulationState(gridEntity1, gridEntity2) );
            }

            {
                //Init cells, make sure cells arranged according to the Cell index
                var index = 0;
                foreach ( var (trans, _) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Cell>>() )
                {
                    var pos = PositionUtils.IndexToPosition2( index++ );
                    trans.ValueRW.Position.x = pos.x + 0.5f;
                    trans.ValueRW.Position.z = pos.y + 0.5f;
                }
            }

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
