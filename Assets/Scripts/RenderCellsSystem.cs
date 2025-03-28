using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Core
{
    [UpdateAfter(typeof(SimulateSystem))]
    partial struct RenderCellsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Cell>();
            state.RequireForUpdate<CellState>();
            state.RequireForUpdate<Input>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
             var cellsStateBuffer = SystemAPI.GetSingletonBuffer<CellState>();
             var config          = SystemAPI.GetSingleton<Config>();
             var input = SystemAPI.GetSingleton<Input>();
             state.Dependency = UpdateCellColor( state.Dependency, ref state, cellsStateBuffer, config, input );
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
                SelectedCell = input.IsSelectedCell ? input.SelectedCell : new int2(int.MaxValue, int.MaxValue),
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
            public int2 SelectedCell;

            public void Execute( ref URPMaterialPropertyBaseColor color, ref LocalTransform trans, Cell cell, [EntityIndexInQuery] int entityIndex )
            {
                var temperature = StateBuffer[ entityIndex ].Temperature;
                if( temperature >= 0 )
                    color.Value = math.lerp( NeutralColor, HotColor, math.saturate( temperature) );
                else
                    color.Value = math.lerp( NeutralColor, ColdColor, math.saturate( -temperature ) );
                trans.Scale = math.all( cell.position == SelectedCell ) ? 1f : 0.8f;

            }
        }
    }
}
