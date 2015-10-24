using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOLib;

namespace IOLinq
{
    enum ParserType
    {
        ExtEquals,
        ExtIn,
        NameEquals,
        LiekName,
        ExtNotEquals,
        ExtNotIn
    }

    public class SyntaxParser
    {
        private readonly IFileFactory _fileFactory;
        private readonly Queue<string> _syntaxQueue = new Queue<string>();
        private readonly Dictionary<ParserType, Parser> _parserDict = new Dictionary<ParserType, Parser>();
        private readonly Queue<Unit> _operationQueue = new Queue<Unit>();
        private IAbstractFileStructure[] _abstractFiles;

        public SyntaxParser(IFileFactory fileFactory )
        {
            _fileFactory = fileFactory;
            _parserDict.Add(ParserType.ExtEquals, new ExtEqualsParser());
            _parserDict.Add(ParserType.ExtIn, new ExtInParser());
            _parserDict.Add(ParserType.NameEquals, new NameEqualsParser());
            _parserDict.Add(ParserType.LiekName, new NameLikeParser());
            _parserDict.Add(ParserType.ExtNotEquals, new NotExtEqualsParser());
            _parserDict.Add(ParserType.ExtNotIn, new ExtNotInParser());
        }

        public List<IAbstractFileStructure> Perform(string txt, IAbstractFileStructure[] abstractFiles)
        {
            try
            {
                Split(txt);
                _abstractFiles = abstractFiles;
                while (_syntaxQueue.Count > 0)
                {
                    string syntax = _syntaxQueue.Dequeue();
                    if (IsAndOperator(syntax))
                    {
                        InvokeParse(_syntaxQueue.Dequeue());
                        _operationQueue.Enqueue(_operationQueue.Dequeue() & _operationQueue.Dequeue());
                    }
                    else if (IsOrOperator(syntax))
                    {
                        InvokeParse(_syntaxQueue.Dequeue());
                        _operationQueue.Enqueue(_operationQueue.Dequeue() | _operationQueue.Dequeue());
                    }
                    else
                    {
                        InvokeParse(syntax);
                    }
                }
                var result = new List<IAbstractFileStructure>(_operationQueue.Dequeue().AbstractFiles);
                ClearQueue();
                return result;
            }
            catch
            {
                ClearQueue();
                return _fileFactory.CreateEmptyFileList();
            }
        }

        private void ClearQueue()
        {
            _syntaxQueue.Clear();
            _operationQueue.Clear();
        }

        public Task<List<IAbstractFileStructure>> PerformAsync(string txt, IAbstractFileStructure[] abstractFiles)
        {
            return Task<List<IAbstractFileStructure>>.Run(() =>
            {
                return Perform(txt, abstractFiles);
            });
        }

        private void InvokeParse(string syntax)
        {
            _operationQueue.Enqueue(Parse(syntax));
        }

        private Unit Parse(string value)
        {
            if (Parser.IsExtEquals(value))
            {
                return _parserDict[ParserType.ExtEquals].Perform(value, _abstractFiles);
            }
            if (Parser.IsExtIn(value))
            {
                return _parserDict[ParserType.ExtIn].Perform(value, _abstractFiles);
            }
            if (Parser.IsExtNotIn(value))
            {
                return _parserDict[ParserType.ExtNotIn].Perform(value, _abstractFiles);
            }
            if (Parser.IsNotExtEquals(value))
            {
                return _parserDict[ParserType.ExtNotEquals].Perform(value, _abstractFiles);
            }
            if (Parser.IsNameEquals(value))
            {
                return _parserDict[ParserType.NameEquals].Perform(value, _abstractFiles);
            }
            if (Parser.IsLikeNameParser(value))
            {
                return _parserDict[ParserType.LiekName].Perform(value, _abstractFiles);
            }
            throw new InvalidDataException("Syntax is invalid");
        }

        private bool IsAndOperator(string input)
        {
            return input.Equals("&");
        }

        private bool IsOrOperator(string input)
        {
            return input.Equals("|");
        }

        private void Split(string txt)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < txt.Length; i++)
            {
                if (IsAndOperator(txt[i].ToString()) || IsOrOperator(txt[i].ToString()))
                {
                    _syntaxQueue.Enqueue(sb.ToString().Trim());
                    _syntaxQueue.Enqueue(txt[i].ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(txt[i]);
                }
            }
            _syntaxQueue.Enqueue(sb.ToString().Trim());
        }
    }
}