using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InARowCondition : Condition
{

    Direction checkDirection;
    public int targetLength;

    public InARowCondition(Direction d, int length){
        targetLength = length;
        checkDirection = d;
    }

    //? Does any valid line of the valid length exist, for any player
    override public bool Check(Game g, Player p){
        return g.FindLines(checkDirection, targetLength, p, true).Count > 0;
    }

    override public string ToCode(){
        return "MATCH "+checkDirection.ToString()+" "+targetLength;
    }

    public override string Print(){
        return "if they make a line of "+targetLength+" pieces.";
    }    

}
