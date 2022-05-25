using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationMotion : MonoBehaviour
{
    [SerializeField]
    public bool IsLoop = true;

    private void OnEnable()
    {
        this.Run();
    }

    public void Run()
    {
        var animator = gameObject.GetComponent<Animator>();
        if (animator == null)
            return;

        animator.SetBool("loop", this.IsLoop);
        animator.SetTrigger("init");
    }
}
