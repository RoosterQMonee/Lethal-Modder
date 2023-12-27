using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Lethal_Modder
{
    public partial class IDE : Form
    {
        #region Globals

        private string DocsURL = "https://lethal.wiki/";

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


        public IDE()
        {
            InitializeComponent();
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
        }

        private void IDE_Load(object sender, EventArgs e)
        {
            Editor.Text = ModCodeTemplate;
            OutputLog.Items.Clear();
            
            ListAssemblies();
            
            OutputLog.Items.Add("Loaded IDE!");
            GC.Collect(); // just in case :)
        }


        private void OpenDocsButton_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = DocsURL, UseShellExecute = true });
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            CheckProjectFolder();

            File.WriteAllText("./Projects/" + ProjectName.Text.Replace(".dll", "").Replace(".cs", "") + ".cs", Editor.Text);
        }

        private void CompileButton_Click(object sender, EventArgs e)
        {
            CheckProjectFolder();

            Compile(new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } }),
                                        Editor.Text, GetReference(),
                                        "./Projects/" + ProjectName.Text.Replace(".dll", "").Replace(".cs", "") + ".dll");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void AddReferenceButton_Click(object sender, EventArgs e)
        {
            ListAssemblies();

            using (OpenFileDialog diag = new OpenFileDialog())
            {
                diag.InitialDirectory = @"c:\\";
                diag.Filter = "dll files (*.dll)|*.dll";
                diag.FilterIndex = 2;
                diag.RestoreDirectory = true;

                if (diag.ShowDialog() == DialogResult.OK)
                    File.Copy(diag.FileName, Path.Combine("./References/", diag.FileName.Split('\\').Last<string>()));
            }

            ListAssemblies();
        }

        #region Large Variables

        public string ModCodeTemplate = @"// Lethal Company mod template - BepInEx

using BepInEx;

namespace LethalCompanyTemplate
{
    [BepInPlugin(""nami.lc.testmod"", ""test"", ""1.0.0"")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo(""Plugin is loaded!"");
        }
    }
}";

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

        private string[] GetReference()
        {
            List<string> reference = new List<string>();

            foreach (string r in References.Items)
                reference.Add(r);

            return reference.ToArray();
        }

        private void CheckProjectFolder() // other things may need to be added later
        {
            if (!Directory.Exists("./Projects"))
                Directory.CreateDirectory("./Projects");
        }

        private void ListAssemblies()
        {
            References.Items.Clear();

            if (!Directory.Exists("./References"))
                Directory.CreateDirectory("./References");

            foreach (string reference in Directory.EnumerateFiles("./References"))
                References.Items.Add(reference);
        }

        private void Compile(CodeDomProvider codeDomProvider, string source, string[] referencedAssemblies, string output)
        {
            try
            {
                var compilerOptions    = $"/target:library /platform:anycpu /optimize+";
                var compilerParameters = new CompilerParameters(referencedAssemblies)
                {
                    GenerateExecutable      = true,
                    GenerateInMemory        = false,
                    CompilerOptions         = compilerOptions,
                    TreatWarningsAsErrors   = false,
                    IncludeDebugInformation = false,
                    OutputAssembly          = output,
                };

                var compilerResults = codeDomProvider.CompileAssemblyFromSource(compilerParameters, source);

                if (compilerResults.Errors.Count > 0)
                    foreach (CompilerError compilerError in compilerResults.Errors)
                        OutputLog.Items.Add(string.Format("{0}, Line: {1}", compilerError.ErrorText, compilerError.Line));

                else
                    OutputLog.Items.Add("Compiled Project!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lethal IDE - Compiler", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion
    }
}
