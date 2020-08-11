using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAgent : BaseAgent
{
    
    /*
    *  Predictably, the random agent just picks a random tile to tap, every time.
    *
    *  Random agents are useful for certain things, especially testing for very abnormal games.
    *  In a real-time game you might have an agent that does nothing, also, but we can't pass a 
    *  turn here so that's not an option. The random agent is the closest we get.
    * 
    */

    public RandomAgent(int playerCode){
        this.playerCode = playerCode;
    }

    //It's possible to make games that are broken - e.g. you can never win, so eventually
    //the board fills up. We could just test this manually (by looking at the board first)
    //but instead we just randomly try to take a turn 1000 times and if that doesn't work,
    //we give up.
    int cutoff = 1000;

    public override bool TakeTurn(Game g){
        for(int i=0; i<cutoff; i++){
            if(g.TapAction(Random.Range(0, g.boardWidth), Random.Range(0, g.boardHeight))){
                return true;
            }
        }

        // Debug.LogError("Couldn't take a turn after "+cutoff+" tries.");
        return false;
    }

}
