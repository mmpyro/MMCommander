using System;

namespace Comander.Core
{
    public interface IAssemblyVersionResolver
    {
        string GetProductVersion(Type assembly);
    }

    public class AssemblyVersionResolver : IAssemblyVersionResolver
    {
        public  string GetProductVersion(Type assembly)
        {
            var version = assembly.Assembly.GetName().Version;
            return version.ToString(3); // 2.0.0, not 2.0.0.0
        } 
    }
}