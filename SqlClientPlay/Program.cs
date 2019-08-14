using System;
using System.Data.SqlClient;
using System.Reflection;
using Harmony;
using Microsoft.AspNetCore.Authentication.GssKerberos;

namespace SqlClientPlay
{
  internal class Program
  {
    private static string SqlServerAddress = "35.222.32.157";
    private static string ClientSPN = "iwaclient@ALMIREX.DC";
    private static string SqlServerSPN = "MSSQLSvc/almirex.dc"; // you MUST create an SPN in this form or SQL server will reject the ticket
    public static void Main(string[] args)
    {
            
      var harmony = HarmonyInstance.Create("demo");
      harmony.PatchAll(Assembly.GetExecutingAssembly());
      for (int i = 0; i < 1; i++)
      {
        try
        {
          var connection = new SqlConnection($"Server={SqlServerAddress};Database=master;Integrated Security=true;");
          connection.Open();
          
          Console.WriteLine("OPEN!");
        }
        catch (Exception e)
        {
          Console.WriteLine(e.Message);
        }
      }
    }
    
    public static byte[] GetTicket()
    {
      using (var clientCredentials = GssCredentials.FromKeytab(ClientSPN, CredentialUsage.Initiate))
      using (var initiator = new GssInitiator(credential: clientCredentials, spn: SqlServerSPN))
      {
        return initiator.Initiate(null);
      }
    }
  }

  [Harmony]
  public class SNISSPIDataPatch
  {
    static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("System.Data.SqlClient.TdsParser"), "SNISSPIData");

    static bool Prefix(byte[] sendBuff, ref uint sendLength)
    {
      byte[] token = Program.GetTicket();
      token.CopyTo(sendBuff,0);
      sendLength = (uint)token.Length;
      return false;
    }
  }
}