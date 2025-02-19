﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using UXLib.Extensions;

namespace UXLib.Devices.Displays.NEC
{
    public class NecComPortHandler
    {
        public NecComPortHandler(ComPort comPort)
        {
            ComPort = comPort;
            RxQueue = new CrestronQueue<byte>(1000);

            if (!ComPort.Registered)
            {
                if (ComPort.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    ErrorLog.Error("Could not register com port {0}", ComPort.ID);
                }
            }

            ComPort.SetComPortSpec(ComPort.eComBaudRates.ComspecBaudRate9600,
                ComPort.eComDataBits.ComspecDataBits8,
                ComPort.eComParityType.ComspecParityNone,
                ComPort.eComStopBits.ComspecStopBits1,
                ComPort.eComProtocolType.ComspecProtocolRS232,
                ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                false);
        }

        ComPort ComPort;
        CrestronQueue<byte> RxQueue { get; set; }
        Thread RxThread { get; set; }

        public void Initialize()
        {
            CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
            RxThread = new Thread(ReceiveBufferProcess, null, Thread.eThreadStartOptions.CreateSuspended);
            RxThread.Priority = Thread.eThreadPriority.UberPriority;
            RxThread.Name = "NEC ComPort - Rx Handler";
            RxThread.Start();
            ComPort.SerialDataReceived += new ComPortDataReceivedEvent(ComPort_SerialDataReceived);
        }

        void ComPort_SerialDataReceived(ComPort ReceivingComPort, ComPortSerialDataEventArgs args)
        {
            foreach (byte b in args.SerialData.ToByteArray())
                RxQueue.Enqueue(b);
        }

        public event NecComPortReceivedPacketEventHandler ReceivedPacket;

        void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType == eProgramStatusEventType.Stopping)
                RxThread.Abort();
        }

        public void SendCommand(int address, string message)
        {
            string str = "\x02" + message + "\x03";
            this.Send(address, MessageType.Command, str);
        }

        public void SetParameter(int address, string message)
        {
            string str = "\x02" + message + "\x03";
            this.Send(address, MessageType.SetParameter, str);
        }

        public void GetParameter(int address, string message)
        {
            string str = "\x02" + message + "\x03";
            this.Send(address, MessageType.GetParameter, str);
        }

        public void Send(int address, MessageType messageType, string message)
        {
            byte[] messageBytes = new byte[message.Length];
            for (int i = 0; i < message.Length; i++)
            {
                messageBytes[i] = unchecked((byte)message[i]);
            }
            this.Send(address, messageType, messageBytes);
        }

        public void Send(int address, MessageType messageType, byte[] message)
        {
            byte[] header = NecDisplaySocket.CreateHeader(address, messageType, message.Length);
            byte[] packet = new byte[7 + message.Length];
            Array.Copy(header, packet, header.Length);
            Array.Copy(message, 0, packet, header.Length, message.Length);

            int chk = 0;
            for (int i = 1; i < packet.Length; i++)
            {
                chk = chk ^ packet[i];
            }

            byte[] finalPacket = new byte[packet.Length + 2];
            Array.Copy(packet, finalPacket, packet.Length);
            finalPacket[packet.Length] = (byte)chk;
            finalPacket[packet.Length + 1] = 0x0D;
#if DEBUG
            //CrestronConsole.Print("NEC Tx: ");
            //Tools.PrintBytes(finalPacket, finalPacket.Length);
#endif

            this.ComPort.Send(finalPacket, finalPacket.Length);
        }

        protected object ReceiveBufferProcess(object obj)
        {
            Byte[] bytes = new Byte[1000];
            int byteIndex = 0;

            while (true)
            {
                try
                {
                    byte b = RxQueue.Dequeue();

                    // If find byte = CR
                    if (b == 13)
                    {
                        // Copy bytes to new array with length of packet and ignoring the CR.
                        Byte[] copiedBytes = new Byte[byteIndex];
                        Array.Copy(bytes, copiedBytes, byteIndex);

                        byteIndex = 0;

                        int chk = 0;

                        for (int i = 1; i < (copiedBytes.Length - 1); i++)
                        {
                            chk = chk ^ copiedBytes[i];
                        }

                        if (chk == (int)copiedBytes.Last())
                        {
#if DEBUG
                            //CrestronConsole.Print("NEC Rx: ");
                            //Tools.PrintBytes(copiedBytes, copiedBytes.Length);
#endif
                            if (ReceivedPacket != null)
                                ReceivedPacket(this, copiedBytes);
                            CrestronEnvironment.AllowOtherAppsToRun();
                        }
                        else
                        {
#if DEBUG
                            CrestronConsole.PrintLine("NEC Display Rx - Checksum Error");
#endif
                            ErrorLog.Error("NEC Display Rx - Checksum Error");
                        }
                    }
                    else
                    {
                        bytes[byteIndex] = b;
                        byteIndex++;
                    }
                }
                catch (Exception e)
                {
                    if (e.Message != "ThreadAbortException")
                        ErrorLog.Exception("Error in NEC Rx Thread Handler", e);
                }
            }
        }
    }

    public delegate void NecComPortReceivedPacketEventHandler(NecComPortHandler handler, byte[] receivedPacket);
}