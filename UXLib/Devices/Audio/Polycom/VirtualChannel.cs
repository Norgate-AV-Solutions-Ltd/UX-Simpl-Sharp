﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace UXLib.Devices.Audio.Polycom
{
    public class VirtualChannel : ISoundstructureItem
    {
        public VirtualChannel(Soundstructure device, string name, SoundstructureVirtualChannelType vcType, SoundstructurePhysicalChannelType pcType, uint[] values)
        {
            this.Device = device;
            this.Device.ValueChange += new SoundstructureValueChangeHandler(Device_ValueChange);
            this.Name = name;
            this.VirtualChannelType = vcType;
            this.PhysicalChannelType = pcType;
            _physicalChannelIndex = new List<uint>(values);

#if DEBUG
            CrestronConsole.Print("Received {0} with name: {1}, Virtual Type: {2}, Physical Type: {3} Values:",
                        this.GetType().ToString().Split('.').Last(), this.Name, this.VirtualChannelType.ToString(), this.PhysicalChannelType.ToString());

            foreach (uint value in this.PhysicalChannelIndex)
            {
                CrestronConsole.Print(" {0}", value);
            }

            CrestronConsole.PrintLine("");
#endif
        }

        public Soundstructure Device { get; protected set; }
        public string Name { get; protected set; }

        public SoundstructureVirtualChannelType VirtualChannelType { get; protected set; }
        public SoundstructurePhysicalChannelType PhysicalChannelType { get; protected set; }

        List<uint> _physicalChannelIndex;
        public ReadOnlyCollection<uint> PhysicalChannelIndex
        {
            get
            {
                return _physicalChannelIndex.AsReadOnly();
            }
        }

        public virtual void Init()
        {
            if (this.SupportsFader)
                this.Device.Socket.Get(this, SoundstructureCommandType.FADER);
            if (this.SupportsMute)
                this.Device.Socket.Get(this, SoundstructureCommandType.MUTE);
        }

        public bool SupportsFader
        {
            get
            {
                switch (this.PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.SR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_LINE_OUT:
                    case SoundstructurePhysicalChannelType.SR_LINE_OUT:
                    case SoundstructurePhysicalChannelType.PSTN_IN:
                    case SoundstructurePhysicalChannelType.PSTN_OUT:
                    case SoundstructurePhysicalChannelType.VOIP_IN:
                    case SoundstructurePhysicalChannelType.VOIP_OUT:
                    case SoundstructurePhysicalChannelType.CLINK_OUT:
                    case SoundstructurePhysicalChannelType.CLINK_IN:
                    case SoundstructurePhysicalChannelType.SUBMIX:
                        return true;
                }
                return false;
            }
        }

        double _Fader;
        public double Fader
        {
            get
            {
                return _Fader;
            }
            set
            {

                if (this.Device.Socket.Set(this, SoundstructureCommandType.FADER, value))
                {
                    if (value <= FaderMax && value >= FaderMin)
                        _Fader = value;
                }
            }
        }

        public double FaderMin { get; protected set; }
        public double FaderMax { get; protected set; }

        public bool SupportsMute
        {
            get
            {
                switch (this.PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.SR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_LINE_OUT:
                    case SoundstructurePhysicalChannelType.SR_LINE_OUT:
                    case SoundstructurePhysicalChannelType.PSTN_IN:
                    case SoundstructurePhysicalChannelType.PSTN_OUT:
                    case SoundstructurePhysicalChannelType.VOIP_IN:
                    case SoundstructurePhysicalChannelType.VOIP_OUT:
                    case SoundstructurePhysicalChannelType.CLINK_OUT:
                    case SoundstructurePhysicalChannelType.CLINK_IN:
                    case SoundstructurePhysicalChannelType.SUBMIX:
                    case SoundstructurePhysicalChannelType.SIG_GEN:
                        return true;
                }
                return false;
            }
        }

        public bool IsMic
        {
            get
            {
                switch (this.PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.SR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_MIC_IN:
                        return true;
                }
                return false;
            }
        }

        public bool IsVoip
        {
            get
            {
                switch (this.PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.VOIP_IN:
                    case SoundstructurePhysicalChannelType.VOIP_OUT:
                        return true;
                }
                return false;
            }
        }

        public bool IsPSTN
        {
            get
            {
                switch (this.PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.PSTN_IN:
                    case SoundstructurePhysicalChannelType.PSTN_OUT:
                        return true;
                }
                return false;
            }
        }

        bool _mute;
        public bool Mute
        {
            get
            {
                return _mute;
            }
            set
            {
                if (this.Device.Socket.Set(this, SoundstructureCommandType.MUTE, value))
                    _mute = value;
            }
        }

        protected virtual void OnFeedbackReceived(SoundstructureCommandType commandType, string commandModifier, double value)
        {
            switch (commandType)
            {
                case SoundstructureCommandType.MUTE: _mute = Convert.ToBoolean(value);
                    break;
                case SoundstructureCommandType.FADER:
                    if (commandModifier == "min")
                        FaderMin = value;
                    else if (commandModifier == "max")
                        FaderMax = value;
                    else
                        _Fader = value;
                    break;
            }
        }

        void Device_ValueChange(ISoundstructureItem item, SoundstructureValueChangeEventArgs args)
        {
            try
            {
                if (item == this)
                {
                    OnFeedbackReceived(args.CommandType, args.CommandModifier, args.Value);
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("{0} Error in Device_ValueChange(): {1}", this.GetType().ToString().Split('.').Last(), e.Message);
            }
        }
    }
}