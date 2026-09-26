using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Parallax.Tests.EditMode
{
    // PAX-075 (D-079): the route harness lives in the Editor assembly, which this assembly doesn't
    // reference, so tests reach it by reflection (as with every other Editor type).
    static class RouteTestApi
    {
        public struct Rec
        {
            public int Tick, RoomLifeTick, Move; public float X, Y, Vx, Vy;
            public bool Grounded, GravityUp, Dead, Complete, JumpPressed; public string Ground;
            public int[] FireTick;
        }

        public static Type T(string name)
        {
            Type type = Type.GetType("Parallax.Editor.Routes." + name + ", Parallax.Editor");
            Assert.NotNull(type, "Parallax.Editor.Routes." + name + " not found.");
            return type;
        }

        public static object Call(Type type, string method, params object[] args)
        {
            // PAX-087: R.Hold has two overloads (int, Vertical); pick the one whose parameters take these arguments.
            MethodInfo m = null;
            foreach (MethodInfo candidate in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (candidate.Name != method) continue;
                ParameterInfo[] ps = candidate.GetParameters();
                if (ps.Length != args.Length) continue;
                bool fits = true;
                for (int i = 0; i < ps.Length && fits; i++)
                    fits = args[i] == null ? !ps[i].ParameterType.IsValueType || Nullable.GetUnderlyingType(ps[i].ParameterType) != null : ps[i].ParameterType.IsInstanceOfType(args[i]);
                if (fits) { m = candidate; break; }
            }
            Assert.NotNull(m, type.Name + "." + method + " not found for these arguments.");
            try { return m.Invoke(null, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
        }

        // R20: earlier tests that build objects in the active scene (e.g. LevelSelectLayoutTests) leave the Test
        // Runner's untitled scratch scene dirty. When it is the only scene loaded it holds nothing to save, so tests
        // start from a fresh one; RouteSession's guard still refuses every other dirty scene.
        public static void FreshScratchScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (SceneManager.sceneCount == 1 && string.IsNullOrEmpty(active.path) && active.isDirty)
                Call(T("RouteSession"), "RecreateUntitledScene", true);
        }

        public static IDisposable OpenSession()
        {
            FreshScratchScene();
            try { return (IDisposable)Activator.CreateInstance(T("RouteSession")); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
        }

        public static object Case(string fixture, params object[] args) => Call(T("RouteFixtures"), fixture, args);

        public static object Options(int step, string mode, int delta, int authoredStartTick = -1, bool resolveCause = true)
        {
            object o = Activator.CreateInstance(T("ReplayOptions"));
            Set(o, "TimedStep", step);
            Set(o, "Mode", Enum.Parse(T("TimedMode"), mode));
            Set(o, "Delta", delta);
            Set(o, "AuthoredStartTick", authoredStartTick);
            Set(o, "ResolveCause", resolveCause);
            return o;
        }

        public static object Replay(IDisposable session, object routeCase, object options = null) =>
            Call(T("RouteHarness"), "Replay", session, F(routeCase, "Room"), F(routeCase, "Route"), options);

        public static object ReplayRoute(IDisposable session, object room, object route, object options = null) =>
            Call(T("RouteHarness"), "Replay", session, room, route, options);

        public static List<Rec> Records(object replay)
        {
            var list = new List<Rec>();
            foreach (object r in (IEnumerable)F(replay, "Records"))
                list.Add(new Rec
                {
                    Tick = (int)F(r, "Tick"), RoomLifeTick = (int)F(r, "RoomLifeTick"), Move = (int)F(r, "Move"),
                    X = (float)F(r, "X"), Y = (float)F(r, "Y"), Vx = (float)F(r, "Vx"), Vy = (float)F(r, "Vy"),
                    Grounded = (bool)F(r, "Grounded"), GravityUp = (bool)F(r, "GravityUp"), Dead = (bool)F(r, "Dead"),
                    Complete = (bool)F(r, "Complete"), JumpPressed = (bool)F(r, "JumpPressed"), Ground = (string)F(r, "Ground"),
                    FireTick = (int[])F(r, "FireTick"),
                });
            return list;
        }

        public static int ElementIndex(object replay, string name) => ((List<string>)F(replay, "Elements")).IndexOf(name);

        public static string Dump(object replay) => (string)replay.GetType().GetMethod("Dump").Invoke(replay, new object[] { 0, int.MaxValue });

        public static object F(object o, string name)
        {
            Type t = o.GetType();
            FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) return f.GetValue(o);
            PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(p, t.Name + "." + name + " not found.");
            return p.GetValue(o);
        }

        public static void Set(object o, string name, object value)
        {
            FieldInfo f = o.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(f, o.GetType().Name + "." + name + " not found.");
            f.SetValue(o, value);
        }
    }
}
