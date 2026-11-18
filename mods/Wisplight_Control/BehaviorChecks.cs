using System;
using OctoberJemini.WisplightControl;
class BehaviorChecks
{
    static int count;
    static void Check(bool ok, string label) { count++; if (!ok) throw new Exception(label); }
    static void Main()
    {
        for (int quality=1;quality<=4;quality++) Check(WispRules.Radius(quality,15,5)==10+quality*5,"Default range at quality "+quality);
        Check(WispRules.Radius(99,15,5)==30,"Excess quality bounded");
        Check(WispRules.Radius(4,15,0)==15,"Zero configured radius growth");
        Check(WispRules.CharacterGlow(true,1.8f),"Hidden wisp enables attached glow");
        Check(!WispRules.CharacterGlow(false,1.8f),"Visible wisp disables attached glow");
        Check(!WispRules.CharacterGlow(true,0),"Zero intensity disables glow");
        Check(WispRules.IsWispHidden(null),"Initial state hidden");
        Check(!WispRules.IsWispHidden("visible"),"Visible preference reloads");
        Check(WispRules.IsWispHidden("hidden"),"Hidden preference reloads");
        Check(WispRules.Toggle("hidden")=="visible","Eye can show wisp");
        Check(WispRules.Toggle("visible")=="hidden","Eye can hide wisp");
        Check(WispRules.Toggle(WispRules.Toggle("visible"))=="visible","Keyboard and eye share reversible state");
        Console.WriteLine("Passed "+count+" radius, character glow and visibility checks.");
    }
}
