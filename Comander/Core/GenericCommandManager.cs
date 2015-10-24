using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Comander.ViewModel.Commands;
using LogLib;

namespace Comander.Core
{
    public interface IGenericCommandManager
    {
        GenericCommandInvoker GetCommandByKey(string key);
        GenericCommandInvoker GetCommandByName(string name);
    }

    public class GenericCommandManager : IGenericCommandManager
    {
        private readonly ILogger _logger;
        private const string InitFilePath = "Commands.xml";
        private readonly Dictionary<string, GenericCommandInvoker> _dict;

        public GenericCommandManager(ILogger logger)
        {
            _logger = logger;
            try
            {
                _dict = (from item in XDocument.Load(InitFilePath).Descendants("Command")
                    select new GenericCommandInvoker( item.Attribute("name").Value, item.Attribute("key").Value, item.Element("Parameter").Value)).ToDictionary(t => t.Key);
            }
            catch (Exception e)
            {
                _logger.WriteLine(e, LogInfo.Error);
            }
        }

        public GenericCommandInvoker GetCommandByKey(string key)
        {
            return _dict[key];
        }

        public GenericCommandInvoker GetCommandByName(string name)
        {
            return _dict.Single(t => t.Value.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)).Value;
        }

    }
}