using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseAgent
{

    protected int playerCode;

    public virtual bool TakeTurn(Game g){
        return false;
    }

}
