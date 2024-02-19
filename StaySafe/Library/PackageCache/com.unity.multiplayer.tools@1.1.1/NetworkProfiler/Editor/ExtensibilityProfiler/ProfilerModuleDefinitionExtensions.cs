﻿#if UNITY_2021_2_OR_NEWER
using System.Linq;
using Unity.Profiling;
using Unity.Profiling.Editor;

namespace Unity.Multiplayer.Tools.NetworkProfiler.Editor
{
    internal static class ProfilerModuleDefinitionExtensions
    {
        public static ProfilerCounterDescriptor[] CountersAsDescriptors(this ProfilerModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Counters
                .Select(counter => new ProfilerCounterDescriptor(counter, ProfilerCategory.Network.Name))
                .ToArray();
        }
    }
}
#endif