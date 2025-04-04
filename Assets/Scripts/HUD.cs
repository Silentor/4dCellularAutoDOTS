using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Core
{
    public class HUD : MonoBehaviour
    {
        private EntityQuery _inputQuery;
        private EntityQuery _configQuery;
        private int _fpsMeter;
        private float _fpsTimer;
        private World _world;
        private Label _info;
        private Label _fps;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            //Application.targetFrameRate = 60;

            _world = World.DefaultGameObjectInjectionWorld;
            var uiDocument = GetComponent<UIDocument>();
            _info = uiDocument.rootVisualElement.Q<Label>( "Info" );
            _fps = uiDocument.rootVisualElement.Q<Label>( "Fps" );
        }

        // Update is called once per frame
        void Update()
        {
            if ( _world.IsCreated )
            {
                if ( !_world.EntityManager.IsQueryValid( _configQuery ) )
                {
                    _configQuery = _world.EntityManager.CreateEntityQuery( new ComponentType( typeof(Config), ComponentType.AccessMode.ReadOnly ) );
                    _inputQuery = _world.EntityManager.CreateEntityQuery( new ComponentType( typeof(Input), ComponentType.AccessMode.ReadOnly ) );
                }
            }
            else
                return;

            if(_configQuery.TryGetSingleton( out Config config ) & _inputQuery.TryGetSingleton( out Input input )) 
                _info.text = $"Workflow {config.Workflow}, size {Config.GridSize}, WCoord: {input.WCoord}, change {input.ChangeMode}, time {(input.IsTimeFreezed ? "stop" : "play")}, carve {input.CameraCarveSize}";

            if( Time.time > _fpsTimer + 1f )
            {
                _fps.text = $"FPS: {_fpsMeter}";
                _fpsMeter = 0;
                _fpsTimer += 1f;
            }
            else
            {
                _fpsMeter++;
            }
        }
    }
}
