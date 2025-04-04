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
    partial struct SimulateSystem3d : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Tag_3dWorkflow>();
            state.RequireForUpdate<SimulationState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state )
        {
            var simulState = SystemAPI.GetSingleton<SimulationState>();
            if( !simulState.ProcessSimulation)
                return;

            var currentBuffer = SystemAPI.GetBuffer<CellState>( simulState.CellsBuffer ); 
            var config          = SystemAPI.GetSingleton<Config>();

            state.Dependency = SimulateCellularAuto( state.Dependency, ref state,  currentBuffer, config );
        }

        [BurstCompile]
        private JobHandle SimulateCellularAuto(JobHandle dependency, ref SystemState state, DynamicBuffer<CellState> buffer, Config config )
        {
            var input = buffer.ToNativeArray( state.WorldUpdateAllocator );
            var output = buffer.AsNativeArray();

            var job = new SimulateJob()
                      {
                              Input               = input,
                              Output              = output,
                              ThermalConductivity = math.saturate( config.HeatSpreadSpeed ),
                              WaveDampingCoeff    = math.saturate( config.WaveDampCoeff ),
                              IllSpeed            = math.saturate( config.IllSpeed ),
                              Seed = (config.Seed % 7919) + 1,             //Big seeds spoil math.cnoise result
                      };
            dependency = job.Schedule( input.Length, 2048, dependency );
            return dependency;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        [BurstCompile]
        public unsafe struct SimulateJob : IJobParallelFor
        {
            private const int NeimannNeighborsCount = 6;

            [ReadOnly] public NativeArray<CellState> Input;
            public            NativeArray<CellState> Output;

            public float ThermalConductivity;
            public float WaveDampingCoeff;
            public float IllSpeed;
            public float Seed;

            public void Execute(Int32 index )
            {
                var pos = PositionUtils.IndexToPosition3( index );
                var x = pos.x;
                var y = pos.y;
                var z = pos.z;

                //Grid is wrapped
                var prevX = (x - 1 + Config.GridSize) % Config.GridSize;
                var nextX = (x     + 1)               % Config.GridSize;
                var prevY = (y - 1 + Config.GridSize) % Config.GridSize;
                var nextY = (y     + 1)               % Config.GridSize;
                var prevZ = (z - 1 + Config.GridSize) % Config.GridSize;
                var nextZ = (z     + 1)               % Config.GridSize;

                // Span<CellState> neimannNeighbors = stackalloc CellState[ NeimannNeighborsCount ];        //Safe alternative, slower by ~10%
                CellState* neimannNeighbors = stackalloc CellState[ NeimannNeighborsCount ];
                neimannNeighbors[0] = Input[ PositionUtils.PositionToIndex( prevX, y, z ) ]; 
                neimannNeighbors[1] = Input[ PositionUtils.PositionToIndex( nextX, y, z ) ];
                neimannNeighbors[2] = Input[ PositionUtils.PositionToIndex( x, prevY, z ) ];
                neimannNeighbors[3] = Input[ PositionUtils.PositionToIndex( x, nextY, z ) ];
                neimannNeighbors[4] = Input[ PositionUtils.PositionToIndex( x, y, prevZ ) ];
                neimannNeighbors[5] = Input[ PositionUtils.PositionToIndex( x, y, nextZ ) ];
                // var neimannNeighborsRO = (ReadOnlySpan<CellState>)neimannNeighbors;

                var outputState = Output[ index ];

                HeatSpread( neimannNeighbors, ref outputState, ThermalConductivity );                                                     
                WaveSpread( neimannNeighbors, ref outputState, WaveDampingCoeff );
                IllSpread( pos, neimannNeighbors, ref outputState, IllSpeed, Seed );

                Output[ index ] = outputState;
            }

            private static void HeatSpread( CellState* neighbors, ref CellState output, float thermalConductivity )
            {
                var average = 0f;
                average += neighbors[0].Temperature;
                average += neighbors[1].Temperature;
                average += neighbors[2].Temperature;
                average += neighbors[3].Temperature;
                average += neighbors[4].Temperature;
                average += neighbors[5].Temperature;
                average /= 6f;

                var currentTemp = output.Temperature;
                var tempChange = average - currentTemp;
                tempChange  *= thermalConductivity;                    //Temperature conductivity
                currentTemp += tempChange;

                output.Temperature = currentTemp;
            }

            //Outstanding algorithm for CA wave spread
            //https://web.archive.org/web/20160418004149/http://freespace.virgin.net/hugo.elias/graphics/x_water.htm
            private static void WaveSpread( CellState* neighbors, ref CellState output, float dampingCoeff )
            {
                var smoothedHeight = 0f;
                smoothedHeight += neighbors[0].Height;
                smoothedHeight += neighbors[1].Height;
                smoothedHeight += neighbors[2].Height;
                smoothedHeight += neighbors[3].Height;
                smoothedHeight += neighbors[4].Height;
                smoothedHeight += neighbors[5].Height;
                smoothedHeight /= 3f;            //Intentional, neighbors count / 2

                var oldHeight = output.Height;
                var waveHeightChange = oldHeight - output.HeightDiff;       //Get value from prev prev state
                var height = smoothedHeight      - waveHeightChange;       //Move height to zero with velocity from past wave height. Smart trick!
                height            *= dampingCoeff;                              //Damping
                output.Height     =  height;
                output.HeightDiff =  height - oldHeight;
            }

            private static void IllSpread( float3 pos, CellState* neighbors, ref CellState output, float illSpeed, float seed )
            {
                var maxIllness = 0f;
                maxIllness = math.max( maxIllness, neighbors[0].Illness );
                maxIllness = math.max( maxIllness, neighbors[1].Illness );
                maxIllness = math.max( maxIllness, neighbors[2].Illness );
                maxIllness = math.max( maxIllness, neighbors[3].Illness );
                maxIllness = math.max( maxIllness, neighbors[4].Illness );
                maxIllness = math.max( maxIllness, neighbors[5].Illness );

                var illSpeedNoise = noise.cnoise( (pos + seed ) / 6.7f ) ; //Add some random to ill spread speed
                illSpeedNoise = math.smoothstep( -1.1f, 1, illSpeedNoise );
                var currentIllness = output.Illness;
                var diff =  math.max(maxIllness - currentIllness, 0) * illSpeed * illSpeedNoise; 
                currentIllness = math.saturate( currentIllness + diff );
                output.Illness = currentIllness;
            }
        }         
    }
}
