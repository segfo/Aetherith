using UnityEngine;
using UniVRM10;

interface IVRMCharacter
{
    string Name { get; }
    Animator Animator { get; }
    BlinkController blinkController { get; }
    bool ready { get; }

    void SetExpression(ExpressionKey exp, float value);
    int vrmFileConfigSelector { get; }
}