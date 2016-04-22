﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXmlLinq;
using Crestron.SimplSharpPro.CrestronThread;

namespace UXLib.Devices.VC.Cisco
{
    public class CiscoCodec
    {
        public CiscoCodec(string hostNameOrIPAddress, string username, string password, EthernetAdapterType ethernetAdapter, int feedbackListenerPort)
        {
            HttpClient = new CodecHTTPClient(hostNameOrIPAddress, username, password);
            FeedbackServer = new CodecFeedbackServer(this, ethernetAdapter, feedbackListenerPort);
            FeedbackServer.ReceivedData += new CodecFeedbackServerReceiveEventHandler(FeedbackServer_ReceivedData);
            this.password = password;
            KeyboardInteractiveConnectionInfo sshInfo = new KeyboardInteractiveConnectionInfo(hostNameOrIPAddress, 22, username);
            sshInfo.AuthenticationPrompt += new EventHandler<Crestron.SimplSharp.Ssh.Common.AuthenticationPromptEventArgs>(sshInfo_AuthenticationPrompt);
            SSHClient = new CodecSSHClient(sshInfo);
            SSHClient.OnConnect += new CodecSSHClientConnectedEventHandler(SSHClient_OnConnect);
            CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
            SystemUnit = new SystemUnit(this);
            SystemUnit.State.SystemStateChange += new SystemUnitStateSystemChangeEventHandler(State_SystemStateChange);
            Audio = new Audio(this);
            Calls = new Calls(this);
            Conference = new Conference(this);
            Network = new Network(this);
            Phonebook = new Phonebook(this);
        }

        CodecHTTPClient HttpClient { get; set; }
        public CodecFeedbackServer FeedbackServer { get; protected set; }
        CodecSSHClient SSHClient { get; set; }
        public SystemUnit SystemUnit { get; private set; }
        public Audio Audio { get; private set; }
        public Calls Calls { get; private set; }
        public Conference Conference { get; private set; }
        public Network Network { get; private set; }
        public Phonebook Phonebook { get; private set; }
        Thread CheckStatus { get; set; }

        public void Initialize()
        {
            

            SSHClient.Connect();
        }

        public void Registerfeedback()
        {
            this.FeedbackServer.Register(1, new string[] {
                "/Configuration",
                "/Status/SystemUnit",
                "/Status/Audio",
                "/Status/Standby",
                "/Status/Video/Input",
                "/Status/Video/Selfview",
                "/Status/Cameras/SpeakerTrack",
                "/Event/IncomingCallIndication",
                "/Status/Call",
                "/Status/Conference"
            });
        }

        public event CodecConnectedEventHandler HasConnected;

        void SSHClient_OnConnect(CodecSSHClient client)
        {
            new Thread(GetStatusThread, null, Thread.eThreadStartOptions.Running);
            CheckStatus = new Thread(CheckStatusThread, null, Thread.eThreadStartOptions.Running);
        }

        object GetStatusThread(object threadObject)
        {
            Thread.Sleep(5000);
#if DEBUG
            CrestronConsole.PrintLine("\r\nCodec connected... getting status updates... \r\n");
#endif
            string standbyStatus = RequestPath("Status/Standby", true).FirstOrDefault().Elements().FirstOrDefault().Value;
            if (standbyStatus == "On") _StandbyActive = true;
            else _StandbyActive = false;

            bool registered = this.FeedbackServer.Registered;

#if DEBUG
            CrestronConsole.PrintLine("Standby = {0}", StandbyActive);
            CrestronConsole.PrintLine("Feedback Registered = {0}", registered);
#endif
            if (!registered)
            {
#if DEBUG
                CrestronConsole.PrintLine("Registering Feedback");
#endif
                this.Registerfeedback();
            }

            if (HasConnected != null)
                HasConnected(this);

            return null;
        }

        object CheckStatusThread(object threadObject)
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(60000);

