using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Comander.ViewModel;
using Comander.ViewModel.Commands;
using LogLib;

namespace Comander.Core
{
    public interface IGenericCommandManager
    {
        IEnumerable<GenericCommand> GetCommands();
    }

    public class GenericCommandManager : IGenericCommandManager
    {
        private readonly ILogger _logger;
        private const string InitFilePath = "Commands.xml";
        private readonly IEnumerable<GenericCommand> _list;


        public GenericCommandManager(IOManager io1, IOManager io2, ILogger logger)
        {
            _logger = logger;
            GenericCommandInvoker invoker = new GenericCommandInvoker(io1, io2, _logger);
            try
            {
                _list = (from item in XDocument.Load(InitFilePath).Descendants("Command")
                         select new GenericCommand(item.Attribute("name").Value, item.Element("Parameter").Value, invoker));
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public IEnumerable<GenericCommand> GetCommands()
        {
            return _list;
        }
    }
}