using System;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;

namespace MyShortcutsHelper
{
    static partial class FormCommonMethod
    {
        public static Control[] GetAllControls(this Control top)
        {
            ArrayList buf = new ArrayList();
            foreach (Control c in top.Controls)
            {
                buf.Add(c);
                buf.AddRange(GetAllControls(c));
            }
            return (Control[])buf.ToArray(typeof(Control));
        }

        public static string DoDosCommand(string Arguments, bool IsConsoleApp, bool GetResult)
        {
            System.Diagnostics.ProcessStartInfo psi =
                new System.Diagnostics.ProcessStartInfo();

            //ComSpecのパスを取得する
            psi.FileName = System.Environment.GetEnvironmentVariable("ComSpec");

            psi.UseShellExecute = !IsConsoleApp;
            psi.RedirectStandardInput = false;
            psi.RedirectStandardOutput = GetResult;
            //ウィンドウを表示しないようにする
            psi.CreateNoWindow = true;
            //コマンドラインを指定（"/c"は実行後閉じるために必要）
            psi.Arguments = Arguments;
            //起動
            System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
            //出力を読み取る
            string results;
            if (GetResult)
            {
                results = p.StandardOutput.ReadToEnd();
                results = results.Replace("\n", "\r\n");
            }
            else
            {
                results = "";
            }
            //WaitForExitはReadToEndの後である必要がある
            //(親プロセス、子プロセスでブロック防止のため)
            p.WaitForExit();

            //出力された結果を表示
            return results;
        }

        public static bool VersionCompare(string tmpVersion, string CurrentVersion)
        {
            int tmpVersNumOfOcted = tmpVersion.Split('.').Count();
            int CurrentVersNumOfOcted = CurrentVersion.Split('.').Count();
            //少ない方のオクテッド数に合わせる
            int NumOfOcted = Math.Min(CurrentVersNumOfOcted, tmpVersNumOfOcted);
            for (int OctedCnt = 0; OctedCnt < NumOfOcted; OctedCnt++)
            {
                if (int.Parse(tmpVersion.Split('.')[OctedCnt]) > int.Parse(CurrentVersion.Split('.')[OctedCnt]))
                {
                    return true;
                }
                else if (int.Parse(tmpVersion.Split('.')[OctedCnt]) < int.Parse(CurrentVersion.Split('.')[OctedCnt]))
                {
                    return false;
                }
            }

            if (tmpVersNumOfOcted > CurrentVersNumOfOcted)
            {
                return true;
            }
            if (tmpVersNumOfOcted < CurrentVersNumOfOcted)
            {
                return false;
            }
            return true;
        }


