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
  float templeRadius=LandmarkPolicy.EnterRadius("MWL_FulingTemple1",8,20);
  Check(templeRadius==20,"Fuling Temple uses the loaded exterior footprint instead of half its spawn clearance");
  Check(LandmarkPolicy.Entered(false,false,false,15*15,templeRadius,2),"Walking fifteen metres from the temple origin marks an outdoor visit");
  Check(!LandmarkPolicy.Entered(false,false,false,21*21,templeRadius,2),"Walking outside the temple footprint does not mark a visit");
  Check(!LandmarkPolicy.Entered(false,false,false,100,templeRadius,25),"An underground or high overhead player does not mark a temple visit");
  Check(LandmarkPolicy.EnterRadius("MWL_FulingTempleBroken1",8,20)==20,"Broken temple variants share the exterior handling");
  Check(LandmarkPolicy.EnterRadius("MWL_FulingTemple5",8,45)==20,"Temple visit range remains bounded");
  Check(LandmarkPolicy.EnterRadius("MWL_FulingTemple1",8,0)==8,"Unloaded temple fallback uses full configured radius");
  Check(LandmarkPolicy.EnterRadius("GoblinCamp2",30,45)==15,"Other outdoor landmarks retain their visit radius");
  Check(!LandmarkPolicy.Entered(true,false,false,0,20,0),"True dungeons still require interior entry");
  Check(LandmarkPolicy.Entered(true,true,true,0,20,0),"Correct dungeon interior still marks a visit");
  Console.WriteLine("Passed 19 recognition, saved-pin and temple visit regression checks.");
 }
}
