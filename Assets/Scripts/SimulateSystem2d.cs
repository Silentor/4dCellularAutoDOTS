using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Core
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    partial struct SimulateSystem2d : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Tag_2dWorkflow>();
            state.RequireForUpdate<SimulationState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state )
        {
            var simulState = SystemAPI.GetSingleton<SimulationState>();
            var currentBuffer = SystemAPI.GetBuffer<CellState>( simulState.GetCurrentBuffer() ); //actually frame - 2, also it will be current buffer
            var prevBuffer = SystemAPI.GetBuffer<CellState>( simulState.GetPreviousBuffer() );   //actually frame - 1
            var config          = SystemAPI.GetSingleton<Config>();

            state.Dependency = SimulateCellularAuto( state.Dependency, ref state, prevBuffer, currentBuffer, config );
        }

        [BurstCompile]
        private JobHandle SimulateCellularAuto(JobHandle dependency, ref SystemState state, DynamicBuffer<CellState> prevBuffer, DynamicBuffer<CellState> prevPrev_CurrentBuffer, Config config )
        {
            var input = prevBuffer.ToNativeArray( state.WorldUpdateAllocator );
            var output = prevPrev_CurrentBuffer.AsNativeArray();
            var job1 = new HeatSpreadJob()
                      {
                              Input                = input,
                              Output                = output,
                              ThermalConductivity = math.saturate( config.HeatSpreadSpeed ),
                      };
            dependency = job1.Schedule( dependency );
            var job2 = new WaveSpreadJob()
                      {
                              Buffer1                 = input,
                              Buffer2                = output,
                              DampingCoeff = math.saturate( config.WaveDampCoeff ),
                      };
            // var job2 = new WaveSpreadHookeJob()
            //            {
            //                    Input      = input,
            //                    Output      = output,
            //                    RigidCoeff = config.WaveRigidCoeff,
            //                    DampingCoeff = config.WaveDampCoeff,
            //            };
            dependency = job2.Schedule( dependency );
            var job3 = new IllSpreadJob()
                       {
                               Input      = input,
                               Output      = output,
                               IllSpeed = math.saturate( config.IllSpeed ),
                       };
            return job3.Schedule( dependency );
        }



        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        [BurstCompile]
        public struct HeatSpreadJob : IJob
        {
            public            NativeArray<CellState> Output;
            [ReadOnly] public NativeArray<CellState> Input;
            public            float                  ThermalConductivity;

            public void Execute()
            {
                for (int i = 0; i < Input.Length; i++)
                {
                    var pos = PositionUtils.IndexToPosition2( i );
                    var x = pos.x;
                    var y = pos.y;

                    //Grid is wrapped
                    var prevX = (x - 1 + Config.GridSize) % Config.GridSize;
                    var nextX = (x     + 1)               % Config.GridSize;
                    var prevY = (y - 1 + Config.GridSize) % Config.GridSize;
                    var nextY = (y + 1) % Config.GridSize;

                    //Average temperature of the 4 neighbors
                    var targetAverage = 0f;
                    targetAverage += GetTemperature( prevX, y );
                    targetAverage += GetTemperature( nextX, y );
                    targetAverage += GetTemperature( x, prevY );
                    targetAverage += GetTemperature( x, nextY );
                    targetAverage /= 4f;

                    var currentTemp = Input[i].Temperature;
                    var tempChange = targetAverage - currentTemp;
                    tempChange *= ThermalConductivity;                    //Temperature conductivity
                    currentTemp += tempChange;

                    var state = Output[ i ];
                    state.Temperature = currentTemp;
                    Output[i] = state;
                }
            }

            private float GetTemperature(int row, int col)
            {
                return Input[PositionUtils.PositionToIndex( row, col )].Temperature;
            }
        }

        [BurstCompile]
        public struct IllSpreadJob : IJob
        {
            public            NativeArray<CellState> Output;
            [ReadOnly] public NativeArray<CellState> Input;
            public            float                  IllSpeed;

            public void Execute()
            {
                for (int i = 0; i < Input.Length; i++)
                {
                    var pos = PositionUtils.IndexToPosition2( i );
                    var x = pos.x;
                    var y = pos.y;

                    //Grid is wrapped
                    var prevX = (x - 1 + Config.GridSize) % Config.GridSize;
                    var nextX = (x     + 1)               % Config.GridSize;
                    var prevY = (y - 1 + Config.GridSize) % Config.GridSize;
                    var nextY = (y     + 1)               % Config.GridSize;

                    var maxIllness = 0f;
                    maxIllness = math.max( maxIllness, GetIllness( prevX, y ));
                    maxIllness = math.max( maxIllness, GetIllness( nextX, y ));
                    maxIllness = math.max( maxIllness, GetIllness( x, prevY ));
                    maxIllness = math.max( maxIllness, GetIllness( x, nextY ));
                    //targetAverage /= 4f;

                    var illSpeedNoise = math.clamp( noise.cnoise( (float2)pos / 3.7f ) / 2 + 0.5f, 0.1f, 1f ); //Add some random
                    var currentIllness = Input[i].Illness;
                    var diff = math.saturate( (maxIllness - currentIllness) * IllSpeed * illSpeedNoise ); 
                    currentIllness = math.saturate( currentIllness + diff );  

                    var state = Output[ i ];
                    state.Illness = currentIllness;
                    Output[i]         = state;
                }
            }

            private float GetIllness(int x, int y)
            {
                return Input[PositionUtils.PositionToIndex( x, y )].Illness;
            }
        }

        //https://web.archive.org/web/20160418004149/http://freespace.virgin.net/hugo.elias/graphics/x_water.htm
        [BurstCompile]
        public struct WaveSpreadJob : IJob
        {
            public            NativeArray<CellState> Buffer2;
            [ReadOnly] public NativeArray<CellState> Buffer1;
            public            float                  DampingCoeff;

            public void Execute()
            {
                for (int i = 0; i < Buffer1.Length; i++)
                {
                    var pos = PositionUtils.IndexToPosition2( i );
                    var row = pos.x;
                    var col = pos.y;

                    //Grid is wrapped
                    var prevCol = (col - 1 + Config.GridSize) % Config.GridSize;
                    var nextCol = (col     + 1)               % Config.GridSize;
                    var prevRow = (row - 1 + Config.GridSize) % Config.GridSize;
                    var nextRow = (row     + 1)               % Config.GridSize;

                    var smoothedHeight = 0f;
                    smoothedHeight += GetHeight( prevRow, col );
                    smoothedHeight += GetHeight( nextRow, col );
                    smoothedHeight += GetHeight( row, prevCol );
                    smoothedHeight += GetHeight( row, nextCol );
                    smoothedHeight /= 2f;                      //Intentional

                    var state = Buffer2[ i ];
                    var height = state.Height;     //PrevPrev height aka -wave welocity
                    height = smoothedHeight - height;       //Move height to zero with velocity from past wave height. Smart trick!
                    height *= DampingCoeff;                              //Damping
                    state.Height = height;
                    Buffer2[ i ] = state;
                }
            }

            private float GetHeight(int row, int col)
            {
                return Buffer1[PositionUtils.PositionToIndex( row, col )].Height;
            }
        }

        // [BurstCompile]
        // public struct WaveSpreadHookeJob : IJob
        // {
        //     public            NativeArray<CellState> Output;
        //     [ReadOnly] public NativeArray<CellState> Input;
        //
        //     public float RigidCoeff;
        //     public float DampingCoeff;
        //
        //     public void Execute( )
        //     {
        //         for (int i = 0; i < Input.Length; i++)
        //         {
        //             var height = Input[ i ].Height;
        //             var velocity = Input[ i ].HeightVelo;
        //             var accel = -height;
        //             
        //
        //             if( i == 0 )
        //                 Debug.Log( $"h {height}, a {accel}, v {velocity}" );
        //
        //             var pos = PositionUtils.IndexToPosition2( i );
        //             var row = pos.x;
        //             var col = pos.y;
        //             
        //             //Grid is wrapped
        //             var prevCol = (col - 1 + Config.GridSize) % Config.GridSize;
        //             var nextCol = (col     + 1)               % Config.GridSize;
        //             var prevRow = (row - 1 + Config.GridSize) % Config.GridSize;
        //             var nextRow = (row     + 1)               % Config.GridSize;
        //             
        //             var averageAccel = 0f;
        //             averageAccel += GetHeight( prevRow, col );
        //             averageAccel += GetHeight( nextRow, col );
        //             averageAccel += GetHeight( row, prevCol );
        //             averageAccel += GetHeight( row, nextCol );
        //             averageAccel /= -4f;                      
        //
        //             var cellInertValue = 1 - math.saturate( math.abs(height) * math.abs(velocity)) ;
        //
        //             accel += averageAccel * cellInertValue; //Inert cells more influenced by active neighbors
        //
        //             // var averageVelo = 0f;
        //             // averageVelo += GetHeightVelocity( prevRow, col );
        //             // averageVelo += GetHeightVelocity( nextRow, col );
        //             // averageVelo += GetHeightVelocity( row, prevCol );
        //             // averageVelo += GetHeightVelocity( row, nextCol );
        //             // averageVelo /= 32f;
        //             //velocity += averageVelo;
        //
        //             accel *= RigidCoeff; //Hooke's law
        //             velocity += accel * 0.1f;
        //             height += velocity * 0.1f;
        //             height *= DampingCoeff; 
        //
        //
        //             
        //             var state = Output[ i ];
        //             state.Height     = height;
        //             state.HeightVelo = velocity;
        //             Output[ i ]      = state;
        //
        //
        //             // var state = Buffer2[ i ];
        //             // var height = targetAverage - state.Height;       //Move height to zero with velocity from past wave height. Smart trick!
        //             // height       -= (height * DampingCoeff);                              //Damping
        //             // state.Height =  height;
        //             //
        //             // Buffer2[ i ] = state;
        //         }
        //     }
        //
        //     private float GetHeightVelocity(int x, int y )
        //     {
        //         return Input[PositionUtils.PositionToIndex( x, y )].HeightVelo;
        //     }
        //
        //     private float GetHeight(int x, int y )
        //     {
        //         return Input[PositionUtils.PositionToIndex( x, y )].Height;
        //     }
        //
        // }

    }
}
