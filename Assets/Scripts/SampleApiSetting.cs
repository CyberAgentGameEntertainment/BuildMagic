// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SampleApiSettings",
    menuName = "Build Magic/Samples/Sample ApiSettings")]
[Serializable]
public class SampleApiSetting : ScriptableObject
{
    [SerializeField] public string Url;

    [SerializeField] public int Port;
}
