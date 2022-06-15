using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;
using Newtonsoft.Json;

public class Session
{
  // private WSManConnectionInfo connectionInfo;
  private const string ShellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
  private Runspace? Runspace { get; set; }

  public Session(ConnectionDto connDto)
  {
    var securePasswd = new PSCredential(connDto.username, StringToSecureString(connDto.passwd));
    var connectionInfo = new WSManConnectionInfo(
      false, connDto.hostname, 5985, "/wsman", ShellUri, securePasswd, 1 * 60 * 1000)
    {
      AuthenticationMechanism = AuthenticationMechanism.Negotiate,
      SkipCACheck = true,
      SkipCNCheck = true,
    };
    var initialSessionState = InitialSessionState.CreateDefault();
    var r = RunspaceFactory.CreateRunspace(connectionInfo);
    r.Open();
    this.Runspace = r;
  }

  public string Execute(string script)
  {
    if (Runspace == null)
    {
      throw new Exception("Runspace is null");
    }

    if (!Monitor.TryEnter(Runspace, new TimeSpan(1, 0, 0)))
      throw new Exception("timed out waiting to get lock on runspace");
    try
    {
      var ps = PowerShell.Create();
      ps.Runspace = Runspace;
      var res = InvokePowerShell(script, ps);
      ps.Dispose();
      return res;
    }
    finally
    {
      Monitor.Exit(Runspace);
    }
  }

  private static string InvokePowerShell(string scriptContent, PowerShell shell, string[]? ignoredCommandExceptions = null, int timeoutMinutes = 5)
  {
    shell.AddScript(scriptContent);
    StringBuilder sb = new StringBuilder();
    var results = shell.Invoke();
    foreach (var x in results)
    {
      //var z = x.ToString();
      sb.AppendLine(x.ToString());
      //Console.WriteLine(z);
    }
    var y = sb.ToString();
    //sb.Clear();
    return y;
  }

  public void Close()
  {
    if (Runspace == null) return;
    try
    {
      Runspace.Close();
      Runspace.Dispose();
    }
    finally
    {
      Runspace = null;
    }
  }

  public void CheckValidity()
  {
    if (Runspace == null)
    {
      throw new Exception("Runspace is null");
    }

    if (!Monitor.TryEnter(Runspace, new TimeSpan(1, 0, 0))) return;
    try
    {
      if (Runspace.RunspaceStateInfo.State != RunspaceState.Opened)
      {
        throw new Exception($"Runspace is not in Opened state, current state is {Runspace.RunspaceStateInfo.State}");
      }
    }
    finally { Monitor.Exit(Runspace); }
  }

  private static SecureString StringToSecureString(string password)
  {
    if (password == null)
      throw new ArgumentNullException(nameof(password));
    var securePassword = new SecureString();
    password.ToList().ForEach(x => securePassword.AppendChar(x));
    return securePassword;
  }

}

public record ConnectionDto(string hostname, string username, string passwd, int num);
public record RunDto(string sessionId, string script);
