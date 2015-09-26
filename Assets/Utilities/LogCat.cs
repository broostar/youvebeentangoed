using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class LogCat {

    private List<string> m_lines;
    private List<string> m_undecoratedLines;
    private StringBuilder m_builder;

    public LogCat() {
        m_lines = new List<string>();
        m_undecoratedLines = new List<string>();
        m_builder = new StringBuilder();
    }

    public void AddLine( string line ) {
        if( line.StartsWith( "W/" ) ) {
            m_lines.Add( "<color=#ffff00ff><b>" + line + "</b></color>" );
        } else if(line.StartsWith("E/")){
            m_lines.Add( "<color=#ff0000ff><b>" + line + "</b></color>" );
        } else if( line.StartsWith( "I/" ) ) {
            m_lines.Add( "<color=#0000ffff><b>" + line + "</b></color>" );
        } else if( line.StartsWith( "D/" ) ) {
            m_lines.Add( "<color=#008000ff><b>" + line + "</b></color>" );
        } else {
            m_lines.Add( line );
        }
        m_undecoratedLines.Add( line );
    }

    public void Clear() {
        m_lines.Clear();
    }

    public string GetLines( int start, int count, int maxCharCount ) {
        m_builder.Remove( 0, m_builder.Length );
        if( m_lines.Count < start ){
            Debug.Log( "Start cannot be after end !" );
            return string.Empty; 
        }
        if( start + count > m_lines.Count ) {//need to deal with when count is bigger than num lines
            start = m_lines.Count - count;
        }
        int charCount = 0;
        for( int i = start, len = start + count; i < len; i++ ) {
            if( (charCount += (m_lines[i].Length+2)) >= maxCharCount) {//add number of lines (newline chars get added)
                Debug.Log( "Char max hit! [line "+i+"]" );
                break;
            }
            m_builder.AppendLine( m_lines[i] );
        }
        return m_builder.ToString();
    }

    public int LineCount {
        get { return m_lines.Count; }
    }
}
