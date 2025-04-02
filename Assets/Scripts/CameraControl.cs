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
        private ConfigAuthor _config;
        private EntityQuery _inputComponentQuery;


        private void Start( )
        {
            _inputComponentQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery( typeof(Input) );

            _lookAroundAction = InputSystem.actions.FindAction( "Look" );
            _moveAction       = InputSystem.actions.FindAction( "Move" );
            _sprintAction     = InputSystem.actions.FindAction( "Sprint" );

            _camera = Camera.main;
            _cameraTransform  = _camera.transform;
            _eulerRotation    = _cameraTransform.rotation.eulerAngles;

            _world = World.DefaultGameObjectInjectionWorld;
            _config = FindAnyObjectByType<ConfigAuthor>();
        }

        private void Update( )
        {
            if( !_world.IsCreated || _inputComponentQuery.IsEmpty )
                return;

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

            //Select cell by mouse hover
            int2 selectedCell = int2.zero;
            bool isSelectedCell = false;
            var mouseRay = _camera.ScreenPointToRay( mousePosition );
            
            // var gridPlane = new Plane( Vector3.up, 0 );
            // if( gridPlane.Raycast( mouseRay, out var enter ) )
            // {
            //      var hitPoint = mouseRay.GetPoint( enter );
            //      selectedCell = new int2( Mathf.FloorToInt( hitPoint.x ), Mathf.FloorToInt( hitPoint.z ) );
            //      if( selectedCell.x >= 0 && selectedCell.x < Config.GridSize &&
            //          selectedCell.y >= 0 && selectedCell.y < Config.GridSize )
            //      {
            //          isSelectedCell = true;
            //      }
            // }

            //Update Input component for ECS
            ref var input = ref _inputComponentQuery.GetSingletonRW<Input>().ValueRW;
            input.CameraPosition = transform.position;
            input.MouseRay = mouseRay.direction;
            input.CameraCarveSize = _config.CameraCarveSize;
        }

    }
}