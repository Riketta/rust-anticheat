using System;
using System.Collections.Generic;
using System.Reflection;

namespace Loader
{
    public static class Program
    {
        public static void Load()
        {
            new RGuard.RGuard().Main();
        }
    }
}