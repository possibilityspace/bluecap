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
        string exp = "If they have at least "+targetLength+" pieces in a sequence ";
        switch(checkDirection){
            case Direction.LINE:
                exp += "(in any direction)";
                break;
            case Direction.ROW:
                exp += "(in a horizontal row only)";
                break;
            case Direction.COL:
                exp += "(in a vertical column only)";
                break;
            case Direction.CARDINAL:
                exp += "(horizontal or vertical lines only)";
                break;
        }

        return exp;
    }    

}
