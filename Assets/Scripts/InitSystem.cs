using Unity.Entities;

namespace Core
{
    [CreateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InitSystem : SystemBase
    {
        protected override void OnCreate( )
        {
            base.OnCreate();

            RequireForUpdate<Config>();
        }

        protected override void OnUpdate( )
        {
            var config = SystemAPI.GetSingleton<Config>();
            var fixedGroup = World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            if ( fixedGroup.Timestep != config.Timestep )
                fixedGroup.Timestep = config.Timestep;            
        }
    }
}