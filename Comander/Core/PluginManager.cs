using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using CommanderPlugin;
using IOLib;

namespace Comander.Core
{
    public interface IPluginManager
    {
        [ImportMany(typeof (ICommanderPlugin))]
        IEnumerable<Lazy<ICommanderPlugin, IOperationName>> Plugins { get; set; }

        void LoadPlugins(string path);
        IEnumerable<string> GetMethods();
        void InvokeMethod(string name, IEnumerable<IAbstractFileStructure> files);
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

        public void InvokeMethod(string name, IEnumerable<IAbstractFileStructure> files)
        {

        }
    }

    public class PluginManager : IPluginManager
    {
        [ImportMany(typeof(ICommanderPlugin))]
        public IEnumerable<Lazy<ICommanderPlugin, IOperationName>> Plugins { get; set; }

        public void LoadPlugins(string path)
        {
            var catalog = new AggregateCatalog();
            var di = new DirectoryInfo(path);
            foreach (var file in di.GetFileSystemInfos())
            {
                try
                {
                    var ac = new AssemblyCatalog(Assembly.LoadFile(file.FullName));
                    ac.Parts.ToArray();
                    catalog.Catalogs.Add(ac);
                }
                catch (ReflectionTypeLoadException)
                {
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

        public void InvokeMethod(string name, IEnumerable<IAbstractFileStructure> files)
        {
            var plugin = Plugins.Single(t => t.Metadata.Name.Equals(name));
            plugin.Value.Invoke(files);
        }

    }
}