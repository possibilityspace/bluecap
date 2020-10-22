using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GameEvaluation : MonoBehaviour
{
    [Header("Allotted turn taking time in seconds", order = 1)]
    [Space(-10, order = 2)]
    [Header("Longer time leads to better evaluations for Greedy and MCTS", order = 3)]
    public float TimeAllottedPerTurn = 1f;
    
    void Start(){
        return;
        Debug.Log("---");
        Game g = new Game(4, 4);
        g.winCondition = new InARowCondition(Direction.LINE, 3);
        g.loseCondition = new InARowCondition(Direction.LINE, 2);
        MCTSAgent p = new MCTSAgent(1);
        MCTSAgent q = new MCTSAgent(2);

        g.PrintBoard();
        for(int i=0; i<5; i++){
            p.TakeTurn(g);
            g.PrintBoard();
            if(g.endStatus > 0){
                break;
            }
            q.TakeTurn(g);
            g.PrintBoard();
            if(g.endStatus > 0){
                break;
            }
        }
        Debug.Log(g.endStatus);
        Debug.Log("---");
    }

    public void PlayRandomGreedy(Game game){
        RandomAgent player1 = new RandomAgent(1);
        GreedyAgent player2 = new GreedyAgent(2);
        PlayGame(game, player1, player2);
    }

    public void PlaySkilledMirrorMatch(Game game){
        MCTSAgent player1 = new MCTSAgent(1);
        MCTSAgent player2 = new MCTSAgent(2);
        PlayGame(game, player1, player2);
    }

    public IEnumerator PlayGame(Game game, BaseAgent player1, BaseAgent player2, int turnLimit = 100){
        int turn = 0;
        //? Always reset the game before playing.
        game.ResetState();
        
        var time = Stopwatch.StartNew();
        
        // game.PrintBoard();
        while(turn < turnLimit){
            if(!player1.TakeTurn(game, TimeAllottedPerTurn))
                break;
            if(game.endStatus > 0)
                break;
            // game.PrintBoard();
            turn++;
            
            //Take a break when we reach 16ms (approx. 1 frame)
            if (time.ElapsedMilliseconds > 16)
            {
                time.Restart();
                yield return 0;
            }
            
            if(!player2.TakeTurn(game, TimeAllottedPerTurn))
                break;
            if(game.endStatus > 0)
                break;
            // game.PrintBoard();
            turn++;
            
            //Take a break when we reach 16ms (approx. 1 frame)
            if (time.ElapsedMilliseconds > 16)
            {
                time.Restart();
                yield return 0;
            }
        }

        // game.PrintBoard();
        
        if(turn >= turnLimit){
            // Debug.Log("Game tied: turn limit exceeded.");
        }
        else{
            switch(game.endStatus){
                case 1:
                    // Debug.Log("Player 1 wins (in "+turn+" turns)");
                    break;
                case 2:
                    // Debug.Log("Player 2 wins (in "+turn+" turns)");
                    break;
                case 3:
                    // Debug.Log("Game tied (in "+turn+" turns)");
                    break;
            }
        }
        
        yield return 0;
    }

    public static GameEvaluation instance;
    void Awake(){
        GameEvaluation.instance = this;
    }

}
