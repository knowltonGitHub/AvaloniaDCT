using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCT.Classes
{
    public static class Helper
    {        
        public static void CreateCopyOfCurrentDB(int rowcount, string message)
        {
            DateTime dt = new DateTime();
            dt = DateTime.Now;
            string Year = dt.Year.ToString();
            string Month = CreateDoubleDigit(dt.Month);
            string Day = CreateDoubleDigit(dt.Day);
            string Hour = CreateDoubleDigit(dt.Hour);
            string Minute = CreateDoubleDigit(dt.Minute);
            string Second = CreateDoubleDigit(dt.Second);

            string tempfname = "CBDB" +
                Year + Month + Day + Hour + Minute + Second + "_" + message + "_" + rowcount.ToString() + ".db";

            System.IO.File.Copy("CBDB.db", tempfname);

            System.IO.File.SetLastWriteTime(tempfname, dt);
        }

        private static string CreateDoubleDigit(int digit)
        {
            string temp = digit < 10 ? "0" + digit.ToString() : digit.ToString();

            return temp;
        }
    }

    
}
