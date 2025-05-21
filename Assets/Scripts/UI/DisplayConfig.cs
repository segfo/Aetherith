using System.IO;
using LLMUnity;
using UnityEngine;
using System;
using System.Text;
using UniVRM10;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections;
using Unity.VisualScripting;
using System.Threading;
using UnityEngine.EventSystems;

public class UIResDisplayConfig : MonoBehaviour
{
    private void Awake()
    {
        QualitySettings.vSyncCount = 0; // VSyncを無効にする
        Application.targetFrameRate = 60; // フレームレートを30FPSに設定
    }
}