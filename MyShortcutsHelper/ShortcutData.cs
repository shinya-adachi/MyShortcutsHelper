using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MyShortcutsHelper
{
    [Serializable]
    public class ShortcutData
    {
        [DllImport("kernel32", EntryPoint = "GlobalAddAtomA")]
        static extern short GlobalAddAtom(string lpString);

        public string AppPath { get; set; }
        public string AppName { get; set; }
        public string AppKey { get; set; }
        public short AppKeyID { get; set; }
        public ShortcutData() { }
        public ShortcutData(string OneLineData)
        {
            string[] splitData = OneLineData.Split(',');
            if (splitData.Count() != 3)
            {
                AppName = "";
                AppPath = "";
                AppKey = "";
                AppKeyID = 0;
            }
            else
            {
                AppPath = splitData[0];
                AppName = splitData[1];
                AppKey = splitData[2];
                AppKeyID = GlobalAddAtom("GlobalHotKey " + AppKey);
            }
            return;
        }
        public ShortcutData(string AppPath, string AppName, string AppKey, short AppKeyID)
        {
            this.AppKey = AppKey;
            this.AppPath = AppPath;
            this.AppName = AppName;
            this.AppKeyID = AppKeyID;
        }
    }
}
