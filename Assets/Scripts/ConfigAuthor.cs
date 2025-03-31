using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Core
{
    public class ConfigAuthor : MonoBehaviour
    {
        public EWorkflow Workflow;
        // [Min(4)]
        // public int GridSize = 256;
        public float HeatSpreadSpeed = 0.1f;
        public Color NeutralColor = new Color( 0.5f, 0.5f, 0.5f, 1 );
        public Color HotColor = new Color( 1f, 0f, 0f, 1 );
        public Color ColdColor = new Color( 0f, 0f, 1f, 1 );
        public GameObject CellPrefab;

        
    }

    public enum EWorkflow
    {
        Mode2D,
        Mode3D,
        Mode4D,
    }

    class ConfigBaker : Baker<ConfigAuthor>
    {
        public override void Bake(ConfigAuthor authoring)
        {
             var entity = GetEntity(TransformUsageFlags.None);
             var config = new Config()
                          {
                                  HeatSpreadSpeed = authoring.HeatSpreadSpeed,
                                  NeutralColor    = (Vector4)authoring.NeutralColor,
                                  HotColor        = (Vector4)authoring.HotColor,
                                  ColdColor       = (Vector4)authoring.ColdColor,
                                  CellPrefab      = GetEntity( authoring.CellPrefab, TransformUsageFlags.Renderable ),
                                  Seed            = (uint)UnityEngine.Random.Range( 1, uint.MaxValue ),
                                  Workflow = authoring.Workflow,
                                  GridTotalCount = authoring.Workflow switch {
                                                           EWorkflow.Mode2D => Config.GridSize * Config.GridSize,
                                                           EWorkflow.Mode3D => Config.GridSize * Config.GridSize * Config.GridSize,
                                                           EWorkflow.Mode4D => Config.GridSize * Config.GridSize * Config.GridSize * Config.GridSize,
                                                           _ => throw new ArgumentOutOfRangeException()
                                                   }
                          }; 
             AddComponent( entity, config ) ;
             switch ( authoring.Workflow )
             {
                 case EWorkflow.Mode2D:
                     AddComponent( entity, new Tag_2dWorkflow() );
                     break;
                 case EWorkflow.Mode3D:
                     AddComponent( entity, new Tag_3dWorkflow() );
                     break;
                 default:
                     AddComponent( entity, new Tag_4dWorkflow() );
                     break;
             }
        }
    }

    public struct Config : IComponentData
    {
        public uint Seed;

        public float  HeatSpreadSpeed;
        public float4 NeutralColor;
        public float4 HotColor;
        public float4 ColdColor;

        public Entity CellPrefab;

        public EWorkflow Workflow;
        public const int GridSize = 64;
        public int GridTotalCount;
    }

    public struct Tag_2dWorkflow : IComponentData{ }
    public struct Tag_3dWorkflow : IComponentData{ }
    public struct Tag_4dWorkflow : IComponentData{ }
}
