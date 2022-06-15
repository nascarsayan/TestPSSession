string hostname = "10.225.91.184", username = "cdmlab\\cdmlabuser", passwd = "!!123abc";
ConnectionDto connDto = new ConnectionDto(hostname, username, passwd, 0);

string script;

while (true)
{
  var session = new Session(connDto);
  var iter = 1;
  for (var i = 1; i <= 40; i++)
  {
    script = $"Find-SCJob -MaxCount {100 + i*10} -VMMServer localhost | ConvertTo-Json";
    session.Execute(script);
    Thread.Sleep(10);
    Console.WriteLine($"loop = {iter} job-count = {i}");
  }
  iter++;
  session.Close();
}

// string hostname = "10.225.91.184", username = "cdmlab\\cdmlabuser", passwd = "!!123abc";
// ConnectionDto connDto = new ConnectionDto(hostname, username, passwd, 0);

// string script;

// while (true)
// {
//   var session = new Session(connDto);
//   var iter = 1;
//   script = $"echo 'Iteration Number {iter}'; find-scjob -vmmserver localhost -maxcount {100 + iter * 50} | ConvertTo-Json -compress";
//   var x = session.Execute(script);
//   Console.WriteLine($"loop = {iter}");
//   iter++;
//   GC.Collect();
//   GC.WaitForPendingFinalizers();
//   Thread.Sleep(100);
//   session.Close();
// }
