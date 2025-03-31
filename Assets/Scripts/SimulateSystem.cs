using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Core
{
    partial struct SimulateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
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
            if( input.IsSelectedCell && input.Clicked )
                //ChangeTemperature( prevBuffer, input.SelectedCell.x, input.SelectedCell.y, input.TemperatureDiff );
                AddWave( prevBuffer, input.SelectedCell.x, input.SelectedCell.y, input.HeightDiff );

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
            var job = new WaveSpreadJob_SingleThreaded()
                      {
                              Buffer1                 = inputBuffer,
                              Buffer2                = outputBuffer,
                              DampingDivisor = 64,
                      };
            return job.Schedule( dependency );
        }

        private void ChangeTemperature(DynamicBuffer<CellState> cellsStateBuffer, int row, int col, float tempDiff)
        {
            var index = PositionUtils.PositionToIndex( row, col );
            cellsStateBuffer.ElementAt( index ).Temperature += tempDiff;
        }

        private void AddWave(DynamicBuffer<CellState> cellsStateBuffer, int row, int col, float waveHeight )
        {
            var index = PositionUtils.PositionToIndex( row, col );
            cellsStateBuffer.ElementAt( index ).Height += waveHeight;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        [BurstCompile]
        public struct HeatSpreadJob_SingleThreaded : IJob
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
        public struct WaveSpreadJob_SingleThreaded : IJob
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
                    var pos = PositionUtils.IndexToPosition2( i );
                    var row = pos.x;
                    var col = pos.y;

                    //Grid is wrapped
                    var prevCol = (col - 1 + Config.GridSize) % Config.GridSize;
                    var nextCol = (col     + 1)               % Config.GridSize;
                    var prevRow = (row - 1 + Config.GridSize) % Config.GridSize;
                    var nextRow = (row     + 1)               % Config.GridSize;

                    var targetAverage = 0f;
                    targetAverage += GetHeight( prevRow, col );
                    targetAverage += GetHeight( nextRow, col );
                    targetAverage += GetHeight( row, prevCol );
                    targetAverage += GetHeight( row, nextCol );
                    targetAverage /= 4f;

                    var state = Buffer2[ i ];
                    var height = targetAverage - state.Height;       //Move height to zero with velocity from past wave height. Smart trick!
                    height -= (height / DampingDivisor);                              //Damping
                    state.Height = height;

                    Buffer2[ i ] = state;
                }
            }

            private float GetHeight(int row, int col)
            {
                return Buffer1[PositionUtils.PositionToIndex( row, col )].Height;
            }
        }

    }
}
