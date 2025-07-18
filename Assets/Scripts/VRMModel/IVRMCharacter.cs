using UnityEngine;

interface IVRMCharacter
{
    string Name { get; }
    Animator Animator { get; }
    BlinkController blinkController { get; }
    bool ready { get; }
}