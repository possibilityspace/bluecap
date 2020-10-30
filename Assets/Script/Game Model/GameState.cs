using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This is used for recording a snapshot of a game at a point in time. This is very useful
 * for certain kinds of algorithm that need to create lots of simulations of all the possible
 * outcomes of different moves in a game. 
 *
 * It's quite important that this kind of code is quite efficient, because many algorithms
 * need to make and store thousands or millions of these things. My implementation is *okay*
 * but I wouldn't recommend you used it in a super high-efficiency system or anything like that. 
*/
public class GameState
{
    public int width, height;
    
    /*
     * We use a single 64-bit long to represent the game board. This is a hidden detail I didn't
     * mention anywhere else, but the upper limit on board size is really 8x8. This allows each
     * bit of the long here to represent a single tile on the board, which makes it very quick
     * to compare boards, or set bits, or copy game states.
     *
     * It's a bit of a cheat in some ways because it means you can't use boards bigger than 8x8
     * without updating this - otherwise the AI players won't be able to play properly.
    */
    public ulong player1 = 0;
    public ulong player2 = 0;
    public int currentPlayer = 1;

    public Point latestMove;
    
    public GameState Copy(){
        GameState res = new GameState(width, height);
        res.player1 = this.player1;
        res.player2 = this.player2;
        res.currentPlayer = this.currentPlayer;
        res.latestMove = this.latestMove;
        return res;
    }

    public GameState(int w, int h){
        this.width = w;
        this.height = h;
    }

    /*
     * This lets us set a board location (x,y) to the player p. We do this by flipping the bit
     * in the appropriate ulong.
    */
    public void Set(int x, int y, Player p){
        ulong mask = (ulong)1 << (x + (y*width));
        if(p == Player.CURRENT){
            if(currentPlayer == 1){
                player1 |= mask;
            }
            else{
                player2 |= mask;
            }
        }
        else{
            if(currentPlayer == 1){
                player2 |= mask;
            }
            else{
                player1 |= mask;
            }
        }
        
        latestMove = new Point(x,y);
    }

    public void Set(int x, int y, int p){
        ulong mask = (ulong)1 << (x + (y*width));
        if(p == 1){
            player1 |= mask;
            player2 &= ~mask;
        }
        else if(p == 2){
            player2 |= mask;
            player1 &= ~mask;
        }
        //i.e. unset
        else{
            player1 &= ~mask;
            player2 &= ~mask;
        }
        
        latestMove = new Point(x,y);
    }

    public int GetPlayerValue(Player player){
        if(player == Player.CURRENT){
            return currentPlayer;
        }
        else{
            //! This is actually a nasty hack based on the fact that we return '1' for player 1 here
            //! and '2' for player 2 - even though elsewhere we use 0 and 1? It's a bit inconsistent
            //! but you can happily ignore code like this (unless you want more than 2 players...)
            return 1+(currentPlayer % 2);
        }
    }

    public void AdvancePlayerOrder(){
        currentPlayer = 1+(currentPlayer%2);
    }

    //! For efficiency, we store the board representation in a 64-bit long.
    //! To check a value at a point, we make a bitmask and check if that bit is set.
    //! This just speeds things up a little. Feel free to ignore this, there's nicer
    //! ways to do this, and more readable ways too.
    public int Value(int x, int y){
        ulong mask = (ulong)1 << (x + (y*width));
        if((player1 & mask) > 0){
            return 1;
        }
        else if((player2 & mask) > 0){
            return 2;
        }
        return 0;
    }
}
