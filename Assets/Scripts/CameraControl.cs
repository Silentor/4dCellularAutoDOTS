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

        private void Start( )
        {
            _lookAroundAction = InputSystem.actions.FindAction( "Look" );
            _moveAction       = InputSystem.actions.FindAction( "Move" );
            _sprintAction     = InputSystem.actions.FindAction( "Sprint" );
            _hotAction     = InputSystem.actions.FindAction( "Attack" );
            _coldAction     = InputSystem.actions.FindAction( "Cold" );
            _camera = Camera.main;
            _cameraTransform  = _camera.transform;
            _eulerRotation    = _cameraTransform.rotation.eulerAngles;

            _world = World.DefaultGameObjectInjectionWorld;
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
                _eulerRotation            =  new Vector3( Mathf.Clamp( _eulerRotation.x, -45, 45 ), _eulerRotation.y, 0 );
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

            SetInputToECS( isSelectedCell, clicked, selectedCell, temperatureDiff );
        }

        private void SetInputToECS( bool isSelectedCell, bool isClicked, int2 selectedCell, float temperatureDiff )
        {
            if ( _world.IsCreated )
            {
                if ( !_world.EntityManager.Exists( _inputEntity ) )
                    _inputEntity = _world.EntityManager.CreateSingleton<Input>(  );

                _world.EntityManager.SetComponentData( _inputEntity, new Input()
                                                                     {
                                                                             IsSelectedCell = isSelectedCell,
                                                                             Clicked = isClicked,
                                                                             SelectedCell = selectedCell,
                                                                             //TemperatureDiff = temperatureDiff,
                                                                             HeightDiff = temperatureDiff
                                                                     } );
            }
        }
    }
}