using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.CursorSystem
{
    /// <summary>
    /// Static, controller-independent stack of cursor claims. Sources push a claim describing
    /// which cursor they want and at what priority; the highest-priority (newest on ties) claim wins.
    /// Disposing a claim reveals the claim beneath it, so transient states (build mode, pause, hover)
    /// restore automatically. Claims pushed before a <see cref="MouseCursorController"/> exists are
    /// resolved as soon as one subscribes to <see cref="Changed"/>.
    /// </summary>
    public static class CursorClaimStack
    {
        static readonly List<CursorClaim> claims = new List<CursorClaim>();
        static long sequenceCounter;

        /// <summary>Raised whenever the set of claims changes (push, update, dispose, prune).</summary>
        public static event Action Changed;

        /// <summary>
        /// Push a new claim. Keep the returned handle and dispose it to release the claim.
        /// <paramref name="owner"/> is used as a destroyed-object backstop: claims whose Unity owner
        /// dies are pruned on the next resolution even if the handle was never disposed.
        /// </summary>
        public static CursorClaimHandle Push(
            string presetId,
            string setId,
            int priority,
            object owner,
            bool allowFallback = true,
            bool? visibility = null,
            CursorLockMode? lockMode = null)
        {
            AssertMainThread();

            var claim = new CursorClaim
            {
                PresetId = presetId,
                SetId = setId,
                Priority = priority,
                Owner = owner,
                AllowFallback = allowFallback,
                Visibility = visibility,
                LockMode = lockMode,
                Sequence = ++sequenceCounter,
            };

            claims.Add(claim);
            RaiseChanged();
            return new CursorClaimHandle(claim);
        }

        /// <summary>Highest-priority live claim; ties broken by newest sequence.</summary>
        public static bool TryGetWinner(out CursorClaim winner)
        {
            PruneDeadOwners();

            winner = null;
            for (int i = 0; i < claims.Count; i++)
            {
                var candidate = claims[i];
                if (winner == null ||
                    candidate.Priority > winner.Priority ||
                    (candidate.Priority == winner.Priority && candidate.Sequence > winner.Sequence))
                {
                    winner = candidate;
                }
            }

            return winner != null;
        }

        internal static void Remove(CursorClaim claim)
        {
            AssertMainThread();

            if (claim == null || claim.Disposed)
                return;

            claim.Disposed = true;
            claims.Remove(claim);
            RaiseChanged();
        }

        internal static void NotifyUpdated(CursorClaim claim)
        {
            AssertMainThread();

            claim.Sequence = ++sequenceCounter;
            RaiseChanged();
        }

        static void PruneDeadOwners()
        {
            bool removedAny = false;
            for (int i = claims.Count - 1; i >= 0; i--)
            {
                var claim = claims[i];
                if (claim.Owner is UnityEngine.Object unityOwner && unityOwner == null)
                {
                    claim.Disposed = true;
                    claims.RemoveAt(i);
                    removedAny = true;
                }
            }

            // No RaiseChanged here: pruning runs inside TryGetWinner, which is itself called
            // from the Changed handler — re-raising would recurse for no benefit.
            _ = removedAny;
        }

        static void RaiseChanged() => Changed?.Invoke();

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        static void AssertMainThread()
        {
            // UnityEngine.Object comparisons and Cursor APIs are main-thread only.
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1)
                Debug.LogError("[CursorClaimStack] Must be used from the main thread.");
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForDomainReloadDisabled()
        {
            // Supports "Enter Play Mode Options" without domain reload.
            claims.Clear();
            sequenceCounter = 0;
            Changed = null;
        }
#endif
    }

    /// <summary>A single cursor claim. Mutable internally; external code interacts via <see cref="CursorClaimHandle"/>.</summary>
    public sealed class CursorClaim
    {
        public string PresetId { get; internal set; }
        public string SetId { get; internal set; }
        public int Priority { get; internal set; }
        public bool AllowFallback { get; internal set; }
        public bool? Visibility { get; internal set; }
        public CursorLockMode? LockMode { get; internal set; }
        public object Owner { get; internal set; }

        internal long Sequence;
        internal bool Disposed;
    }

    /// <summary>
    /// Handle to a pushed claim. Dispose to release; releasing reveals the next claim beneath.
    /// <see cref="Update"/> changes the shown preset without re-stacking (priority stays fixed).
    /// </summary>
    public sealed class CursorClaimHandle : IDisposable
    {
        CursorClaim claim;

        internal CursorClaimHandle(CursorClaim claim) => this.claim = claim;

        public bool IsActive => claim != null && !claim.Disposed;

        /// <summary>Change preset/set of this claim in place. Bumps recency so it wins priority ties.</summary>
        public void Update(string presetId, string setId = null)
        {
            if (!IsActive)
            {
                Debug.LogWarning("[CursorClaimHandle] Update called on a disposed claim; ignoring.");
                return;
            }

            claim.PresetId = presetId;
            if (setId != null)
                claim.SetId = setId;

            CursorClaimStack.NotifyUpdated(claim);
        }

        public void Dispose()
        {
            if (claim == null)
                return;

            CursorClaimStack.Remove(claim);
            claim = null;
        }
    }
}
