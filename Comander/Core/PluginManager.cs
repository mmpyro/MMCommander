using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using CommanderPlugin;
using LogLib;
using System.Threading.Tasks;
using Comander.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Comander.Core
{
    public interface IPluginManager
    {
        void LoadPlugins(string path);
        IEnumerable<string> GetMethods();

        /// <param name="name">Method name</param>
        /// <param name="dir">Directory full name</param>
        /// <param name="files">Files full name</param>
        Task InvokeMethod(string name, string dir, IEnumerable<string> files);
    }

    public class NullPluginManager : IPluginManager
    {
        public void LoadPlugins(string path)
        {

        }

        public IEnumerable<string> GetMethods()
        {
            return new List<string>();
        }

        public Task InvokeMethod(string name, string dir, IEnumerable<string> files)
        {
            return Task.CompletedTask;
        }
    }

    public class PluginManager : IPluginManager
    {
        private readonly ILogger _logger;
        private readonly List<ICommanderPlugin> _plugins = new List<ICommanderPlugin>();

        public PluginManager(ILogger logger)
        {
            _logger = logger;
        }

        // Plugins/<Name>/<Name>.dll (with its own dependencies) or a single Plugins/*.dll.
        // A file that fails to load is logged and skipped, not fatal for the others (BUG-20).
        public void LoadPlugins(string path)
        {
            if (!Directory.Exists(path))
                return;
            var files = Directory.GetFiles(path, "*.dll")
                .Concat(Directory.GetDirectories(path).Select(d => Path.Combine(d, Path.GetFileName(d) + ".dll")).Where(File.Exists));
            foreach (var file in files)
            {
                try
                {
                    var assembly = new PluginLoadContext(file).LoadFromAssemblyPath(Path.GetFullPath(file));
                    foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(ICommanderPlugin).IsAssignableFrom(t)))
                        _plugins.Add((ICommanderPlugin)Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    _logger.Error("Cannot load plugin " + file, e);
                }
            }
        }

        public IEnumerable<string> GetMethods()
        {
            return _plugins.Select(p => p.Name).ToList();
        }

        public async Task InvokeMethod(string name, string dir, IEnumerable<string> files)
        {
            var plugin = _plugins.First(p => p.Name == name);
            var response = await plugin.InvokeAsync(dir, files);
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

    // Each plugin gets its own load context, so its dependencies can differ from the app's.
    // CommanderPlugin is always taken from the app, or the plugin's ICommanderPlugin would be a different type.
    internal class PluginLoadContext : AssemblyLoadContext
    {
        private static readonly string Contract = typeof(ICommanderPlugin).Assembly.GetName().Name;
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(Path.GetFileNameWithoutExtension(pluginPath))
        {
            _resolver = new AssemblyDependencyResolver(Path.GetFullPath(pluginPath));
        }

        // null = fall back to the app's copy (shared framework, Avalonia, CommanderPlugin...).
        protected override Assembly Load(AssemblyName name)
        {
            if (name.Name == Contract)
                return null;
            var path = _resolver.ResolveAssemblyToPath(name);
            return path == null ? null : LoadFromAssemblyPath(path);
        }

        protected override IntPtr LoadUnmanagedDll(string name)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(name);
            return path == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
        }
    }
}
