using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MCTSAgent : BaseAgent
{
    /*
    *   When I was just starting my PhD in 2010, MCTS was *the* exciting thing in game AI.
    *   It's kind of hard to imagine now, in 2020, what game AI research was like in a 
    *   pre-AlphaGo, pre-GPT-3, pre-boom world, but thinking back it all seems kind of 
    *   quaint now. MCTS is still popular and you see a lot of papers about it now, too
    *   (and, of course, it played a huge role in AlphaGo and other ML systems too!)
    *
    *   MCTS is an algorithm that plays games by slowly building a tree of possibilities.
    *   By using a mixture of careful planning (weighing up how much an action has been tried
    *   and how good the reward was from taking it) and random play, it can play many games
    *   to an acceptable skill level, with absolutely no knowledge about the game. This is
    *   really crucial for automated game design, because we (generally) know nothing about
    *   the games we're testing.
    *
    *   MCTS has firmly added itself to the "algorithms I've written so many times I'm sick
    *   of them" by now, but like A* search it's just really handy and sometimes you have 
    *   to come out of retirement to write it one more time - just for you folks!
    */

    //More iterations = better search/better agent performance
    public int iterations = 100000;
    
    /// <summary>
    /// Take a turn, but use no more than the allotted time limit.
    /// </summary>
    /// <param name="g">The game to play</param>
    /// <param name="timeLimit">Time limit in seconds</param>
    /// <returns>Returns whether we managed to take a turn or not.</returns>
    public override bool TakeTurn(Game g, float timeLimit = 1f){

        bool wasInteractive = g.interactiveMode;
        g.interactiveMode = false;
        
        //NOTE: use timeLimit in addition to cutoff, if TapAction takes a lot of time for some reason.
        var timer = Stopwatch.StartNew();
        var timeLimitInMillis = timeLimit * 1000f;

        //Because we're going to be resetting the state a LOT, we just save our own copy here.
        GameState rootCopy = g.state.Copy();

        Node root = new Node(null,0,0);
        //We never actually use the root node's reward, but all of our child nodes represent actions
        //we are taking, so the reward modifier of their parent must be -1. 
        root.rewardModifier = -1f;

        for(int it=0; it<iterations; it++){
            //? Step zero: reset the game
            g.SetState(rootCopy.Copy(), 0);

            //Check if the time is up!
            if (timer.ElapsedMilliseconds > timeLimitInMillis) break;
            
            //? Step one: descend the tree from the root
            Node current = root;
            while(current.firstChild != null){
                //Select nodes based on their UCB score
                current = PickNext(current);
                //Apply the action represented by this node
                g.TapAction(current.ax, current.ay);
            }

            //The first time we meet a node we expand it!
            if(current.isExpanded == false){
                //? We're now at a node with no children, so we generate all the possible actions
                GenerateChildren(current, g);

                //? It's possible to generate zero children, i.e. if the board is full
                if(current.numChildren > 0){
                    //? Now pick a child at random
                    int randomChild = Random.Range(0, current.numChildren);
                    // Debug.Log("Picking child "+randomChild+" of "+current.numChildren);
                    current = current.firstChild;
                    for(int i=0; i<randomChild-1; i++){
                        current = current.nextSibling;
                    }
                    g.TapAction(current.ax, current.ay);
                }
            }
            
            //? Roll the node out
            float score = Rollout(current, g);

            //? Then go back up the tree and update all the nodes on the way up
            while(current != null){
                current.selections++;
                current.score += score * current.rewardModifier;
                current = current.parent;
            }
        }

        //? When we're done, the action we take is simply the most-visited child of the root node.
        int mostVisits = 0;
        Node nextAction = null;
        Node child = root.firstChild;
        while(child != null){
            //Debug.Log("Playing at "+child.ax+","+child.ay+" - sel: "+child.selections+", rew: "+child.score+", avg. rew: "+child.score/child.selections+", ucb: "+UCB(child));
            if(child.selections > mostVisits){
                mostVisits = child.selections;
                nextAction = child;
            }
            child = child.nextSibling;
        }

        if(nextAction == null)
            return false;

        g.interactiveMode = wasInteractive;

        g.SetState(rootCopy.Copy(), 0);
        g.TapAction(nextAction.ax, nextAction.ay);
        return true;
    }

    public Node PickNext(Node n){
        Node c = n.firstChild;

        Node bestChild = null;
        float bestChildScore = -100000f;

        float thisNodeScore = 0;
        while(c != null){
            thisNodeScore = UCB(c);
            
            if(thisNodeScore > bestChildScore){
                bestChild = c;
                bestChildScore = thisNodeScore;
            }
            c = c.nextSibling;
        }

        return bestChild;
    }

    public void GenerateChildren(Node n, Game g){
        //Although we don't know *most* things about the games our system generates, 
        //we do know a few things based on the structure of the system. One of these
        //is: we know you can only tap empty squares. We use this to generate all possible
        //actions.

        Node prev = null;
        int numChildren = 0;
        
        //The first time we try to generate children, we mark it as expanded.
        n.isExpanded = true;

        //If the game ended as we selected this node, just return.
        if (g.endStatus > 0) return;

        for(int i=0; i<g.boardWidth; i++){
            for(int j=0; j<g.boardHeight; j++){
                if(g.state.Value(i, j) == 0){
                    //New child
                    Node child = new Node(n, i, j);
                    //Set up all the connections in the tree
                    child.prevSibling = prev;
                    if(prev != null){
                        prev.nextSibling = child;
                    }
                    else{
                        //If there's no previous child, we must be the first
                        n.firstChild = child;
                    }
                    prev = child;
                    child.parent = n;
                    //Our reward mod is always the inverse of our parent's, since we alternate turns
                    child.rewardModifier = n.rewardModifier*-1;
                    numChildren++;
                }
            }
        }

        n.numChildren = numChildren;
    }

    //The rollout length can be longer than the area of the board, since some rules remove pieces
    public int rolloutLength = 50;

    public float Rollout(Node n, Game g){
        //Couple of ways we could pick random moves. Here we mimic the approach RandomAgent takes, it's
        //a little lazy and inefficient - would be better to collect possible actions and sample them,
        //I'll try and add this before release - if you're reading this, I totally did not do that.
        int triesPerTurn = 150; bool noMoveFound = false;
        for(int t=0; t<rolloutLength; t++){
            //Game ended for some reason or other
            if(g.endStatus > 0){
                break;
            }

            noMoveFound = true;
            for(int i=0; i<triesPerTurn; i++){
                if(g.TapAction(Random.Range(0, g.boardWidth), Random.Range(0, g.boardHeight))){
                    noMoveFound = false;
                    break;
                }
            }
            //Couldn't move, i.e. board full
            if(noMoveFound){
                //We can treat this like a tie.
                g.endStatus = 3;
                break;
            }
        }
        if(g.endStatus == 3 || g.endStatus == 0)
            return 0;
        if(g.endStatus == playerCode){
            return 1f;
        }
        else{
            return -1f;
        }
    }

    //The C value moderates how much the MCTS agent balances between choosing high-scoring nodes,
    //and choosing under-explored nodes. Higher C value will mean it explores less-chosen nodes
    //at the expense of more promising ones.
    float C = 1.42f;

    public float UCB(Node node){
        if(node.selections == 0){
            return float.MaxValue;
        }

        //This is the average reward for the node
        float LHS = node.score/node.selections;
        //This is how often we've visited it proportional to its parent
        float RHS = Mathf.Sqrt(Mathf.Log(node.parent.selections)/node.selections);

        return LHS + (C * RHS);
    }

    public class Node{
        public int selections;
        public float score;
        public int numChildren;
        public bool isExpanded;

        //The action taken (decomposed into x/y);
        public int ax;
        public int ay; //lmao

        //Is (ax,ay) our action? If it's our opponents, any reward they get should be inverted
        public float rewardModifier = 1;

        //Links for the tree structure
        public Node parent;
        public Node firstChild;
        public Node nextSibling;
        public Node prevSibling;

        public int id = 0;
        public static int g_id = 1;

        public Node(Node parent, int x, int y, float rewardMod=1){
            this.parent = parent;
            this.ax = x;
            this.ay = y;
            this.rewardModifier = rewardMod;
            this.id = g_id;
            g_id++;
        }
    }

    public MCTSAgent(int playerCode){
        this.playerCode = playerCode;
    }

}
