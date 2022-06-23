using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;

string hostname = Environment.GetEnvironmentVariable("PSTEST_HOSTNAME") ?? throw new Exception("Hostname not set. Please set $env:PSTEST_HOSTNAME");
string username = Environment.GetEnvironmentVariable("PSTEST_USERNAME") ?? throw new Exception("Username not set. Please set $env:PSTEST_USERNAME");
string passwd = Environment.GetEnvironmentVariable("PSTEST_PASSWORD") ?? throw new Exception("Password not set. Please set $env:PSTEST_PASSWORD");
// string ShellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";

Console.WriteLine($"Hostname = '{hostname}' Username = '{username}' Password = '{passwd}'");

for (int i = 0; i < 1000; i++)
{
  using PowerShellProcessInstance pwshProcess = new PowerShellProcessInstance();
  pwshProcess.Process.StartInfo.FileName = @"/usr/bin/pwsh";
  pwshProcess.Process.Start();
  var creds = new PSCredential(username, StringToSecureString(passwd));
  string[] script = { "hostname", "ipconfig", "pwd" };

  var rsList = new List<Runspace>();
  for (int j = 0; j < script.Length; j++)
  {
    var runspace = RunspaceFactory.CreateOutOfProcessRunspace(new TypeTable(new string[0]), pwshProcess);
    runspace.Open();
    Console.WriteLine("Created runspace " + j);
    // The second runspace creation is failing.
    rsList.Add(runspace);
  }
  Console.WriteLine("Created runspaces");

  var tasks = new List<Task>();

  var outputs = new ConcurrentBag<string>();

  for (int j = 0; j < script.Length; j++)
  {
    tasks.Add(Task.Run(() =>
    {
      using var pwsh = PowerShell.Create(RunspaceMode.NewRunspace);
      pwsh.Runspace = rsList[j];
      rsList[j].SessionStateProxy.SetVariable("creds", creds);
      var fullScript =
        "$so = New-PSSessionOption -SkipCACheck -SkipCNCheck; " +
        "$session = New-PSSession -ComputerName " + hostname + " -Authentication 'negotiate' -Credential $creds -SessionOption $so;";
      Console.WriteLine(fullScript);
      pwsh.AddScript(fullScript);
      var invokeRes = pwsh.Invoke();
      invokeRes.Clear();
      fullScript = "Invoke-Command -Session $session -ScriptBlock { " + script[j % 3] + " };";
      Console.WriteLine(fullScript);
      pwsh.AddScript(fullScript);
      invokeRes = pwsh.Invoke();
      StringBuilder sb = new StringBuilder();
      foreach (var y in invokeRes)
      {
        sb.AppendLine(y.ToString());
      }
      outputs.Add(sb.ToString());
      sb.Clear();
      invokeRes.Clear();
      ThrowOnError(pwsh, script[j % 3]);
      pwsh.Dispose();
    }));
  }

  Task t = Task.WhenAll(tasks);
  try
  {
    Console.WriteLine("Waiting for tasks to complete");
    t.Wait();
  }
  catch { }
  if (t.Status == TaskStatus.RanToCompletion)
    Console.WriteLine("All tasks succeeded.");
  else if (t.Status == TaskStatus.Faulted)
    Console.WriteLine("Some tasks failed");

  for (int j = 0; j < script.Length; j++)
  {
    rsList[j].Close();
    rsList[j].Dispose();
  }

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