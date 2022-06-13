
string hostname = "", username = "", passwd = "";
ConnectionDto connDto = new ConnectionDto(hostname, username, passwd, 0);

string script;

while (true)
{
  var session = new Session(connDto);
  var iter = 1;
  script = $"hostname | ConvertTo-Json";
  session.Execute(script);
  Console.WriteLine($"loop = {iter} job-count = {i}");
  iter++;
  Thread.Sleep(100);
  session.Close();
}
