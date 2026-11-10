using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly string InitFilePath = Path.Combine(AppContext.BaseDirectory, "Commands.xml");
        private readonly IReadOnlyList<GenericCommand> _list = new List<GenericCommand>();

        public GenericCommandManager(IOManager io1, IOManager io2, ILogger logger)
        {
            var invoker = new GenericCommandInvoker(io1, io2);
            try
            {
                _list = (from item in XDocument.Load(InitFilePath).Descendants("Command")
                         select new GenericCommand(item.Attribute("name").Value, item.Element("Parameter").Value, invoker)).ToList();
            }
            catch (Exception e)
            {
                // A broken Commands.xml must not stop the app from starting (BUG-08).
                logger.Error(e);
            }
        }

        public IEnumerable<GenericCommand> GetCommands()
        {
            return _list;
        }
    }
}
