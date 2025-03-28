using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Core
{
    class ConfigAuthor : MonoBehaviour
    {
        public float HeatSpreadSpeed = 0.1f;
        public Color NeutralColor = new Color( 0.5f, 0.5f, 0.5f, 1 );
        public Color HotColor = new Color( 1f, 0f, 0f, 1 );
        public Color ColdColor = new Color( 0f, 0f, 1f, 1 );
        public GameObject CellPrefab;
    }

    class ConfigBaker : Baker<ConfigAuthor>
    {
        public override void Bake(ConfigAuthor authoring)
        {
             var entity = GetEntity(TransformUsageFlags.None);
             AddComponent( entity, new Config()
                                          {
                                                HeatSpreadSpeed = authoring.HeatSpreadSpeed,
                                                NeutralColor    = (Vector4)authoring.NeutralColor,
                                                HotColor        = (Vector4)authoring.HotColor,
                                                ColdColor       = (Vector4)authoring.ColdColor,
                                                CellPrefab = GetEntity( authoring.CellPrefab, TransformUsageFlags.Renderable ),
                                                Seed = (uint)UnityEngine.Random.Range( 1, uint.MaxValue ),
                                          } ) ;

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

        public const int GridSize = 256;
    }
}
