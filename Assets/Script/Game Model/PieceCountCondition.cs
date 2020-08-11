using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceCountCondition : Condition
{
    public int countTarget;

    /*
        If you wanted to expand this, you'd probably add a little enum for LESS THAN, GREATER THAN, etc.
        I didn't add it here as I wanted to keep this fairly simple, but it's an easy add-on.
    */
    public PieceCountCondition(int count){
        countTarget = count;
    }

    public override bool Check(Game g, Player playerType){
        int code = g.state.GetPlayerValue(playerType);
        int count = 0;
        for(int i=0; i<g.boardWidth; i++){
            for(int j=0; j<g.boardHeight; j++){
                if(g.state.Value(i, j) == code)
                    count++;
            }
        }
        return count == countTarget;
    }

    override public string ToCode(){
        return "COUNT "+countTarget;
    }

    public override string Print(){
        return "if they have "+countTarget+" pieces on the board.";
    }    

}
