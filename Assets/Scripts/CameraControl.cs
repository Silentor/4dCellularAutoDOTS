using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

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


        private void Start( )
        {
            _lookAroundAction = InputSystem.actions.FindAction( "Look" );
            _moveAction       = InputSystem.actions.FindAction( "Move" );
            _sprintAction     = InputSystem.actions.FindAction( "Sprint" );

            _camera = Camera.main;
            _cameraTransform  = _camera.transform;
            _eulerRotation    = _cameraTransform.rotation.eulerAngles;
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
        }
    }
}