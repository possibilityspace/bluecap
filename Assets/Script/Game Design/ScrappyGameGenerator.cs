using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class ScrappyGameGenerator : MonoBehaviour
{
    
    /*
    *  Now we can put it all together.
    *
    *  This is the most basic, silly kind of game generator possible. 
    *  It randomly generates games, tests them, and at the end gives you the best one.
    *  It's included here as an example of how to use agents to rank games. Ideally we'd
    *  probably opt for a more 'intelligent' design process, perhaps an evolutionary one
    *  that uses agent ranking as fitness. If I get time I'll add this to the codebase,
    *  but as I'm putting this together I wanted to include a basic example just to be safe.
    * 
    */

    [Header("Pool Size")]
    public int numberOfGamesToTest = 10;

    [Header("Testing Composition")]
    public int randomRandomMatches = 20;
    public int greedyRandomMatches = 10;
    public int greedySkilledMatches = 10;
    public int skilledMirrorMatches = 10;

    [Header("Scene Interface")]
    public bool interfaceEnabled = false;
    public TMPro.TextMeshProUGUI bestRulesText;
    public TMPro.TextMeshProUGUI bestScoresText;
    public TMPro.TextMeshProUGUI bestOverallScoreText;
    public TMPro.TextMeshProUGUI currentRulesText;
    public TMPro.TextMeshProUGUI currentScoresText;
    public TMPro.TextMeshProUGUI progressBarText;
    public TMPro.TextMeshProUGUI timeRemainingText;
    public UnityEngine.UI.Image progressBar;

    [Header("Best Rules")]
    [TextArea]
    public string bestRulesCode;
    public bool gameTestingFinished;
    
    [Header("Evaluation Time")]
    public float totalTimeSpent;
    public float estimatedTotalTime;

    public float estimatedTimeLeft;

    string goodScoreColor = "<#33aa33>";
    string averageScoreColor = "<#E57517>";
    string badScoreColor = "<#CF1200>";
    string regularText = "<#3D2607>";

    void Start(){
        StartCoroutine(RandomlyTestGames());
        StartCoroutine(EstimatedTimeUpdater());
    }


   
    
    public IEnumerator EstimatedTimeUpdater()
    {
        var timer = Stopwatch.StartNew();
        while (!gameTestingFinished)
        {
            var t = TimeSpan.FromSeconds(estimatedTimeLeft);

            timeRemainingText.text = "Estimated time remaining: "+string.Format("{0:D2}h:{1:D2}m:{2:D2}s", 
                t.Hours, 
                t.Minutes, 
                t.Seconds);

            yield return 0;

            //Remove the elapsed time since we last updated from estimatedTimeLeft.
            estimatedTimeLeft -= timer.ElapsedMilliseconds / 1000f;
            timer.Restart();
        }
        
        yield return 0;
    }
    
    public IEnumerator RandomlyTestGames(){
        //Overestimate initial evaluation time as: (100 turns per game) * (max time per turn) * (number of games to test)
        estimatedTimeLeft = 100 * GameEvaluation.instance.TimeAllottedPerTurn * numberOfGamesToTest;
        
        float bestScore = float.MinValue;
        Game bestGame = null;

        var timer = Stopwatch.StartNew();
        
        for(int g=0; g<numberOfGamesToTest; g++){
            if(interfaceEnabled){
                progressBarText.text = "Evaluating Game "+g+"/"+numberOfGamesToTest;
                progressBar.fillAmount = (float)g/numberOfGamesToTest;
            }

            yield return 0;

            Game game = GameGeneration.instance.GenerateRandomGame();
            
            //NOTE MJ: Don't think you need to StartCoroutine: "yield return ScoreGame(game);" should do.
            yield return ScoreGame(game);

            //NOTE: use StopWatch instead of Unity Time, because that is Time since frame start, not actual time.
            float timeTaken = timer.ElapsedMilliseconds / 1000f;
            timer.Restart();

            //This is a pretty bad estimate, because some games are a lot harder to evaluate than others.
            //In particular, in search-based approaches (like computational evolution) will find better
            //games as the search goes on, which means your system will get slower as the generation goes 
            //on. That doesn't happen here because our approach is completely random.
            totalTimeSpent += timeTaken;
            var averageTimeTaken = totalTimeSpent/(g+1);
            estimatedTotalTime = averageTimeTaken * numberOfGamesToTest;
            estimatedTimeLeft = estimatedTotalTime - totalTimeSpent;
            
            var t = TimeSpan.FromSeconds(estimatedTimeLeft);

            timeRemainingText.text = "Estimated time remaining: "+string.Format("{0:D2}h:{1:D2}m:{2:D2}s", 
                t.Hours, 
                t.Minutes, 
                t.Seconds);

            if(game.evaluatedScore > bestScore){
                bestGame = game;
                bestScore = game.evaluatedScore;
                bestRulesCode = game.GameToCode();
                
                if(interfaceEnabled){
                    bestScoresText.text = 
                    "First Play Bias: "+ToScore(playerBiasScore)+"\n"+
                    "Simple Beats Random: "+ToScore(greedIsGoodScore)+"\n"+
                    "Clever Beats Simple: "+ToScore(skillIsBetterScore)+"\n"+
                    "Avoid Draws: "+ToScore(drawsAreBadScore)+"\n"+
                    "High Skill Mirror Matchup: "+ToScore(highSkillBalanceScore)+"\n"
                    ;

                    bestOverallScoreText.text = "Overall evaluation score: "+ToScore(bestScore);

                    bestRulesText.text = game.GameToString();
                }
            }
        }

        if(interfaceEnabled){
            progressBar.fillAmount = 1f;
            progressBarText.text = "Generation Process Complete!";
        }

        gameTestingFinished = true;
        // Debug.Log("Best game score: "+bestScore);
        Debug.Log("Finished evaluating games, best game rules found:\n" + 
                  bestGame.GameToString()+ 
                  "\n"+
                  "Copy this Code into the Play scene to test it yourself: \n"+ 
                  bestGame.GameToCode());
    }

    public string ToScore(float val){
        string sc = "";
        if(val < 0.25f)
            sc += badScoreColor;
        else if(val < 0.5f)
            sc += averageScoreColor;
        if(val > 0.75f)
            sc += goodScoreColor;
        sc += val.ToString("0%");
        sc += regularText;
        return sc;
    }

    //? We save the last scores in each category so we can use them, if necessary, in the interface.
    float playerBiasScore = 0;
    float greedIsGoodScore = 0;
    float skillIsBetterScore = 0;
    float drawsAreBadScore = 0;
    float highSkillBalanceScore = 0;

    //Used for capturing footage for the tutorial
    IEnumerator FakeScoreGame(Game game){
        currentRulesText.text = game.GameToString();
        string fill = "";
        for(int i=0; i<10; i++){
            currentScoresText.text = fill+
            "First Play Bias: Playing "+i+"/10";
            yield return new WaitForSeconds(Random.Range(0.25f, 0.5f));
        }
        fill = "First Play Bias: "+ToScore(Random.Range(0f, 1f))+"\n";
        for(int i=0; i<10; i++){
            currentScoresText.text = fill+
            "Simple Beats Random: Playing "+i+"/10";
            yield return new WaitForSeconds(Random.Range(0.25f, 0.5f));
        }
        fill += "Simple Beats Random: "+ToScore(Random.Range(0f, 1f))+"\n";
        for(int i=0; i<10; i++){
            currentScoresText.text = fill+
            "Clever Beats Simple: Playing "+i+"/10";
            yield return new WaitForSeconds(Random.Range(0.25f, 0.5f));
        }
        fill += "Clever Beats Simple: "+ToScore(Random.Range(0f, 1f))+"\n";
        for(int i=0; i<10; i++){
            currentScoresText.text = fill+
            "Avoid Draws: Playing "+i+"/10";
            yield return new WaitForSeconds(Random.Range(0.25f, 0.5f));
        }
        fill += "Avoid Draws: "+ToScore(Random.Range(0f, 1f))+"\n";
        for(int i=0; i<10; i++){
            currentScoresText.text = fill+
            "High Skill Mirror Matchup: Playing "+i+"/10";
            yield return new WaitForSeconds(Random.Range(0.25f, 0.5f));
        }
        fill += "High Skill Mirror Matchup: "+ToScore(Random.Range(0f, 1f))+"\n";
        bestOverallScoreText.text = fill;
        currentScoresText.text = "";
        
        // "Simple Beats Random: Playing ("+i+"/"+greedyRandomMatches+")\n"+
        // "Clever Beats Simple: Playing ("+i+"/"+greedySkilledMatches+")\n"+
        // "Avoid Draws: Playing ("+i+"/"+skilledMirrorMatches+")\n"+
        // "High Skill Mirror Matchup: Playing ("+i+"/"+skilledMirrorMatches+")\n";;
        // yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
    }


    //? To score a game we set up three kinds of matchup, condense each one into a score, 
    //? add the scores together and then normalise them between 0 and 1.
    public IEnumerator ScoreGame(Game game){
        //? Reset
        playerBiasScore = 0f;
        greedIsGoodScore = 0f;
        skillIsBetterScore = 0f;
        drawsAreBadScore = 0f;
        highSkillBalanceScore = 0f;
        string scoresSoFar = "";

        if(interfaceEnabled){
            currentRulesText.text = game.GameToString();
            scoresSoFar = "First Play Bias: "+ToScore(playerBiasScore)+"\n";
            currentScoresText.text = scoresSoFar;
            yield return 0;
        }

        //? Random vs. Random: These games can go either way, the only thing we're interested
        //? is if there's a clear bias towards playing first or second. This is a good indicator.
        //? Score is therefore proportional to the amount one agent won over the other.
        RandomAgent randomAgent1 = new RandomAgent(1);
        RandomAgent randomAgent2 = new RandomAgent(2);
        int firstWon = 0; int secondWon = 0; 
        for(int i=0; i<randomRandomMatches; i++){
            if(interfaceEnabled)
                currentScoresText.text = "First Play Bias: Playing ("+i+"/"+randomRandomMatches+")\n";

            //NOTE MJ: Playing the games could be coroutines, so they don't block UI.
            //res is redundant game.endStatus already has info.
            yield return GameEvaluation.instance.PlayGame(game, randomAgent1, randomAgent2);
            if(game.endStatus == 1) firstWon++;
            if(game.endStatus == 2) secondWon++;
            //? Yield after each playout - we could yield more frequently, this is OK though.
            yield return 0;
        }

        playerBiasScore = 1-(Mathf.Abs(firstWon-secondWon) / randomRandomMatches);

        if(interfaceEnabled){
            scoresSoFar = "First Play Bias: "+ToScore(playerBiasScore)+"\n";
            currentScoresText.text = scoresSoFar;
            yield return 0;
        }

        //? We could also add in a measure of 'decisiveness' - i.e. games shouldn't end in draws.
        //? However for random agents this might happen just because they aren't very good.

        //? Random vs. Greedy: Greedy may not always win the game, but we expect it to
        //? win more than random. Score is proportion to the number of games greedy won or tied.
        int randomAgentWon = 0;
        for(int i=0; i<greedyRandomMatches; i++){
            if (interfaceEnabled)
            {
                currentScoresText.text = scoresSoFar+"Simple Beats Random: Playing ("+i+"/"+greedyRandomMatches+")\n";
                yield return 0;
            }

            //? Small detail: note that we swap who plays first, to compensate
            //? for first-player advantage
            RandomAgent randomAgent = new RandomAgent(1+(i%2));
            GreedyAgent greedyAgent = new GreedyAgent(2-(i%2));
            
            //NOTE MJ: Playing the games could be coroutines, so they don't block UI. res could be an out parameter.
            yield return GameEvaluation.instance.PlayGame(game, randomAgent, greedyAgent);
            if(game.endStatus == 1+(i%2)){
                randomAgentWon++;
            }
            yield return 0;
        }

        greedIsGoodScore = 1 - ((float)randomAgentWon/greedyRandomMatches);

        if(interfaceEnabled){
            scoresSoFar += "Simple Beats Random: "+ToScore(greedIsGoodScore)+"\n";
            currentScoresText.text = scoresSoFar;
            yield return 0;
        }

        //? Greedy vs. MCTS: We know that greedy players will avoid causing their own loss, and
        //? win if given the opportunity, but MCTS players can look ahead and plan. As a result,
        //? a more strategic game should be won by MCTS agents. Score is proportion of games MCTS
        //? agent won. Note that we might need to give the MCTS agent more computational resources
        //? for some games to ensure it is performing better.
        int mctsAgentWon = 0;

        for(int i=0; i<greedySkilledMatches; i++){
            if (interfaceEnabled)
            {
                currentScoresText.text = scoresSoFar+"Clever Beats Simple: Playing ("+i+"/"+greedySkilledMatches+")\n";
                yield return 0;
            }

            MCTSAgent skilledAgent = new MCTSAgent(1+(i%2));
            GreedyAgent greedyAgent = new GreedyAgent(2-(i%2));
            
            //NOTE MJ: Playing the games could be coroutines, so they don't block UI. res could be an out parameter.
            yield return GameEvaluation.instance.PlayGame(game, skilledAgent, greedyAgent);
            if(game.endStatus == 1+(i%2)){
                mctsAgentWon++;
            }
            yield return 0;
        }

        skillIsBetterScore = (float)mctsAgentWon/greedySkilledMatches;

        if(interfaceEnabled){
            scoresSoFar += "Clever Beats Simple: "+ToScore(skillIsBetterScore)+"\n";
            currentScoresText.text = scoresSoFar;
            yield return 0;
        }

        //? Finally, MCTS vs MCTS. If we wanted more depth, we could do two version of this, 
        //? one with MCTS agents that are given different amounts of computation, to really 
        //? test to see if more thinking time = better play. However, here we're just going to
        //? test a good old fashioned mirror matchup. For two good equal players, we want
        //? a) not too much imbalance in favour of either player and b) not too many draws.
        int drawnGames = 0;
        int firstPlayerWon = 0; int secondPlayerWon = 0;
        MCTSAgent skilledAgent1 = new MCTSAgent(1);
        MCTSAgent skilledAgent2 = new MCTSAgent(2);
        for(int i=0; i<skilledMirrorMatches; i++){
            if(interfaceEnabled)
            {
                currentScoresText.text = scoresSoFar+
                                       "Avoid Draws: Playing ("+i+"/"+skilledMirrorMatches+")\n"+
                                       "High Skill Mirror Matchup: Playing ("+i+"/"+skilledMirrorMatches+")\n";
                yield return 0;
            }

            //NOTE MJ: Playing the games could be coroutines, so they don't block UI. res could be an out parameter.
            yield return GameEvaluation.instance.PlayGame(game, skilledAgent1, skilledAgent2);
            if(game.endStatus == 1) firstPlayerWon++;
            if(game.endStatus == 2) secondPlayerWon++;
            if(game.endStatus == 3 || game.endStatus == 0) drawnGames++; 
            yield return 0;
        }

        drawsAreBadScore = 1-((float)drawnGames/skilledMirrorMatches);
        highSkillBalanceScore = Mathf.Abs(firstPlayerWon-secondPlayerWon) / skilledMirrorMatches;

        if(interfaceEnabled){
            currentScoresText.text = scoresSoFar + "Avoid Draws: "+ToScore(drawsAreBadScore)+"\n"+
            "High Skill Mirror Matchup: "+ToScore(highSkillBalanceScore)+"\n";
            yield return 0;
        }

        //? Now we can add up the scores and return them. If we wanted we could balance them so
        //? some scores are more important than others, or we could partition them into "must-haves"
        //? and "nice-to-haves". I discuss this in the tutorial video.

        // Debug.Log("Random vs. Random: "+playerBiasScore);
        // Debug.Log("Greedy vs. Random: "+greedIsGoodScore);
        // Debug.Log("MCTS vs. Greedy: "+skillIsBetterScore);
        // Debug.Log("MCTS vs. MCTS (draws): "+drawsAreBadScore);
        // Debug.Log("MCTS vs. MCTS (win balance): "+highSkillBalanceScore);

        game.evaluatedScore = (playerBiasScore + greedIsGoodScore + skillIsBetterScore + drawsAreBadScore + highSkillBalanceScore)/5f;
    }

}
