﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace UXLib.Devices.VC.Cisco
{
    public class PhonebookSearchResults : IEnumerable<IPhonebookItem>
    {
        public PhonebookSearchResults(bool error)
        {
            StatusOk = false;
            this.Results = new List<IPhonebookItem>();
        }

        public PhonebookSearchResults(IEnumerable<IPhonebookItem> items, int offset, int limit)
        {
            this.Results = new List<IPhonebookItem>(items
                .OrderBy(i => i.Name)
                .OrderBy(i => i.ItemType == PhonebookItemType.Contact));
            Offset = offset;
            Limit = limit;
        }

        List<IPhonebookItem> Results;
        public bool StatusOk = true;
        public int Offset { get; protected set; }
        public int Limit { get; protected set; }

        public IPhonebookItem this[int index]
        {
            get
            {
                return Results[index];
            }
        }

        public IPhonebookItem this[string id]
        {
            get
            {
                return Results.Where(e => e.ID == id).FirstOrDefault();
            }
        }

        public int Count { get { return Results.Count; } }

        #region IEnumerable<PhonebookContact> Members

        public IEnumerator<IPhonebookItem> GetEnumerator()
        {
            return this.Results.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}