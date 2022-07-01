using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;

string hostname = Environment.GetEnvironmentVariable("PSTEST_HOSTNAME") ?? throw new Exception("Hostname not set. Please set $env:PSTEST_HOSTNAME");
string username = Environment.GetEnvironmentVariable("PSTEST_USERNAME") ?? throw new Exception("Username not set. Please set $env:PSTEST_USERNAME");
string passwd = Environment.GetEnvironmentVariable("PSTEST_PASSWORD") ?? throw new Exception("Password not set. Please set $env:PSTEST_PASSWORD");
string ShellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";

bool waitForDump = bool.Parse(Environment.GetEnvironmentVariable("WAIT_FOR_DUMP") ?? "False");

Console.WriteLine($"Hostname = '{hostname}' Username = '{username}' Password = '{passwd}'");

for (int i = 0; i < 1000; i++)
{
  var securePasswd = new PSCredential(username, StringToSecureString(passwd));
  var connectionInfo = new WSManConnectionInfo(
    false, hostname, 5985, "/wsman", ShellUri, securePasswd, 1 * 60 * 1000)
  {
    AuthenticationMechanism = AuthenticationMechanism.Negotiate,
    SkipCACheck = true,
    SkipCNCheck = true,
  };
  using var runspace = RunspaceFactory.CreateRunspace(connectionInfo);
  runspace.Open();
  using var pwsh = PowerShell.Create(RunspaceMode.NewRunspace);
  pwsh.Runspace = runspace;
  var script = "return";
  pwsh.AddScript(script);
  var x = pwsh.Invoke();
  foreach (var y in x) {
    Console.WriteLine(y.ToString().Length);
  }
  ThrowOnError(pwsh, script);
  x.Clear();
  pwsh.Dispose();
  runspace.Close();
  runspace.Dispose();
  Console.WriteLine($"i = {i}");
  if (waitForDump && (i == 10 || i == 500)) {
    Console.WriteLine("Time to take memory dump");
    Thread.Sleep(10000);
  }
  if (i > 500) {
    Thread.Sleep(30000);
  }
}

SecureString StringToSecureString(string password)
{
  if (password == null)
    throw new ArgumentNullException(nameof(password));
  var securePassword = new SecureString();
  password.ToList().ForEach(x => securePassword.AppendChar(x));
  return securePassword;
}

void ThrowOnError(PowerShell pwsh, string script) {
  if (pwsh.Streams.Error.Count > 0)
  {
    var errorString = string.Join(
        Environment.NewLine,
        pwsh.Streams.Error.Select(
            _ =>
            (_.ErrorDetails == null ? null : _.ErrorDetails.ToString() + " at " + _.ScriptStackTrace)
            ?? (_.Exception == null ? "Naos.WinRM: No error message available" : _.Exception.ToString() + " at " + _.ScriptStackTrace)));
    throw new Exception(
        "Failed to run script (" + script + ") on " + hostname + " got errors: "
        + errorString);
  }
}
