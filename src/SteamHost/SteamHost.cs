// UnderrailSteamHost.exe: a 32-bit helper for a 64-bit UnderRail (experimental-64bit). It loads the game's own
// 32-bit SteamHelper.dll (and through it steam_api.dll) and runs the game's Steam calls for it: one command per line
// on stdin ("SetAchievement NAME"), one result per line on stdout ("1"/"0", a number, or "" for void). It exits
// when stdin closes, i.e. when the game does.
using System;
using System.Runtime.InteropServices;

static class SteamHost
{
    static int Main()
    {
        string line;
        while ((line = Console.In.ReadLine()) != null)
        {
            string[] p = line.Split(new[] { ' ' }, 2);
            string arg = p.Length > 1 ? p[1] : "";
            string reply;
            try
            {
                switch (p[0])
                {
                    case "InitSteam": reply = B(SteamHelper_InitSteam()); break;
                    case "ShutdownSteam": SteamHelper_ShutdownSteam(); reply = ""; break;
                    case "SetAchievement": reply = B(SteamHelper_SetAchievement(arg)); break;
                    case "ClearAchievement": reply = B(SteamHelper_ClearAchievement(arg)); break;
                    case "IncrementStat": reply = SteamHelper_IncrementStat(arg).ToString(); break;
                    case "ResetAllStats": reply = B(SteamHelper_ResetAllStats(arg == "1")); break;
                    case "IsOnSteamDeck": reply = B(SteamHelper_IsOnSteamDeck()); break;
                    case "IsRunningOnWine": reply = B(SteamHelper_IsRunningOnWine()); break;
                    case "ShowKeyboard":
                        string[] n = arg.Split(' ');
                        reply = B(SteamHelper_ShowKeyboard(int.Parse(n[0]), int.Parse(n[1]), int.Parse(n[2]), int.Parse(n[3])));
                        break;
                    default: reply = "?"; break;
                }
            }
            catch (Exception ex) { reply = "error " + ex.GetType().Name; }
            Console.Out.WriteLine(reply);
            Console.Out.Flush();
        }
        return 0;
    }

    static string B(bool v) { return v ? "1" : "0"; }

    [DllImport("SteamHelper.dll", CallingConvention = CallingConvention.Cdecl)] static extern bool SteamHelper_InitSteam();
    [DllImport("SteamHelper.dll", CallingConvention = CallingConvention.Cdecl)] static extern void SteamHelper_ShutdownSteam();
    [DllImport("SteamHelper.dll", CallingConvention = CallingConvention.Cdecl)] static extern bool SteamHelper_SetAchievement(string name);
    [DllImport("SteamHelper.dll", CallingConvention = CallingConvention.Cdecl)] static extern bool SteamHelper_ClearAchievement(string name);
    [DllImport("SteamHelper.dll", CallingConvention = CallingConvention.Cdecl)] static extern int SteamHelper_IncrementStat(string name);
    [DllImport("SteamHelper.dll", CallingConvention = CallingConvention.Cdecl)] static extern bool SteamHelper_ResetAllStats(bool achievements);
    [DllImport("SteamHelper.dll", CallingConvention = CallingConvention.Cdecl)] static extern bool SteamHelper_IsOnSteamDeck();
    [DllImport("SteamHelper.dll", CallingConvention = CallingConvention.Cdecl)] static extern bool SteamHelper_IsRunningOnWine();
    [DllImport("SteamHelper.dll", CallingConvention = CallingConvention.Cdecl)] static extern bool SteamHelper_ShowKeyboard(int a, int b, int c, int d);
}
