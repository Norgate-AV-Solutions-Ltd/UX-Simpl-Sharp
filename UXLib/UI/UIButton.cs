﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;

namespace UXLib.UI
{
    public class UIButton : UIObject
    {
        public UIButton(BoolOutputSig pressDigitalJoin)
        {
            this.PressDigitalJoin = pressDigitalJoin;
            this.SubscribeToSigChanges();
        }

        public UIButton(BasicTriList device, uint pressDigitalJoinNumber)
            : this(device.BooleanOutput[pressDigitalJoinNumber]) { }

        public UIButton(BoolOutputSig pressDigitalJoin, BoolInputSig feedbackDigitalJoin)
            : this(pressDigitalJoin)
        {
            this.FeedbackDigitalJoin = feedbackDigitalJoin;
        }

        public UIButton(BasicTriList device, uint pressDigitalJoinNumber, uint feedbackDigitalJoinNumber)
            : this(device.BooleanOutput[pressDigitalJoinNumber],
            device.BooleanInput[feedbackDigitalJoinNumber]) { }

        public UIButton(BoolOutputSig pressDigitalJoin, BoolInputSig feedbackDigitalJoin,
            StringInputSig textSerialJoin)
            : this(pressDigitalJoin, feedbackDigitalJoin)
        {
            this.TextSerialJoin = textSerialJoin;
        }

        public UIButton(BasicTriList device, uint pressDigitalJoinNumber, uint feedbackDigitalJoinNumber,
            uint textSerialJoinNumber)
            : this(device.BooleanOutput[pressDigitalJoinNumber],
            device.BooleanInput[feedbackDigitalJoinNumber],
            device.StringInput[textSerialJoinNumber]) { }

        public UIButton(BoolOutputSig pressDigitalJoin, BoolInputSig feedbackDigitalJoin,
            StringInputSig textSerialJoin, BoolInputSig enableDigitalJoin, BoolInputSig visibleDigitalJoin)
            : this(pressDigitalJoin, feedbackDigitalJoin, textSerialJoin)
        {
            this.EnableDigitalJoin = enableDigitalJoin;
            this.VisibleDigitalJoin = visibleDigitalJoin;
        }

        public UIButton(BasicTriList device, uint pressDigitalJoinNumber, uint feedbackDigitalJoinNumber,
            uint textSerialJoinNumber, uint enableDigitalJoinNumber, uint visibleDigitalJoinNumber)
            : this(device.BooleanOutput[pressDigitalJoinNumber],
            device.BooleanInput[feedbackDigitalJoinNumber],
            device.StringInput[textSerialJoinNumber],
            device.BooleanInput[enableDigitalJoinNumber],
            device.BooleanInput[visibleDigitalJoinNumber]) { }

        public string Title
        {
            get
            {
                return this.Text;
            }
            set
            {
                this.Text = value;
            }
        }
    }
}