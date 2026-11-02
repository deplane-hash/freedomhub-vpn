using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace v2rayN.Views;

public partial class FreedomHubHomeView
{
    private string? _baseUrl;
    private FreedomHubAccount _account = new();
    private string _realityLink = "";
    private bool _requesting;

    public event System.Action<string, string>? ConnectRequested; // vless link, server ip
    public event System.Action? PowerRequested;

    public const string ServerAddress = "62.171.165.206";
    public event System.Action? DisconnectRequested;

    public FreedomHubHomeView()
    {
        InitializeComponent();
        lblVersion.Text = "v25 · " + Utils.GetVersion();
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

            _realityLink = _account.VlessReality;
            if (!ServiceLib.Handler.FreedomHubConnectionPolicy.IsAllowedLink(_realityLink))
            {
                lblLoginError.Text = "The REALITY profile is unavailable. Please try again shortly.";
                return;
            }
            txtPassword.Password = "";

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
        lblState.Text = "Ready · VLESS REALITY · port 443";
    }

    private void UpdateQuota()
    {
        if (_account.Unlimited)
        {
            lblQuota.Text = "Unlimited data";
            lblQuota.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
        }
        else if (_account.RemainingGb > 0)
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

    private void BtnReality_Click(object sender, RoutedEventArgs e) => RequestConnection();

    public async void RequestConnection()
    {
        if (_requesting) return;
        if (_baseUrl is null || string.IsNullOrWhiteSpace(_account.Token))
        {
            lblState.Text = "Sign in to connect.";
            return;
        }
        _requesting = true;
        btnPower.IsEnabled = false;
        var token = _account.Token;
        try
        {
            var fresh = await FreedomHubApi.ConfigAsync(_baseUrl, token);
            if (_account.Token != token) return;
            if (!fresh.Success || !ServiceLib.Handler.FreedomHubConnectionPolicy.IsAllowedLink(fresh.VlessReality))
            {
                lblState.Text = string.IsNullOrEmpty(fresh.Error) ? "Connection unavailable. Check your account and data allowance." : fresh.Error;
                return;
            }
            _account = fresh;
            _realityLink = fresh.VlessReality;
            UpdateQuota();
            lblState.Text = "Connecting…";
            ConnectRequested?.Invoke(_realityLink, ServerAddress);
        }
        catch { lblState.Text = "Could not refresh your VPN profile. Try again."; }
        finally { _requesting = false; btnPower.IsEnabled = true; }
    }

    public void UpdateConnectionState(bool running)
    {
        btnPower.Content = running ? "Disconnect" : "Connect";
        lblState.Text = running ? "Connection started · VLESS REALITY" : "Disconnected";
    }

    private void BtnPower_Click(object sender, RoutedEventArgs e)
    {
        PowerRequested?.Invoke();
    }


    public void SetState(string text)
    {
        lblState.Text = text;
    }

    private void BtnSignOut_Click(object sender, RoutedEventArgs e)
    {
        _account = new FreedomHubAccount();
        DisconnectRequested?.Invoke();
        _realityLink = "";
        panelHome.Visibility = Visibility.Collapsed;
        panelLogin.Visibility = Visibility.Visible;
        btnSignOut.Visibility = Visibility.Collapsed;
        lblSignedInRow.Visibility = Visibility.Collapsed;
        lblState.Text = "";
        txtPassword.Password = "";
        lblLoginError.Text = "";
    }
}