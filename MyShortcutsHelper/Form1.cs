using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Reflection;

namespace MyShortcutsHelper
{

    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        extern static int RegisterHotKey(IntPtr HWnd, int ID, int MOD_KEY, int KEY);


        [DllImport("user32.dll")]
        extern static int UnregisterHotKey(IntPtr HWnd, int ID);

        [DllImport("kernel32",EntryPoint = "GlobalAddAtomA")]
        static extern short GlobalAddAtom(string lpString);
        [DllImport("kernel32")]
        static extern short GlobalDeleteAtom(short nAtom);

        const int MOD_ALT = 0x0001;
        const int MOD_CONTROL = 0x0002;
        const int MOD_SHIFT = 0x0004;
        const int MOD_WIN = 0x0008;
        const int WM_HOTKEY = 0x0009;
        private const string defFileName = "mscHelpConf.xml";
        public List<ShortcutData> ShortcutsGridData;

        public Form1()
        {
            if (!LoadXML(defFileName))
            {
                string pdesktop = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                ShortcutsGridData = new List<ShortcutData>(){
                    new ShortcutData(pdesktop + "\\Tool","CdiDatabaseViewer","W",0),
                    new ShortcutData(pdesktop + "\\Tool","CdiDatabaseViewer","X",0),
                    new ShortcutData(pdesktop + "\\Tool","CdiDatabaseViewer","Y",0)
                };
            }
            InitializeComponent();
            
            ShortcutsGridData.ForEach(RegistShortCut);

            this.Controls.Cast<Control>().Where(c => c.Name != "EditButton" && c.Name != "ResidentModeButton").ToList().ForEach(c => c.Visible = false);
            this.Width = 160;
            this.Height = SystemInformation.CaptionHeight + this.EditButton.Height + this.ResidentModeButton.Height + 10;            
        }

        private void RegistShortCut(ShortcutData sc)
        {
            KeysConverter keyConv = new KeysConverter();
            sc.AppKeyID = GlobalAddAtom("GlobalHotKey " + sc.AppKey);
            RegisterHotKey(this.Handle, sc.AppKeyID, MOD_ALT | MOD_SHIFT, (int)keyConv.ConvertFromString(sc.AppKey));
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            this.Width = 800;
            this.Height = 600;
            this.Controls.Cast<Control>().ToList().ForEach(c => c.Visible = true);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            ShortcutsGridData.ForEach(sc =>UnregisterHotKey(this.Handle, sc.AppKeyID));
            //throw new Exception("TODO");
            notifyIcon.Visible = false;

        }

        private void ShortcutsListDGV_VisibleChanged(object sender, EventArgs e)
        {
            if (ShortcutsListDGV.Visible) setShortcutsListDGV();
        }

