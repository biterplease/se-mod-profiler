using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using SEProfiler.Sinks;
using VRage.Game.Components;

namespace SEProfiler
{
    // Outer class holds shared state. Nested classes carry [HarmonyPatch] attributes
    // and are discovered automatically by harmony.PatchAll().
    //
    // Static timestamp fields are safe here because SE's update methods run
    // sequentially on the main simulation thread — Prefix and Postfix are never
    // interleaved for the same method.
    public static class HarmonyPatches
    {
        // Set by Plugin when a scope is active; null means patches are silent.
        public static AggregateSink Sink;

        // Null = observe all assemblies; set by ModResolver after world load.
        public static Assembly TargetAssembly;

        private static bool ShouldRecord(object instance)
        {
            if (Sink == null || instance == null)
                return false;

            Assembly target = TargetAssembly;
            return target == null || instance.GetType().Assembly == target;
        }

        private static double ElapsedMs(long startTimestamp)
        {
            return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
        }

        // ── UpdateAfterSimulation ────────────────────────────────────────────

        [HarmonyPatch(typeof(MyGameLogicComponent), "UpdateAfterSimulation")]
        private static class UpdateAfterSimulationPatch
        {
            private static long _start;
            private static int  _gc0;

            private static void Prefix()
            {
                _start = Stopwatch.GetTimestamp();
                _gc0   = GC.CollectionCount(0);
            }

            private static void Postfix(MyGameLogicComponent __instance)
            {
                if (!HarmonyPatches.ShouldRecord(__instance)) return;
                HarmonyPatches.Sink.RecordFrameworkScope(
                    "UpdateAfterSimulation", ElapsedMs(_start), GC.CollectionCount(0) - _gc0);
            }
        }

        // ── UpdateBeforeSimulation ───────────────────────────────────────────

        [HarmonyPatch(typeof(MyGameLogicComponent), "UpdateBeforeSimulation")]
        private static class UpdateBeforeSimulationPatch
        {
            private static long _start;
            private static int  _gc0;

            private static void Prefix()
            {
                _start = Stopwatch.GetTimestamp();
                _gc0   = GC.CollectionCount(0);
            }

            private static void Postfix(MyGameLogicComponent __instance)
            {
                if (!HarmonyPatches.ShouldRecord(__instance)) return;
                HarmonyPatches.Sink.RecordFrameworkScope(
                    "UpdateBeforeSimulation", ElapsedMs(_start), GC.CollectionCount(0) - _gc0);
            }
        }

        // ── UpdateOnceBeforeFrame ────────────────────────────────────────────

        [HarmonyPatch(typeof(MyGameLogicComponent), "UpdateOnceBeforeFrame")]
        private static class UpdateOnceBeforeFramePatch
        {
            private static long _start;
            private static int  _gc0;

            private static void Prefix()
            {
                _start = Stopwatch.GetTimestamp();
                _gc0   = GC.CollectionCount(0);
            }

            private static void Postfix(MyGameLogicComponent __instance)
            {
                if (!HarmonyPatches.ShouldRecord(__instance)) return;
                HarmonyPatches.Sink.RecordFrameworkScope(
                    "UpdateOnceBeforeFrame", ElapsedMs(_start), GC.CollectionCount(0) - _gc0);
            }
        }

        // ── Init (TargetMethod avoids referencing MyObjectBuilder_EntityBase) ─

        [HarmonyPatch]
        private static class InitPatch
        {
            private static long _start;
            private static int  _gc0;

            private static MethodBase TargetMethod()
            {
                foreach (MethodInfo m in typeof(MyGameLogicComponent)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.Name == "Init" && m.GetParameters().Length == 1)
                        return m;
                }
                return null;
            }

            private static void Prefix()
            {
                _start = Stopwatch.GetTimestamp();
                _gc0   = GC.CollectionCount(0);
            }

            private static void Postfix(MyGameLogicComponent __instance)
            {
                if (!HarmonyPatches.ShouldRecord(__instance)) return;
                HarmonyPatches.Sink.RecordFrameworkScope(
                    "Init", ElapsedMs(_start), GC.CollectionCount(0) - _gc0);
            }
        }

        // ── Close ────────────────────────────────────────────────────────────

        [HarmonyPatch(typeof(MyGameLogicComponent), "Close")]
        private static class ClosePatch
        {
            private static long _start;
            private static int  _gc0;

            private static void Prefix()
            {
                _start = Stopwatch.GetTimestamp();
                _gc0   = GC.CollectionCount(0);
            }

            private static void Postfix(MyGameLogicComponent __instance)
            {
                if (!HarmonyPatches.ShouldRecord(__instance)) return;
                HarmonyPatches.Sink.RecordFrameworkScope(
                    "Close", ElapsedMs(_start), GC.CollectionCount(0) - _gc0);
            }
        }

        // ── MySessionComponentBase.UpdateAfterSimulation ─────────────────────

        [HarmonyPatch(typeof(MySessionComponentBase), "UpdateAfterSimulation")]
        private static class SessionUpdateAfterSimulationPatch
        {
            private static long _start;
            private static int  _gc0;

            private static void Prefix()
            {
                _start = Stopwatch.GetTimestamp();
                _gc0   = GC.CollectionCount(0);
            }

            private static void Postfix(MySessionComponentBase __instance)
            {
                if (!HarmonyPatches.ShouldRecord(__instance)) return;
                HarmonyPatches.Sink.RecordFrameworkScope(
                    "Session.UpdateAfterSimulation", ElapsedMs(_start), GC.CollectionCount(0) - _gc0);
            }
        }
    }
}
