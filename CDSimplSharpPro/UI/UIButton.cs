﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;

namespace CDSimplSharpPro.UI
{
    public class UIButton
    {
        public string KeyName { get; private set; }
        public object LinkedObject;
        string _Title;
        public string Title
        {
            set
            {
                this._Title = value;
                if (this.SerialInputJoin != null)
                    this.SerialInputJoin.StringValue = this._Title;
            }
            get
            {
                return this._Title;
            }
        }
        public bool Feedback
        {
            set
            {
                if (this.DigitalInputJoin != null)
                    this.DigitalInputJoin.BoolValue = value;
            }
            get
            {
                if (this.DigitalInputJoin != null)
                    return this.DigitalInputJoin.BoolValue;
                return false;
            }
        }
        public bool Enabled
        {
            set
            {
                if (this.EnableJoin != null)
                    this.EnableJoin.BoolValue = value;
            }
            get
            {
                if(this.EnableJoin != null)
                    return this.EnableJoin.BoolValue;
                return true;
            }
        }
        public bool Visible
        {
            set
            {
                if(this.VisibleJoin != null)
                    this.VisibleJoin.BoolValue = value;
            }
            get
            {
                if(this.VisibleJoin != null)
                    return this.VisibleJoin.BoolValue;
                return true;
            }
        }
        public GenericBase Owner
        {
            get
            {
                return this.DigitalOutputJoin.Owner;
            }
        }
        private bool _Down;
        public bool Down
        {
            set
            {
                if (value == true && this._Down == false)
                {
                    this._Down = value;

                    if (this.HoldTime > 0 && this.HoldTimer == null || this.HoldTimer.Disposed)
                    {
                        this.CurrentHoldTime = 0;
                        this.HoldTimer = new CTimer(HoldTimerUpdate, null, 100, 100);
                    }

                    if (this.ButtonEvent != null)
                    {
                        this.ButtonEvent(this, new UIButtonEventArgs(eUIButtonEventType.Pressed, this.CurrentHoldTime));
                    }
                }
                else if (value == false && this._Down == true)
                {
                    this._Down = value;

                    if(this.HoldTimer != null)
                        this.HoldTimer.Dispose();

                    if (this.PageToShowOnRelease != null)
                    {
                        this.PageToShowOnRelease.Show();
                    }

                    if (this.ButtonEvent != null)
                    {
                        this.ButtonEvent(this, new UIButtonEventArgs(eUIButtonEventType.Released, this.CurrentHoldTime));
                    }

                    if (this.CurrentHoldTime < this.HoldTime)
                    {
                        if (this.ButtonEvent != null)
                        {
                            this.ButtonEvent(this, new UIButtonEventArgs(eUIButtonEventType.Tapped, this.CurrentHoldTime));
                        }
                    }
                }
            }
        }
        private CTimer HoldTimer;
        private long CurrentHoldTime;
        public long HoldTime;

        BoolOutputSig DigitalOutputJoin;
        BoolInputSig DigitalInputJoin;
        StringInputSig SerialInputJoin;
        BoolInputSig EnableJoin;
        BoolInputSig VisibleJoin;

        public UIPage PageToShowOnRelease;

        public uint JoinNumber
        {
            get
            {
                return this.DigitalOutputJoin.Number;
            }
        }

        public event UIButtonEventHandler ButtonEvent;

        public UIButton(string keyName, BoolOutputSig digitalPressJoin)
        {
            this.KeyName = keyName;
            this._Title = this.KeyName;
            this.HoldTime = 500;
            this.DigitalOutputJoin = digitalPressJoin;
        }

        public UIButton(string keyName, BoolOutputSig digitalPressJoin, BoolInputSig digitalFeedbackJoin)
        {
            this.KeyName = keyName;
            this._Title = this.KeyName;
            this.HoldTime = 500;
            this.DigitalOutputJoin = digitalPressJoin;
            this.DigitalInputJoin = digitalFeedbackJoin;
        }
        
        public UIButton(string keyName, BoolOutputSig digitalPressJoin, BoolInputSig digitalFeedbackJoin,
            StringInputSig serialJoinSig)
        {
            this.KeyName = keyName;
            this._Title = this.KeyName;
            this.HoldTime = 500;
            this.DigitalOutputJoin = digitalPressJoin;
            this.DigitalInputJoin = digitalFeedbackJoin;
            this.SerialInputJoin = serialJoinSig;
            this.SerialInputJoin.StringValue = this._Title;
        }

        public UIButton(string keyName, BoolOutputSig digitalOutputJoin, BoolInputSig digitalFeedbackJoin,
            StringInputSig serialJoinSig, BoolInputSig enableJoinSig, BoolInputSig visibleJoinSig)
        {
            this.KeyName = keyName;
            this._Title = this.KeyName;
            this.HoldTime = 500;
            this.DigitalOutputJoin = digitalOutputJoin;
            this.DigitalInputJoin = digitalFeedbackJoin;
            this.SerialInputJoin = serialJoinSig;
            this.SerialInputJoin.StringValue = this._Title;
            this.EnableJoin = enableJoinSig;
            this.EnableJoin.BoolValue = true;
            this.VisibleJoin = visibleJoinSig;
            this.VisibleJoin.BoolValue = true;
        }

        public void Show()
        {
            this.Visible = true;
        }

        public void Hide()
        {
            this.Visible = false;
        }

        public void Enable()
        {
            this.Enabled = true;
        }

        public void Disable()
        {
            this.Enabled = false;
        }

        private void HoldTimerUpdate(object obj)
        {
            this.CurrentHoldTime = this.CurrentHoldTime + 100;

            if (this.CurrentHoldTime == this.HoldTime)
            {
                if (this.ButtonEvent != null)
                {
                    this.ButtonEvent(this, new UIButtonEventArgs(eUIButtonEventType.Held, this.CurrentHoldTime));
                }
            }
        }
    }

    public delegate void UIButtonEventHandler(UIButton button, UIButtonEventArgs args);

    public class UIButtonEventArgs : EventArgs
    {
        public eUIButtonEventType EventType;
        public long HoldTime;
        public UIButtonEventArgs(eUIButtonEventType type, long holdTime)
            : base()
        {
            this.EventType = type;
            this.HoldTime = holdTime;
        }
    }

    public enum eUIButtonEventType
    {
        Pressed,
        Tapped,
        Held,
        Released
    }
}