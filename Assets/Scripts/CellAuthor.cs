using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Core
{
    public class CellAuthor : MonoBehaviour
    {
        private   class Baker : Baker<CellAuthor>
        {
            public override void Bake(CellAuthor authoring )
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Renderable);
                AddComponent<Cell>(entity);
                AddComponent<URPMaterialPropertyBaseColor>(entity);
            }
        }
    }
}