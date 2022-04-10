using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace hsh
{
    public static class HsEditor
    {
        public static readonly char BlankChar = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\0' : ' ';

        public readonly struct EditorResult
        {
            public readonly List<string> Lines;
            public readonly bool Write;

            public EditorResult(List<string> lines, bool write)
            {
                Lines = lines;
                Write = write;
            }
        }

        public static EditorResult Open(string content, bool readOnly) => Open(content.Split('\n'), readOnly);

        public static EditorResult Open(IEnumerable<string> lines, bool readOnly)
        {
            StringBuilder menuSb = new();
            bool menu = false;
            EditorView editorView = new(lines, Console.BufferWidth, Console.WindowHeight - 1);
            editorView.Redraw();
            editorView.PlaceCursor();
            List<BlockChange?> changes = new();
            while (true)
            {
                var key = Console.ReadKey(true);
                changes.Add(editorView.SetWidth(Console.BufferWidth));
                changes.Add(editorView.SetHeight(Console.WindowHeight - 1));
                if (!menu)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            {
                                menu = true;
                                PrintStatusBar("", true);
                                break;
                            }
                        case ConsoleKey.Backspace:
                            {
                                int r = editorView.Row, c = editorView.Column;
                                string str = editorView.Lines[r];
                                if (c == 0)
                                {
                                    if (r != 0)
                                    {
                                        string prevStr = editorView.Lines[r - 1];
                                        changes.Add(editorView.ModifyLine(prevStr + str, r - 1));
                                        changes.Add(editorView.RemoveLine(r));
                                        changes.Add(editorView.SetCursor(r - 1, prevStr.Length));
                                    }
                                }
                                else if (str.Length != 0)
                                {
                                    str = str.Remove(c - 1, 1);
                                    changes.Add(editorView.ModifyCurrentLine(str));
                                    if (c != str.Length + 1)
                                        editorView.MoveLeft();
                                }

                                break;
                            }
                        case ConsoleKey.Enter:
                            {
                                int r = editorView.Row, c = editorView.Column;
                                string line = editorView.Lines[r];
                                string before = line[..c], after = line[c..];
                                changes.Add(editorView.ModifyLine(before, r));
                                changes.Add(editorView.AddLine(after, r + 1));
                                changes.Add(editorView.SetCursor(r + 1, 0));
                                break;
                            }
                        case ConsoleKey.LeftArrow:
                            {
                                editorView.MoveLeft();
                                break;
                            }
                        case ConsoleKey.RightArrow:
                            {
                                editorView.MoveRight();
                                break;
                            }
                        case ConsoleKey.UpArrow:
                            {
                                changes.Add(editorView.MoveUp());
                                break;
                            }
                        case ConsoleKey.DownArrow:
                            {
                                changes.Add(editorView.MoveDown());
                                break;
                            }
                        default:
                            if (key.KeyChar != '\0')
                            {
                                changes.Add(editorView.ModifyCurrentLine(editorView.Lines[editorView.Row]
                                    .Insert(editorView.Column, key.KeyChar.ToString())));
                                editorView.MoveRight();
                            }

                            break;
                    }

                    var change = GetCombinationChange(changes);
                    if (change != null) editorView.Redraw(change.Value);

                    changes.Clear();

                    editorView.PlaceCursor();
                }
                else
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            {
                                menuSb.Clear();
                                PrintStatusBar("", false);
                                menu = false;
                                editorView.Redraw();
                                editorView.PlaceCursor();
                                break;
                            }
                        case ConsoleKey.Backspace:
                            {
                                if (menuSb.Length != 0)
                                {
                                    menuSb.Remove(menuSb.Length - 1, 1);
                                    PrintStatusBar(menuSb.ToString(), true);
                                }

                                break;
                            }
                        case ConsoleKey.Enter:
                            {
                                string cmd = menuSb.ToString();
                                if (cmd.Contains('q', StringComparison.InvariantCultureIgnoreCase))
                                {
                                    bool write = cmd.Contains('w', StringComparison.InvariantCultureIgnoreCase);
                                    if (readOnly && write)
                                    {
                                        PrintStatusBar("Cannot write a read-only buffer", true);
                                        menuSb.Clear();
                                    }
                                    else
                                    {
                                        Console.Clear();
                                        return new EditorResult(editorView._lines, cmd.Contains('w'));
                                    }
                                }
                                else
                                {
                                    PrintStatusBar("Not a command/flag. w=write, q=quit", true);
                                    menuSb.Clear();
                                }

                                break;
                            }
                        default:
                            {
                                if (key.KeyChar != '\0')
                                {
                                    menuSb.Append(key.KeyChar);
                                    PrintStatusBar(menuSb.ToString(), true);
                                }

                                break;
                            }
                    }
                }
            }
        }

        private static void PrintStatusBar(string bar, bool commandEntry)
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write(new string(BlankChar, Console.BufferWidth - 1));
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write(commandEntry ? $"> {bar}" : bar);
        }

        private static BlockChange? GetCombinationChange(IEnumerable<BlockChange?> ranges)
        {
            BlockChange? res = null;
            foreach (var range in ranges)
            {
                if (range == null) continue;
                if (res == null)
                    res = range;
                else
                {
                    int startColumn;
                    int endColumn;
                    if (range.Value._viewRows.End.IsFromEnd)
                    {
                        Range viewRows;
                        if (res.Value._viewRows.Start.Value == range.Value._viewRows.Start.Value)
                        {
                            viewRows = Range.StartAt(Math.Min(res.Value._viewRows.Start.Value,
                                range.Value._viewRows.Start.Value));
                            startColumn = Math.Min(res.Value._startColumn, range.Value._startColumn);
                            endColumn = Math.Max(res.Value._endColumn, range.Value._endColumn);
                        }
                        else if (res.Value._viewRows.Start.Value < range.Value._viewRows.Start.Value)
                        {
                            viewRows = Range.StartAt(res.Value._viewRows.Start.Value);
                            startColumn = res.Value._startColumn;
                            endColumn = Math.Max(res.Value._endColumn, range.Value._endColumn);
                        }
                        else
                        {
                            viewRows = Range.StartAt(range.Value._viewRows.Start.Value);
                            startColumn = range.Value._startColumn;
                            endColumn = Math.Max(res.Value._endColumn, range.Value._endColumn);
                        }

                        res = new BlockChange(viewRows, startColumn, endColumn);
                    }
                    else
                    {
                        int startRow;
                        int endRow;
                        if (res.Value._viewRows.Start.Value == range.Value._viewRows.Start.Value)
                        {
                            startRow = Math.Min(res.Value._viewRows.Start.Value, range.Value._viewRows.Start.Value);
                            startColumn = Math.Min(res.Value._startColumn, range.Value._startColumn);
                        }
                        else if (res.Value._viewRows.Start.Value < range.Value._viewRows.Start.Value)
                        {
                            startRow = res.Value._viewRows.Start.Value;
                            startColumn = res.Value._startColumn;
                        }
                        else
                        {
                            startRow = range.Value._viewRows.Start.Value;
                            startColumn = range.Value._startColumn;
                        }

                        if (res.Value._viewRows.End.Value == range.Value._viewRows.End.Value)
                        {
                            endRow = Math.Max(res.Value._viewRows.End.Value, range.Value._viewRows.End.Value);
                            endColumn = Math.Max(res.Value._endColumn, range.Value._endColumn);
                        }
                        else if (res.Value._viewRows.End.Value < range.Value._viewRows.End.Value)
                        {
                            endRow = range.Value._viewRows.End.Value;
                            endColumn = range.Value._endColumn;
                        }
                        else
                        {
                            endRow = res.Value._viewRows.End.Value;
                            endColumn = res.Value._endColumn;
                        }

                        res = new BlockChange(new Range(startRow, endRow), startColumn, endColumn);
                    }
                }
            }

            return res;
        }

        private readonly struct BlockChange
        {
            public readonly Range _viewRows;
            public readonly int _startColumn;
            public readonly int _endColumn;

            public BlockChange(Range viewRows, int startColumn, int endColumn)
            {
                _viewRows = viewRows;
                _startColumn = startColumn;
                _endColumn = endColumn;
            }
        }

        private class EditorView
        {
            private readonly List<List<SubLineInfo>> _subLineInfos;
            internal List<string> _lines;
            private int _width;
            private int _height;
            private int _viewStartRow;
            private string _clear;
            public IReadOnlyList<string> Lines => _lines;
            public int Row { get; private set; }
            public int Column { get; private set; }
            private int _totalHeight;

            public EditorView(IEnumerable<string> seed, int width, int height)
            {
                _subLineInfos = new List<List<SubLineInfo>>();
                _lines = new List<string>();
                _clear = "";
                SetWidth(width);
                SetHeight(height);
                AddLines(seed, 0);
                if (_lines.Count == 0)
                    AddLine("", 0);
            }

            public BlockChange? MoveDown()
            {
                Row = Math.Clamp(Row + 1, 0, _lines.Count - 1);
                Column = Math.Clamp(Column, 0, _lines[Row].Length);
                return LimitStartLine();
            }

            public BlockChange? MoveUp()
            {
                Row = Math.Clamp(Row - 1, 0, _lines.Count - 1);
                Column = Math.Clamp(Column, 0, _lines[Row].Length);
                return LimitStartLine();
            }

            public void MoveRight()
            {
                Column = Math.Clamp(Column + 1, 0, _lines[Row].Length);
            }

            public void MoveLeft()
            {
                Column = Math.Clamp(Column - 1, 0, _lines[Row].Length);
            }

            public BlockChange? SetCursor(int row, int column)
            {
                Row = Math.Clamp(row, 0, _lines.Count - 1);
                Column = Math.Clamp(column, 0, _lines[Row].Length);
                return LimitStartLine();
            }

            private BlockChange? LimitStartLine()
            {
                BlockChange? change = null;
                if (_viewStartRow > Row)
                {
                    _viewStartRow = Row;
                    change = new BlockChange(Range.All, 0, 0);
                }
                else
                {
                    int curDisplayRowEnd = GetDisplayRow(Row) + _subLineInfos[Row].Count;
                    while (_viewStartRow != Row && curDisplayRowEnd > GetDisplayRow(_viewStartRow) + _height)
                    {
                        _viewStartRow++;
                        change = new BlockChange(Range.All, 0, 0);
                    }
                }

                return change;
            }

            public void Redraw()
            {
                Redraw(new BlockChange(Range.All, 0, 0));
            }

            public void Redraw(BlockChange change)
            {
                if (change._viewRows.Start.Value == change._viewRows.End.Value &&
                    !change._viewRows.End.IsFromEnd) return;
                int conRow = 0;
                int row = _viewStartRow;
                int rowDisplay = GetDisplayRow(row);
                int availableSubRows = Math.Min(_height, _totalHeight - rowDisplay);
                Range displayed = new(_viewStartRow, _viewStartRow + availableSubRows);
                if (change._viewRows.Start.Value > displayed.End.Value) return;
                if (!change._viewRows.End.IsFromEnd && change._viewRows.End.Value < displayed.Start.Value) return;
                bool cascading = change._viewRows.End.IsFromEnd;

                int min = Math.Max(change._viewRows.Start.Value, displayed.Start.Value),
                    max =
                        Math.Min(change._viewRows.End.IsFromEnd ? displayed.End.Value : change._viewRows.End.Value,
                            displayed.End.Value);
                int minCol = min == change._viewRows.Start.Value ? change._startColumn : 0;

                int sub = 0;
                while (rowDisplay < min)
                {
                    NextSubLine(ref row, ref sub);
                    rowDisplay++;
                    conRow++;
                }

                for (int i = min; i < max; i++)
                {
                    if (i == min)
                    {
                        Console.SetCursorPosition(minCol, conRow);
                        Console.Write(_clear[minCol..]);
                        Console.SetCursorPosition(minCol, conRow);
                        Console.Write(_subLineInfos[row][sub]._line[minCol..]);
                    }
                    else
                    {
                        Console.SetCursorPosition(0, conRow);
                        Console.Write(_clear);
                        Console.SetCursorPosition(0, conRow);
                        Console.Write(_subLineInfos[row][sub]._line);
                    }

                    NextSubLine(ref row, ref sub);
                    conRow++;
                }

                int frameEnd = _viewStartRow + _height;
                if (cascading && max < frameEnd)
                {
                    for (int i = max + 1; i < frameEnd; i++)
                    {
                        Console.SetCursorPosition(0, conRow);
                        Console.Write(_clear);
                        conRow++;
                    }
                }

                Console.CursorTop = _height - 1;
            }

            public void PlaceCursor()
            {
                int conRow = 0;
                int row = _viewStartRow;
                int rowDisplay = GetDisplayRow(row);
                int availableSubRows = Math.Min(_height, _totalHeight - rowDisplay);
                Range displayed = new(_viewStartRow, _viewStartRow + availableSubRows);
                int sub = 0;
                int subTotal = 0;
                while (rowDisplay < displayed.Start.Value)
                {
                    NextSubLine(ref row, ref sub);
                    if (sub == 0)
                        subTotal = 0;
                    else
                        subTotal += _subLineInfos[row][sub - 1]._line.Length;
                    rowDisplay++;
                    conRow++;
                }

                int end = displayed.End.Value;
                for (int i = displayed.Start.Value; i < end; i++)
                {
                    if (row == Row)
                    {
                        var sli = _subLineInfos[row];
                        string subLine = sli[sub]._line;
                        if (subTotal <= Column &&
                            Column < subTotal + subLine.Length + (sub == sli.Count - 1 ? 1 : 0))
                        {
                            int lcl = Column - subTotal;
                            Console.SetCursorPosition(new StringInfo(subLine[..lcl]).LengthInTextElements, conRow);
                            return;
                        }
                    }

                    NextSubLine(ref row, ref sub);
                    if (sub == 0)
                        subTotal = 0;
                    else
                        subTotal += _subLineInfos[row][sub - 1]._line.Length;
                    conRow++;
                }

                Console.SetCursorPosition(0, 0);
            }

            private void NextSubLine(ref int line, ref int subLine)
            {
                if (_subLineInfos[line].Count == subLine + 1)
                {
                    line++;
                    subLine = 0;
                }
                else
                {
                    subLine++;
                }
            }

            public BlockChange AddLine(string added, int addedLine)
            {
                var lines = new List<SubLineInfo>();
                AddSubLines(added, lines);
                _subLineInfos.Insert(addedLine, lines);
                _lines.Insert(addedLine, added);
                _totalHeight += lines.Count;
                return new BlockChange(Range.StartAt(GetDisplayRow(addedLine)), 0, 0);
            }

            private BlockChange? AddLines(IEnumerable<string> lines, int addedLine)
            {
                BlockChange? res = null;
                foreach (string line in lines)
                {
                    var v = AddLine(line, addedLine++);
                    res ??= v;
                }

                return res;
            }

            public BlockChange RemoveLine(int removedLine, int count = 1)
            {
                int baseRow = Row, baseColumn = Column;
                int startRow = GetDisplayRow(removedLine);
                count = Math.Min(Math.Max(0, count), _lines.Count - removedLine);
                int removedCount;
                if (removedLine + count >= _lines.Count)
                    removedCount = _totalHeight - startRow;
                else
                    removedCount = GetDisplayRow(removedLine + count) - startRow;
                for (int i = 0; i < count; i++)
                {
                    _subLineInfos.RemoveAt(removedLine);
                    _lines.RemoveAt(removedLine);
                }

                _totalHeight -= removedCount;
                var allRange = SetCursor(baseRow, baseColumn);
                return allRange ?? new BlockChange(Range.StartAt(startRow), 0, 0);
            }

            public BlockChange? ModifyCurrentLine(string modified) => ModifyLine(modified, Row);

            public BlockChange? ModifyLine(string modified, int modifiedLine)
            {
                int baseRow = Row, baseColumn = Column;
                var lines = _subLineInfos[modifiedLine];
                int origLen = lines.Count;
                var tempOrig = ArrayPool<string>.Shared.Rent(Math.Max(origLen, 8));
                try
                {
                    for (int i = 0; i < origLen; i++)
                        tempOrig[i] = lines[i]._line;
                    lines.Clear();
                    AddSubLines(modified, lines);
                    _lines[modifiedLine] = modified;
                    var allRange = SetCursor(baseRow, baseColumn);
                    int startDisplayRow = GetDisplayRow(modifiedLine);
                    int resLen = lines.Count;
                    _totalHeight += resLen - origLen;
                    int min = Math.Min(origLen, resLen);
                    for (int i = 0; i < min; i++)
                    {
                        string subNew = lines[i]._line, subOld = tempOrig[i];
                        if (subNew != subOld)
                        {
                            int similar = 0;
                            int commonLength = Math.Min(subNew.Length, subOld.Length);
                            for (int j = 0; j < commonLength; j++)
                                if (subNew[j] != subOld[j])
                                    break;
                                else
                                    similar++;
                            return allRange ?? (origLen == resLen
                                ? new BlockChange(new Range(startDisplayRow + i, startDisplayRow + resLen), similar,
                                    _width)
                                : new BlockChange(Range.StartAt(startDisplayRow + i), similar, 0));
                        }
                    }

                    if (origLen == resLen)
                        return allRange;
                    return allRange ?? new BlockChange(Range.StartAt(startDisplayRow + min), 0, 0);
                }
                finally
                {
                    ArrayPool<string>.Shared.Return(tempOrig);
                }
            }

            public BlockChange? SetWidth(int width)
            {
                if (width == _width) return null;
                _width = width;
                var lines = _lines;
                _lines = new List<string>();
                _subLineInfos.Clear();
                _clear = new string(BlankChar, Console.BufferWidth);
                return AddLines(lines, 0);
            }

            public BlockChange? SetHeight(int height)
            {
                if (height == _height) return null;
                _height = height;
                return new BlockChange(Range.StartAt(Math.Min(_height, _totalHeight)), 0, 0);
            }

            private int GetDisplayRow(int line)
            {
                int displayRow = 0;
                for (int i = 0; i < line; i++)
                    displayRow += _subLineInfos[i].Count;
                return displayRow;
            }

            private void AddSubLines(string line, List<SubLineInfo> lineInfos)
            {
                int i = 0;
                int c = 0;
                while (line.Length != 0)
                {
                    var info = new StringInfo(line);
                    int endSlice = Math.Min(info.LengthInTextElements, _width);
                    string cur = info.SubstringByTextElements(0, endSlice);
                    lineInfos.Add(new SubLineInfo(cur, i, i + endSlice));
                    line = endSlice != info.LengthInTextElements ? info.SubstringByTextElements(endSlice) : "";
                    i += cur.Length;
                    c++;
                }

                if (c == 0 || lineInfos[^1]._endIdx - lineInfos[^1]._startIdx == _width)
                    lineInfos.Add(new SubLineInfo(line, 0, 0));
            }

            private readonly struct SubLineInfo
            {
                public readonly string _line;
                public readonly int _startIdx;
                public readonly int _endIdx;

                public SubLineInfo(string line, int startIdx, int endIdx)
                {
                    _line = line;
                    _startIdx = startIdx;
                    _endIdx = endIdx;
                }
            }
        }
    }
}
