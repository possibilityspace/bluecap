using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Thinking about it now, this should be called InASequence or something. Ah, regrets.
public class InARowEffect : Effect
{
    TriggeredEffect onTriggerEffect;
    Direction checkDirection;
    int rowLength;

    //Note for this one we just always use checkDirection.LINE here, you can extend it if you like!
    public InARowEffect(TriggeredEffect e, int n){
        onTriggerEffect = e;
        rowLength = n;
        checkDirection = Direction.LINE;
    }

    override public string ToCode(){
        return "MATCH "+checkDirection.ToString()+" "+rowLength+" "+onTriggerEffect.ToString();
    }

    public override void Apply(Game g){
        List<Point> ps = g.FindLines(checkDirection, rowLength, Player.CURRENT, false, onTriggerEffect == TriggeredEffect.CASCADE);

        //? This is pretty inefficient! It would've been nicer to have a third option, Player.ANY.
        //? If I was building this as a big system I'd definitely refactor it, but I'm going to do 
        //? this tiny hack here in the name of saving time, and keeping the code elsewhere simple.
        if (onTriggerEffect != TriggeredEffect.CASCADE)
        {
            ps.AddRange(g.FindLines(checkDirection, rowLength, Player.OPPONENT));    
        }

        //Now apply the effect to any of the matched pieces
        foreach(Point p in ps){
            if(onTriggerEffect == TriggeredEffect.DELETE){
                g.DeletePiece(p.x, p.y);
            }
            else if(onTriggerEffect == TriggeredEffect.FLIP || onTriggerEffect == TriggeredEffect.CASCADE){
                g.FlipPiece(p.x, p.y);
            }
        }
    }

    public override string Print(){
        string exp = "If there are at least "+rowLength+" pieces of the same type in a sequence ";
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

        exp += ", then ";

        switch(onTriggerEffect){
            case TriggeredEffect.DELETE:
                exp += "the pieces are removed from play.";
                break;
            case TriggeredEffect.FLIP:
                exp += "the pieces flip to the other player's colour.";
                break;
            case TriggeredEffect.CASCADE:
                exp += "pieces connected to the latest move flip to the other player's colour.";
                break;
            
        }
        return exp;
    }

}
