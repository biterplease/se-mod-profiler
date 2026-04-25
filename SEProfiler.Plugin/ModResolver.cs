using System;
using System.Reflection;
using Sandbox.Game.World;

namespace SEProfiler
{
    public sealed class ModResolver
    {
        public bool IsSessionReady
        {
            get { return MySession.Static != null; }
        }

        // Returns null when modId is empty (observe all boundaries)
        // or when no matching assembly is found
        public Assembly Resolve(string modId)
        {
            if (string.IsNullOrEmpty(modId))
                return null;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly asm in assemblies)
            {
                if (asm.IsDynamic)
                    continue;

                try
                {
                    // Pre-compiled workshop mods are loaded from a path containing the mod ID
                    if (asm.Location.IndexOf(modId, StringComparison.OrdinalIgnoreCase) >= 0)
                        return asm;
                }
                catch (NotSupportedException)
                {
                    // Dynamic or in-memory assemblies throw on Location access
                }
            }

            // Script-compiled mods produce a dynamic assembly whose FullName
            // may contain the mod name. Try matching on FullName as a fallback.
            foreach (Assembly asm in assemblies)
            {
                if (!asm.IsDynamic)
                    continue;

                if (asm.FullName != null &&
                    asm.FullName.IndexOf(modId, StringComparison.OrdinalIgnoreCase) >= 0)
                    return asm;
            }

            return null;
        }
    }
}
