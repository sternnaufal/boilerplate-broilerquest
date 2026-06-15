using System.Collections;
using UnityEngine;

public static class CoroutineHelper
{
    public static void StopSafe(MonoBehaviour owner, ref Coroutine coroutine)
    {
        if (owner == null || coroutine == null)
            return;

        owner.StopCoroutine(coroutine);
        coroutine = null;
    }

    public static void StopAndStart(MonoBehaviour owner, ref Coroutine coroutine, IEnumerator routine)
    {
        if (owner == null || routine == null)
            return;

        StopSafe(owner, ref coroutine);
        coroutine = owner.StartCoroutine(routine);
    }
}
