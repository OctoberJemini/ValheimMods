using System.Collections.Generic;
namespace OctoberJemini.ArmorInscriptions {
 internal static class ExtensionRules {
  internal static bool Allows(int tier,bool rack,bool tools){return tier>=1&&tier<=7&&(tier<=3||(rack&&(tier<=5||tools)));}
 }
}
