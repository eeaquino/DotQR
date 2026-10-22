using P = DotQR.PayloadGenerator;

namespace DotQR.Playground.Payloads;

public static partial class PayloadCatalog
{
    private static IEnumerable<PayloadDefinition> Networks()
    {
        yield return new("WiFi", "Wi-Fi network", "Network & security", [Text("ssid", "Network name", "DotQR Guest", true), Text("password", "Password", "sample-password", kind: FieldKind.Password), Choice("authentication", "Authentication", P.WiFi.Authentication.WPA), Flag("hidden", "Hidden SSID"), Flag("escapeHex", "Escape hexadecimal-looking strings", true)],
            v => new P.WiFi(v.Text("ssid"), v.Text("password"), v.Enum<P.WiFi.Authentication>("authentication"), v.Boolean("hidden"), v.Boolean("escapeHex")));
        yield return new("OneTimePassword", "Authenticator / OTP", "Network & security",
        [
            Choice("type", "OTP type", P.OneTimePassword.OneTimePasswordAuthType.TOTP), Text("secret", "Base32 secret", "JBSWY3DPEHPK3PXP", true, FieldKind.Password),
            Text("issuer", "Issuer", "DotQR Demo"), Text("label", "Account label", "alex@example.com"), Choice("algorithm", "Hash algorithm", P.OneTimePassword.OneTimePasswordAuthAlgorithm.SHA1),
            Text("digits", "Digits", "6", true, FieldKind.WholeNumber), Text("period", "Period (seconds)", "30", kind: FieldKind.WholeNumber, visible: v => v.Text("type") == "TOTP"),
            Text("counter", "Counter", "0", true, FieldKind.WholeNumber, visible: v => v.Text("type") == "HOTP")
        ], v => new P.OneTimePassword
        {
            Type = v.Enum<P.OneTimePassword.OneTimePasswordAuthType>("type"),
            Secret = v.Text("secret"),
            Issuer = v.OptionalText("issuer"),
            Label = v.OptionalText("label"),
            AuthAlgorithm = v.Enum<P.OneTimePassword.OneTimePasswordAuthAlgorithm>("algorithm"),
            Digits = v.WholeNumber("digits"),
            Counter = v.Text("type") == "HOTP" ? v.OptionalInteger("counter") : null,
            Period = v.Text("type") == "TOTP" ? v.OptionalInteger("period") : null
        });
        yield return new("ShadowSocksConfig", "Shadowsocks proxy", "Network & security",
        [
            Text("hostname", "Host", "proxy.example.com", true), Text("port", "Port", "8388", true, FieldKind.WholeNumber), Text("password", "Password", "sample-password", true, FieldKind.Password),
            Choice("method", "Encryption", P.ShadowSocksConfig.Method.Aes256Gcm), Text("tag", "Tag", "Demo proxy"),
            new("mode", "Options mode", FieldKind.Select, "None", Choices: ["None", "Plugin", "Parameters"]),
            Text("plugin", "Plugin", "obfs-local", true, visible: v => v.Text("mode") == "Plugin"), Text("pluginOption", "Plugin options", "obfs=http", visible: v => v.Text("mode") == "Plugin"),
            Text("parameters", "Extra parameters (one key=value per line)", "plugin=obfs-local;obfs=http", kind: FieldKind.Multiline, visible: v => v.Text("mode") == "Parameters")
        ], CreateShadowsocks);
    }

    private static P.ShadowSocksConfig CreateShadowsocks(PayloadValues v)
    {
        var method = v.Enum<P.ShadowSocksConfig.Method>("method");
        if (v.Text("mode") == "Plugin")
            return new(v.Text("hostname"), v.WholeNumber("port"), v.Text("password"), method, v.Text("plugin"), v.OptionalText("pluginOption"), v.OptionalText("tag"));
        Dictionary<string, string>? parameters = null;
        if (v.Text("mode") == "Parameters")
        {
            parameters = new(StringComparer.Ordinal);
            foreach (var line in v.Text("parameters").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
                    throw new ArgumentException("Parameters: use one key=value per line.");
                if (!parameters.TryAdd(parts[0], parts[1]))
                    throw new ArgumentException("Parameters: keys must be unique.");
            }
        }
        return new(v.Text("hostname"), v.WholeNumber("port"), v.Text("password"), method, parameters, v.OptionalText("tag"));
    }
}
