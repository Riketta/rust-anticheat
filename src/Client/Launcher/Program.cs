using System;
using System.Collections.Generic;
using System.Reflection;

namespace Loader
{
    public static class Program
    {
        public static void Load()
        {
            new RowClient.RGuard().Main();
        }
    }
}