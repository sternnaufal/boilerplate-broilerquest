using System.Collections.Generic;
using UnityEngine;

public partial class StarterKandangSlot
{
    private void FindAnimator()
    {
        if (chickenAnimator == null && chickenVisual != null)
            chickenAnimator = chickenVisual.GetComponentInChildren<Animator>(true);
    }

    private void AssignChickenAnimator(GameObject source)
    {
        chickenAnimator = source != null ? source.GetComponentInChildren<Animator>(true) : null;
    }

    private void UpdateAnimationByNeed(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Heating:
                TrySetAnimationBool(heatAnimParam, true);
                break;
            case ChickenNeed.Cooling:
                TrySetAnimationBool(coldAnimParam, true);
                break;
            case ChickenNeed.Feed:
                ResetAnimationToNormal();
                break;
        }
    }

    private void ResetAnimationToNormal()
    {
        if (!occupied)
            return;

        TrySetAnimationBool(heatAnimParam, false);
        TrySetAnimationBool(coldAnimParam, false);
    }

    private void TrySetAnimationBool(string paramName, bool value)
    {
        if (string.IsNullOrWhiteSpace(paramName))
            return;

        List<Animator> animators = GetActiveChickenAnimators();
        if (animators.Count == 0)
            return;

        bool anyAnimatorHandled = false;
        foreach (Animator animator in animators)
        {
            if (!HasAnimatorParameter(animator, paramName))
                continue;

            animator.SetBool(paramName, value);
            anyAnimatorHandled = true;
        }

        if (!anyAnimatorHandled)
            Debug.LogWarning($"{name}: Animator parameter '{paramName}' tidak ditemukan.");
    }

    private List<Animator> GetActiveChickenAnimators()
    {
        List<Animator> animators = new List<Animator>();
        foreach (GameObject visual in GetActiveChickenVisuals())
        {
            Animator animator = visual != null ? visual.GetComponentInChildren<Animator>(true) : null;
            if (animator != null)
                animators.Add(animator);
        }

        if (animators.Count == 0 && chickenAnimator != null)
            animators.Add(chickenAnimator);

        return animators;
    }

    private bool HasAnimatorParameter(Animator animator, string parameterName)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
