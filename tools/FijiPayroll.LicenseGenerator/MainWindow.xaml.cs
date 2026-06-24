using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;

namespace FijiPayroll.LicenseGenerator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string DefaultPrivateKeyPem = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDmcmimtfts6N9X
eHkQLgJngWCT946Cx4pMaCz0aYsd4ZUGsjYXEruPA3/aa3GFvzn2/Jp1iLcCs9n7
x4gGD0bFJEI4FKlEvhpCNq5zbBvnrooasBT/eyxT9I0WXBI0Lgn7TOg2VNiv8C11
kHgSefJ826gP/pBREa4Sx3TDyaDOK1PxWYf9/CQ8hQzfDJa0sCR354CGv+xtEbxZ
dnqAyVdfwz4QrDe5puK5/1xH+8PyoQK8tA9pQthJs45CtmFmP2Y1X1iVoSVRw7me
lYoypz3zVtCMDcvagJ97QL3qQ8hC3q78eOl08Qn4Lzw7zZDXMe5k41vNxVuiK261
UbUwIhntAgMBAAECggEAePwh0zyBlqkf8IVQUd1F5991u9lhWWm3QuwChgMPRY3U
NqLDYRO1opy8uAhmnkhJ/1CZKxGuu11/GP+le0Dz77ZciaLXRz7i/FZG+lQMxnLN
ELvXGlYpbJ5coBuQdxKgrO2wkC21YZEf3LQPRev+Ee0ka9lDHTzB/hv3Qn8NzI7V
xiq/Blu/6aRkpLPzitQn+MeCqN9ipaD1RD12D38LC4vKErSTJSSIHFgNsuTulr44
K+VWnBq0JlggtGkTB58WIFpPVy51FnMcAxhgk7LwOjtL2QUDVU5oNbsB2kPXSFgJ
91t5mrTVA2bcM4TJDbM8E9rK75TMMWu16qWWAb9FRQKBgQDxtuGB7er9WBwgN3cP
xPeYc/9xgr1RruRjmeFGrO4GOaPTm81V6tc12sRWKHDp0paV6riaDCJFgFzI/hmO
+WeZJaHKZmA4KkoskivyPrPo9ZA2rhbYPWXEs/B25TarBcHp9OJf1M+W1hynXimO
v0LQV9a90UzokQemIBJMgtEC4wKBgQD0EQ1QFBvZYDM3x3JY6Xz1bFIaRwo4MgTR
4/92lxF8coV8QAoTCvBnSnHZ/MqRTSzf4Hy1WPNsaKgyhoc79/+4aUgiVy4V3lPJ
q8CFhcrXmQ8yJ1mOT52Zl7k4G3TWBLKBGpCo9oCdJaFIadTuf9vRm7JSmQwRUGX4
aCmzVqJ47wKBgCLHo88kQsnNYc6o7HLSbqX7GuhkXYVhWu+R9r5Kp70xkgcixfr0
3Z6cKeAT1Ztvd8d+jK1tzYienbs2BMtzy5pXtd3/uRybySx8o+Ipb423t9aGWjcn
LnuNQK568NDO9UYKvH/5iR01Fc5nWCd4Ec8UtIt/kEduhuE6gCeOMzDtAoGBANO5
+zeNyj8AAk6QOfVB0EJDztG28PmhAqdmR9aD5Qp1erE4CMVORxED9tJpRv1X2ub4
IpdbbAiOneL+61Atquw6gPYxdOxJq5wW3/O1BiuUPyd+FWWsUYbNpUM0Jl4HQydW
eUnqVdZ9r3VXQf4IcxRaIg8fb+WRPnSJQCuwq0+HAoGAQxXdJd57QFJlBfGC8fVw
Bpt9NRSuf4x5HSiszgKwBBvl6D2QAYU45clmkef/D8W8cf1QU0HVaYZDsPSu/8DF
m75FHgmaBulP4bsXlmjjtImzx9S85nmIYDrID2tPo4+DYd4uIYx6sd+adyRHcD6p
1K15G/WAXOnhGwovc5F8DMk=
-----END PRIVATE KEY-----";

    private bool _updatingCheckboxes = false;

    public MainWindow()
    {
        InitializeComponent();
        SetInitialDefaults();
        LoadConfigJson();
    }

    private void SetInitialDefaults()
    {
        // Default expiry date: 1 year from today
        DateExpiry.SelectedDate = DateTime.Today.AddYears(1);
        
        // Default output path: Desktop/license.fplic
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        TxtOutputPath.Text = string.IsNullOrEmpty(desktopPath) 
            ? "license.fplic" 
            : Path.Combine(desktopPath, "license.fplic");

        // Default features: All
        SetFeaturesFromString("*");
    }

    private void LoadConfigJson()
    {
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("DefaultFeatures", out var featuresProp))
                {
                    SetFeaturesFromString(featuresProp.GetString() ?? "*");
                }
                if (root.TryGetProperty("PrivateKeyPath", out var keyPathProp))
                {
                    string kPath = keyPathProp.GetString() ?? "";
                    if (!string.IsNullOrEmpty(kPath))
                    {
                        TxtPrivateKeyPath.Text = Path.IsPathRooted(kPath) ? kPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, kPath);
                    }
                }
                if (root.TryGetProperty("DefaultExpiryDays", out var expiryDaysProp) && expiryDaysProp.TryGetInt32(out int days))
                {
                    DateExpiry.SelectedDate = DateTime.Today.AddDays(days);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Warning: Failed to load config.json: {ex.Message}", Colors.Orange);
            }
        }
    }

    private void ShowStatus(string message, Color color)
    {
        TxtStatus.Text = message;
        TxtStatus.Foreground = new SolidColorBrush(color);
    }

    private void PresetDev_Click(object sender, RoutedEventArgs e)
    {
        TxtCompany.Text = "Fiji Payroll Developer";
        DateExpiry.SelectedDate = DateTime.Today.AddYears(1);
        TxtHardwareHash.Text = string.Empty;
        SetFeaturesFromString("*");
        ShowStatus("Developer preset applied.", Colors.LightBlue);
    }

    private void PresetTrial_Click(object sender, RoutedEventArgs e)
    {
        TxtCompany.Text = "Standard Demo Customer";
        DateExpiry.SelectedDate = DateTime.Today.AddDays(30);
        TxtHardwareHash.Text = string.Empty;
        SetFeaturesFromString("Payroll,Reports");
        ShowStatus("Trial preset applied (30 days limit).", Colors.LightBlue);
    }

    private void PresetEnterprise_Click(object sender, RoutedEventArgs e)
    {
        TxtCompany.Text = "Enterprise Client Corp";
        DateExpiry.SelectedDate = DateTime.Today.AddYears(3);
        TxtHardwareHash.Text = "FIPL-ENTERPRISE-NODE-HASH-99812";
        SetFeaturesFromString("*");
        ShowStatus("Enterprise preset applied (Hardware bound, 3 years limit).", Colors.LightBlue);
    }

    private void BrowsePrivateKey_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "PEM Files (*.pem)|*.pem|Key Files (*.key;*.txt)|*.key;*.txt|All Files (*.*)|*.*",
            Title = "Select RSA Private Key File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            TxtPrivateKeyPath.Text = openFileDialog.FileName;
        }
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "License Files (*.fplic)|*.fplic|All Files (*.*)|*.*",
            Title = "Select License Save Path",
            FileName = "license.fplic"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            TxtOutputPath.Text = saveFileDialog.FileName;
        }
    }

    private void ChkSelectAll_Checked(object sender, RoutedEventArgs e)
    {
        if (_updatingCheckboxes) return;
        _updatingCheckboxes = true;

        ChkPayroll.IsChecked = true;
        ChkReports.IsChecked = true;
        ChkEss.IsChecked = true;
        ChkCompliance.IsChecked = true;
        ChkAudit.IsChecked = true;

        _updatingCheckboxes = false;
        UpdateFeaturesString();
    }

    private void ChkSelectAll_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_updatingCheckboxes) return;
        _updatingCheckboxes = true;

        ChkPayroll.IsChecked = false;
        ChkReports.IsChecked = false;
        ChkEss.IsChecked = false;
        ChkCompliance.IsChecked = false;
        ChkAudit.IsChecked = false;

        _updatingCheckboxes = false;
        UpdateFeaturesString();
    }

    private void FeatureCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (_updatingCheckboxes) return;
        
        _updatingCheckboxes = true;
        bool allChecked = ChkPayroll.IsChecked == true &&
                          ChkReports.IsChecked == true &&
                          ChkEss.IsChecked == true &&
                          ChkCompliance.IsChecked == true &&
                          ChkAudit.IsChecked == true;
                           
        ChkSelectAll.IsChecked = allChecked ? true : (bool?)null;
        
        if (ChkPayroll.IsChecked == false && 
            ChkReports.IsChecked == false && 
            ChkEss.IsChecked == false && 
            ChkCompliance.IsChecked == false && 
            ChkAudit.IsChecked == false)
        {
            ChkSelectAll.IsChecked = false;
        }
        _updatingCheckboxes = false;
        
        UpdateFeaturesString();
    }

    private string GetSelectedFeaturesString()
    {
        if (ChkSelectAll.IsChecked == true)
        {
            return "*";
        }
        
        var list = new System.Collections.Generic.List<string>();
        if (ChkPayroll.IsChecked == true) list.Add("Payroll");
        if (ChkReports.IsChecked == true) list.Add("Reports");
        if (ChkEss.IsChecked == true) list.Add("ESS");
        if (ChkCompliance.IsChecked == true) list.Add("Compliance");
        if (ChkAudit.IsChecked == true) list.Add("Audit");
        
        return list.Count > 0 ? string.Join(",", list) : string.Empty;
    }

    private void UpdateFeaturesString()
    {
        if (TxtFeatureString != null)
        {
            string selected = GetSelectedFeaturesString();
            TxtFeatureString.Text = string.IsNullOrEmpty(selected) ? "(no features selected)" : selected;
        }
    }

    private void SetFeaturesFromString(string features)
    {
        _updatingCheckboxes = true;
        if (string.IsNullOrWhiteSpace(features) || features == "*")
        {
            ChkSelectAll.IsChecked = true;
            ChkPayroll.IsChecked = true;
            ChkReports.IsChecked = true;
            ChkEss.IsChecked = true;
            ChkCompliance.IsChecked = true;
            ChkAudit.IsChecked = true;
        }
        else
        {
            ChkSelectAll.IsChecked = null;
            var parts = features.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            bool payroll = false, reports = false, ess = false, compliance = false, audit = false;
            foreach (var p in parts)
            {
                string clean = p.Trim();
                if (clean.Equals("Payroll", StringComparison.OrdinalIgnoreCase)) payroll = true;
                else if (clean.Equals("Reports", StringComparison.OrdinalIgnoreCase)) reports = true;
                else if (clean.Equals("ESS", StringComparison.OrdinalIgnoreCase)) ess = true;
                else if (clean.Equals("Compliance", StringComparison.OrdinalIgnoreCase)) compliance = true;
                else if (clean.Equals("Audit", StringComparison.OrdinalIgnoreCase)) audit = true;
            }
            ChkPayroll.IsChecked = payroll;
            ChkReports.IsChecked = reports;
            ChkEss.IsChecked = ess;
            ChkCompliance.IsChecked = compliance;
            ChkAudit.IsChecked = audit;

            bool all = payroll && reports && ess && compliance && audit;
            bool none = !payroll && !reports && !ess && !compliance && !audit;
            if (all) ChkSelectAll.IsChecked = true;
            else if (none) ChkSelectAll.IsChecked = false;
        }
        _updatingCheckboxes = false;
        UpdateFeaturesString();
    }

    private void GenerateLicense_Click(object sender, RoutedEventArgs e)
    {
        string company = TxtCompany.Text.Trim();
        DateTime? selectedDate = DateExpiry.SelectedDate;
        string hardwareHash = TxtHardwareHash.Text.Trim();
        string featureFlags = GetSelectedFeaturesString();
        string privKeyPath = TxtPrivateKeyPath.Text.Trim();
        string outputPath = TxtOutputPath.Text.Trim();

        // Validations
        if (string.IsNullOrEmpty(company))
        {
            ShowStatus("Validation Error: Company Name is required.", Colors.Red);
            return;
        }

        if (selectedDate == null)
        {
            ShowStatus("Validation Error: Expiry Date is required.", Colors.Red);
            return;
        }

        string expiryStr = selectedDate.Value.ToString("yyyy-MM-dd");

        if (string.IsNullOrEmpty(featureFlags))
        {
            ShowStatus("Validation Error: Please select at least one feature flag.", Colors.Red);
            return;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            ShowStatus("Validation Error: Output File Path is required.", Colors.Red);
            return;
        }

        try
        {
            // Load private key
            string privateKeyPem;
            if (!string.IsNullOrEmpty(privKeyPath))
            {
                if (!File.Exists(privKeyPath))
                {
                    ShowStatus($"Error: Private key file not found at '{privKeyPath}'.", Colors.Red);
                    return;
                }
                privateKeyPem = File.ReadAllText(privKeyPath);
            }
            else
            {
                privateKeyPem = DefaultPrivateKeyPem;
            }

            // Build canonical message
            string canonicalMessage = $"Company={company}&ExpiryDate={expiryStr}&HardwareHash={hardwareHash}&FeatureFlags={featureFlags}";

            // Sign with RSA-SHA256
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            byte[] messageBytes = Encoding.UTF8.GetBytes(canonicalMessage);
            byte[] signatureBytes = rsa.SignData(messageBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            string signature = Convert.ToBase64String(signatureBytes);

            // Build license XML
            var licenseXml = new XDocument(
                new XElement("License",
                    new XElement("Company", company),
                    new XElement("ExpiryDate", expiryStr),
                    new XElement("HardwareHash", hardwareHash),
                    new XElement("FeatureFlags", featureFlags),
                    new XElement("Signature", signature),
                    new XElement("GeneratedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                    new XElement("GeneratorVersion", "1.0.0")
                )
            );

            // Ensure directory exists
            string? outDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            // Write output
            File.WriteAllText(outputPath, licenseXml.ToString());

            // Success feedback
            ShowStatus($"✔ License generated successfully for '{company}'!", Colors.LightGreen);
            BtnOpenFolder.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowStatus($"FATAL ERROR: {ex.Message}", Colors.Red);
            BtnOpenFolder.Visibility = Visibility.Collapsed;
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = TxtOutputPath.Text.Trim();
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
            }
            else
            {
                string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
                if (Directory.Exists(dir))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open output directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
