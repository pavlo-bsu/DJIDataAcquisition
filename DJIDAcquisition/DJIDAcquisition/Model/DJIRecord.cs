using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pavlo.DJIDAcquisition.Model
{
    public class DJIRecord
    {
        int _ID;
        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        System.DateTime _Date = System.DateTime.Now;
        public System.DateTime Date
        {
            get { return _Date; }
            set { _Date = value; }
        }

        string _Description = string.Empty;
        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        public DJIRecord()
        { }
    }
}
