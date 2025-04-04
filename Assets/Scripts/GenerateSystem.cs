using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;


namespace Core
{
    partial struct GenerateSystem : ISystem
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

            // Create a new render entity for each cell in the grid (but only for 3d max)
            var maxRenderableEntities = math.min( config.GridTotalCount, Config.GridSize * Config.GridSize * Config.GridSize );
            for (int i = 0; i < maxRenderableEntities; i++)
            {
                state.EntityManager.Instantiate(cellPrefab);
            }

            {
                //Create simulation grid state buffer
                var simulStateEntity = state.EntityManager.CreateSingleton<SimulationState>( "SimulState" );
                var gridEntity1 = state.EntityManager.CreateEntity( );
                state.EntityManager.AddBuffer<CellState>( gridEntity1 );
                var buffer1     = state.EntityManager.GetBuffer<CellState>( gridEntity1 );
                buffer1.Length = config.GridTotalCount;
                for ( int i = 0; i < config.GridTotalCount; i++ )
                {
                    var initState = new CellState()
                                 {
                                         Temperature = rnd.NextFloat( -1, 1 ),
                                 };
                    buffer1[ i ] = initState;
                }
                
                state.EntityManager.SetComponentData( simulStateEntity, new SimulationState(gridEntity1) );
            }

            {
                //Init cells, make sure cells arranged according to the Cell index
                var index = 0;
                if ( config.Workflow == EWorkflow.Mode2D )
                {
                    foreach ( var (trans, _) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Cell>>() )
                    {
                        var pos = PositionUtils.IndexToPosition2( index++ );
                        trans.ValueRW.Position.x = pos.x + 0.5f;
                        trans.ValueRW.Position.z = pos.y + 0.5f;
                        trans.ValueRW.Position.y = 0.5f;
                    }
                }
                else if ( config.Workflow == EWorkflow.Mode3D || config.Workflow == EWorkflow.Mode4D )
                {
                    foreach ( var (trans, _) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Cell>>() )
                    {
                        var pos = PositionUtils.IndexToPosition3( index++ );
                        trans.ValueRW.Position.x = pos.x + 0.5f;
                        trans.ValueRW.Position.y = pos.y + 0.5f;
                        trans.ValueRW.Position.z = pos.z + 0.5f;
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
