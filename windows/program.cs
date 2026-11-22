using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

// ===== НАСТРОЙКИ ПРОЕКТА =====
const string Host = "31.77.232.41";
const int Port = 3431;
const string VkUrl = "https://vk.com/";
const string DiscordUrl = "https://discord.com/";
const string GameExe = "samp.exe";
// =============================

ApplicationConfiguration.Initialize();
Application.Run(new MainForm());

class Cfg { public string Nick { get; set; } = ""; public string GamePath { get; set; } = ""; }

class MainForm : Form
{
    const string Host = "31.77.232.41";
    const int Port = 3431;
    const string GameExe = "samp.exe";
    static readonly string CfgFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DVLauncher", "config.json");
    Cfg cfg = new();
    TextBox nick = new(); Label status = new(); Label path = new();
    static readonly Color Bg = Color.FromArgb(18, 20, 28), Accent = Color.FromArgb(214, 40, 57);

    public MainForm()
    {
        try { cfg = JsonSerializer.Deserialize<Cfg>(File.ReadAllText(CfgFile)) ?? new(); } catch { }
        Text = "КРМП · Дальневосточный"; ClientSize = new Size(640, 380);
        FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen; BackColor = Bg; ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        Controls.Add(new Label { Text = "ДАЛЬНЕВОСТОЧНЫЙ", Font = new Font("Segoe UI", 26, FontStyle.Bold),
            ForeColor = Accent, AutoSize = true, Location = new Point(30, 24) });
        Controls.Add(new Label { Text = "КРМП · ролевой проект", ForeColor = Color.Gray, AutoSize = true, Location = new Point(34, 78) });

        Controls.Add(new Label { Text = "Никнейм", AutoSize = true, Location = new Point(34, 130) });
        nick = new TextBox { Text = cfg.Nick, Location = new Point(34, 156), Width = 300, Font = new Font("Segoe UI", 12),
            BackColor = Color.FromArgb(32, 35, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        Controls.Add(nick);

        status = new Label { Text = "Сервер: опрос…", AutoSize = true, Location = new Point(34, 205), ForeColor = Color.Gainsboro };
        Controls.Add(status);
        Controls.Add(new Label { Text = $"{Host}:{Port}", AutoSize = true, Location = new Point(34, 230), ForeColor = Color.Gray });

        path = new Label { AutoSize = true, Location = new Point(34, 330), ForeColor = Color.Gray };
        UpdatePath(); Controls.Add(path);

        Controls.Add(Btn("ИГРАТЬ", 380, 150, 220, 54, Accent, (_, _) => Play()));
        Controls.Add(Btn("Папка с игрой", 380, 215, 220, 36, Color.FromArgb(45, 49, 66), (_, _) => PickFolder()));
        Controls.Add(Btn("ВКонтакте", 380, 260, 106, 36, Color.FromArgb(45, 49, 66), (_, _) => Open("https://vk.com/")));
        Controls.Add(Btn("Discord", 494, 260, 106, 36, Color.FromArgb(45, 49, 66), (_, _) => Open("https://discord.com/")));

        var t = new System.Windows.Forms.Timer { Interval = 10000 }; t.Tick += async (_, _) => await Refresh(); t.Start();
        Shown += async (_, _) => await Refresh();
        FormClosing += (_, _) => Save();
    }

    Button Btn(string text, int x, int y, int w, int h, Color c, EventHandler click)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), FlatStyle = FlatStyle.Flat,
            BackColor = c, ForeColor = Color.White, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderSize = 0; b.Click += click; return b;
    }

    void Save() { cfg.Nick = nick.Text.Trim(); Directory.CreateDirectory(Path.GetDirectoryName(CfgFile)!); File.WriteAllText(CfgFile, JsonSerializer.Serialize(cfg)); }
    void UpdatePath() => path.Text = string.IsNullOrEmpty(cfg.GamePath) ? "Папка с игрой не выбрана" : "Игра: " + cfg.GamePath;
    static void Open(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    void PickFolder()
    {
        using var d = new FolderBrowserDialog { Description = "Выберите папку с игрой (где лежит " + GameExe + ")" };
        if (d.ShowDialog() == DialogResult.OK) { cfg.GamePath = d.SelectedPath; UpdatePath(); Save(); }
    }

    void Play()
    {
        var n = nick.Text.Trim();
        if (!Regex.IsMatch(n, @"^[A-Za-z0-9_\[\]\.\$=@]{3,20}$"))
        { MessageBox.Show("Ник: 3–20 символов, латиница, цифры, _ [ ] . $ = @"); return; }
        if (string.IsNullOrEmpty(cfg.GamePath) || !File.Exists(Path.Combine(cfg.GamePath, GameExe))) PickFolder();
        var exe = Path.Combine(cfg.GamePath, GameExe);
        if (!File.Exists(exe)) { MessageBox.Show($"Не найден {GameExe} в выбранной папке."); return; }
        try { using var k = Registry.CurrentUser.CreateSubKey(@"Software\SAMP"); k.SetValue("PlayerName", n); } catch { }
        Save();
        Process.Start(new ProcessStartInfo(exe, $"-c -n {n} -h {Host} -p {Port}") { WorkingDirectory = cfg.GamePath, UseShellExecute = true });
    }

    async Task Refresh()
    {
        try
        {
            var ip = (await Dns.GetHostAddressesAsync(Host)).First(a => a.AddressFamily == AddressFamily.InterNetwork).GetAddressBytes();
            using var u = new UdpClient(); u.Connect(Host, Port);
            var pk = new List<byte>(Encoding.ASCII.GetBytes("SAMP")); pk.AddRange(ip);
            pk.Add((byte)(Port & 255)); pk.Add((byte)(Port >> 8)); pk.Add((byte)'i');
            var sw = Stopwatch.StartNew(); await u.SendAsync(pk.ToArray());
            var d = (await u.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(2))).Buffer; sw.Stop();
            int p = BitConverter.ToUInt16(d, 12), m = BitConverter.ToUInt16(d, 14);
            status.Text = $"Онлайн: {p}/{m}   ·   Пинг: {sw.ElapsedMilliseconds} мс"; status.ForeColor = Color.LightGreen;
        }
        catch { status.Text = "Сервер не отвечает"; status.ForeColor = Color.IndianRed; }
    }
}
