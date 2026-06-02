using UnityEngine;

public class Staring : MonoBehaviour 
{
    public Transform tintaMea; 
    
    [Range(0,1)]
    public float catDeMultSeUita = 1.0f;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK()
    {
        if (animator && tintaMea != null)
        {
            animator.SetLookAtWeight(catDeMultSeUita, 0.3f, 1.0f, 0.5f, 0.7f);
            animator.SetLookAtPosition(tintaMea.position);
        }
    }
}