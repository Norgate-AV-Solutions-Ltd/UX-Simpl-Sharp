﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.Fusion;

namespace UXLib.Models.Fusion
{
    public interface IFusionAsset
    {
        AssetTypeName AssetTypeName { get; }
        void AssignFusionAsset(FusionController fusionInstance, FusionAssetBase asset);
        string Name { get; }
        void FusionUpdate();
        void FusionError(string errorDetails);
    }

    public enum AssetTypeName
    {
        TouchPanel,
        Display,
        Source,
        DSP,
        VideoConferenceCodec
    }
}