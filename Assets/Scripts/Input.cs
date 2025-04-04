using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Core
{
    public struct Input : IComponentData
    {
        public bool IsSelectedCell;
        public bool Clicked;
        public bool AltClicked;
        public EChangeMode ChangeMode;

        public int4 SelectedCell;
        public int WCoord;

        public float3 CameraPosition;
        public float3 MouseRay;
        public float CameraCarveSize;

        public bool  IsTimeFreezed;
    }

    public enum EChangeMode
    {
        Temp,
        Wave,
        Ill
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class InputSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate( )
        {
            base.OnCreate();

            // Set the system update order
            AddSystemToUpdateList( World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ReadInput>() );
            AddSystemToUpdateList( World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ProcessInput>() );

        }
    }

}