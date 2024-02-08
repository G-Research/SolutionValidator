// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Serilog;

namespace SlnUtils
{
    // Stupid-simple console manager
    public class Reporter
    {
        public static void Info(string message)
        {
            Log.Information(message);
        }

        public static void Error(string message)
        {
            Log.Error(message);
        }

        public static void Verbose(string message)
        {
            Log.Verbose(message);
        }

    }
}