        //メモリデータをDGVにセット
        private void setShortcutsListDGV()
        {
            ShortcutsListDGV.Rows.Clear();
            //ShortcutsGridData.ForEach(sc => UnregisterHotKey(this.Handle, sc.AppKeyID));
            //ShortcutsGridData.Clear();
            //Properties.Settings.Default.ShortcutsString.Split("\r\n").
            foreach (var ShortcutData in ShortcutsGridData)
            {
                DataGridViewRow dgvRow = new DataGridViewRow();
                dgvRow.CreateCells(this.ShortcutsListDGV);
                dgvRow.Cells[0].Value = ShortcutData.AppPath;
                dgvRow.Cells[1].Value = ShortcutData.AppName;
                dgvRow.Cells[2].Value = ShortcutData.AppKey;
                var dirPath = Environment.ExpandEnvironmentVariables(ShortcutData.AppPath);
                if (ShortcutData.AppName != "")
                {
                    dgvRow.Cells[3].Value = FormCommonMethod.GetLatestVersionName(dirPath, ShortcutData.AppName);
                    if (dgvRow.Cells[3].Value.ToString() == string.Empty) dgvRow.Cells[3].Value = "★なぞのえらーだよ";
                    if (dgvRow.Cells[3].Value.ToString().IndexOf("★") > -1)
                    {
                        dgvRow.Cells[3].Style.BackColor = Color.FromArgb(244, 44, 44);
                    }
                    else
                    {
                        dgvRow.Cells[3].Style.BackColor = Color.FromArgb(192, 255, 192);
                    }
                }
                else
                {
                    dgvRow.Cells[3].Value = ShortcutData.AppPath.Substring(ShortcutData.AppPath.LastIndexOf(@"\")+1) + "フォルダ";
                    dgvRow.Cells[3].Style.BackColor = Color.FromArgb(192, 255, 192);
                }
                ShortcutsListDGV.Rows.Add(dgvRow);
            }
        }

        private void ReloadButton_Click(object sender, EventArgs e)
        {
            setShortcutsListDGV();
        }

        //DGVをメモリデータにセット(かつローカル記憶領域に保存)
        private void SaveButton_Click(object sender, EventArgs e)
        {

            ShortcutsGridData.ForEach(sc => UnregisterHotKey(this.Handle, sc.AppKeyID));
            ShortcutsGridData.Clear();

            var scList = Enumerable.Range(0, ShortcutsListDGV.Rows.Count)
                .Select(i =>
                {
                    string data(int n) => ShortcutsListDGV.Rows[i].Cells[n].Value.ToString();
                    DataGridViewCell cell(int n) => ShortcutsListDGV.Rows[i].Cells[n];
                    if (cell(1).Value == null) cell(1).Value = "";
                    cell(3).Style.BackColor = Color.White;
                    if (cell(0).Value == null || cell(2).Value == null)
                    {
                        cell(3).Value = "未入力のため未登録";
                        cell(3).Style.BackColor = Color.Red;
                        return null;
                    }

                    if (!Directory.Exists(Environment.ExpandEnvironmentVariables(data(0))))
                    {
                        cell(3).Value = "フォルダが見つからず未登録";
                        cell(3).Style.BackColor = Color.Red;
                        return null;
                    }
                    cell(3).Value = "登録OK";
                    return new ShortcutData(){
                        AppPath = data(0),
                        AppName = data(1),
                        AppKey = data(2)
                    };
                })
                .Where(s => s != null)
                .ToList();
            ShortcutsGridData.AddRange(scList);
            ShortcutsGridData.ForEach(RegistShortCut);
            SaveXML(defFileName, ShortcutsGridData);
        }

        private void ShortcutsListDGV_KeyDown(object sender, KeyEventArgs e)
        {
            if (ShortcutsListDGV.CurrentCell.ColumnIndex == 2)
            {
                ShortcutsListDGV.CurrentCell.Value = e.KeyCode.ToString();
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            //File.WriteAllText(
            //    Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"mscHelpConf.csv")
            //    ,Properties.Settings.Default.ShortcutsString
            //    );
            SaveXML(defFileName, ShortcutsGridData);
            MessageBox.Show($"実行ファイルのフォルダに{defFileName}を作成しました。");
        }

        private void InportButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            ofd.FileName = defFileName;
            ofd.Filter = "xmlファイル(*.xml)|*.xml|すべてのファイル(*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.Title = "設定をインポートするファイルを選択して下さい。";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                
                if (LoadXML(ofd.FileName))
                {
                    setShortcutsListDGV();
                    MessageBox.Show("インポートした設定をツールに保存する場合は、「保存」ボタンを押してください。");
                }
                else
                {
                    MessageBox.Show("なんかしっぱい");
                }
            }
            ofd.Dispose();
        }

        private void ResidentModeButton_Click(object sender, EventArgs e)
        {
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
            this.Hide();

            contextMenu.Items.Clear();
            ShortcutsGridData.ForEach(sc => {
                contextMenu.Items.Add(sc.AppName != "" ? sc.AppName : $"{sc.AppPath.Substring(sc.AppPath.LastIndexOf(@"\") + 1)}フォルダ");
            });

            contextMenu.Items.Add("終了", null, new EventHandler(ExitForm));
            contextMenu.Items[contextMenu.Items.Count-1].Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            notifyIcon.Visible = true;
            
        }

        private void ExitForm(object sender, EventArgs e) => this.Close();

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            //ApplicationExitイベントハンドラを削除
            Application.ApplicationExit -= new EventHandler(Application_ApplicationExit);
            if (notifyIcon != null) notifyIcon.Visible = false;
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            notifyIcon.Visible = false;
        }

