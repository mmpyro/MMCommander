using System;

namespace Comander.Core
{
    public class AssemblyVersionResolver
    {
        public static string GetProductVersion(Type assembly)
        {
            var version = assembly.Assembly.GetName().Version;
            return version.ToString();
        } 
    }
}