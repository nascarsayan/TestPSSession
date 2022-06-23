using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;

string hostname = Environment.GetEnvironmentVariable("PSTEST_HOSTNAME") ?? throw new Exception("Hostname not set. Please set $env:PSTEST_HOSTNAME");
string username = Environment.GetEnvironmentVariable("PSTEST_USERNAME") ?? throw new Exception("Username not set. Please set $env:PSTEST_USERNAME");
string passwd = Environment.GetEnvironmentVariable("PSTEST_PASSWORD") ?? throw new Exception("Password not set. Please set $env:PSTEST_PASSWORD");
// string ShellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";

Console.WriteLine($"Hostname = '{hostname}' Username = '{username}' Password = '{passwd}'");

List<PSObject> RunLocalCommand(Runspace runspace, Command arbitraryCommand)
{
  using (var powershell = PowerShell.Create())
  {
    powershell.Runspace = runspace;

    powershell.Commands.AddCommand(arbitraryCommand);

    var output = powershell.Invoke();

    // ThrowOnError(powershell, arbitraryCommand.CommandText, "localhost");

    var ret = output.ToList();
    return ret;
  }
}

for (int i = 0; i < 1000; i++)
{
  using PowerShellProcessInstance pwshProcess = new PowerShellProcessInstance();
  pwshProcess.Process.StartInfo.FileName = @"/usr/bin/pwsh";
  pwshProcess.Process.Start();
  var creds = new PSCredential(username, StringToSecureString(passwd));
  using var runspace = RunspaceFactory.CreateOutOfProcessRunspace(new TypeTable(new string[0]), pwshProcess);
  runspace.Open();
  //
  string[] script = {"hostname", "ipconfig", "pwd"};

  using var pwsh = PowerShell.Create(RunspaceMode.NewRunspace);
  pwsh.Runspace = runspace;
  runspace.SessionStateProxy.SetVariable("creds", creds);
  var fullScript = 
    "$so = New-PSSessionOption -SkipCACheck -SkipCNCheck; "+
    "$session = New-PSSession -ComputerName " + hostname + " -Authentication 'negotiate' -Credential $creds -SessionOption $so;";
  Console.WriteLine(fullScript);
  pwsh.AddScript(fullScript);
  var x = pwsh.Invoke();
  x.Clear();
  for (int j = 0; j < 50; j++) {
    fullScript = "Invoke-Command -Session $session -ScriptBlock { " + script[j%3] + " };";
    Console.WriteLine(fullScript);
    pwsh.AddScript(fullScript);
    x = pwsh.Invoke();
    // foreach (var y in x)
    // {
    //   Console.WriteLine(y.ToString());
    // }
    x.Clear();
    ThrowOnError(pwsh, script[j%3]);
  }
  pwsh.Dispose();
  runspace.Close();
  runspace.Dispose();
  Console.WriteLine($"i = {i}");
  if (i == 10 || i == 500)
  {
    Console.WriteLine("Time to take memory dump");
    Thread.Sleep(10000);
  }
  if (i > 500)
  {
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

void ThrowOnError(PowerShell powershell, string attemptedScriptBlock)
{
  if (powershell.Streams.Error.Count > 0)
  {
    var errorString = string.Join(
        Environment.NewLine,
        powershell.Streams.Error.Select(
            _ =>
            (_.ErrorDetails == null ? null : _.ErrorDetails.ToString() + " at " + _.ScriptStackTrace)
            ?? (_.Exception == null ? "Naos.WinRM: No error message available" : _.Exception.ToString() + " at " + _.ScriptStackTrace)));
    throw new Exception(
        "Failed to run script (" + attemptedScriptBlock + ") on " + "localhost" + " got errors: "
        + errorString);
  }
}