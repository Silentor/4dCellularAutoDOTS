using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Core
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    partial struct RenderCellsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Cell>();
            state.RequireForUpdate<SimulationState>();
            state.RequireForUpdate<Input>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulState = SystemAPI.GetSingleton<SimulationState>();
            var currentBuffer = state.EntityManager.GetBuffer<CellState>( simulState.GetCurrentBuffer() );
             var config          = SystemAPI.GetSingleton<Config>();
             var input = SystemAPI.GetSingleton<Input>();
             state.Dependency = UpdateCellColor( state.Dependency, ref state, currentBuffer, config, input );
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        private JobHandle UpdateCellColor( JobHandle dependency, ref SystemState state, DynamicBuffer<CellState> cellState, Config config, Input input )
        {
            var job = new ColorCellJob
            {
                StateBuffer = cellState,
                NeutralColor = config.NeutralColor,
                HotColor = config.HotColor,
                ColdColor = config.ColdColor,
                SelectedCellIndex = input.IsSelectedCell ? PositionUtils.PositionToIndex( input.SelectedCell ) : -1,
                WCoord = input.WCoord,
            };
            return job.Schedule( dependency );
        }

        [BurstCompile]
        [WithAll(typeof(Cell))]
        public partial struct ColorCellJob : IJobEntity
        {
            [ReadOnly] public DynamicBuffer<CellState> StateBuffer;
            public float4 NeutralColor;
            public float4 HotColor;
            public float4 ColdColor;
            public int SelectedCellIndex;
            public int WCoord;

            public void Execute( ref URPMaterialPropertyBaseColor color, ref LocalTransform trans, [EntityIndexInQuery] int entityIndex )
            {
                var coords4d = PositionUtils.IndexToPosition4( entityIndex );
                coords4d.w = WCoord;
                entityIndex = PositionUtils.PositionToIndex( coords4d );

                var temperature = StateBuffer[ entityIndex ].Temperature;
                if( temperature >= 0 )
                    color.Value = math.lerp( NeutralColor, HotColor, math.saturate( temperature) );
                else
                    color.Value = math.lerp( NeutralColor, ColdColor, math.saturate( -temperature ) );

                var height = StateBuffer[ entityIndex ].Height;
                trans.Scale = math.lerp( 0.5f, 1.5f, (height + 1 ) / 2 ) * 0.8f;
                trans.Scale = entityIndex == SelectedCellIndex ? 1f : trans.Scale;

            }
        }
    }
}
