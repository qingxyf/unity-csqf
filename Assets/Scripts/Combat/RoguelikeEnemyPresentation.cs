using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Original two-pose SD sprites animated with Unity's native legacy clips.
/// Only the visual child moves; combat coordinates and colliders stay fixed.
/// </summary>
public class RoguelikeEnemyPresentation : MonoBehaviour
{
    public enum MotionProfile { Skirmisher, Duelist, Caster }
    public enum MotionState { Idle, Attack, Hit, Death }

    public SpriteRenderer body;
    public Sprite idleSprite;
    public Sprite attackSprite;
    public MotionProfile profile;

    public MotionState CurrentState { get; private set; }
    public float DeathDuration => 0.45f;
    public float RemainingDeathDuration => CurrentState == MotionState.Death
        ? Mathf.Max(0f, DeathDuration - (Time.time - stateStartedAt)) : 0f;
    public float AttackDuration => profile == MotionProfile.Duelist ? 0.9f :
        (profile == MotionProfile.Caster ? 0.8f : 0.6f);

    private Animation motion;
    private readonly List<AnimationClip> ownedClips = new List<AnimationClip>();
    private float stateStartedAt;

    private void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (body == null) return;
        if (idleSprite == null) idleSprite = body.sprite;
        motion = body.GetComponent<Animation>();
        if (motion == null) motion = body.gameObject.AddComponent<Animation>();
        motion.playAutomatically = false;
        motion.cullingType = AnimationCullingType.AlwaysAnimate;

        AddClip(MotionState.Idle, 1.4f, true, 0f, 0.035f, 1.018f);
        float reach = profile == MotionProfile.Caster ? -0.12f :
            (profile == MotionProfile.Duelist ? -0.32f : -0.5f);
        AddClip(MotionState.Attack, AttackDuration, false, reach, 0.06f, 0.97f);
        AddClip(MotionState.Hit, 0.24f, false, 0.12f, 0f, 0.96f);
        AddClip(MotionState.Death, DeathDuration, false, 0f, -0.2f, 0.85f);
    }

    private void OnEnable()
    {
        Play(MotionState.Idle);
    }

    private void Update()
    {
        if (body == null) return;
        float elapsed = Time.time - stateStartedAt;
        if (CurrentState == MotionState.Death)
        {
            Color color = body.color;
            color.a = 1f - Mathf.Clamp01(elapsed / DeathDuration);
            body.color = color;
            return;
        }

        if (CurrentState == MotionState.Attack)
        {
            body.sprite = attackSprite != null && elapsed >= AttackDuration * 0.18f &&
                elapsed < AttackDuration * 0.72f ? attackSprite : idleSprite;
            if (elapsed >= AttackDuration) Play(MotionState.Idle);
        }
        else if (CurrentState == MotionState.Hit)
        {
            body.color = Color.Lerp(new Color(1f, 0.5f, 0.45f), Color.white,
                Mathf.Clamp01(elapsed / 0.24f));
            if (elapsed >= 0.24f) Play(MotionState.Idle);
        }
    }

    public void PlayAttack()
    {
        if (CurrentState != MotionState.Death) Play(MotionState.Attack);
    }

    public void PlayHit()
    {
        if (CurrentState != MotionState.Death) Play(MotionState.Hit);
    }

    public void PlayDeath()
    {
        Play(MotionState.Death);
    }

    private void Play(MotionState state)
    {
        if (CurrentState == MotionState.Death && state != MotionState.Death) return;
        CurrentState = state;
        stateStartedAt = Time.time;
        if (body != null)
        {
            body.sprite = idleSprite;
            body.color = Color.white;
        }
        if (motion != null) motion.Play(state.ToString(), PlayMode.StopAll);
    }

    private void AddClip(MotionState state, float duration, bool loop,
        float offsetX, float offsetY, float scaleY)
    {
        AnimationClip clip = new AnimationClip
        {
            name = state.ToString(), legacy = true,
            wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever
        };
        float endX = state == MotionState.Death ? offsetX : 0f;
        float endY = state == MotionState.Death ? offsetY : 0f;
        float endScale = state == MotionState.Death ? scaleY : 1f;
        clip.SetCurve("", typeof(Transform), "localPosition.x", Curve(duration, 0f, offsetX, endX));
        clip.SetCurve("", typeof(Transform), "localPosition.y", Curve(duration, 0f, offsetY, endY));
        clip.SetCurve("", typeof(Transform), "localScale.y", Curve(duration, 1f, scaleY, endScale));
        motion.AddClip(clip, clip.name);
        ownedClips.Add(clip);
    }

    private static AnimationCurve Curve(float duration, float start, float middle, float end)
    {
        return new AnimationCurve(new Keyframe(0f, start),
            new Keyframe(duration * 0.4f, middle), new Keyframe(duration, end));
    }

    private void OnDestroy()
    {
        foreach (AnimationClip clip in ownedClips)
        {
            if (Application.isPlaying) Destroy(clip);
            else DestroyImmediate(clip);
        }
    }
}
