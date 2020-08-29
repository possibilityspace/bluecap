using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Condition
{

    /*
     * This is a general-purpose class for a Condition.
     * Check() is a 'virtual' method, which means when we create new Condition classes
     * they can define their own version of the Check method. The other two are used for 
     * debug printing, and for saving the code of a game to a file.
    */
    public virtual bool Check(Game g, Player p){
        return false;
    }

    public virtual string Print(){
        return "Generic condition";
    }

    public virtual string ToCode(){
        return "<error - did not override ToCode()>";
    }

}
