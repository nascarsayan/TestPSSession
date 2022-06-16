string hostname = "10.225.91.184", username = "cdmlab\\cdmlabuser", passwd = "!!123abc";
ConnectionDto connDto = new ConnectionDto(hostname, username, passwd, 0);

string script;

var iter = 1;
while (true)
{
  var session = new Session(connDto);
  for (var i = 1; i <= 20; i++)
  {
    var jobCount = 100 + i * 10;
    script = $"Find-SCJob -MaxCount {jobCount} -VMMServer localhost | ConvertTo-Json";
    var x = await session.Execute(script);
    Console.WriteLine(x.Length);
    Thread.Sleep(10);
    Console.WriteLine($"loop = {iter} job-count = {jobCount}");
  }
  iter++;
  session.Close();
  session.Dispose();
  Console.WriteLine("Disposed session");
}