        // <summary>
        /// ファイルパスのバージョンと現在のバージョンを比較し、最新のバージョンを取得する。
        /// </summary>
        /// <param name="FilePath">対象ファイルパス</param>
        /// <param name="CurrentVersion">現在のバージョン</param>
        /// <param name="ListCnt"></param>
        /// <returns>最新バージョン</returns>
        public static string GetLatestVersionName(string FilePath, string FileNameOrg)
        {
            int checkCounter = 0;
            List<string> TargetFileList;
            #region フォルダ内容ゲット
            do
            {
                if (Directory.Exists(FilePath))
                {
                    // dirPathのディレクトリは存在する
                    TargetFileList = new List<string>(Directory.GetFiles(FilePath).Where(
                        FileName =>
                            Path.GetFileNameWithoutExtension(FileName).IndexOf(FileNameOrg) == 0
                            && Path.GetExtension(FileName).ToLower() != ".zip"
                            ));
                    break;
                }
                else
                {
                    // dirPathのディレクトリは存在しない
                    if (checkCounter == 20)
                    {
                        return "★パスが見つからないよ";
                    }
                    else
                    {
                        checkCounter++;
                    }
                }
            } while (true);

            if (TargetFileList.Count() == 0) return "★ファイルが見つからないよ";

            #endregion

            string HighestVersion = "";
            string HighestVersionName = "";
            string tmpVersion;
            string tmpFileName;
            System.Diagnostics.FileVersionInfo vi;
            foreach (var tmpFilePath in TargetFileList)
            {
                tmpVersion = "";
                tmpFileName = Path.GetFileName(tmpFilePath);
                vi = null;
                if (tmpFileName.ToLower().IndexOf(".bat") > -1 || tmpFileName.ToLower().IndexOf(".lnk") > -1)
                {
                    tmpVersion = "1.0.0";
                }
                if (tmpFileName.ToLower().IndexOf(".exe") > -1)
                {
                    vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(tmpFilePath);
                    if (vi.FileVersion != null)
                    {
                        tmpVersion = vi.FileVersion;
                    }

                }

                if (tmpVersion == "")
                {
                    int LenCnt;
                    int tmpNumber;
                    #region ファイル名からバージョン抽出
                    for (LenCnt = 0; LenCnt < tmpFileName.Length; LenCnt++)
                    {
                        if (int.TryParse(tmpFileName.Substring(LenCnt, 1), out tmpNumber))
                        {
                            tmpVersion = tmpVersion + tmpNumber;
                        }
                        else if (tmpVersion != "")
                        {
                            if (tmpFileName.Substring(LenCnt, 1) == ".")
                            {
                                if (tmpVersion.Substring(tmpVersion.Length - 1) != ".")
                                {
                                    tmpVersion = tmpVersion + ".";
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    #endregion

                    #region 余分なピリオド削除
                    do
                    {
                        if (tmpVersion.Length >= 1)
                        {
                            if (tmpVersion.Substring(tmpVersion.Length - 1) == ".")
                            {
                                tmpVersion = tmpVersion.Substring(0, tmpVersion.Length - 1);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    } while (true);
                    #endregion
                }

                if (tmpVersion != "" && tmpFileName.IndexOf("~$") == -1)
                {
                    #region 戻り値設定
                    //比較元より新しいバージョンならFilePathを返す。(もし、Server確認ならば、ファイルパスもメモしておく)
                    if (HighestVersion == "")
                    {
                        HighestVersion = tmpVersion;
                        HighestVersionName = tmpFileName;
                    }
                    else if (HighestVersion == tmpVersion)
                    {
                        if (File.GetCreationTime(FilePath + @"\" + HighestVersionName) <
                            File.GetCreationTime(tmpFilePath))
                        {
                            HighestVersion = tmpVersion;
                            HighestVersionName = tmpFileName;
                        }
                    }
                    else if (VersionCompareWithFilePath(tmpVersion, HighestVersion, tmpFilePath, FilePath + @"\" + HighestVersionName))
                    {
                        HighestVersion = tmpVersion;
                        HighestVersionName = tmpFileName;
                    }
                    #endregion
                }
            }

            return HighestVersionName;

        }
        public static bool VersionCompareWithFilePath(string tmpVersion, string CurrentVersion, string tmpFilePath, string CurrentFilePath)
        {

            int tmpVersNumOfOcted = tmpVersion.Split('.').Count();
            int CurrentVersNumOfOcted = CurrentVersion.Split('.').Count();
            int NumOfOcted = 0;
            //少ない方のオクテッド数に合わせる
            if (tmpVersNumOfOcted > CurrentVersNumOfOcted)
            {
                NumOfOcted = CurrentVersNumOfOcted;
            }
            else
            {
                NumOfOcted = tmpVersNumOfOcted;
            }

            for (int OctedCnt = 0; OctedCnt < NumOfOcted; OctedCnt++)
            {
                if (int.Parse(tmpVersion.Split('.')[OctedCnt]) > int.Parse(CurrentVersion.Split('.')[OctedCnt]))
                {
                    return true;
                }
                else if (int.Parse(tmpVersion.Split('.')[OctedCnt]) < int.Parse(CurrentVersion.Split('.')[OctedCnt]))
                {
                    return false;
                }
            }

            if (tmpVersNumOfOcted > CurrentVersNumOfOcted)
            {
                return true;
            }
            if (tmpVersNumOfOcted < CurrentVersNumOfOcted)
            {
                return false;
            }
            if (File.GetLastWriteTime(tmpFilePath) > File.GetLastWriteTime(CurrentFilePath))
            {
                return true;
            }
            else
            {
                return false;
            }

        }
    }
    
    
}