        private void contextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var scList = ShortcutsGridData.Where(sc =>
                 sc.AppName == e.ClickedItem.Text ||
                 (sc.AppName == "" && sc.AppPath.Substring(sc.AppPath.LastIndexOf(@"\") + 1) + "フォルダ" == e.ClickedItem.Text));
            ExecShortCut(scList);
        }

        private void ExecShortCut(IEnumerable<ShortcutData> scList)
        {
            scList.ToList().ForEach(ExecShortCut);
        }

        private void ExecShortCut(ShortcutData sc)
        {
            var dirPath = Environment.ExpandEnvironmentVariables(sc.AppPath);
            if (sc.AppName != "")
            {
                string StartFileName = "";
                StartFileName = FormCommonMethod.GetLatestVersionName(dirPath, sc.AppName);
                string path = Path.Combine(dirPath, StartFileName);
                if (File.Exists(path))
                {
                    System.Diagnostics.Process.Start(path);
                }
            }
            else
            {
                if (Directory.Exists(dirPath))
                {
                    System.Diagnostics.Process.Start(dirPath);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            short mwprm = (short)m.WParam;
            ExecShortCut(ShortcutsGridData.Where(shtkey => shtkey.AppKeyID == mwprm));
            //foreach(var ShortcutData in )
            //{
            //    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
            //    psi.WorkingDirectory = ShortcutData.AppPath;
            //    //psi.UserName = "nagano";
            //    //var sc = new System.Security.SecureString();
            //    //foreach (char c in "552519Yui") sc.AppendChar(c);
            //    //psi.Password = sc;
            //    psi.UseShellExecute = false;
            //    if (ShortcutData.AppName != "")
            //    {
            //        string StartFileName = "";
            //        StartFileName = FormCommonMethod.GetLatestVersionName(ShortcutData.AppPath, ShortcutData.AppName);
            //        if (File.Exists(ShortcutData.AppPath + @"\" + StartFileName))
            //        {
            //            psi.FileName = StartFileName;
            //            System.Diagnostics.Process.Start(psi);
            //        }
            //    }
            //    else
            //    {
            //        if (Directory.Exists(ShortcutData.AppPath))
            //        {
            //            System.Diagnostics.Process.Start(ShortcutData.AppPath);
            //        }
            //    }
                
            //}
        }

        private bool LoadXML(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    XmlSerializer xs = new XmlSerializer(typeof(List<ShortcutData>));
                    using (FileStream fs = File.OpenRead(path))
                    {
                        ShortcutsGridData = (xs.Deserialize(fs) as List<ShortcutData>);
                    }
                    return ShortcutsGridData != null;
                }
                return false;
            }
            catch (Exception e)
            {
                Program.ShowError(e, "LoadXML");
                return false;
            }
        }
        private bool SaveXML(string path, List<ShortcutData> scList)
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<ShortcutData>));
                if (File.Exists(path)) File.Delete(path);
                if (!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(path)))) return false;

                using (FileStream fs = File.OpenWrite(path))
                {
                    xs.Serialize(fs, scList);
                }
                return true;
            }
            catch(Exception e)
            {
                Program.ShowError(e, "SaveXML");
                return false;
            }

        }
    }
}
