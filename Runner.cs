using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;

string hostname = Environment.GetEnvironmentVariable("PSTEST_HOSTNAME") ?? throw new Exception("Hostname not set. Please set $env:PSTEST_HOSTNAME");
string username = Environment.GetEnvironmentVariable("PSTEST_USERNAME") ?? throw new Exception("Username not set. Please set $env:PSTEST_USERNAME");
string passwd = Environment.GetEnvironmentVariable("PSTEST_PASSWORD") ?? throw new Exception("Password not set. Please set $env:PSTEST_PASSWORD");
string ShellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";

for (int i = 0; i < 1000000; i++)
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
  // pwsh.AddScript("echo 'a'");
  pwsh.AddScript("$i = 0");
  var x = pwsh.Invoke();
  foreach (var y in x) {
    Console.WriteLine(y.ToString().Length);
  }
  x.Clear();
  pwsh.Dispose();
  runspace.Close();
  runspace.Dispose();
  Console.WriteLine($"i = {i}");
}

SecureString StringToSecureString(string password)
{
  if (password == null)
    throw new ArgumentNullException(nameof(password));
  var securePassword = new SecureString();
  password.ToList().ForEach(x => securePassword.AppendChar(x));
  return securePassword;
}
