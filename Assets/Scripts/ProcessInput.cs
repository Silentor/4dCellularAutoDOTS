using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

namespace Core
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ReadInput))]
    public partial struct ProcessInput : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Input>();
            state.RequireForUpdate<SimulationState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            ref var input = ref SystemAPI.GetSingletonRW<Input>().ValueRW;

            input.IsSelectedCell = TryFindSelectedCell( config.Workflow, input.CameraPosition, input.MouseRay, input.CameraCarveSize, out var selectedCell );
            input.SelectedCell = new int4( selectedCell, input.WCoord );

            //Debug.Log( $"ray {input.MouseRay}, is selected {input.IsSelectedCell}, pos {input.SelectedCell}" );
            ref var simulState = ref SystemAPI.GetSingletonRW<SimulationState>().ValueRW;
            simulState.ProcessSimulation = !input.IsTimeFreezed;

            if ( input.IsSelectedCell && (input.Clicked || input.AltClicked))
            {
                var effectMult = input.Clicked ? 1 : input.AltClicked ? -1 : 0;
                var currentBufferEntity = simulState.CellsBuffer;         
                var currentBuffer = SystemAPI.GetBuffer<CellState>( currentBufferEntity );
                var clickedPos = input.SelectedCell;
                var waveheight = config.Workflow switch
                                 {
                                         EWorkflow.Mode2D => 1,
                                         EWorkflow.Mode3D => 2,
                                         EWorkflow.Mode4D => 3,
                                         _ => throw new ArgumentOutOfRangeException()
                                 };

                switch ( input.ChangeMode )
                {
                    case EChangeMode.Temp: ChangeTemperature( currentBuffer, clickedPos, 1 * effectMult );  break;
                    case EChangeMode.Wave: AddWave( currentBuffer, clickedPos, waveheight * effectMult); break;
                    case EChangeMode.Ill: AddIllness( currentBuffer, clickedPos, 1 * effectMult );  break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void ChangeTemperature(DynamicBuffer<CellState> cellsStateBuffer, int4 pos, float tempDiff)
        {
            var index = PositionUtils.PositionToIndex( pos );
            cellsStateBuffer.ElementAt( index ).Temperature += tempDiff;
        }

        private static void AddWave(DynamicBuffer<CellState> cellsStateBuffer, int4 pos, float waveHeight )
        {
            var index = PositionUtils.PositionToIndex( pos );
            cellsStateBuffer.ElementAt( index ).Height = waveHeight;
        }

        private static void AddIllness(DynamicBuffer<CellState> cellsStateBuffer, int4 pos, float illLevel )
        {
            var index = PositionUtils.PositionToIndex( pos );
            cellsStateBuffer.ElementAt( index ).Illness = illLevel;
        }


        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

         private static bool TryFindSelectedCell( EWorkflow workflow, float3 rayOrigin, float3 rayDir, float carvingRange, out int3 result )
         {
            result = new int3( -1, -1, -1 );
            if( rayDir.Equals( float3.zero ) )
                return false;

            float3 minaabb = float3.zero;         
            float3 maxAabb = workflow == EWorkflow.Mode2D ? new float3( Config.GridSize, 1, Config.GridSize ) : new float3( Config.GridSize );
            //
            // var intersectionResult = IntersectAABB( rayOrigin, rayDir, minaabb, maxAabb );
            // if ( intersectionResult.x < intersectionResult.y )
            // {
            //     Debug.Log( intersectionResult );
            //     var rayDirNorm = normalize( rayDir );
            //     var hitPos = rayOrigin + rayDirNorm * intersectionResult.x;
            //     result = (int3)round( hitPos );
            //     if ( workflow == EWorkflow.Mode2D )
            //         result = result.xzy;
            //     return true;
            // }

            result = RayMarch( rayOrigin, rayDir, minaabb, maxAabb, carvingRange );
            if ( workflow == EWorkflow.Mode2D )
                result = result.xzy;
            if( !result.Equals( new int3( -1, -1, -1 ) ) )
                return true;

            return false;
         }

         private static float2 IntersectAABB(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax)
        {
            float3 tMin = (boxMin - rayOrigin) / rayDir;
            float3 tMax = (boxMax - rayOrigin) / rayDir;
            float3 t1 = min( tMin, tMax );
            float3 t2 = max( tMin, tMax );
            float tNear = max( max( t1.x, t1.y ), t1.z );
            float tFar = min( min( t2.x, t2.y ), t2.z );
            return float2( tNear, tFar );
        }

        private static int3 RayMarch(float3 rayOrigin, float3 rayDir, float3 boundsMin, float3 boundsMax, float carvingRange )
        {
            var t = carvingRange + 0.5f;       //Do not select blocks in camera carving range
            var step = 0.1f;
            var maxDistance = 100f;
            var hitPos = rayOrigin;

            while ( t < maxDistance )
            {
                hitPos = rayOrigin + rayDir * t;
                if ( all( hitPos > boundsMin ) && all ( hitPos < boundsMax ) )
                {
                    var intHitPos = (int3)(hitPos);
                    {
                        return intHitPos;
                    }
                }
                t += step;
            }

            return new int3( -1, -1, -1 );
        }
    }
}