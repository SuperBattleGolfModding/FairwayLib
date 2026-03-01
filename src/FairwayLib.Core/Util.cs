using System;

namespace FairwayLib.Core;

public class Util
{
    public static string GenerateGuid(string prefix, string name)
    {
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(prefix + name);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return new Guid(hashBytes).ToString();
        }
    }
}