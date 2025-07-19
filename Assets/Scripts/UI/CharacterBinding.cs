using System;
using System.Threading.Tasks;
using UnityEngine;
using UniVRM10;

class CharacterBinding : MonoBehaviour, IVRMCharacter
{
    [SerializeField] private CharacterController character;
    public LipSyncSimulator lipSync { get; private set; }
    public string Name => character.Name;

    public Animator Animator => character.Animator;

    public BlinkController blinkController => character.blinkController;

    public bool ready => character.ready;

    public int vrmFileConfigSelector => character.vrmFileConfigSelector;

    public void SetExpression(ExpressionKey exp, float value)
    {
        character.SetExpression(exp, value);
    }

    internal async Task DoneThinking()
    {
        // ThinkingAnimationの処理が終わるまで待機
        await character.DoneThinking();
    }

    internal void DoThinking()
    {
        character.DoThinking();
    }
    public void SetCharacter(CharacterController character)
    {
        this.character = character;
        InitCharacter();
    }

    void InitCharacter()
    {
        lipSync = character.GetComponent<LipSyncSimulator>();
    }

    private void Awake()
    {
        InitCharacter();
    }
}