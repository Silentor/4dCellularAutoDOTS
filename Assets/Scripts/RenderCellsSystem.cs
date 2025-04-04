using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Core
{
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
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
            var currentBuffer = SystemAPI.GetBuffer<CellState>( simulState.CellsBuffer );
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
                StateBuffer = cellState.AsNativeArray(),
                NeutralColor = config.NeutralColor,
                HotColor = config.HotColor,
                ColdColor = config.ColdColor,
                SelectedCellIndex = input.IsSelectedCell ? PositionUtils.PositionToIndex( input.SelectedCell ) : -1,
                WCoord = input.WCoord,
                Workflow = config.Workflow,
                CameraPos = input.CameraPosition,
                CarveSizeSq = input.CameraCarveSize * input.CameraCarveSize,
            };
            return job.Schedule( dependency );
        }

        [BurstCompile]
        [WithAll(typeof(Cell))]
        public partial struct ColorCellJob : IJobEntity
        {
            [ReadOnly] public NativeArray<CellState> StateBuffer;
            public float4 NeutralColor;
            public float4 HotColor;
            public float4 ColdColor;
            public int SelectedCellIndex;
            public int WCoord;

            public EWorkflow Workflow;
            public float3 CameraPos;
            public float CarveSizeSq;

            public void Execute( ref URPMaterialPropertyBaseColor color, ref LocalTransform trans, [EntityIndexInQuery] int entityIndex )
            {
                var coords4d = PositionUtils.IndexToPosition4( entityIndex );
                coords4d.w = WCoord;
                entityIndex = PositionUtils.PositionToIndex( coords4d );

                var state = StateBuffer[ entityIndex ];
                var temperature = state.Temperature;
                if( temperature >= 0 )
                    color.Value = math.lerp( NeutralColor, HotColor, math.saturate( temperature) );
                else
                    color.Value = math.lerp( NeutralColor, ColdColor, math.saturate( -temperature ) );

                var height = state.Height;
                trans.Scale = math.lerp( 0.5f, 1.5f, (height + 1 ) / 2 ) * 0.8f;
                trans.Scale = entityIndex == SelectedCellIndex ? 1f : trans.Scale;

                color.Value = math.lerp( color.Value, new float4( 0, 0, 0, 1 ), state.Illness );

                //Process carving
                if ( Workflow >= EWorkflow.Mode3D )
                {
                    var coords3d = coords4d.xyz;
                    var cameraVector = coords3d - CameraPos;
                    var squareDestination = LenghtSq( cameraVector );
                    if ( squareDestination < CarveSizeSq )
                    {
                        var distanceRatio  = squareDestination / CarveSizeSq;
                        var carveAmount = math.max( math.remap( 1f, 0.8f, 1f, 0, distanceRatio ), 0 );
                        trans.Scale *= carveAmount;
                    }
                }

            }

            public static float LenghtSq( float3 vector )
            {
                return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
            }
        }

    }
}
