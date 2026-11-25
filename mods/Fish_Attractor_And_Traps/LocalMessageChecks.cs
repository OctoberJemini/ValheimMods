using System;
using System.IO;
using OctoberJemini.FishAttractor;
class LocalMessageChecks {
 static void Main(){
  var raw=new ZPackage();raw.Write(123L);bool reproduced=false;
  try{raw.ReadLong();}catch(EndOfStreamException){reproduced=true;}
  if(!reproduced)throw new Exception("Expected old direct-read failure was not reproduced");
  for(int route=0;route<3;route++){
   var p=new ZPackage();p.Write(456L);p.Write("host route "+route);p.Write(78L);p.Write(true);p.Write(25f);p.Write(1234.5);
   var read=LocalMessage.ReadFromStart(p);
   if(read.ReadLong()!=456L||read.ReadString()!="host route "+route||read.ReadLong()!=78L||!read.ReadBool()||read.ReadSingle()!=25f||read.ReadDouble()!=1234.5)throw new Exception("Local message round trip failed");
  }
  System.Console.WriteLine("PASS: reproduced original end-of-stream error using game ZPackage; three local message round trips pass after rewind.");
 }
}
