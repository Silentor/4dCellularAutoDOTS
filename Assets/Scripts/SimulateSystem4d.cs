using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Core
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    partial struct SimulateSystem4d : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Tag_4dWorkflow>();
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
                               Input               = input,
                               Output              = output,
                               ThermalConductivity = math.saturate( config.HeatSpreadSpeed ),
                       };
            dependency = job1.Schedule( input.Length, 2048, dependency );
            var job2 = new WaveSpreadJob()
                       {
                               Buffer1      = input,
                               Buffer2      = output,
                               DampingCoeff = math.saturate( config.WaveDampCoeff ),
                       };
            dependency = job2.Schedule( input.Length, 2048, dependency );
            var job3 = new IllSpreadJob()
                       {
                               Input    = input,
                               Output   = output,
                               IllSpeed = math.saturate( config.IllSpeed ),
                       };
            return job3.Schedule( input.Length, 2048, dependency );

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        [BurstCompile]
        public struct HeatSpreadJob : IJobParallelFor         
        {
            public            NativeArray<CellState> Output;
            [ReadOnly] public NativeArray<CellState> Input;
            public            float                  ThermalConductivity;

            public void Execute(  int i )
            {
                var pos = PositionUtils.IndexToPosition4( i );
                var x = pos.x;
                var y = pos.y;
                var z = pos.z;
                var w = pos.w;

                //Grid is wrapped
                var prevX = (x - 1 + Config.GridSize) % Config.GridSize;
                var nextX = (x     + 1)               % Config.GridSize;
                var prevY = (y - 1 + Config.GridSize) % Config.GridSize;
                var nextY = (y     + 1)               % Config.GridSize;
                var prevZ = (z - 1 + Config.GridSize) % Config.GridSize;
                var nextZ = (z     + 1)               % Config.GridSize;
                var prevW = (w - 1 + Config.GridSize) % Config.GridSize;
                var nextW = (w     + 1)               % Config.GridSize;

                //Average temperature of the neighbors
                var targetAverage = 0f;
                targetAverage += GetTemperature( prevX, y, z, w );
                targetAverage += GetTemperature( nextX, y, z, w );
                targetAverage += GetTemperature( x, prevY, z, w );
                targetAverage += GetTemperature( x, nextY, z, w );
                targetAverage += GetTemperature( x, y, prevZ, w );
                targetAverage += GetTemperature( x, y, nextZ, w );
                targetAverage += GetTemperature( x, y, z, prevW );
                targetAverage += GetTemperature( x, y, z, nextW );
                targetAverage /= 8f;
                    
                var currentTemp = Input[i].Temperature;
                var tempChange = targetAverage - currentTemp;
                tempChange  *= ThermalConductivity;                    //Temperature conductivity
                currentTemp += tempChange;

                var state = Output[ i ];
                state.Temperature = currentTemp;
                Output[i]         = state;
            }

            private float GetTemperature(int x, int y, int z, int w)
            {
                return Input[PositionUtils.PositionToIndex( x, y, z, w )].Temperature;
            }
        }

        //https://web.archive.org/web/20160418004149/http://freespace.virgin.net/hugo.elias/graphics/x_water.htm
        [BurstCompile]
        public struct WaveSpreadJob : IJobParallelFor                 
        {
            public            NativeArray<CellState> Buffer2;
            [ReadOnly] public NativeArray<CellState> Buffer1;
            public            float                  DampingCoeff;

            public void Execute( Int32 index)
            {
                var pos = PositionUtils.IndexToPosition4( index );
                var x = pos.x;
                var y = pos.y;
                var z = pos.z;
                var w = pos.w;

                //Grid is wrapped
                var prevX = (x - 1 + Config.GridSize) % Config.GridSize;
                var nextX = (x     + 1)               % Config.GridSize;
                var prevY = (y - 1 + Config.GridSize) % Config.GridSize;
                var nextY = (y     + 1)               % Config.GridSize;
                var prevZ = (z - 1 + Config.GridSize) % Config.GridSize;
                var nextZ = (z     + 1)               % Config.GridSize;
                var prevW = (w - 1 + Config.GridSize) % Config.GridSize;
                var nextW = (w     + 1)               % Config.GridSize;

                var smoothedHeight = 0f;
                smoothedHeight += GetHeight( prevX, y, z, w );
                smoothedHeight += GetHeight( nextX, y, z, w );
                smoothedHeight += GetHeight( x, prevY, z, w );
                smoothedHeight += GetHeight( x, nextY, z, w );
                smoothedHeight += GetHeight( x, y, prevZ, w );
                smoothedHeight += GetHeight( x, y, nextZ, w );
                smoothedHeight += GetHeight( x, y, z, prevW );
                smoothedHeight += GetHeight( x, y, z, nextW );

                smoothedHeight /= 4f;            //Intentional

                var state = Buffer2[ index ];
                var height = state.Height;     //PrevPrev height aka -wave welocity
                height       =  smoothedHeight - height;       //Move height to zero with velocity from past wave height. Smart trick!
                height       *= DampingCoeff;                              //Damping
                state.Height =  height;
                Buffer2[ index ] =  state;
            }

            private float GetHeight(int x, int y, int z, int w)
            {
                return Buffer1[PositionUtils.PositionToIndex( x, y, z, w )].Height;
            }
        }

        [BurstCompile]
        public struct IllSpreadJob : IJobParallelFor
        {
            public            NativeArray<CellState> Output;
            [ReadOnly] public NativeArray<CellState> Input;
            public            float                  IllSpeed;

            public void Execute( int index )
            {
                var pos = PositionUtils.IndexToPosition4( index  );
                var x = pos.x;
                var y = pos.y;
                var z = pos.z;
                var w = pos.w;

                //Grid is wrapped
                var prevX = (x - 1 + Config.GridSize) % Config.GridSize;
                var nextX = (x     + 1)               % Config.GridSize;
                var prevY = (y - 1 + Config.GridSize) % Config.GridSize;
                var nextY = (y     + 1)               % Config.GridSize;
                var prevZ = (z - 1 + Config.GridSize) % Config.GridSize;
                var nextZ = (z     + 1)               % Config.GridSize;
                var prevW = (w - 1 + Config.GridSize) % Config.GridSize;
                var nextW = (w     + 1)               % Config.GridSize;


                //Average illness of the 4 neighbors
                var targetAverage = 0f;
                targetAverage = math.max( targetAverage, GetIllness( prevX, y, z, w));
                targetAverage = math.max( targetAverage, GetIllness( nextX, y, z, w));
                targetAverage = math.max( targetAverage, GetIllness( x, prevY, z, w));
                targetAverage = math.max( targetAverage, GetIllness( x, nextY, z, w));
                targetAverage = math.max( targetAverage, GetIllness( x, y, prevZ, w));
                targetAverage = math.max( targetAverage, GetIllness( x, y, nextZ, w));
                targetAverage = math.max( targetAverage, GetIllness( x, y, z, prevW));
                targetAverage = math.max( targetAverage, GetIllness( x, y, z, nextW));
                //targetAverage /= 4f;

                var illSpeedNoise = math.clamp( noise.cnoise( (float4)pos / 3.7f ) / 2 + 0.5f, 0.1f, 1f ); //Add some random
                var currentIllness = Input[index].Illness;
                var diff = math.saturate( (targetAverage - currentIllness) * IllSpeed * illSpeedNoise ); 
                currentIllness = math.saturate( currentIllness + diff );  

                var state = Output[ index ];
                state.Illness = currentIllness;
                Output[index]     = state;
            }

            private float GetIllness(int x, int y, int z, int w)
            {
                return Input[PositionUtils.PositionToIndex( x, y, z, w )].Illness;
            }
        }


        
    }
}
