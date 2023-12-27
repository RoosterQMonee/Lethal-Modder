using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace Lethal_Modder
{
    public partial class LethalModder : Form
    {
        #region Globals

        FileSystemWatcher watcher;
        IDE ModEditor;

        private bool IsInstalled = false;
        private bool IsAwaiting = false;

        private string BepInExURL = "https://github.com/RoosterQMonee/Lethal-Modder/raw/main/Dependencies/BepInEx-5.4.2100.zip";
        private string GameLocation = "";
        private string RelativeGameLocation = "/steamapps/common/Lethal Company";
        private string SteamLocation = "";
        private string[] SteamLocations = { @"C:/Program Files/Steam", @"C:/Program Files (x86)/Steam", @"D:/Program Files/Steam", @"D:/Program Files (x86)/Steam" };
        
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

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

        public LethalModder()
        {
            InitializeComponent();
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
        }

        private void Form1_Load(object sender, EventArgs e)
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

            IsAwaiting = IsInstalled;

            if (IsAwaiting)
                WatchModFolder();
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            CheckInstallation();

            using (WebClient client = new WebClient())
                client.DownloadFile(BepInExURL, GameLocation + "/BepInEx.zip");

            if (File.Exists(GameLocation + "/BepInEx.zip"))
                OutputLog.Items.Add("Loaded BepInEx!");

            else
                OutputLog.Items.Add("Failed download.");

            ZipFile.ExtractToDirectory(GameLocation + "/BepInEx.zip", GameLocation);
            File.Delete(GameLocation + "/BepInEx.zip");

            IsInstalled = Directory.Exists(GameLocation + "/BepInEx");
            UpdateStatus();

            IsAwaiting = IsInstalled;

            if (IsAwaiting)
                WatchModFolder();
        }
        
        private void RemoveButton_Click(object sender, EventArgs e)
        {
            CheckInstallation();

            if (!Directory.Exists(GameLocation + "/BepInEx"))
                return;
            
            try
            {
                watcher.Changed -= OnNewModAdded;
                watcher.Dispose();
            }
            catch { } // same error, every time.


            Directory.Delete(GameLocation + "/BepInEx", true);

            File.Delete(GameLocation + "/doorstop_config.ini");
            File.Delete(GameLocation + "/winhttp.dll");

            OutputLog.Items.Add("Deleted Mods!");

            IsInstalled = Directory.Exists(GameLocation + "/BepInEx");
            IsAwaiting = false;

            UpdateStatus();
            UpdateModList();
        }

        private void OpenEditorButton_Click(object sender, EventArgs e)
        {
            ModEditor = new IDE();
            ModEditor.Show();
        }

        #region Prototypes

        private void ModList_SelectedIndexChanged(object sender, EventArgs e) { }

        #endregion
        #region Window Handlers

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        #endregion
        #region Utils

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

            InstallButton.Enabled = !IsInstalled;
            InstallButton.Visible = !IsInstalled;

            RemoveButton.Enabled = IsInstalled;
            RemoveButton.Visible = IsInstalled;

            DeleteButton.Enabled = IsInstalled;
            DeleteButton.Visible = IsInstalled;
        }

        private void UpdateModList()
        {
            ModList.Items.Clear();

            if (!Directory.Exists(GameLocation + "/BepInEx") || !Directory.Exists(GameLocation + "/BepInEx/plugins"))
                return;

            foreach (string mod in Directory.EnumerateDirectories(GameLocation + "/BepInEx/plugins"))
                ModList.Items.Add(mod.Replace(".dll", "").Split('/').Last<string>());

            foreach (string mod in Directory.EnumerateFiles(GameLocation + "/BepInEx/plugins"))
                ModList.Items.Add(mod.Replace(".dll", "").Split('/').Last<string>());
        }


        #endregion
        #region Background

        private void OnNewModAdded(object source, FileSystemEventArgs e)
        {
            try
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    UpdateModList();
                }));
            }
            catch { } // nothing happens.
        }

        private void WatchModFolder()
        {

            if (Directory.Exists(GameLocation + "/BepInEx") && !Directory.Exists(GameLocation + "/BepInEx/plugins"))
                Directory.CreateDirectory(GameLocation + "/BepInEx/plugins");

            if (!Directory.Exists(GameLocation + "/BepInEx/plugins"))
                return;

            watcher = new FileSystemWatcher();

            watcher.Path = GameLocation + "/BepInEx/plugins";
            watcher.Filter = "*.dll";
            
            watcher.Changed += new FileSystemEventHandler(OnNewModAdded);
            watcher.Deleted += new FileSystemEventHandler(OnNewModAdded);

            watcher.EnableRaisingEvents = true;
        }

        private void LethalModder_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!IsAwaiting)
                return;

            watcher.Changed -= OnNewModAdded;
            watcher.Dispose();
        }

        #endregion
    }
}
