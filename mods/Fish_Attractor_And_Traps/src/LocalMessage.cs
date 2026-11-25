namespace OctoberJemini.FishAttractor {
 internal static class LocalMessage {
  // Network delivery creates a readable package; direct host calls reuse a just-written
  // stream, whose cursor is at the END. Rewind all three local message routes explicitly.
  internal static ZPackage ReadFromStart(ZPackage package){package.SetPos(0);return package;}
 }
}
