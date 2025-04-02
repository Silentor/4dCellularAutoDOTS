using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Core
{
    public class HUD : MonoBehaviour
    {
        public TMP_Text WCoord;
        //public Button ChangeTemp;
        //public Button ChangeWave;
        //public Button ChangeIll;
        private CameraControl _control;
        private ConfigAuthor _config;
        private EntityQuery _inputComponentQuery;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Application.targetFrameRate = 60;

            _config = FindAnyObjectByType<ConfigAuthor>();
            _control = FindAnyObjectByType<CameraControl>();
            _inputComponentQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery( typeof(Input) );

            // ChangeTemp.onClick.AddListener( () =>
            // {
            //     _control.ChangeMode = CameraControl.EChangeMode.Temp;
            // });
            //
            // ChangeWave.onClick.AddListener( () =>
            // {
            //     _control.ChangeMode = CameraControl.EChangeMode.Wave;
            // });
            //
            // ChangeIll.onClick.AddListener( () =>
            // {
            //     _control.ChangeMode = CameraControl.EChangeMode.Ill;
            // });

        }

        // Update is called once per frame
        void Update()
        {

            if(_inputComponentQuery.TryGetSingleton( out Input input ) )
                WCoord.text = $"Workflow {_config.Workflow}, size {Config.GridSize}, WCoord: {input.WCoord}, change mode {input.ChangeMode}";

            // switch ( _control.ChangeMode )
            // {
            //     case CameraControl.EChangeMode.Temp:
            //         ChangeTemp.transform.localScale = Vector3.one * 1.1f;
            //         ChangeWave.transform.localScale = Vector3.one;
            //         ChangeIll.transform.localScale = Vector3.one;
            //         break;
            //     case CameraControl.EChangeMode.Wave:
            //         ChangeTemp.transform.localScale = Vector3.one;
            //         ChangeWave.transform.localScale = Vector3.one * 1.1f;
            //         ChangeIll.transform.localScale = Vector3.one;
            //         break;
            //     case CameraControl.EChangeMode.Ill:
            //         ChangeTemp.transform.localScale = Vector3.one;
            //         ChangeWave.transform.localScale = Vector3.one;
            //         ChangeIll.transform.localScale = Vector3.one * 1.1f;
            //         break;
            // }
        }
    }
}
