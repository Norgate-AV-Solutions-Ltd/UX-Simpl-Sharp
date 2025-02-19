﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXmlLinq;

namespace UXLib.Devices.VC.Cisco
{
    public class Conference
    {
        internal Conference(CiscoCodec codec)
        {
            Codec = codec;
            Sites = new Sites(Codec);
            DoNotDisturb = new DoNotDisturb(this);
        }

        internal CiscoCodec Codec { get; set; }
        public Sites Sites { get; protected set; }
        public DoNotDisturb DoNotDisturb { get; protected set; }
    }
}