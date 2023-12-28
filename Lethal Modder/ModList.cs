using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Lethal_Modder
{
    public partial class ModList : Form
    {
        // just gonna redo everything, it'd be easier for both dev and user :)
        #region Globals

        public class ModItem
        {
            public ModItem(string Display, string Url, string Author, string[] dependencies)
            {
                this.Display = Display;
                this.Url = Url;
                this.Author = Author;
                this.Dependencies = dependencies;
            }

            public string Display;
            public string Url;
            public string Author;
            public string[] Dependencies;
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
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

            // makes it super easy to add more :)
            InstalledModList.Add(new ModItem("LC API", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LC_API.zip", "2018", new string[]{  } ));
            InstalledModList.Add(new ModItem("LethalLib", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LethalLib.zip", "Evaisa", new string[] { "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/HookGenPatcher.zip" }));
            InstalledModList.Add(new ModItem("HookGenPatcher", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/HookGenPatcher.zip", "Evaisa", new string[] { }));
            
            InstalledModList.Add(new ModItem("More Company", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/MoreCompany.zip", "notnotnotswipez", new string[] {  }));
            InstalledModList.Add(new ModItem("Late Company", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LateCompany.zip", "anormaltwig", new string[] {  }));
            InstalledModList.Add(new ModItem("More Suits",   "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/MoreSuits.zip", "x753", new string[] {  }));
            InstalledModList.Add(new ModItem("More Emotes",  "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/MoreEmotes.zip", "Sligili", new string[] {  }));
            InstalledModList.Add(new ModItem("Additional Suits", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/AdditionalSuits.zip", "AlexCodesGames", new string[] { }));
            InstalledModList.Add(new ModItem("Hide Chat", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/HideChat.zip", "Monkeytype", new string[] { }));
            InstalledModList.Add(new ModItem("Brutal Company Plus", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/BrutalCompanyPlus.zip", "Nips", new string[] { }));
            InstalledModList.Add(new ModItem("Employee Assignments", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/EmployeeAssignments.zip", "amnsoft", new string[] { }));
            InstalledModList.Add(new ModItem("More (storage) Items", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/MoreItems.zip", "Drakorle", new string[] { }));
            InstalledModList.Add(new ModItem("Lategame Upgrade", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LategameUpgrades.zip", "malco", new string[] { "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LethalLib.zip", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/HookGenPatcher.zip" }));
            InstalledModList.Add(new ModItem("Helmet Cameras", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/HelmetCameras.zip", "RickArg", new string[] {  }));
            InstalledModList.Add(new ModItem("LetMeLookDown", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LetMeLookDown.zip", "FlipMods", new string[] { }));
            InstalledModList.Add(new ModItem("LCBetterSaves", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LCBetterSaves.zip", "Pooble", new string[] { }));
            InstalledModList.Add(new ModItem("Weather Multipliers", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/WeatherMultipliers.zip", "Blorb", new string[] { }));
            InstalledModList.Add(new ModItem("Lethal Escape", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LethalEscape.zip", "xCeezy", new string[] { }));

            InstalledModList.Add(new ModItem("Yippie Hoarders", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/YippieMod.zip", "sunnobunno", new string[] {  }));
            InstalledModList.Add(new ModItem("Lethal Things", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LethalThings.zip", "Evaisa", new string[] { "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LethalLib.zip", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/HookGenPatcher.zip" }));
            InstalledModList.Add(new ModItem("Youtube Boombox", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/YoutubeBoombox.zip", "steven4547466", new string[] { "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LC_API.zip" }));
            InstalledModList.Add(new ModItem("Landmine Fart Reverb", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LandmineFartReverb.zip", "sunnobunno", new string[] {  }));
            InstalledModList.Add(new ModItem("Buyable Shotgun Shells", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/BuyableShotgunShells.zip", "MegaPiggy", new string[] { "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LethalLib.zip", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/HookGenPatcher.zip" }));
            InstalledModList.Add(new ModItem("Mirror Decor", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/MirrorDecor.zip", "quackandcheese", new string[] { "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/LethalLib.zip", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/HookGenPatcher.zip" }));
            InstalledModList.Add(new ModItem("Jump Delay Patch", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/JumpDelayPatch.zip", "monkes_mods", new string[] {  }));
            InstalledModList.Add(new ModItem("1000 Quota Stare", "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/ThousandQuotaStare.zip", "ManiaBania", new string[] { "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/VerifiedMods/MoreSuits.zip" }));
            // InstalledModList.Add(new ModItem("name", "url", "author", new string[] { }));
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
                    DownloadMod(mod.Url);

                    if (mod.Dependencies.Length > 0)
                        foreach (string dep in mod.Dependencies)
                            DownloadMod(dep);

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

        private void GetFromFile_Click(object sender, EventArgs e)
        {
            CheckInstallation();
            
            using (OpenFileDialog diag = new OpenFileDialog())
            {
                diag.InitialDirectory = @"c:\\";
                diag.Filter = "mods files (*.mods)|*.mods";
                diag.FilterIndex = 2;
                diag.RestoreDirectory = true;

                if (diag.ShowDialog() == DialogResult.OK)
                {
                    var lines = File.ReadLines(diag.FileName);
                 
                    foreach (string line in lines)
                        DownloadMod(line);
                }
            }
        }

        #region Utils

        private void DownloadMod(string url)
        {
            using (WebClient client = new WebClient())
                client.DownloadFile(url, GameLocation + "/DownloadedMod.zip");

            if (File.Exists(GameLocation + "/DownloadedMod.zip"))
                OutputLog.Items.Add("Loaded Mod!");

            else
                OutputLog.Items.Add("Failed install.");

            // ZipFile.ExtractToDirectory(GameLocation + "/DownloadedMod.zip", GameLocation);

            using (var strm = File.OpenRead(GameLocation + "/DownloadedMod.zip"))
                using (ZipArchive a = new ZipArchive(strm))
                    ExtractToDirectoryOverwrite(a, GameLocation, true);

            File.Delete(GameLocation + "/DownloadedMod.zip");
        }

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

        private void ExtractToDirectoryOverwrite(ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }

            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;

            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                }

                if (file.Name == "")
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }

                file.ExtractToFile(completeFileName, true);
            }
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
