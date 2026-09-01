using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace v2rayN.Views;

public partial class FreedomHubHomeView
{
    private string? _baseUrl;
    private FreedomHubAccount _account = new();
    private string _deLink = "";
    private string _nlLink = "";
    private string _activeLocation = "";

    public event System.Action<string, string>? ConnectRequested; // vless link, server ip
    public event System.Action? PowerRequested;
    public event System.Action? AdvancedRequested;

    public const string DeAddress = "5.45.110.86";
    public const string NlAddress = "113.30.188.9";

    public FreedomHubHomeView()
    {
        InitializeComponent();
        lblVersion.Text = "v14 · " + Utils.GetVersion();
    }

    private async void BtnSignIn_Click(object sender, RoutedEventArgs e)
    {
        var username = txtUsername.Text.Trim();
        var password = txtPassword.Password;
        if (username == "" || password == "")
        {
            lblLoginError.Text = "Enter your username and password.";
            return;
        }

        lblLoginError.Text = "Signing in…";
        try
        {
            _baseUrl ??= await FreedomHubApi.GetBaseAsync();
            if (_baseUrl is null)
            {
                lblLoginError.Text = "Could not reach FreedomHub servers.";
                return;
            }

            _account = await FreedomHubApi.LoginAsync(_baseUrl, username, password);
            if (!_account.Success)
            {
                lblLoginError.Text = string.IsNullOrWhiteSpace(_account.Error) ? "Sign in failed." : _account.Error;
                return;
            }

            if (_account.Token != "")
            {
                try
                {
                    var fresh = await FreedomHubApi.ConfigAsync(_baseUrl, _account.Token);
                    if (fresh.Success) _account = fresh;
                }
                catch
                {
                    // keep the login payload
                }
            }

            _deLink = _account.VlessDirectDe;
            _nlLink = _account.VlessDirectNl;
            if (_deLink == "") _deLink = _account.VlessCdn;
            if (_nlLink == "") _nlLink = _deLink;

            ShowHome(username);
        }
        catch
        {
            lblLoginError.Text = "Network error while signing in.";
        }
    }

    private void ShowHome(string username)
    {
        panelLogin.Visibility = Visibility.Collapsed;
        panelHome.Visibility = Visibility.Visible;
        btnSignOut.Visibility = Visibility.Visible;
        lblSignedInRow.Visibility = Visibility.Visible;
        lblSignedInUser.Text = username;
        lblGreeting.Text = "Signed in as " + username + " · FreedomHub";

        UpdateQuota();
        var initial = _activeLocation != "" ? _activeLocation : "de";
        SelectLocation(initial);
    }

    private void UpdateQuota()
    {
        if (_account.RemainingGb > 0)
        {
            lblQuota.Text = $"{_account.RemainingGb:0.##} GB left";
            lblQuota.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
        }
        else if (_account.OverQuota || _account.QuotaGb <= 0 && _account.RemainingGb <= 0)
        {
            lblQuota.Text = "Out of data";
            lblQuota.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));
        }
        else
        {
            lblQuota.Text = $"{_account.UsedGb:0.##} / {_account.QuotaGb:0.##} GB used";
            lblQuota.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
        }

        var plan = _account.Plan.ToUpperInvariant();
        var premium = plan == "PREMIUM" || plan == "ADMIN";
        lblPlan.Text = plan == "" ? "FREE" : plan;
        lblPlan.Foreground = premium ? new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)) : new SolidColorBrush(Color.FromRgb(0x1F, 0xD6, 0xFF));
        badgePlan.BorderBrush = premium ? new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)) : new SolidColorBrush(Color.FromRgb(0x1F, 0xD6, 0xFF));

        lblQuotaSub.Text = _account.QuotaGb > 0
            ? $"of {_account.QuotaGb:0} GB weekly · resets {_account.Resets}"
            : $"week {_account.Week}";
    }

    private void SelectLocation(string code)
    {
        _activeLocation = code;
        HighlightLocation(code);
    }

    private void HighlightLocation(string code)
    {
        var cyan = new SolidColorBrush(Color.FromRgb(0x1F, 0xD6, 0xFF));
        var line = new SolidColorBrush(Color.FromRgb(0x27, 0x40, 0x63));
        var active = new SolidColorBrush(Color.FromRgb(0x14, 0x2E, 0x4B));

        btnDe.Background = code == "de" ? active : Brushes.Transparent;
        btnNl.Background = code == "nl" ? active : Brushes.Transparent;
        btnDe.BorderBrush = code == "de" ? cyan : line;
        btnNl.BorderBrush = code == "nl" ? cyan : line;
    }

    private void BtnDe_Click(object sender, RoutedEventArgs e)
    {
        SelectLocation("de");
        RaiseConnect("de");
    }

    private void BtnNl_Click(object sender, RoutedEventArgs e)
    {
        SelectLocation("nl");
        RaiseConnect("nl");
    }

    private void BtnPower_Click(object sender, RoutedEventArgs e)
    {
        PowerRequested?.Invoke();
    }

    private void BtnAdvanced_Click(object sender, RoutedEventArgs e) => AdvancedRequested?.Invoke();

    private void RaiseConnect(string code)
    {
        var link = code == "nl" ? _nlLink : _deLink;
        var ip = code == "nl" ? NlAddress : DeAddress;
        if (string.IsNullOrWhiteSpace(link))
        {
            lblState.Text = "Sign in first to get a connection link.";
            return;
        }
        lblState.Text = $"Switching to {(code == "nl" ? "Netherlands" : "Germany")}…";
        ConnectRequested?.Invoke(link, ip);
    }

    public void SetState(string text)
    {
        lblState.Text = text;
    }

    private void BtnSignOut_Click(object sender, RoutedEventArgs e)
    {
        _account = new FreedomHubAccount();
        _deLink = "";
        _nlLink = "";
        _activeLocation = "";
        panelHome.Visibility = Visibility.Collapsed;
        panelLogin.Visibility = Visibility.Visible;
        btnSignOut.Visibility = Visibility.Collapsed;
        lblSignedInRow.Visibility = Visibility.Collapsed;
        lblState.Text = "";
        txtPassword.Password = "";
        lblLoginError.Text = "";
    }
}