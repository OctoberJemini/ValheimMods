using System;using System.Linq;using System.Collections.Generic;using OctoberJemini.AndvaranautEnchanting;
class PolicyChecks{
 static int n;static void Check(bool x){n++;if(!x)throw new Exception("Check "+n+" failed");}
 static void Main(){
  Check(MarkerPolicy.MarkerIndices(new string[0]).Length==0);
  Check(MarkerPolicy.MarkerIndices(new[]{"Wishbone","ModifyDamage"}).Length==0);
  Check(MarkerPolicy.MarkerIndices(new[]{"Andvaranaut"}).SequenceEqual(new[]{0}));
  Check(MarkerPolicy.MarkerIndices(new[]{"Andvaranaut","ModifyDamage","Andvaranaut","Luck"}).SequenceEqual(new[]{2,0}));
  var list=new List<string>{"Andvaranaut","Luck","Indestructible","Andvaranaut","ModifyDamage"};foreach(int i in MarkerPolicy.MarkerIndices(list))list.RemoveAt(i);
  Check(list.SequenceEqual(new[]{"Luck","Indestructible","ModifyDamage"}));Check(MarkerPolicy.MarkerIndices(list).Length==0);
  Check(MarkerPolicy.EmptyOriginal(0,0,0,null,null,false,null));
  Check(!MarkerPolicy.EmptyOriginal(1,0,0,null,null,false,null));
  Check(!MarkerPolicy.EmptyOriginal(0,1,0,null,null,false,null));
  Check(!MarkerPolicy.EmptyOriginal(0,0,1,null,null,false,null));
  Check(!MarkerPolicy.EmptyOriginal(0,0,0,"legend",null,false,null));
  Check(!MarkerPolicy.EmptyOriginal(0,0,0,null,"set",false,null));
  Check(!MarkerPolicy.EmptyOriginal(0,0,0,null,null,true,null));
  Check(!MarkerPolicy.EmptyOriginal(0,0,0,null,null,false,"custom name"));
  Console.WriteLine("PASS: "+n+" marker migration policy checks; native game integration is not exercised.");
 }
}
