using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGeneration : MonoBehaviour
{
    
    [Header("Board Settings")]
    public int minBoardDimension = 3;
    public int maxBoardDimension = 6;
    public bool forceSquareBoard = false;

    [Header("Update Rule Settings")]
    public int minUpdateEffects = 1;
    public int maxUpdateEffects = 2;
    public Heading[] allowedFallDirections;
    public TriggeredEffect[] allowedTriggeredEffects;
    
    [Header("Win/Loss Settings")]
    public bool includeLossCondition = false;
    public Direction[] allowedLineDirections;
    public int minLineLength = 3;
    public int maxLineLength = 4;

    //Rather than min/max numbers that we randomise between, we might just want to offer fixed values.
    //I chose these as they seem like chunky milestones that would be interesting to try. This is
    //also an example of data which depends on other data (i.e. board size) which we don't validate here.
    public int[] pieceCountTargets = new int[]{5, 10, 15};


    /*
        Random game generation is one of the places that your choice of system design really comes up.
        If your setup is based more on a design language, enums, rule chunks, then it can be as simple
        as shuffling cards - you list the rule components you want to be legal, and you just uniformly
        pick from them.

        In the setup I've designed here, where rules are built as class objects, it's a bit clumsier.
        I did it this way here so the code was easier and more modular, simpler to read and parse.
        It has some other small advantages (for example, we can easily specify the exact probability that
        a particular rule should appear in a game). But it's not my personal favourite way to do it.
    */
    public Game GenerateRandomGame(){
        int w = Random.Range(minBoardDimension, maxBoardDimension+1);
        int h = Random.Range(minBoardDimension, maxBoardDimension+1);
        if(forceSquareBoard){
            h = w;
        }
        Game g = new Game(w, h);

        //! Win Condition
        //We only have two win condition types: in-a-row, or piece count, so here we toss a coin
        //to include them equally. You could parameterise this if you wanted, to tip the balance.
        //Or you could balance it to reflect the actual distribution of rule types (i.e. there
        //are more ways to make an in-a-row condition than a piece-count one).
        if(Random.Range(0f, 1f) < 0.5f){
            g.winCondition = 
                new InARowCondition(
                    allowedLineDirections[Random.Range(0, allowedLineDirections.Length)],
                    Random.Range(minLineLength, maxLineLength+1));
        }
        else{
            g.winCondition = 
                new PieceCountCondition(
                    pieceCountTargets[Random.Range(0, pieceCountTargets.Length)]
                );
        }

        //! Lose Condition
        //You might want to make this a dice roll, like 50% of games have a loss condition.
        if(includeLossCondition){
            if(Random.Range(0f, 1f) < 0.5f){
                g.loseCondition = 
                    new InARowCondition(
                        allowedLineDirections[Random.Range(0, allowedLineDirections.Length)],
                        Random.Range(minLineLength, maxLineLength+1));
            }
            else{
                g.loseCondition = 
                    new PieceCountCondition(
                        pieceCountTargets[Random.Range(0, pieceCountTargets.Length)]
                    );
            }
        }

        //! Update Effects
        int numberOfUpdateEffects = Random.Range(minUpdateEffects, maxUpdateEffects);

        //Lots more options here! We could stop duplicate effects in the same game, for example.
        //As usual, we're keeping it simple here and not worrying, but try tweaking it!

        for(int i=0; i<numberOfUpdateEffects; i++){
            //3 effect types, so let's toss a, uh, 3-sided coin
            switch(Random.Range(0, 3)){
                case 0:
                    //Settle the board/fall pieces in a certain direction
                    g.updatePhase.Add(new FallPiecesEffect(allowedFallDirections[Random.Range(0, allowedFallDirections.Length)]));
                    break;
                case 1:
                    //End-to-end piece capturing in the style of Reversi
                    g.updatePhase.Add(new CappedEffect(allowedTriggeredEffects[Random.Range(0, allowedTriggeredEffects.Length)]));
                    break;
                case 2:
                    //X-in-a-row logic
                    //Note I reuse the valid triggered effects, and the valid line lengths. You could imagine
                    //having custom limits here, or specifying it some other way.
                    g.updatePhase.Add(new InARowEffect(
                        allowedTriggeredEffects[Random.Range(0, allowedTriggeredEffects.Length)],
                        Random.Range(minLineLength, maxLineLength)));
                    break;
            }
        }

        return g;
    }


    public static GameGeneration instance;
    void Awake(){
        GameGeneration.instance = this;
    }

}
