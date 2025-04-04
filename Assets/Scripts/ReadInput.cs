using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = System.Object;

namespace Core
{
    [UpdateInGroup(typeof(InputSystemGroup), OrderFirst = true)]
    public partial class ReadInput : SystemBase
    {
        private InputAction _changeTempAction;
        private InputAction _changeWaveAction;
        private InputAction _changeIllAction;
        private InputAction _wCoordDeltaAction;
        private InputAction _attackAction;
        private InputAction _altAttackAction;
        private InputAction _changeCarveSizeAction;
        private InputAction _timeFreezeAction;

        private Camera _camera;

        protected override void OnCreate( )
        {
            base.OnCreate();

            RequireForUpdate<Config>();

            _changeCarveSizeAction = InputSystem.actions.FindAction( "ChangeCarveSize" );
            _changeTempAction = InputSystem.actions.FindAction( "ChangeTemp" );
            _changeWaveAction = InputSystem.actions.FindAction( "ChangeWave" );
            _changeIllAction  = InputSystem.actions.FindAction( "ChangeIll" );
            _wCoordDeltaAction = InputSystem.actions.FindAction( "WCoordDelta" );
            _attackAction    = InputSystem.actions.FindAction( "Attack" );
            _altAttackAction = InputSystem.actions.FindAction( "AltAttack" );
            _timeFreezeAction = InputSystem.actions.FindAction( "TimeFreeze" );
        }

        protected override void OnUpdate( )
        {
            var config = SystemAPI.GetSingleton<Config>();

            if ( !SystemAPI.TryGetSingletonRW( out RefRW<Input> inputRW ) )
            {
                var inputEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<Input>( inputEntity );
                EntityManager.SetComponentData( inputEntity, new Input()
                                                             {
                                                                     CameraCarveSize = config.CameraCarveSize,
                                                             } );
            }

            inputRW = SystemAPI.GetSingletonRW<Input>();
            ref var input = ref inputRW.ValueRW;


            if ( _changeTempAction.triggered )
                input.ChangeMode = EChangeMode.Temp;
            if ( _changeWaveAction.triggered )
                input.ChangeMode = EChangeMode.Wave;
            if ( _changeIllAction.triggered )
                input.ChangeMode = EChangeMode.Ill;

            //Scroll W coord for 4D
            if ( config.Workflow == EWorkflow.Mode4D && _wCoordDeltaAction.triggered )
            {
                var deltaWCoord = Mathf.RoundToInt( _wCoordDeltaAction.ReadValue<float>() );
                input.WCoord = (input.WCoord + deltaWCoord + Config.GridSize) % Config.GridSize;
            }

            input.Clicked = _attackAction.IsPressed();
            input.AltClicked = _altAttackAction.IsPressed();

            if ( config.Workflow >= EWorkflow.Mode3D )
            {
                var deltaCarveSize = _changeCarveSizeAction.ReadValue<float>();
                input.CameraCarveSize = math.clamp( input.CameraCarveSize + deltaCarveSize, 0, Config.GridSize );
            }

            if ( !_camera )
            {
                _camera = Camera.main;
            }

            if ( _camera )
            {
                //Select cell by mouse hover
                var mousePosition = Mouse.current.position.ReadValue();
                var mouseRay = _camera.ScreenPointToRay( mousePosition );

                input.CameraPosition = _camera.transform.position;
                input.MouseRay       = mouseRay.direction;
            }

            input.IsTimeFreezed = _timeFreezeAction.IsPressed();
        }
    }
}