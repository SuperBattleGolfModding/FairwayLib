using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

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
    
    [CanBeNull]
    public static AssetBundle LoadEmbeddedAssetBundle(Assembly assembly, string resourceName)
    {
        var dataResourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(resourceName));

        if (dataResourceName == null)
        {
            CorePlugin.Logger.LogError($"Embedded resource {resourceName} not found in assembly {assembly.FullName}");
            return null;
        }

        AssetBundle assets;
        
        using (var stream = assembly.GetManifestResourceStream(dataResourceName))
        using (var ms = new MemoryStream())
        {
            stream.CopyTo(ms);
            assets = AssetBundle.LoadFromMemory(ms.ToArray());
        }

        if (assets == null)
        {
            CorePlugin.Logger.LogError("Failed to load asset bundle from " + dataResourceName);
            return null;
        }

        CorePlugin.Logger.LogInfo($"AssetBundle {resourceName} loaded successfully.");
        return assets;
    }
}