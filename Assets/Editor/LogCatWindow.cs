using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System;

public class LogCatWindow : EditorWindow {

    [MenuItem( "Tools/LogCat" )]
    public static void ShowLogCat() {
        GetWindow<LogCatWindow>();
    }

    private readonly string ADB = "N:/adt-bundle-windows-x86-20140321/sdk/platform-tools/adb.exe";

    private Process m_adbProcess;
    private ProcessStartInfo m_adbStartInfo;

    private static Mutex m_mutex = new Mutex();
    private Queue<string> m_messages;

    private float m_lastScrollPos;
    private float m_scrollPos;
    private Vector2 m_scrollViewPos;
    private string m_logText = "";
    private LogCat m_logCat;
    private int m_maxChars;
    private int m_maxLines;

    private Rect m_txtAndScrollArea;
    private Rect m_textArea;
    private Vector2 m_charSize;
    private GUIStyle m_textAreaStyle;

    private Vector2 m_lastWindowSize;

    public void DataReceivedEventHandler( object sender, DataReceivedEventArgs e ){
        //UnityEngine.Debug.Log( "Size: " + e.Data.Length );
        m_mutex.WaitOne();
        m_messages.Enqueue( e.Data );
        m_mutex.ReleaseMutex();
    }

    private void InitProcess() {
        if( m_adbProcess == null ) {
            m_adbStartInfo = new ProcessStartInfo( ADB, "logcat libc:V Unity:V Tangoed:V *:D" );
            m_adbStartInfo.UseShellExecute = false;
            m_adbStartInfo.RedirectStandardOutput = true;

            m_adbProcess = new Process();
            m_adbProcess.StartInfo = m_adbStartInfo;
            
        }
    }

    public bool IsRunning( Process process ) {
        try { Process.GetProcessById( process.Id ); } 
        catch( InvalidOperationException ) { return false; } 
        catch( ArgumentException ) { return false; }
        return true;
    }

    public void StartLogCat() {
        if( m_adbProcess.Start() ) {
            m_adbProcess.OutputDataReceived += DataReceivedEventHandler;
            m_adbProcess.BeginOutputReadLine();
        }
    }

    public void StopLogCat() {
        m_adbProcess.CancelOutputRead();
        m_adbProcess.OutputDataReceived -= DataReceivedEventHandler;
        m_adbProcess.Kill();
        m_adbProcess.Dispose();
    }

    public void OnDisable() {
        StopLogCat();
    }

    public void OnEnable() {
        m_messages = new Queue<string>();
        m_logCat = new LogCat();
        m_lastScrollPos = 1f;
        InitProcess();
        m_textAreaStyle = new GUIStyle();
        m_textAreaStyle.normal.textColor = Color.grey;
        m_textAreaStyle.richText = true;
        m_textAreaStyle.wordWrap = true;
    }

    public void OnGUI() {
        GUILayout.BeginHorizontal();
        if( GUILayout.Button( "Start" ) ) {
            //m_logCat.Clear();
            StartLogCat();
        }
        if( GUILayout.Button( "Stop" ) ) {
            //m_logCat.Clear();
            StopLogCat();
        }
        GUILayout.EndHorizontal();
        if( Event.current.type == EventType.Repaint ) {
            Rect buttonsRect = GUILayoutUtility.GetLastRect();

            m_txtAndScrollArea = new Rect( 0, buttonsRect.height + 5, position.width, position.height - buttonsRect.height - 5 );
            m_charSize = m_textAreaStyle.CalcSize( new GUIContent( "<b>w</b>" ) );
           
        }

        GUILayout.BeginArea( m_txtAndScrollArea );
        GUILayout.BeginHorizontal();
        
        if( !IsRunning( m_adbProcess ) ) {
            GUILayout.Label( "CharSize: " + m_charSize+"; MaxChars: "+m_maxChars, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
        } else {
            GUILayout.Label( m_logText.Substring(0, Mathf.Min(m_logText.Length, m_maxChars)), m_textAreaStyle, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight(true) );
        }

        if( Event.current.type == EventType.Repaint ) {
            m_textArea = GUILayoutUtility.GetLastRect();

            m_maxChars = (int)((m_textArea.width / m_charSize.x) * (m_txtAndScrollArea.height / m_charSize.y));
            m_maxLines = (int)(m_txtAndScrollArea.height / m_charSize.y);
        }

        if( Event.current.type == EventType.ScrollWheel ) {
            m_scrollPos += Event.current.delta.y;
        }
        m_scrollPos = GUILayout.VerticalScrollbar( m_scrollPos, 1, 1, m_logCat.LineCount, GUILayout.ExpandHeight(true) );
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    public void Update() {

        m_scrollPos = Mathf.Floor( m_scrollPos );
        if( (m_lastScrollPos != m_scrollPos && m_scrollPos != 0) || position.size != m_lastWindowSize ) {
            m_lastWindowSize = position.size;

            UnityEngine.Debug.Log( "SCROLLING: " + m_scrollPos + "; lines: " + m_maxLines + "; max chars: " + m_maxChars );

            m_logText = m_logCat.GetLines( (int)m_scrollPos, m_maxLines, m_maxChars );
            
            UnityEngine.Debug.Log( "<SCROLLING> percentage: " + (int)(m_scrollPos / (float)m_logCat.LineCount * 100) + "; Got chars: "+m_logText.Length);
            
            m_lastScrollPos = m_scrollPos;

        }

        m_mutex.WaitOne();

        if( m_messages.Count <= 0 ) {
            m_mutex.ReleaseMutex();
            Repaint();
            return;
        }
        int messageCount = m_messages.Count;
        while( messageCount-- > 0 ) {
            m_logCat.AddLine( m_messages.Dequeue() );
        }
        

        m_mutex.ReleaseMutex();
        Repaint();
    }

}
