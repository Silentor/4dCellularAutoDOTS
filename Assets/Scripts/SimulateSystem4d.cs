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
    partial struct SimulateSystem4d : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Tag_4dWorkflow>();
            state.RequireForUpdate<SimulationState>();
            state.RequireForUpdate<Input>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state )
        {
            var simulStateEntity = SystemAPI.GetSingletonEntity<SimulationState>();
            var simulationState = state.EntityManager.GetComponentData<SimulationState>( simulStateEntity );
            var currentBuffer = state.EntityManager.GetBuffer<CellState>( simulationState.GetCurrentBuffer() ); //actually frame - 2
            var prevBuffer = state.EntityManager.GetBuffer<CellState>( simulationState.GetPreviousBuffer() );   //actually frame - 1
            var config          = SystemAPI.GetSingleton<Config>();
            var input = SystemAPI.GetSingleton<Input>();

            //Process input
            if ( input.IsSelectedCell && input.Clicked )
                    //ChangeTemperature( prevBuffer, input.SelectedCell.x, input.SelectedCell.y, input.TemperatureDiff );
            {
                var selectedCell4d = new int4( input.SelectedCell, input.WCoord );
                AddWave( prevBuffer, selectedCell4d, input.HeightDiff );
            }

            state.Dependency = SimulateCellularAuto( state.Dependency, ref state, prevBuffer, currentBuffer, config );
        }

        [BurstCompile]
        private JobHandle SimulateCellularAuto(JobHandle dependency, ref SystemState state, DynamicBuffer<CellState> inputBuffer, DynamicBuffer<CellState> outputBuffer, Config config )
        {
            // var job = new HeatSpreadJob_SingleThreaded()
            //           {
            //                   Input                = inputBuffer,
            //                   Output                = outputBuffer,
            //                   HeatSpreadSpeedScaled = SystemAPI.Time.DeltaTime * config.HeatSpreadSpeed,
            //           };
            if ( inputBuffer.Length <= 65536 )
            {
                var job = new WaveSpreadJob_SingleThreaded()
                          {
                                  Buffer1        = inputBuffer,
                                  Buffer2        = outputBuffer,
                                  DampingDivisor = 64,
                          };
                return job.Schedule( dependency );
            }
            else
            {
                var job = new WaveSpreadJob_Parallel()
                          {
                                  Buffer1        = inputBuffer,
                                  Buffer2        = outputBuffer,
                                  DampingDivisor = 64,
                          };
                return job.Schedule( inputBuffer.Length, 2048, dependency );
            }
        }

        private void ChangeTemperature(DynamicBuffer<CellState> cellsStateBuffer, int row, int col, float tempDiff)
        {
            var index = PositionUtils.PositionToIndex( row, col );
            cellsStateBuffer.ElementAt( index ).Temperature += tempDiff;
        }

        private void AddWave(DynamicBuffer<CellState> cellsStateBuffer, int4 pos, float waveHeight )
        {
            var index = PositionUtils.PositionToIndex( pos );
            cellsStateBuffer.ElementAt( index ).Height += waveHeight;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        [BurstCompile]
        public struct HeatSpreadJob_SingleThreaded : IJob         //Not converted
        {
            [NativeDisableContainerSafetyRestriction]
            public            DynamicBuffer<CellState> Output;
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public DynamicBuffer<CellState> Input;

            public            float                  HeatSpreadSpeedScaled;

            public void Execute()
            {
                for (int i = 0; i < Input.Length; i++)
                {
                    var pos = PositionUtils.IndexToPosition2( i );
                    var row = pos.x;
                    var col = pos.y;

                    //Grid is wrapped
                    var prevCol = (col - 1 + Config.GridSize) % Config.GridSize;
                    var nextCol = (col + 1) % Config.GridSize;
                    var prevRow = (row - 1 + Config.GridSize) % Config.GridSize;
                    var nextRow = (row  + 1) % Config.GridSize;

                    //Average temperature of the 4 neighbors
                    var targetAverage = 0f;
                    targetAverage += GetTemperature( prevRow, col );
                    targetAverage += GetTemperature( nextRow, col );
                    targetAverage += GetTemperature( row, prevCol );
                    targetAverage += GetTemperature( row, nextCol );
                    targetAverage /= 4f;
                    var difference = targetAverage - Input[i].Temperature;

                    //Slow down the dissipation
                    var value = Input[i].Temperature + math.clamp( difference, -HeatSpreadSpeedScaled, HeatSpreadSpeedScaled );

                    var state = Output[ i ];
                    state.Temperature = value;
                    Output[i] = state;
                }
            }

            private float GetTemperature(int row, int col)
            {
                return Input[PositionUtils.PositionToIndex( row, col )].Temperature;
            }
        }

        //https://web.archive.org/web/20160418004149/http://freespace.virgin.net/hugo.elias/graphics/x_water.htm
        [BurstCompile]
        public struct WaveSpreadJob_SingleThreaded : IJob                 //4D
        {
            [NativeDisableContainerSafetyRestriction]
            public            DynamicBuffer<CellState> Buffer2;
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public DynamicBuffer<CellState> Buffer1;
            public            float                  DampingDivisor;

            public void Execute()
            {
                for (int i = 0; i < Buffer1.Length; i++)
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

                    var targetAverage = 0f;
                    targetAverage += GetHeight( prevX, y, z, w );
                    targetAverage += GetHeight( nextX, y, z, w );
                    targetAverage += GetHeight( x, prevY, z, w );
                    targetAverage += GetHeight( x, nextY, z, w );
                    targetAverage += GetHeight( x, y, prevZ, w );
                    targetAverage += GetHeight( x, y, nextZ, w );
                    targetAverage += GetHeight( x, y, z, prevW );
                    targetAverage += GetHeight( x, y, z, nextW );
                    targetAverage /= 8f;

                    var state = Buffer2[ i ];
                    var height = targetAverage - state.Height;       //Move height to zero with velocity from past wave height. Smart trick!
                    height -= (height / DampingDivisor);                              //Damping
                    state.Height = height;

                    Buffer2[ i ] = state;
                }
            }

            private float GetHeight(int x, int y, int z, int w)
            {
                return Buffer1[PositionUtils.PositionToIndex( x, y, z, w )].Height;
            }
        }

        //https://web.archive.org/web/20160418004149/http://freespace.virgin.net/hugo.elias/graphics/x_water.htm
        [BurstCompile]
        public struct WaveSpreadJob_Parallel : IJobParallelFor
        {
            [NativeDisableContainerSafetyRestriction]
            public            DynamicBuffer<CellState> Buffer2;
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public DynamicBuffer<CellState> Buffer1;
            public            float                  DampingDivisor;

            public void Execute(Int32 index )
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

                var targetAverage = 0f;
                targetAverage += GetHeight( prevX, y, z, w );
                targetAverage += GetHeight( nextX, y, z, w );
                targetAverage += GetHeight( x, prevY, z, w );
                targetAverage += GetHeight( x, nextY, z, w );
                targetAverage += GetHeight( x, y, prevZ, w );
                targetAverage += GetHeight( x, y, nextZ, w );
                targetAverage += GetHeight( x, y, z, prevW );
                targetAverage += GetHeight( x, y, z, nextW );
                targetAverage /= 8f;

                var state = Buffer2[ index ];
                var height = targetAverage - state.Height;       //Move height to zero with velocity from past wave height. Smart trick!
                height       -= (height / DampingDivisor);                              //Damping
                state.Height =  height;

                Buffer2[ index ] = state;
            }

            private float GetHeight(int x, int y, int z, int w)
            {
                return Buffer1[PositionUtils.PositionToIndex( x, y, z, w )].Height;
            }
        }
    }
}
