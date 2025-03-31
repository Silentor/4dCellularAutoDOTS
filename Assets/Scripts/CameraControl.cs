using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = System.Object;

namespace Core
{
    public class CameraControl : MonoBehaviour
    {
        public Vector2 LookAroundSpeed = new Vector2( 0.2f, 0.2f );

        public Int32 SelectedWCoord => _wCoord;

        private Transform   _cameraTransform;

        private Vector3     _eulerRotation;
        private InputAction _lookAroundAction;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private Camera _camera;
        private World _world;
        private Entity _inputEntity;
        private InputAction _hotAction;
        private InputAction _coldAction;
        private InputAction _wCoordDeltaAction;
        private Int32 _wCoord;
        private ConfigAuthor _config;



        private void Start( )
        {
            _lookAroundAction = InputSystem.actions.FindAction( "Look" );
            _moveAction       = InputSystem.actions.FindAction( "Move" );
            _sprintAction     = InputSystem.actions.FindAction( "Sprint" );
            _hotAction     = InputSystem.actions.FindAction( "Attack" );
            _coldAction     = InputSystem.actions.FindAction( "Cold" );
            _wCoordDeltaAction     = InputSystem.actions.FindAction( "WCoordDelta" );
            _camera = Camera.main;
            _cameraTransform  = _camera.transform;
            _eulerRotation    = _cameraTransform.rotation.eulerAngles;

            _world = World.DefaultGameObjectInjectionWorld;
            _config = FindAnyObjectByType<ConfigAuthor>();
        }

        private void Update( )
        {
            //Look around
            var mousePosition = Mouse.current.position.ReadValue();
            if ( !(mousePosition.x < 0) && !(mousePosition.x > Screen.width) &&
                 !(mousePosition.y < 0) && !(mousePosition.y > Screen.height) )
            {
                var lookDelta          = _lookAroundAction.ReadValue<Vector2>() * LookAroundSpeed;
                _eulerRotation            += new Vector3( -lookDelta.y,                             lookDelta.x,      0 );
                _eulerRotation            =  new Vector3( Mathf.Clamp( _eulerRotation.x, -80, 80 ), _eulerRotation.y, 0 );
                _cameraTransform.rotation =  Quaternion.Euler( _eulerRotation );
            }

            //Movement
            var moveDelta     = _moveAction.ReadValue<Vector2>();
            if ( moveDelta != Vector2.zero )
            {
                var worldMovement = new Vector3( moveDelta.x, 0, moveDelta.y );
                if ( _sprintAction.IsPressed() )
                    worldMovement *= 10;
                var localMovement = _cameraTransform.rotation * worldMovement;
                _cameraTransform.position += localMovement;
            }

            //Scroll W coord for 4D
            int wCoordDelta = 0;
            if( _config.Workflow == EWorkflow.Mode4D )
                wCoordDelta = Mathf.RoundToInt( _wCoordDeltaAction.ReadValue<float>() );

            //Change cell by mouse
            var clicked = false;
            var temperatureDiff = 0f;
            if ( _hotAction.IsPressed() )
            {
                temperatureDiff = 10;
                //temperatureDiff += Time.deltaTime;
                clicked = true;
            }

            if ( _coldAction.IsPressed() )
            {
                temperatureDiff = -10;
                //temperatureDiff -= Time.deltaTime;
                clicked = true;
            }

            //Select cell by mouse hover
            int2 selectedCell = int2.zero;
            bool isSelectedCell = false;
            var mouseRay = _camera.ScreenPointToRay( mousePosition );
            var gridPlane = new Plane( Vector3.up, 0 );
            if( gridPlane.Raycast( mouseRay, out var enter ) )
            {
                 var hitPoint = mouseRay.GetPoint( enter );
                 selectedCell = new int2( Mathf.FloorToInt( hitPoint.x ), Mathf.FloorToInt( hitPoint.z ) );
                 if( selectedCell.x >= 0 && selectedCell.x < Config.GridSize &&
                     selectedCell.y >= 0 && selectedCell.y < Config.GridSize )
                 {
                     isSelectedCell = true;
                 }
            }

            SetInputToECS( isSelectedCell, clicked, selectedCell, wCoordDelta, temperatureDiff );
        }

        private void SetInputToECS( bool isSelectedCell, bool isClicked, int2 selectedCell, int wCoordDelta, float temperatureDiff )
        {
            if ( _world.IsCreated )
            {
                if ( !_world.EntityManager.Exists( _inputEntity ) )
                    _inputEntity = _world.EntityManager.CreateSingleton<Input>(  );

                _wCoord = (_wCoord + wCoordDelta + Config.GridSize) % Config.GridSize;
                _world.EntityManager.SetComponentData( _inputEntity, new Input()
                                                                     {
                                                                             IsSelectedCell = isSelectedCell,
                                                                             Clicked = isClicked,
                                                                             SelectedCell = new int3( selectedCell.x, selectedCell.y, 0 ),
                                                                             //TemperatureDiff = temperatureDiff,
                                                                             HeightDiff = temperatureDiff,
                                                                             WCoord = _wCoord
                                                                     } );
            }
        }
    }
}