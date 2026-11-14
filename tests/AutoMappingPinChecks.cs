using System;
using OctoberJemini.QuietMap;
class AutoMappingPinChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
  Check(LandmarkPolicy.OutdoorName("MountainCave02")=="Frost Cave","Vanilla frost cave recognized");
  Check(!LandmarkPolicy.CanReuseSavedLocationPin(true,true,false,0,"Moder","Frost Cave","MountainCave02"),"Moder pin must not stand in for a cave");
  Check(!LandmarkPolicy.CanReuseSavedLocationPin(true,false,false,1,"Moder","Frost Cave","MountainCave02"),"Manually named Moder marker must not stand in for a cave");
  Check(!LandmarkPolicy.CanReuseSavedLocationPin(true,false,false,1,"Silver","Frost Cave","MountainCave02"),"Unrelated resource marker must not stand in for a cave");
  Check(LandmarkPolicy.CanReuseSavedLocationPin(true,false,false,1,"Frost Cave","Frost Cave","MountainCave02"),"Existing matching cave pin must be reused");
  Check(LandmarkPolicy.CanReuseSavedLocationPin(true,false,false,1,"MountainCave02","Frost Cave","MountainCave02"),"Existing prefab-name pin must be reused");
  Check(LandmarkPolicy.CanReuseSavedLocationPin(true,false,false,1,"Burial Chamber","Burial Chambers","Crypt2"),"Legacy burial chamber names preserved");
  Check(!LandmarkPolicy.CanReuseSavedLocationPin(true,false,true,1,"Frost Cave","Frost Cave","MountainCave02"),"Portal pins remain separate");
  Check(!LandmarkPolicy.CanReuseSavedLocationPin(true,false,false,225,"Frost Cave","Frost Cave","MountainCave02"),"Different distant cave remains separate");
  Console.WriteLine("Passed 9 cave recognition and saved-pin regression checks.");
 }
}
