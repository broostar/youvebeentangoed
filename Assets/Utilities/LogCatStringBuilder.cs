using UnityEngine;
using System.Collections;
using System.Text;

public class LogCatStringBuilder {

    private StringBuilder m_builder;

    private int m_lineCount = 0;

    public int Capacity {
        get { return m_builder.Capacity; }
    }

    public int Length {
        get { return m_builder.Length; }
    }

    public int LineCount {
        get { return m_lineCount; }
    }

    public int MaxCapacity {
        get { return m_builder.MaxCapacity; }
    }

    public LogCatStringBuilder() {
        m_builder = new StringBuilder();
    }

    public void AddLine( string line ) {
        m_builder.AppendLine( line );
        m_lineCount++;
    }

    /// <summary>
    /// Finds the position of the first newline PRECEDING the start.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="minimum"></param>
    /// <returns></returns>
    private int findLastLine( int start ) {
        int actualStart = Mathf.Max( 0, start - 255 );
        int diff = start - actualStart;
        //don't go below zero, or above the length of the string 
        int index = m_builder.ToString( actualStart, Mathf.Min(m_builder.Length-actualStart, diff) ).LastIndexOf('\n') + 1;
        return index;
    }

    /// <summary>
    /// Finds the position of the first newline AFTER the start
    /// </summary>
    /// <param name="start"></param>
    /// <param name="minimum"></param>
    /// <returns></returns>
    private int findNextLine( int start, int minimum ) {
        int index = m_builder.ToString( start, Mathf.Max( minimum + 2, 255 ) ).IndexOf( '\n' ) + 1;//if 255 isn't enough, we're fucked.
        return index;
    }


    public string GetLinesFromLastLine( int start, int numLines ) {
        int lastLine = findLastLine( start );
        if( lastLine < 0 ) {
            return string.Empty;
        }
        int diff = start - lastLine;
        //don't go above the length of the string
        return m_builder.ToString( lastLine, Mathf.Min( m_builder.Length, numLines + diff ) );
    }

    public int RemoveFirstLine(int minimum) {
        int index = m_builder.ToString(0,Mathf.Max(minimum+2, 255)).IndexOf('\n') + 1;//if 255 isn't enough, then use passed in minimum + 2
        if( index < 0 ) {
            index = m_builder.ToString( 256, 512 ).IndexOf( '\n' ) + 1;//this HAS to be!
        }
        m_builder.Remove( 0, index );//hope so... /whistles away
        return index;
    }
}
