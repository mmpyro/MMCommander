using System;

namespace Comander.Other
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