using System;
using Unity.Entities;
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

        protected override void OnCreate( )
        {
            base.OnCreate();

            RequireForUpdate<Input>();

            _changeTempAction = InputSystem.actions.FindAction( "ChangeTemp" );
            _changeWaveAction = InputSystem.actions.FindAction( "ChangeWave" );
            _changeIllAction  = InputSystem.actions.FindAction( "ChangeIll" );
            _wCoordDeltaAction = InputSystem.actions.FindAction( "WCoordDelta" );
            _attackAction    = InputSystem.actions.FindAction( "Attack" );
            _altAttackAction = InputSystem.actions.FindAction( "AltAttack" );
        }

        protected override void OnUpdate( )
        {
            ref var input = ref SystemAPI.GetSingletonRW<Input>().ValueRW;
            var config = SystemAPI.GetSingleton<Config>();

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
        }
    }
}