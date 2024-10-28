using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;

namespace MergeNow
{
    internal static class Logger
    {
        public static void Error(Exception ex) => Error(ex?.ToString());

        public static void Error(string error, Exception ex) => Error($"{error}, {ex?.ToString()}");

        public static void Error(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return;
            }

            Debug.WriteLine(error);
            ActivityLog.TryLogError("Merge Now", error);
        }
    }
}
