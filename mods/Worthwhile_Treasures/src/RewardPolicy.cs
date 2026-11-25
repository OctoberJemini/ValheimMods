using System;
namespace OctoberJemini.WorthwhileTreasures {
 public static class RewardPolicy {
  public static int Coins(int tier,bool jackpot,int roll) {
   if(tier<1||tier>7||roll<0||roll>200)throw new ArgumentOutOfRangeException();
   int[] low={0,25,50,75,100,100,150,200};
   int[] high={0,75,100,150,200,300,350,400};
   int[] jackpots={0,250,400,600,800,1000,1250,1500};
   return jackpot?jackpots[tier]:low[tier]+roll*(high[tier]-low[tier])/200;
  }
 }
}
