using UnityEngine;

namespace Buzzfield.Core
{
    /// <summary>
    /// Tiny tween runner for UI feedback: scale pops, press-and-hold and a rotation wiggle.
    /// A fixed slot array and no per-frame allocations; the game calls <see cref="Tick"/> once
    /// per frame. Each target has at most one scale tween and one rotation tween; a new tween
    /// on the same channel continues from the current value, so the rest pose never drifts.
    /// </summary>
    public sealed class Tweener
    {
        private enum Kind : byte
        {
            Pop,
            Hold,
            Wiggle,
        }

        private struct Slot
        {
            public Transform Target;
            public Kind Kind;
            public float Time;
            public float Duration;
            public float From;
            public float Amount;
            public float Cycles;
            public float Current;
            public Vector3 BaseScale;
            public Quaternion BaseRotation;
        }

        private readonly Slot[] slots;
        private int count;

        public Tweener(int capacity)
        {
            slots = new Slot[Mathf.Max(1, capacity)];
        }

        public int ActiveCount => count;

        /// <summary>Scale bump of <paramref name="amount"/> (0.2 = +20% at the peak); also releases a hold.</summary>
        public void Pop(Transform target, float amount, float duration)
        {
            int index = ScaleSlot(target);
            if (index < 0)
                return;
            ref Slot slot = ref slots[index];
            Start(ref slot, Kind.Pop, duration);
            slot.Amount = amount;
        }

        /// <summary>Grows the target from zero to its rest scale, overshooting by <paramref name="amount"/>.</summary>
        public void PopIn(Transform target, float amount, float duration)
        {
            int index = ScaleSlot(target);
            if (index < 0)
                return;
            ref Slot slot = ref slots[index];
            slot.Current = 0f;
            Start(ref slot, Kind.Pop, duration);
            slot.Amount = amount;
            // Hide it right away: the first tick only comes next frame.
            target.localScale = Vector3.zero;
        }

        /// <summary>Eases the scale to <paramref name="factor"/> and keeps it there until the next <see cref="Pop"/> or <see cref="Stop"/>.</summary>
        public void Hold(Transform target, float factor, float duration)
        {
            int index = ScaleSlot(target);
            if (index < 0)
                return;
            ref Slot slot = ref slots[index];
            Start(ref slot, Kind.Hold, duration);
            slot.Amount = factor;
        }

        /// <summary>Rotates around the local z axis by up to <paramref name="degrees"/>, fading out over <paramref name="duration"/>.</summary>
        public void Wiggle(Transform target, float degrees, float cycles, float duration)
        {
            int index = Find(target, true);
            if (index < 0)
            {
                index = Add(target);
                if (index < 0)
                    return;
                slots[index].BaseRotation = target.localRotation;
            }
            ref Slot slot = ref slots[index];
            slot.Kind = Kind.Wiggle;
            slot.Time = 0f;
            slot.Duration = duration;
            slot.Amount = degrees;
            slot.Cycles = cycles;
        }

        /// <summary>Puts the target back to its rest pose and drops its tweens.</summary>
        public void Stop(Transform target)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                if (slots[i].Target != target)
                    continue;
                Restore(ref slots[i]);
                RemoveAt(i);
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                ref Slot slot = ref slots[i];
                if (slot.Target == null)
                {
                    RemoveAt(i);
                    continue;
                }

                bool wasDone = slot.Time > 0f && slot.Time >= slot.Duration;
                slot.Time += deltaTime;
                float t = slot.Duration > 0f ? slot.Time / slot.Duration : 1f;
                switch (slot.Kind)
                {
                    case Kind.Pop:
                        slot.Current = TweenMath.Pop(slot.From, slot.Amount, t);
                        slot.Target.localScale = slot.BaseScale * slot.Current;
                        if (t >= 1f)
                            RemoveAt(i);
                        break;

                    case Kind.Hold:
                        // Stays in the list until the next Pop or Stop; nothing to write once it arrived.
                        if (wasDone)
                            break;
                        slot.Current = TweenMath.Hold(slot.From, slot.Amount, t);
                        slot.Target.localScale = slot.BaseScale * slot.Current;
                        break;

                    case Kind.Wiggle:
                        float angle = TweenMath.Wiggle(t, slot.Cycles) * slot.Amount;
                        slot.Target.localRotation = slot.BaseRotation * Quaternion.Euler(0f, 0f, angle);
                        if (t >= 1f)
                            RemoveAt(i);
                        break;
                }
            }
        }

        private int ScaleSlot(Transform target)
        {
            int index = Find(target, false);
            if (index >= 0)
                return index;
            index = Add(target);
            if (index < 0)
                return -1;
            slots[index].BaseScale = target.localScale;
            slots[index].Current = 1f;
            return index;
        }

        private static void Start(ref Slot slot, Kind kind, float duration)
        {
            // Continue from where the previous tween on this channel left the scale.
            slot.From = slot.Current;
            slot.Kind = kind;
            slot.Time = 0f;
            slot.Duration = duration;
        }

        /// <summary>Slot of the target on the rotation channel, or on the scale channel (Pop and Hold).</summary>
        private int Find(Transform target, bool rotation)
        {
            for (int i = 0; i < count; i++)
            {
                if (slots[i].Target == target && (slots[i].Kind == Kind.Wiggle) == rotation)
                    return i;
            }
            return -1;
        }

        private int Add(Transform target)
        {
            if (target == null || count >= slots.Length)
                return -1;
            slots[count] = new Slot { Target = target };
            return count++;
        }

        private static void Restore(ref Slot slot)
        {
            if (slot.Target == null)
                return;
            if (slot.Kind == Kind.Wiggle)
                slot.Target.localRotation = slot.BaseRotation;
            else
                slot.Target.localScale = slot.BaseScale;
        }

        private void RemoveAt(int index)
        {
            count--;
            slots[index] = slots[count];
            slots[count] = default;
        }
    }
}
