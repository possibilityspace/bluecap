using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

    /// <summary>
    /// Take a turn, but use no more than the allotted time limit.
    /// </summary>
    /// <param name="g">The game to play</param>
    /// <param name="timeLimit">Time limit in seconds</param>
    /// <returns>Returns whether we managed to take a turn or not.</returns>
    public override bool TakeTurn(Game g, float timeLimit = 1f){
        //NOTE: use timeLimit in addition to cutoff, if TapAction takes a lot of time for some reason.
        var timer = Stopwatch.StartNew();
        var timeLimitInMillis = timeLimit * 1000f;
        
        for(int i=0; i<cutoff; i++)
        {
            //Check if the time is up!
            if (timer.ElapsedMilliseconds > timeLimitInMillis) break;
            
            if(g.TapAction(Random.Range(0, g.boardWidth), Random.Range(0, g.boardHeight))){
                return true;
            }
        }

        // Debug.LogError("Couldn't take a turn after "+cutoff+" tries.");
        return false;
    }

}
