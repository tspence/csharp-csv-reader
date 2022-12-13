using System;
using System.Collections.Generic;
using System.Text;

// These suggestions from Resharper apply because we don't want it to recommend fixing things needed for Net20:
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ConvertIfStatementToNullCoalescingAssignment
// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchExpression
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ReturnTypeCanBeEnumerable.Global

namespace CSVFile
{
    /// <summary>
    /// The current state of CSV processing, given the text that has been seen so far
    /// </summary>
    public enum CSVState
    {
        /// <summary>
        /// We have reached the end of the CSV and everything is done
        /// </summary>
        Done,

        /// <summary>
        /// We don't need more text at the moment
        /// </summary>
        CanKeepGoing,

        /// <summary>
        /// The CSV reached the end, but there was a missing (unpaired) text qualifier.
        /// For example:
        ///     `1,2,3,"test`
        /// </summary>
        MissingTrailingQualifier
    }

    /// <summary>
    /// This state machine handles all functions of CSV processing except for the I/O, which can come in a variety
    /// of forms, either from a stream or an in-memory collection.
    ///
    /// Since some CSV files have a single row of data that comprises multiple lines, this state machine may or may
    /// not produce one row of data for each chunk of text received.
    /// </summary>
    public class CSVStateMachine
    {
        private readonly CSVSettings _settings;
        private string _line;
        private readonly List<string> _list;
        private readonly StringBuilder _work;
        private int _position;
        private char _delimiter;
        private bool _allowSepLine;
        private bool _inTextQualifier;

        /// <summary>
        /// Whether the state machine has concluded or can continue processing
        /// </summary>
        public CSVState State { get; private set; }

        /// <summary>
        /// Constructs a new state machine to begin processing CSV text
        /// </summary>
        public CSVStateMachine(CSVSettings settings)
        {
            _line = "";
            _list = new List<string>();
            _work = new StringBuilder();
            _settings = settings ?? CSVSettings.CSV;
            _position = -1;

            // The presence of a "sep=" line may affect these values
            _delimiter = _settings.FieldDelimiter;
            _allowSepLine = _settings.AllowSepLine;

            // We are ready for work
            State = CSVState.CanKeepGoing;
        }

        /// <summary>
        /// Parse a single line when read from a stream.
        ///
        /// Call this function when you are using the "ReadLine" or "ReadLineAsync" functions so that
        /// each line will obey the CSV Settings rules for line separators.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="reachedEnd"></param>
        /// <returns></returns>
        public string[] ParseLine(string line, bool reachedEnd)
        {
            if (!string.IsNullOrEmpty(line))
            {
                line += _settings.LineSeparator;
            }
            return ParseChunk(line, reachedEnd);
        }

        /// <summary>
        /// Parse a new chunk of text retrieved via some other means than a stream.
        ///
        /// Call this function when you are retrieving your own text and when each chunk may or may not
        /// include line separators, and your stream does not consume line separators on its own.
        /// </summary>
        /// <param name="chunk">The new data to process</param>
        /// <param name="reachedEnd">Set this value to true </param>
        /// <returns>If this parsing operation produces a valid row, this will be non-null</returns>
        public string[] ParseChunk(string chunk, bool reachedEnd)
        {
            // Detect end of stream
            if (reachedEnd && string.IsNullOrEmpty(chunk) && _position == -1)
            {
                State = CSVState.Done;
                return null;
            }

            // Add this chunk to the current processing logic
            _line += chunk;

            // Check for the presence of a "sep=" line once at the beginning of a stream
            if (_allowSepLine)
            {
                var newDelimiter = CSV.ParseSepLine(_line);
                _allowSepLine = false;
                if (newDelimiter != null)
                {
                    _delimiter = newDelimiter.Value;
                    return null;
                }
            }

            // Process one character at a time from the current line
            while (_position < _line.Length || !reachedEnd)
            {
                _position++;

                // Have we reached the end of the stream?
                if (_position >= _line.Length)
                {
                    if (reachedEnd)
                    {
                        // If we reached the end while still in a text qualifier, the CSV is broken
                        if (_inTextQualifier)
                        {
                            State = CSVState.MissingTrailingQualifier;
                            return null;
                        }

                        // We always add the final work item here because trailing empty strings are valid
                        State = CSVState.Done;
                        _list.Add(_work.ToString());
                        _line = string.Empty;
                        _position = -1;
                        return _list.ToArray();
                    }
                    return null;
                }
                var c = _line[_position];

                // Are we currently processing a text block (which may optionally span multiple lines)?
                if (_inTextQualifier || (!_inTextQualifier && c == _settings.TextQualifier && _work.Length == 0))
                {
                    if (_inTextQualifier)
                    {
                        _work.Append(c);
                    }
                    _inTextQualifier = true;

                    // Our next task is to find the end of this qualified-text field
                    var p2 = -1;
                    while (p2 < 0)
                    {

                        // If we don't see an end in sight, read more from the stream
                        p2 = _line.IndexOf(_settings.TextQualifier, _position + 1);
                        if (p2 < 0)
                        {

                            // No text qualifiers yet? Let's read more from the stream and continue
                            _work.Append(_line.Substring(_position + 1));
                            _line = string.Empty;
                            _position = -1;
                            if (reachedEnd)
                            {
                                State = CSVState.MissingTrailingQualifier;
                            }
                            return null;
                        }

                        // Append the text between the qualifiers
                        _work.Append(_line.Substring(_position + 1, p2 - _position - 1));
                        _position = p2;

                        // If the user put in a doubled-up qualifier, e.g. `""`, insert a single one and continue
                        if (p2 + 1 < _line.Length && _line[p2 + 1] == _settings.TextQualifier)
                        {
                            _work.Append(_settings.TextQualifier);
                            _position++;
                            p2 = -1;
                        }
                    }

                    // We're done parsing this text qualifier
                    _inTextQualifier = false;
                }
                // Are we at a line separator? Let's do a quick test first
                else if (c == _settings.LineSeparator[0] && _position + _settings.LineSeparator.Length <= _line.Length)
                {
                    if (string.Equals(_line.Substring(_position, _settings.LineSeparator.Length),
                            _settings.LineSeparator))
                    {
                        _line = _line.Substring(_position + _settings.LineSeparator.Length);
                        _position = -1;
                        _list.Add(_work.ToString());
                        var row = _list.ToArray();
                        _list.Clear();
                        _work.Length = 0;
                        return row;
                    }
                }
                // Does this start a new field?
                else if (c == _delimiter)
                {
                    // Is this a null token, and do we permit null tokens?
                    var s = _work.ToString();
                    if (_settings.AllowNull && string.Equals(s, _settings.NullToken, StringComparison.Ordinal))
                    {
                        _list.Add(null);
                    }
                    else
                    {
                        _list.Add(s);
                    }
                    _work.Length = 0;

                    // Test for special case: when the user has written a casual comma, space, and text qualifier, skip the space
                    // Checks if the second parameter of the if statement will pass through successfully
                    // e.g. `"bob", "mary", "bill"`
                    if (_position + 2 <= _line.Length - 1)
                    {
                        if (_line[_position + 1].Equals(' ') && _line[_position + 2].Equals(_settings.TextQualifier))
                        {
                            _position++;
                        }
                    }
                }
                // Regular character
                else
                {
                    _work.Append(c);
                }
            }

            State = CSVState.Done;
            return null;
        }
    }
}