using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomStateChooser : StateMachineBehaviour
{
    public string StateName;
    
    public int PresetCount = 1;

    public bool ShouldChangePreset = false;

    private int m_lastState = -1;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(ShouldChangePreset || m_lastState == -1)
        {
            animator.SetInteger(StateName, Random.Range(0, PresetCount));
            m_lastState = animator.GetInteger(StateName);
        }
    }
}
