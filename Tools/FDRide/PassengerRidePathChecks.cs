using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using SecretsThatBreathe;
using UnityEngine;

public static class PassengerRidePathChecks
{
    public static int Main()
    {
        // Pure path maths only; no Unity process, native scene or screen control.
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
            string file = Path.Combine("C:/Program Files/Unity/Hub/Editor/6000.3.8f1/Editor/Data/Managed/UnityEngine", new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(file) ? Assembly.LoadFrom(file) : null;
        };
        try { Run(); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static bool Near(Vector3 a, Vector3 b, float tolerance = .001f) => (a - b).magnitude <= tolerance;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        var straight = new PassengerRidePath(Vector3.zero, null, new Vector3(0, 0, 10), 5);
        straight.Evaluate(4, out var mid, out var direction);
        Require(Near(mid, new Vector3(0, 0, 4)) && Near(direction, Vector3.forward), "Straight distance evaluation");
        straight.Evaluate(-10, out mid, out _);
        Require(Near(mid, Vector3.zero), "Negative distance must clamp to start");
        straight.Evaluate(100, out mid, out _);
        Require(Near(mid, new Vector3(0, 0, 10)), "Overshoot must clamp to end");

        var start = new Vector3(209.3862f, 5.09909f, 127.37f);
        var corner = new Vector3(209.3862f, 5.09909f, 330.94418f);
        var end = new Vector3(140, 5.09909f, 330.94418f);
        var route = new PassengerRidePath(start, new[] { corner }, end, 5);
        Require(route.Length > 270 && route.Length < 273, "Expected extended road distance");
        route.Evaluate(0, out mid, out direction);
        Require(Near(mid, start) && Near(direction, Vector3.forward), "Initial vehicle heading");
        route.Evaluate(route.Length, out mid, out direction);
        Require(Near(mid, end) && Near(direction, Vector3.left), "Finish west after left turn");
        var previous = start;
        var previousDirection = Vector3.forward;
        float minDot = 1;
        for (float d = .1f; d < route.Length; d += .1f)
        {
            route.Evaluate(d, out var point, out var forward);
            Require(point.x <= start.x + .001f && point.x >= end.x - .001f, "Path overshot road X bounds");
            Require(point.z >= start.z - .001f && point.z <= end.z + .001f, "Path overshot road Z bounds");
            Require(Math.Abs(point.y - start.y) < .001f, "Ride height changed");
            Require(Math.Abs(Vector3.Distance(previous, point) - .1f) < .002f, "Nonuniform travel speed");
            Require(forward.x <= .001f && forward.z >= -.001f, "Wrong turn direction");
            minDot = Math.Min(minDot, Vector3.Dot(previousDirection, forward));
            previous = point; previousDirection = forward;
        }
        Require(minDot > .999f, "Heading snapped along corner");

        var shortRoute = new PassengerRidePath(Vector3.zero,
            new[] { new Vector3(0, 0, 1), new Vector3(-1, 0, 1) }, new Vector3(-1, 0, 2), 50);
        shortRoute.Evaluate(shortRoute.Length, out mid, out _);
        Require(Near(mid, new Vector3(-1, 0, 2)), "Oversized corner radius on short segments");
        var zero = new PassengerRidePath(start, new[] { start, start }, start, 5);
        zero.Evaluate(10, out mid, out _);
        Require(zero.Length == 0 && Near(mid, start), "Duplicate / zero-length route");
        CheckBraking();
        Console.WriteLine("PASS: straight fallback, clamping, left-turn direction, bounded path, uniform distance, continuous heading, radius clamping, duplicate points.");
        Console.WriteLine("FD route length: " + route.Length.ToString("F2") + " m; worst heading cosine per 0.1 m: " + minDot);
    }

    static void CheckBraking()
    {
        foreach (float fps in new[] { 30f, 60f, 144f })
        {
            var stop = new SmoothRideStop(12f, 8f);
            Require(Math.Abs(stop.Duration - 3f) < .0001f, "Brake duration for 12 m at 8 m/s");
            float previousSpeed = stop.Speed, previousRemaining = stop.RemainingDistance, travelled = 0;
            int frames = 0;
            while (!stop.IsComplete && frames < 10000)
            {
                stop.Advance(1f / fps);
                Require(stop.Speed >= 0 && stop.Speed <= previousSpeed, "Braking must reduce speed monotonically");
                Require(stop.RemainingDistance >= 0 && stop.RemainingDistance <= previousRemaining, "Brake distance overshoot");
                Require(Math.Abs(stop.Speed - previousSpeed) <= 4.01f / fps, "Abrupt brake velocity change");
                travelled += previousRemaining - stop.RemainingDistance;
                previousSpeed = stop.Speed; previousRemaining = stop.RemainingDistance; frames++;
            }
            Require(stop.IsComplete && frames <= (int)(3f * fps) + 1, "Stop must finish in finite time");
            Require(stop.Speed == 0 && stop.RemainingDistance == 0, "Stationary endpoint and silent speed gain");
            Require(Math.Abs(travelled - 12f) < .0001f, "Braking travel must equal the remaining 12 m");
            stop.Advance(1f);
            Require(stop.Speed == 0 && stop.RemainingDistance == 0, "Completed stop must stay still");
        }
        var middle = new SmoothRideStop(12f, 8f);
        middle.Advance(1.5f);
        Require(Math.Abs(middle.Speed - 4f) < .001f, "Halfway braking velocity");
        var shortStop = new SmoothRideStop(.1f, 8f);
        shortStop.Advance(.5f);
        Require(shortStop.IsComplete && shortStop.RemainingDistance == 0, "Short remaining distance / long frame");
        Console.WriteLine("PASS: smooth finite braking, exact stopping distance and zero final speed at 30/60/144 FPS.");
    }
}
