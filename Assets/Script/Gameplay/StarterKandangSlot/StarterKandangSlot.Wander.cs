using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class StarterKandangSlot
{
    [Header("Chicken Wander")]
    [SerializeField] private bool enableWander = true;
    [SerializeField] private Vector2 wanderRadius = new Vector2(50f, 20f);
    [SerializeField] private float wanderSpeed = 80f;
    [SerializeField] private float wanderPauseMin = 1.5f;
    [SerializeField] private float wanderPauseMax = 3.5f;

    private Coroutine wanderCoroutine;
    private bool isWanderingPaused;

    private void StartWander()
    {
        if (!ShouldUseWander())
        {
            StopWander();
            return;
        }

        StopWander();
        isWanderingPaused = false;
        wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    private void StopWander()
    {
        CoroutineHelper.StopSafe(this, ref wanderCoroutine);
    }

    private void PauseWander()
    {
        isWanderingPaused = true;
    }

    private bool ShouldUseWander()
    {
        return enableWander && CurrentChickenCount > 0;
    }

    private void UpdateWanderState()
    {
        if (ShouldUseWander())
            StartWander();
        else
            StopWander();
    }

    private IEnumerator WanderRoutine()
    {
        List<RectTransform> visualRects = GetActiveVisualRects();
        if (visualRects.Count == 0)
            yield break;

        Vector2[] startPositions = new Vector2[visualRects.Count];
        Vector2[] targets = new Vector2[visualRects.Count];
        float[] pauses = new float[visualRects.Count];

        for (int i = 0; i < visualRects.Count; i++)
        {
            startPositions[i] = visualRects[i].anchoredPosition;
            targets[i] = GetRandomWanderTarget(startPositions[i], 1f);
            pauses[i] = Random.Range(wanderPauseMin, wanderPauseMax);
        }

        while (true)
        {
            if (isWanderingPaused || !occupied)
            {
                TrySetAnimationBool(idleAnimParam, true);
                yield return null;
                continue;
            }

            float speedMult = 1f;
            float radiusMult = 1f;
            float pauseMin = wanderPauseMin;
            float pauseMax = wanderPauseMax;
            bool canMove = true;

            if (currentState == SlotState.WaitingForCareClick)
            {
                if (currentNeed == ChickenNeed.Feed)
                    canMove = false;
                else
                {
                    speedMult = 2.2f;
                    radiusMult = 1.5f;
                    pauseMin = 0.1f;
                    pauseMax = 0.45f;
                }
            }
            else if (currentState == SlotState.WaitingForSellClick)
            {
                canMove = false;
            }

            if (!canMove)
            {
                TrySetAnimationBool(idleAnimParam, true);
                yield return null;
                continue;
            }

            bool anyMoving = false;
            for (int i = 0; i < visualRects.Count; i++)
            {
                RectTransform visualRect = visualRects[i];
                if (visualRect == null)
                    continue;

                if (pauses[i] > 0f)
                {
                    pauses[i] -= Time.deltaTime;
                    continue;
                }

                float dist = Vector2.Distance(visualRect.anchoredPosition, targets[i]);
                if (dist < 2f)
                {
                    targets[i] = GetRandomWanderTarget(startPositions[i], radiusMult);
                    pauses[i] = Random.Range(pauseMin, pauseMax);
                }
                else
                {
                    Vector2 oldPos = visualRect.anchoredPosition;
                    visualRect.anchoredPosition = Vector2.MoveTowards(
                        oldPos, targets[i], wanderSpeed * speedMult * Time.deltaTime
                    );
                    UpdateChickenFacing(visualRect, oldPos, visualRect.anchoredPosition);
                    anyMoving = true;
                }
            }

            TrySetAnimationBool(idleAnimParam, !anyMoving);
            yield return null;
        }
    }

    private Vector2 GetRandomWanderTarget(Vector2 startPos, float radiusMult)
    {
        return new Vector2(
            startPos.x + Random.Range(-wanderRadius.x * radiusMult, wanderRadius.x * radiusMult),
            startPos.y + Random.Range(-wanderRadius.y * radiusMult, wanderRadius.y * radiusMult)
        );
    }

    private void UpdateChickenFacing(RectTransform visualRect, Vector2 from, Vector2 to)
    {
        if (visualRect == null) return;

        float dirX = to.x - from.x;
        if (Mathf.Abs(dirX) < 0.01f) return;

        Vector3 scale = visualRect.localScale;
        bool facingRight = scale.x < 0;
        bool movingRight = dirX > 0;

        if (movingRight && !facingRight)
            scale.x = -Mathf.Abs(scale.x);
        else if (!movingRight && facingRight)
            scale.x = Mathf.Abs(scale.x);

        visualRect.localScale = scale;
    }

    private List<RectTransform> GetActiveVisualRects()
    {
        List<RectTransform> rects = new List<RectTransform>();
        List<GameObject> visuals = GetActiveChickenVisuals();
        foreach (GameObject visual in visuals)
        {
            RectTransform rect = visual != null ? visual.transform as RectTransform : null;
            if (rect != null)
                rects.Add(rect);
        }

        return rects;
    }
}