                    bool registered = this.FeedbackServer.Registered;

#if DEBUG
                    CrestronConsole.PrintLine("Feedback Registered = {0}", registered);
#endif
                    if (!registered)
                    {
#if DEBUG
                        CrestronConsole.PrintLine("Registering Feedback");
#endif
                        this.Registerfeedback();
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Exception("Error in CiscoCodec.CheckStatusThread", e);
                }
            }
        }

        string password;
        void sshInfo_AuthenticationPrompt(object sender, Crestron.SimplSharp.Ssh.Common.AuthenticationPromptEventArgs e)
        {
            foreach (var prompt in e.Prompts)
            {
                if (prompt.Request.Equals("Password: ", StringComparison.InvariantCultureIgnoreCase))
                {
                    prompt.Response = password;
                }
            }
        }

        void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            CheckStatus.Abort();
            if (FeedbackServer.Active)
                FeedbackServer.Active = false;
            if (SSHClient.IsConnected)
                SSHClient.Disconnect();
        }

        public XDocument SendCommand(string path)
        {
            return this.SSHClient.SendCommand(path);
        }

        public XDocument SendCommand(string path, bool useHttp)
        {
            if (useHttp)
                return this.HttpClient.SendCommand(path);
            return this.SSHClient.SendCommand(path);
        }

        public XDocument SendCommand(string path, CommandArgs args)
        {
            return SSHClient.SendCommand(path, args);
        }

        public XDocument SendCommand(string path, CommandArgs args, bool useHttp)
        {
            if (useHttp)
                return HttpClient.SendCommand(path, args);
            return SSHClient.SendCommand(path, args);
        }

        public IEnumerable<XElement> RequestPath(string path)
        {
            return SSHClient.RequestPath(path);
        }

        public IEnumerable<XElement> RequestPath(string path, bool useHttp)
        {
            if (useHttp)
                return HttpClient.RequestPath(path);
            return SSHClient.RequestPath(path);
        }

        public CallHistory GetCallHistory(int count)
        {
            return new CallHistory(this, count);
        }

        public void SoundPlayBump()
        {
            SoundPlay("Bump", false);
        }

        public void SoundPlay(string sound, bool loop)
        {
            CommandArgs args = new CommandArgs("Sound", sound);
            if (loop) args.Add("Loop", "On");
            else args.Add("Loop", "Off");
            SendCommand("Audio/Sound/Play", args);
        }

        public void SoundStop()
        {
            SendCommand("Audio/Sound/Stop");
        }

        public void PresentationStart()
        {
            SendCommand("Presentation/Start");
        }

        public void PresentationStart(int presentationSource)
        {
            PresentationStart(presentationSource, PresentationSendingMode.LocalRemote);
        }

        public void PresentationStart(int presentationSource, PresentationSendingMode sendingMode)
        {
            CommandArgs args = new CommandArgs("PresentationSource", presentationSource);
            args.Add("SendingMode", sendingMode.ToString());

            SendCommand("Presentation/Start", args);
        }

        public void PresentationStop()
        {
            SendCommand("Presentation/Stop");
        }

        public bool _StandbyActive;
        public bool StandbyActive
        {
            get { return _StandbyActive; }
            set
            {
                if (value)
                    SendCommand("Standby/Activate");
                else
                    SendCommand("Standby/Deactivate");
            }
        }

        public void Sleep() { this.StandbyActive = true; }
        public void Wake() { this.StandbyActive = false; }

        public event CodecStandbyChangeEventHandler StandbyChanged;

        void OnStandbyChange(bool value)
        {
            if (StandbyChanged != null)
                StandbyChanged(this, StandbyActive);
        }

        void State_SystemStateChange(CiscoCodec Codec, SystemState State)
        {
            if (State == SystemState.Initialized && !SSHClient.IsConnected)
                SSHClient.Connect();
        }
        
        void FeedbackServer_ReceivedData(CodecFeedbackServer server, CodecFeedbackServerReceiveEventArgs args)
        {
            switch (args.Path)
            {
                case @"Status/Standby":
                    switch (args.Data.Elements().Where(e => e.XName.LocalName == "Active").FirstOrDefault().Value)
                    {
                        case "Off": _StandbyActive = false; break;
                        case "On": _StandbyActive = true; break;
                    }
                    OnStandbyChange(_StandbyActive);
                    break;
                default:
                    //CrestronConsole.PrintLine("Feedback for path: {0}", args.Path);
                    //CrestronConsole.PrintLine(args.Data.ToString());
                    break;
            }
        }
    }

    public delegate void CodecStandbyChangeEventHandler(CiscoCodec codec, bool StandbyActive);

    public delegate void CodecConnectedEventHandler(CiscoCodec codec);

    public enum PresentationSendingMode
    {
        Off,
        LocalRemote,
        LocalOnly
    }
}