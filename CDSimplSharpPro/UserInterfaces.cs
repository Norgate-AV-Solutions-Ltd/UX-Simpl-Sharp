﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Crestron.SimplSharpPro;

namespace CDSimplSharpPro
{
    public class UserInterfaces : Dictionary<uint, UserInterface>
    {
        public UserInterfaces()
            : base()
        {
            
        }

        public void Add(UserInterface ui)
        {
            base.Add(ui.ID, ui);
        }

        public void Add(CrestronControlSystem controlSystem, uint id, uint ipID, string type, string name, Room defaultRoom)
        {
            UserInterface newUI = new UserInterface(controlSystem, id, ipID, type, defaultRoom);
            newUI.Name = name;

            if (!this.ContainsKey(id))
            {
                base.Add(newUI.ID, newUI);
            }
        }
    }
}