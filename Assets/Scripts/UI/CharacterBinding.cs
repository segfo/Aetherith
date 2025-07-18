using UnityEngine;

class CharacterBinding : MonoBehaviour,IVRMCharacter
{
    [SerializeField] private IVRMCharacter character;

    public string Name => character.Name;

    public Animator Animator => character.Animator;

    public BlinkController blinkController => character.blinkController;

    public bool ready => character.ready;

    private void Awake()
    {
    }
}