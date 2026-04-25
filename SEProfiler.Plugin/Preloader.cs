// Uncomment the next line to enable preloader patching
//#define HAS_PRELOADER_PATCHES

#if HAS_PRELOADER_PATCHES

using System.Collections.Generic;
using Mono.Cecil;

// DO NOT USE A NAMESPACE HERE!
// CRITICAL: Using a namespace here will prevent Pulsar from finding the Preloader class.

public class Preloader
{
    public static IEnumerable<string> TargetDLLs { get; } = new string[0];

    public static void Initialize() { }

    // CRITICAL: This is the AssemblyDefinition class from Mono.Cecil!
    public static void Patch(AssemblyDefinition assembly) { }

    public static void Finish() { }
}

#endif
