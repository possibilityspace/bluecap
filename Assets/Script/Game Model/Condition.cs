using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Condition
{
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
