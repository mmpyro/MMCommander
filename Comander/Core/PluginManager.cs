using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using CommanderPlugin;
using LogLib;
using System.Threading.Tasks;
using Comander.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Comander.Core
{
    public interface IPluginManager
    {
        [ImportMany(typeof (ICommanderPlugin))]
        IEnumerable<Lazy<ICommanderPlugin, IOperationName>> Plugins { get; set; }

        void LoadPlugins(string path);
        IEnumerable<string> GetMethods();

        /// <param name="name">Method name</param>
        /// <param name="dir">Directory full name</param>
        /// <param name="files">Files full name</param>
        Task InvokeMethod(string name, string dir, IEnumerable<string> files);
    }

    public class NullPluginManager : IPluginManager
    {
        public IEnumerable<Lazy<ICommanderPlugin, IOperationName>> Plugins { get; set; }
        public void LoadPlugins(string path)
        {

        }

        public IEnumerable<string> GetMethods()
        {
            return new List<string>();
        }

        public Task InvokeMethod(string name, string dir, IEnumerable<string> files)
        {
            return Task.Run(() => { });
        }
    }

    public class PluginManager : IPluginManager
    {
        private readonly ILogger _logger;

        [ImportMany(typeof(ICommanderPlugin))]
        public IEnumerable<Lazy<ICommanderPlugin, IOperationName>> Plugins { get; set; }

        public PluginManager(ILogger logger)
        {
            _logger = logger;
        }

        // Loads only *.dll files; a file that fails to load is logged and skipped, not fatal for the others (BUG-20).
        public void LoadPlugins(string path)
        {
            var catalog = new AggregateCatalog();
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path, "*.dll"))
                {
                    try
                    {
                        var ac = new AssemblyCatalog(Assembly.LoadFrom(file));
                        ac.Parts.ToArray();
                        catalog.Catalogs.Add(ac);
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Cannot load plugin " + file, e);
                    }
                }
            }

            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        public IEnumerable<string> GetMethods()
        {
            if(Plugins != null && Plugins.Any())
                return Plugins.Select(t => t.Metadata.Name);
            return new List<string>();
        }

        public async Task InvokeMethod(string name, string dir, IEnumerable<string> files)
        {
            var plugin = Plugins.Single(t => t.Metadata.Name.Equals(name));
            var response = await plugin.Value.InvokeAsync(dir,files);
            ReportExecutionStatus(name, response);
        }

        private void ReportExecutionStatus(string taskName, Response response)
        {
            if(response.Status == OperationStatus.Success)
            {
                _logger.Info(string.Format("Task: {0} completed succesfuly.",taskName));
            }
            else
            {
                _logger.Error(response.ErrorMessage);
            }
            WeakReferenceMessenger.Default.Send(new RefreshMessage());
        }
    }
}