using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
             var cellsStateBuffer = SystemAPI.GetSingletonBuffer<CellState>();
             var config          = SystemAPI.GetSingleton<Config>();
             state.Dependency = UpdateCellColor( state.Dependency, ref state, cellsStateBuffer, config );
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        private JobHandle UpdateCellColor( JobHandle dependency, ref SystemState state, DynamicBuffer<CellState> cellState, Config config )
        {
            var job = new ColorCellJob
            {
                StateBuffer = cellState,
                NeutralColor = config.NeutralColor,
                HotColor = config.HotColor,
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

            public void Execute( ref URPMaterialPropertyBaseColor color, [EntityIndexInQuery] int entityIndex )
            {
                var temperature = StateBuffer[ entityIndex ].Temperature;
                color.Value = math.lerp( NeutralColor, HotColor, math.saturate( temperature) );
            }
        }
    }
}
