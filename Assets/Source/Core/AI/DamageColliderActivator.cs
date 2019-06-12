using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageColliderActivator : MonoBehaviour {

    public Collider LeftHand;
    public Collider RightHand;
    public Collider Special1;
    public Collider Special2;
    public Collider Special3;

    public void Activate(string parameter)
    {
        switch(parameter)
        {
            case "left":
                if(LeftHand)
                {
                    LeftHand.enabled = true;
                }
                break;
            case "right":
                if(RightHand)
                {
                    RightHand.enabled = true;
                }
                break;
            case "special1":
                if(Special1)
                {
                    Special1.enabled = true;
                }
                break;
            case "special2":
                if(Special2)
                {
                    Special2.enabled = true;
                }
                break;
            case "special3":
                if(Special3)
                {
                    Special3.enabled = true;
                }
                break;
        }
    }

    public void Deactivate()
    {
        if (LeftHand)
        {
            LeftHand.enabled = false;
        }
        if (RightHand)
        {
            RightHand.enabled = false;
        }
        if (Special1)
        {
            Special1.enabled = false;
        }
        if (Special2) 
        {
            Special2.enabled = false;
        }
        if (Special3)
        {
            Special3.enabled = false;
        } 
    }
}
