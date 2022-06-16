using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;

public class Session
{
  private const string ShellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
  private RunspacePool? _runspacePool;

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
    var r = RunspaceFactory.CreateRunspace(connectionInfo);
    RunspacePool rsPool = RunspaceFactory.CreateRunspacePool(1, 8, connectionInfo);
    rsPool.Open();
    _runspacePool = rsPool;
  }

  public Task<string> Execute(string script)
  {
    if (_runspacePool == null)
    {
      throw new Exception("Runspace is null");
    }

    var res = InvokePowerShell(script);
    return res;
  }

  private PowerShell GetPowerShellInstance()
  {
    var pwsh = PowerShell.Create(RunspaceMode.NewRunspace);
    pwsh.RunspacePool = _runspacePool;
    return pwsh;
  }

  private Task<string> InvokePowerShell(string scriptContent, string[]? ignoredCommandExceptions = null, int timeoutMinutes = 5)
  {
    PowerShell shell = GetPowerShellInstance();
    shell.AddScript(scriptContent);
    var settings = new PSInvocationSettings{};
    return Task.Factory.FromAsync(
      (callback, state) => shell.BeginInvoke(
        new PSDataCollection<PSObject>(), settings, callback, state),
        shell.EndInvoke,
        state: null)
        .ContinueWith(runTask =>
        {
          shell.Dispose();
          StringBuilder sb = new StringBuilder();
          foreach (PSObject obj in runTask.Result)
          {
            sb.AppendLine(obj.ToString());
          }
          var res = new String(sb.ToString());
          sb.Clear();
          GC.Collect();
          GC.WaitForPendingFinalizers();
          return res;
        });
  }

  public void Close()
  {
    if (_runspacePool == null) return;
    try
    {
      _runspacePool.Close();
      _runspacePool.Dispose();
    }
    finally
    {
      _runspacePool = null;
    }
  }

  private static SecureString StringToSecureString(string password)
  {
    if (password == null)
      throw new ArgumentNullException(nameof(password));
    var securePassword = new SecureString();
    password.ToList().ForEach(x => securePassword.AppendChar(x));
    return securePassword;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_runspacePool == null) return;
    try
    {
      _runspacePool.Close();
      _runspacePool.Dispose();
    }
    finally
    {
      _runspacePool = null;
    }
  }

}

public record ConnectionDto(string hostname, string username, string passwd, int num);
public record RunDto(string sessionId, string script);
