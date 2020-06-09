using Common;
using System.Net;
using UnityEngine;

namespace FredericRP.AssetStoreTools
{
  public class BatchSubmitterClient
  {
    static AssetStoreAPI api = new AssetStoreAPI();
    static int requestCount;

    public static void UploadPackages()
    {
      requestCount = 0;
      // Use delegate to upload once the session is connected
      api.SessionConnected = Upload;
      api.AutoLogin();
    }

    static void Upload()
    {
      for (int i = 0; i < api.PackageCount; i++)
      {
        Package package = api.GetPackage(i);
        if (api.ReadyToUpload(package))
        {
          Debug.Log("AutoSubmitterClient export " + package.name);
          api.ExportPackage(package);
          Debug.Log("AutoSubmitterClient upload " + package.name);
          //string rootGuid = AssetDatabase.AssetPathToGUID("Assets" + package.root_path);
          requestCount++;
          api.Upload(package.id,
                    package.PackageExportPath,
                    package.root_path,
                    package.root_guid,
                    Application.dataPath,
                    Application.unityVersion,
                    OnCompleted
                    );
        }
      }
    }

    static void OnCompleted(HttpWebResponse response, string value)
    {
      requestCount--;
      if (response.StatusCode != HttpStatusCode.OK)
      {
        Debug.Log("Request #" + requestCount + " failed: " + value);
      }
      else
        Debug.Log("Request #" + requestCount + " success " + value);
      if (requestCount <= 0)
      {
        Debug.Log("No more request, exit");
        Application.Quit();
      }
    }

  }
}