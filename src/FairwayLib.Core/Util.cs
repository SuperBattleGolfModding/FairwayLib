using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace FairwayLib.Core;

public class Util
{
    /// <summary>
    /// Generates a unique randomized GUID, DO NOT use this for networked things like cosmetics or items !!!!
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GenerateRandomGuid(string prefix, string name)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(prefix + name);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return new Guid(hashBytes).ToString();   
        }
    }
    
    /// <summary>
    /// Generates a deterministic GUID based of the mod's GUID and the asset's name .
    /// </summary>
    /// <param name="pluginGuid">GUID of your mod's plugin info.</param>
    /// <param name="assetName">Name of the assetbundle asset.</param>
    /// <returns>a string of text with the format of a unique consistent GUID.</returns>
    public static string GenerateNamespaceGuid(string pluginGuid, string itemName)
    {
        string source = $"{pluginGuid}_{itemName}";
        using (MD5 md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(source));
            Guid deterministicGuid = new Guid(hash);
            return deterministicGuid.ToString();
        }
    }
    
    /// <summary>
    /// Loads an assetbundle embedded in a mod's assembly.
    /// </summary>
    /// <param name="assembly">Your mod's assembly, you can get it through Assembly.GetExecutingAssembly.</param>
    /// <param name="resourceName">The name of the assetbundle file you want to load.</param>
    /// <returns></returns>
    [CanBeNull]
    public static AssetBundle? LoadEmbeddedAssetBundle(Assembly assembly, string resourceName)
    {
        var dataResourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(resourceName));

        if (dataResourceName == null)
        {
            CorePlugin.Log.LogError($"Embedded resource {resourceName} not found in assembly {assembly.FullName}");
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
            CorePlugin.Log.LogError("Failed to load asset bundle from " + dataResourceName);
            return null;
        }

        CorePlugin.Log.LogInfo($"AssetBundle {resourceName} loaded successfully.");
        return assets;
    }
}