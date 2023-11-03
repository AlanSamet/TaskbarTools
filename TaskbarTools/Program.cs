using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public class SysTrayApp : Form
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;

    public SysTrayApp()
    {
        trayMenu = new ContextMenuStrip();
        trayIcon = new NotifyIcon
        {
            Icon = Icon.FromHandle(new Bitmap("icon.png").GetHicon()),
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        trayIcon.DoubleClick += TrayIcon_DoubleClick;
        trayIcon.MouseUp += TrayIcon_MouseUp;

        UpdateTrayMenu();
    }
    private void TrayIcon_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            UpdateTrayMenu();
        }
    }
    private void UpdateTrayMenu()
    {
        trayMenu.Items.Clear();
        var processes = Process.GetProcesses()
                               .Where(p => p.MainWindowHandle != IntPtr.Zero)
                               .GroupBy(p => p.ProcessName)
                               .OrderBy(g => g.Key)
                               .ToList();

        foreach (var group in processes)
        {
            ToolStripMenuItem procItem = new ToolStripMenuItem($"{group.Key} ({group.Count()})");
            procItem.DropDownItemClicked += ProcessItem_DropDownItemClicked;

            foreach (var proc in group)
            {
                ToolStripMenuItem instanceItem = new ToolStripMenuItem($"{proc.MainWindowTitle} (Instance {proc.Id})", null, null, proc.Id.ToString());

                instanceItem.DropDownItems.Add(new ToolStripMenuItem("Bring To Front", null, (sender, args) => BringToFront(proc.Id)));
                instanceItem.DropDownItems.Add(new ToolStripMenuItem("Force Quit...", null, (sender, args) => ConfirmAndForceQuitProcess(proc.Id, proc.MainWindowTitle)));
                procItem.DropDownItems.Add(instanceItem);
            }

            procItem.DropDownItems.Add("Quit All", null, (sender, args) => QuitAllInstances(group.Key));
            trayMenu.Items.Add(procItem);
        }

        trayMenu.Items.Add(new ToolStripSeparator());
        ToolStripMenuItem snipToolItem = new ToolStripMenuItem("Screen Snip to Clipboard");
        snipToolItem.Click += (sender, args) => SnippingTool.Snip();
        trayMenu.Items.Add(snipToolItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(new ToolStripMenuItem("Exit TaskbarTools", null, (sender, args) =>
        {
            if (MessageBox.Show($"Exit TaskbarTools?", "Confirm Quit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                trayIcon.Visible = false;
                Application.Exit();
            }
        }));
    }

    private void ConfirmAndForceQuitProcess(int processId, string windowTitle)
    {
        if (MessageBox.Show($"Are you sure you want to force quit \"{windowTitle}\"?", "Confirm Force Quit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            ForceQuitProcess(processId);
        }
    }

    private void ProcessItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
    {

    }

    private void TrayIcon_DoubleClick(object sender, EventArgs e)
    {
        UpdateTrayMenu();
        trayMenu.Show(Cursor.Position, ToolStripDropDownDirection.BelowRight);
    }

    private void QuitAllInstances(string processName)
    {
        if (MessageBox.Show($"Are you sure you want to quit all instances of \"{processName}\"?", "Confirm Quit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            return;

        var processes = Process.GetProcesses()
                               .Where(p => p.ProcessName == processName);
        StringBuilder errorMessages = new StringBuilder();

        foreach (var proc in processes)
        {
            try
            {
                proc.CloseMainWindow();
                proc.WaitForExit(2000); // Wait for 2 seconds
                if (!proc.HasExited)
                    proc.Kill();
            }
            catch (Exception ex)
            {
                errorMessages.AppendLine($"Error closing process {proc.Id} ({proc.ProcessName}:{proc.MainWindowTitle}): {ex.Message}");
            }
        }

        if (errorMessages.Length > 0)
        {
            MessageBox.Show($"Errors occurred while closing processes:\n{errorMessages}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ForceQuitProcess(int processId)
    {
        try
        {
            Process.GetProcessById(processId)?.Kill();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error forcefully quitting process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        // Hide form window and remove from taskbar
        Visible = false;
        ShowInTaskbar = false;
        base.OnLoad(e);
    }

    protected override void Dispose(bool isDisposing)
    {
        // Release the icon resource
        if (isDisposing)
        {
            trayIcon.Dispose();
        }
        base.Dispose(isDisposing);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;
    private void BringToFront(int processId)
    {
        try
        {
            var proc = Process.GetProcessById(processId);
            if (proc != null && proc.MainWindowHandle != IntPtr.Zero)
            {
                // If the window is minimized, restore it before setting it to foreground.
                ShowWindow(proc.MainWindowHandle, SW_RESTORE);
                SetForegroundWindow(proc.MainWindowHandle);
            }
        }
        catch (Exception ex)
        {

        }
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SysTrayApp());
    }
}
