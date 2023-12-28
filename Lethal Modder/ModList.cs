using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lethal_Modder
{
    public partial class ModList : Form
    {
        // just gonna redo everything, it'd be easier for both dev and user :)
        #region Globals

        public class ModItem
        {
            public ModItem(string Display, string Url, string Author)
            {
                this.Display = Display;
                this.Url = Url;
                this.Author = Author;
            }

            public string Display;
            public string Url;
            public string Author;
        }

        FileSystemWatcher watcher;

        private bool IsInstalled = false;

        private string GameLocation = "";
        private string RelativeGameLocation = "/steamapps/common/Lethal Company";
        private string SteamLocation = "";
        private string[] SteamLocations = { @"C:/Program Files/Steam", @"C:/Program Files (x86)/Steam", @"D:/Program Files/Steam", @"D:/Program Files (x86)/Steam" };

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public List<ModItem> InstalledModList = new List<ModItem>();

        #endregion
        #region Imports

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // width of ellipse
            int nHeightEllipse // height of ellipse
        );

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        #endregion

        public ModList()
        {
            InitializeComponent();

            // makes it super easy to add more :)
            InstalledModList.Add(new ModItem("LC API",       "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LC_API.zip", "2018"));
            InstalledModList.Add(new ModItem("More Company", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/MoreCompany.zip", "notnotnotswipez"));
            InstalledModList.Add(new ModItem("Late Company", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LateCompany.zip", "anormaltwig"));
            InstalledModList.Add(new ModItem("More Suits",   "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/MoreSuits.zip", "x753"));
            InstalledModList.Add(new ModItem("More Emotes",  "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/MoreEmotes.zip", "Sligili"));
        }

        private void ModList_Load(object sender, EventArgs e)
        {
            OutputLog.Items.Add("Loaded Window!");

            foreach (string file_location in SteamLocations)
            {
                if (Directory.Exists(file_location))
                {
                    SteamLocation = file_location;
                    OutputLog.Items.Add("Found Steam!");

                    if (Directory.Exists(file_location + RelativeGameLocation))
                    {
                        GameLocation = file_location + RelativeGameLocation;
                        OutputLog.Items.Add("Found LC!");
                    }

                    else
                        OutputLog.Items.Add("Unable to find LC.");

                    break;
                }
            }

            if (SteamLocation == "")
                OutputLog.Items.Add("Unable to find Steam.");

            IsInstalled = Directory.Exists(GameLocation + "/BepInEx");
            UpdateStatus();

            if (IsInstalled)
                UpdateModList();
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            string selected = InstalledMods.GetItemText(InstalledMods.SelectedItem);

            foreach (ModItem mod in InstalledModList)
            {
                if (string.Format("{0}: {1}", mod.Display, mod.Author) == selected)
                {
                    CheckInstallation();

                    using (WebClient client = new WebClient())
                        client.DownloadFile(mod.Url, GameLocation + "/DownloadedMod.zip");

                    if (File.Exists(GameLocation + "/DownloadedMod.zip"))
                        OutputLog.Items.Add("Loaded Mod!");

                    else
                        OutputLog.Items.Add("Failed install.");

                    ZipFile.ExtractToDirectory(GameLocation + "/DownloadedMod.zip", GameLocation);
                    File.Delete(GameLocation + "/DownloadedMod.zip");

                    IsInstalled = Directory.Exists(GameLocation + "/BepInEx");
                    UpdateStatus();


                    break;
                }
            }
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = GameLocation + "\\Lethal Company.exe", UseShellExecute = true });
        }


        #region Utils

        private void UpdateModList()
        {
            InstalledMods.Items.Clear();

            if (!Directory.Exists(GameLocation + "/BepInEx") || !Directory.Exists(GameLocation + "/BepInEx/plugins"))
                return;

            foreach (ModItem mod in InstalledModList)
                InstalledMods.Items.Add(string.Format("{0}: {1}", mod.Display, mod.Author));
        }

        private void CheckInstallation()
        {
            if (GameLocation == "")
            {
                MessageBox.Show("Cannot find LC installation! Please select the game's exe.");

                using (OpenFileDialog diag = new OpenFileDialog())
                {
                    diag.InitialDirectory = @"c:\\";
                    diag.Filter = "exe files (*.exe)|*.exe";
                    diag.FilterIndex = 2;
                    diag.RestoreDirectory = true;

                    if (diag.ShowDialog() == DialogResult.OK)
                        GameLocation = diag.FileName.Replace("\\Lethal Company.exe", "");
                }
            }
        }

        private void UpdateStatus()
        {
            StatusLabel.Text = IsInstalled ? "Enabled" : "Disabled";
            StatusLabel.ForeColor = IsInstalled ? Color.LightGreen : Color.IndianRed;
        }

        #endregion
        #region Window Handlers

        private void ExitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        private void ModList_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        #endregion
    }
}
