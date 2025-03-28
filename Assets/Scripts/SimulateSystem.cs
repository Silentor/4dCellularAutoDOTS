using Unity.Burst;
using Unity.Collections;
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
            state.RequireForUpdate<CellState>();
            state.RequireForUpdate<Input>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state )
        {
            var cellsStateBuffer = SystemAPI.GetSingletonBuffer<CellState>();
            var config          = SystemAPI.GetSingleton<Config>();
            var input = SystemAPI.GetSingleton<Input>();
            if( input.IsSelectedCell )
                ChangeTemperature( cellsStateBuffer, input.SelectedCell.x, input.SelectedCell.y, input.TemperatureDiff );

            state.Dependency = SimulateCellularAuto( state.Dependency, ref state, cellsStateBuffer, config );
        }

        private JobHandle SimulateCellularAuto(JobHandle dependency, ref SystemState state, DynamicBuffer<CellState> cellsStateBuffer, Config config )
        {
            var job = new HeatSpreadJob_SingleThreaded()
                      {
                              heatRO                = cellsStateBuffer.ToNativeArray( state.WorldUpdateAllocator ),
                              heatRW                = cellsStateBuffer.AsNativeArray(),
                              HeatSpreadSpeedScaled = SystemAPI.Time.DeltaTime * config.HeatSpreadSpeed,
                      };
            return job.Schedule( dependency );
        }

        private void ChangeTemperature(DynamicBuffer<CellState> cellsStateBuffer, int row, int col, float tempDiff)
        {
            var index = col * Config.GridSize + row;
            cellsStateBuffer.ElementAt( index ).Temperature += tempDiff;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        [BurstCompile]
        public struct HeatSpreadJob_SingleThreaded : IJob
        {
            public            NativeArray<CellState> heatRW;
            [ReadOnly] public NativeArray<CellState> heatRO;
            public            float                  HeatSpreadSpeedScaled;

            public void Execute()
            {
                for (int i = 0; i < heatRO.Length; i++)
                {
                    int row = i / Config.GridSize;
                    int col = i % Config.GridSize;

                    //Grid is wrapped
                    var prevCol = (col - 1 + Config.GridSize) % Config.GridSize;
                    var nextCol = (col + 1) % Config.GridSize;
                    var prevRow = (row - 1 + Config.GridSize) % Config.GridSize;
                    var nextRow = (row  + 1) % Config.GridSize;

                    var targetAverage = 0f;
                    targetAverage += GetTemperature( prevRow, col );
                    targetAverage += GetTemperature( nextRow, col );
                    targetAverage += GetTemperature( row, prevCol );
                    targetAverage += GetTemperature( row, nextCol );
                    targetAverage /= 4f;
                    var difference = targetAverage - heatRO[i].Temperature;

                    var value = heatRO[i].Temperature + math.clamp( difference, -HeatSpreadSpeedScaled, HeatSpreadSpeedScaled );

                    heatRW[i] = new CellState()
                                    {
                                        Temperature       = value,
                                    };
                }
            }

            private float GetTemperature(int row, int col)
            {
                return heatRO[row * Config.GridSize + col].Temperature;
            }
        }
    }
}
