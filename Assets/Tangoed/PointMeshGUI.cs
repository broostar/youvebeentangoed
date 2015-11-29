using UnityEngine;
using System.Collections;
using Tango;
using UnityEngine.UI;

namespace DDS.Tango {
    public class PointMeshGUI : MonoBehaviour {

        public TangoPointsMesh m_pointsMesh;

        private bool m_isStopped = true;

        private TangoApplication m_tangoApplication;
        public Text m_btnGuiText;

        public Button m_btnStartStop;
        public Color m_btnStartColour;
        public Color m_btnStopColour;
        public Color m_txtStartColour;
        public Color m_txtStopColour;

        // Use this for initialization
        void Start() {
            m_tangoApplication = FindObjectOfType<TangoApplication>();
            m_btnGuiText.text = "Start";
            m_btnGuiText.color = m_txtStartColour;
            ColorBlock cb = m_btnStartStop.colors;
            cb.normalColor = m_btnStartColour;
            m_btnStartStop.colors = cb;
        }

        public void OnStartStop() {
            if( m_isStopped ) {
                m_tangoApplication.RegisterOnTangoDepthEvent( m_pointsMesh.OnTangoDepthAvailable );
                m_btnGuiText.text = "Stop";
                m_btnGuiText.color = m_txtStopColour;
                ColorBlock cb = m_btnStartStop.colors;
                cb.normalColor = cb.highlightedColor = m_btnStopColour;
                m_btnStartStop.colors = cb;
            } else {
                m_tangoApplication.UnregisterOnTangoDepthEvent( m_pointsMesh.OnTangoDepthAvailable );
                m_btnGuiText.text = "Start";
                m_btnGuiText.color = m_txtStartColour;
                ColorBlock cb = m_btnStartStop.colors;
                cb.normalColor = cb.highlightedColor = m_btnStartColour;
                m_btnStartStop.colors = cb;
            }
            m_isStopped = !m_isStopped;
        }
    }
}
