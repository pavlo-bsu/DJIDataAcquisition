using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pavlo.DJIDAcquisition.Model
{
    /// <summary>
    /// represents information about DJI drone "event"
    /// </summary>
    public class DJIRecord
    {
        int _ID;
        /// <summary>
        /// Event ID
        /// </summary>
        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        System.DateTime _Date = System.DateTime.Now;
        /// <summary>
        /// Time when event was occured
        /// </summary>
        public System.DateTime Date
        {
            get { return _Date; }
            set { _Date = value; }
        }

        string _Description = string.Empty;
        /// <summary>
        /// Description of event
        /// </summary>
        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        string _Type = string.Empty;
        /// <summary>
        /// type of event
        /// </summary>
        public string Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        public DJIRecord()
        { }

        /// <summary>
        /// All the data converted to a string (json template is used)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string str = $"\"ID\":{ID}, \"Date\":\"{Date.Ticks}\", \"Type\":\"{Type}\", \"Description\":\"{Description}\"";
            return str;
        }
    }
}
