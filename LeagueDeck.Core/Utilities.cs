using System;
using System.Diagnostics;
using System.Reflection;

namespace LeagueDeck.Core
{
    public static class Utilities
    {
        public static Version GetLeagueDeckVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return new Version(fvi.FileVersion);
        }
    }
}
