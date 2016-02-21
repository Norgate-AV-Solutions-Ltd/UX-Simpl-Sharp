﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using UXLib.Sockets;

namespace UXLib.Displays.Canon
{
    public class CanonProjector : DisplayDevice
    {
        public CanonProjector(string ipAddress)
        {
            Socket = new CanonProjectorSocket(ipAddress);
            Socket.SocketConnectionEvent += new SimpleClientSocketConnectionEventHandler(Socket_SocketConnectionEvent);
            Socket.ReceivedPacketEvent += new SimpleClientSocketReceiveEventHandler(Socket_ReceivedPacketEvent);
        }

        public CanonProjector(string ipAddress, UXLib.Relays.UpDownRelays relays)
            : base(relays)
        {
            Socket = new CanonProjectorSocket(ipAddress);
            Socket.SocketConnectionEvent += new SimpleClientSocketConnectionEventHandler(Socket_SocketConnectionEvent);
            Socket.ReceivedPacketEvent += new SimpleClientSocketReceiveEventHandler(Socket_ReceivedPacketEvent);
        }

        public CanonProjectorSocket Socket;
        CTimer pollTimer;

        bool commsEstablished = false;

        public override void Connect()
        {
            this.Socket.Connect(true);
        }

        public override void Disconnect()
        {
            this.Socket.Disconnect();
        }

        void Socket_SocketConnectionEvent(SimpleClientSocket socket, SocketStatus status)
        {
            if (status == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                pollTimer = new CTimer(PollPower, null, 2000, 2000);
            }
            else
            {
                pollTimer.Dispose();
            }
        }

        void Socket_ReceivedPacketEvent(SimpleClientSocket socket, SimpleClientSocketReceiveEventArgs args)
        {
            string receivedString = Encoding.Default.GetString(args.ReceivedPacket, 0, args.ReceivedPacket.Length);

            if (receivedString.StartsWith("g"))
            {
                receivedString = receivedString.Split(':')[1];
                string[] words = receivedString.Split('=');

                if (words.Length == 2)
                {
                    string command = words[0];
                    string value = words[1];

                    switch (command)
                    {
                        case "POWER":
                            switch (value)
                            {
                                case "ON":
                                    PowerStatus = DisplayDevicePowerStatus.PowerOn;
                                    if (!Power && commsEstablished)
                                        // Resend power as should be off
                                        Power = false;
                                    else if (!commsEstablished)
                                    {
                                        // We have comms and the power is on so update the status
                                        commsEstablished = true;
                                        // set base power as true as we don't want to set the powerstatus as warming
                                        base.Power = true;
                                    }
                                    break;
                                case "OFF":
                                    PowerStatus = DisplayDevicePowerStatus.PowerOff;
                                    commsEstablished = true;
                                    if (Power)
                                        Power = true;
                                    break;
                                case "OFF2ON":
                                    PowerStatus = DisplayDevicePowerStatus.PowerWarming;
                                    break;
                                case "ON2OFF":
                                    PowerStatus = DisplayDevicePowerStatus.PowerCooling;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case "INPUT":
                            if (requestedInput.Length > 0 && requestedInput != value)
                                Socket.Send(string.Format("INPUT={0}", requestedInput));
                            break;
                        default:
                            CrestronConsole.PrintLine("Projector {0} = {1}", command, value);
                            break;
                    }
                }
            }
            else
                CrestronConsole.PrintLine("Projector Rx ({0} bytes): {1}", receivedString.Length, receivedString);
        }

        void PollPower(object callBackObject)
        {
            this.Socket.Send("?POWER");
            if (this.Power)
                new CTimer(PollInput, null, 100);
        }

        void PollInput(object callBackObject)
        {
            this.Socket.Send("?INPUT");
        }

        public override bool Power
        {
            get
            {
                return base.Power;
            }
            set
            {
                if (value)
                {
                    SocketErrorCodes err = Socket.Send("POWER ON");
                    if (err == SocketErrorCodes.SOCKET_OK && !base.Power)
                        PowerStatus = DisplayDevicePowerStatus.PowerWarming;
                    base.Power = true;
                }
                else
                {
                    SocketErrorCodes err = Socket.Send("POWER OFF");
                    if (err == SocketErrorCodes.SOCKET_OK && base.Power)
                        PowerStatus = DisplayDevicePowerStatus.PowerCooling;
                    base.Power = false;
                }
            }
        }

        string requestedInput = "";

        public override DisplayDeviceInput Input
        {
            get
            {
                return base.Input;
            }
            set
            {
                if (InputCommandForInput(value).Length > 0)
                {
                    requestedInput = InputCommandForInput(value);
                    Socket.Send(string.Format("INPUT={0}", requestedInput));
                    base.Input = value;
                }
            }
        }

        public string InputCommandForInput(DisplayDeviceInput input)
        {
            switch (input)
            {
                case DisplayDeviceInput.HDMI1:
                case DisplayDeviceInput.HDMI2:
                case DisplayDeviceInput.HDMI3:
                case DisplayDeviceInput.HDMI4:
                    return "HDMI";
                case DisplayDeviceInput.VGA:
                    return "A-RGB";
                case DisplayDeviceInput.DVI:
                    return "D-RGB";
                case DisplayDeviceInput.YUV:
                    return "COMP";
                case DisplayDeviceInput.Composite:
                    return "VIDEO";
                default: return "";
            }
        }
    }
}