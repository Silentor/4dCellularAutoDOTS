using TMPro;
using UnityEngine;
using Object = System.Object;

namespace Core
{
    public class HUD : MonoBehaviour
    {
        public TMP_Text WCoord;
        private CameraControl _control;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _control = FindAnyObjectByType<CameraControl>();
        }

        // Update is called once per frame
        void Update()
        {
            WCoord.text = $"WCoord: {_control.SelectedWCoord}";       
        }
    }
}
