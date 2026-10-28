using System;
using System.IO;
using SecretsThatBreathe;

public static class PulseChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static int Main()
    {
        int doubleLeft = 0, doubleRight = 0;
        foreach (PoliceSirenLights.FlashPattern pattern in Enum.GetValues(typeof(PoliceSirenLights.FlashPattern)))
        {
            for (int i = 0; i < 1000; i++)
            {
                float phase = (i + .25f) / 1000;
                float l = PoliceSirenLights.EvaluatePulse(pattern, phase, false);
                float r = PoliceSirenLights.EvaluatePulse(pattern, phase, true);
                Check(l >= 0 && l <= 1 && r >= 0 && r <= 1, "Pulse outside [0,1]");
                Check(!(l > .001f && r > .001f), "Both banks peak at once");
                Check(Math.Abs(l - PoliceSirenLights.EvaluatePulse(pattern, phase + 1, false)) < .00001f, "Non-periodic pulse");
                if (pattern == PoliceSirenLights.FlashPattern.AlternatingDoubleFlash)
                { if (l > .5f) doubleLeft++; if (r > .5f) doubleRight++; }
            }
        }
        var rotating = PoliceSirenLights.FlashPattern.RotatingBeacon;
        Check(PoliceSirenLights.EvaluatePulse(rotating, 0, false) > .99f, "Left peak missing");
        Check(PoliceSirenLights.EvaluatePulse(rotating, .5f, true) > .99f, "Right peak missing");
        Check(doubleLeft == 210 && doubleRight == 210, "Double-flash duty cycle incorrect");
        string report = "{\"actual_compiled_controller_timing_tested\":true,\"samples\":2000,\"two_patterns_periodic_bounded_alternating\":true,\"double_flash_samples_per_bank\":210}";
        File.WriteAllText("ArtSource/IsuzuSiren/CompileCheck/pulse-checks.json", report);
        Console.WriteLine(report);
        return 0;
    }
}
