using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FredericRP.AssetStoreTools
{
  public class BatchSubmitterWindow : EditorWindow
  {
    static AssetStoreAPI api;
    static PublisherData publisher;
    const string PUBLISHER_DATA_KEY = "FredericRP.PublisherData";

    GUIContent hideButton;
    GUIContent errorStatus;
    bool selectAll;

    public class PublisherData
    {
      public string login;
      [NonSerialized]
      public string password;
      public int selectedAsset;
    }

    [MenuItem("Window/FredericRP/Batch submitter")]
    public static void ShowWindow()
    {
      BatchSubmitterWindow submitter = EditorWindow.GetWindow<BatchSubmitterWindow>();
      submitter.titleContent = new GUIContent("Batch Submitter config");
      submitter.titleContent.image = EditorGUIUtility.IconContent("Favorite").image;
      submitter.Show();
    }

    void OnEnable()
    {
      if (api == null)
        api = new AssetStoreAPI();

      // Can not be used as initializers
      hideButton = EditorGUIUtility.IconContent("animationvisibilitytoggleon", "|Hide package from list");
      errorStatus = EditorGUIUtility.IconContent("console.warnicon", "|Status: ");
    }

    private void OnLoginGUI()
    {
      if (publisher == null)
      {
        publisher = new PublisherData();
        if (EditorPrefs.HasKey(PUBLISHER_DATA_KEY))
        {
          publisher = JsonUtility.FromJson<PublisherData>(EditorPrefs.GetString(PUBLISHER_DATA_KEY));
        }
      }
      if (!api.IsConnected)
      {
        GUI.enabled = !api.CallInProgress;
        if (!api.CanAutoLog())
        {
          publisher.login = EditorGUILayout.TextField("Login", publisher.login);
          publisher.password = EditorGUILayout.PasswordField("Password", publisher.password);
          // Publisher data will be saved when a login attempt is success
        }
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();
        if (api.CanAutoLog())
        {
          if (GUILayout.Button("Auto login", EditorStyles.toolbarButton))
          {
            Login(true);
          }
        }
        else
        {
          if (GUILayout.Button("Login", EditorStyles.toolbarButton))
          {
            Login();
          }
        }
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;
        // TODO: add checkbox "Upload all draft packages once logged"
      }
      else
      {
        // Login already made
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Logged in as:" + api.Session.name);
        if (GUILayout.Button("Logout", EditorStyles.toolbarButton))
        {
          api.Logout();
        }
        EditorGUILayout.EndHorizontal();
      }
    }

    private void OnGUI()
    {
      OnLoginGUI();

      // Can not refresh list if not logged in
      if (api.Session != null)
      {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh"))
        {
          ListPackages();
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Upload selected"))
        {
          UploadSelectedPackages();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
      }
      else
      {
        EditorGUILayout.HelpBox("To enable fetching and uploading packages, you must login your unity publisher account first.", MessageType.Warning);
      }

      // TODO : add "reset cache" button to delete session and package list leys
      EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
      GUILayout.Label(api.PackageCount + " package(s)");
      if (GUILayout.Button("Save", EditorStyles.toolbarButton))
      {
        api.SavePackages();
      }
      if (GUILayout.Button("Show All", EditorStyles.toolbarButton))
      {
        api.ShowAllPackages();
      }
      EditorGUI.BeginChangeCheck();
      selectAll = EditorGUILayout.Toggle(selectAll, GUILayout.Width(24));
      if (EditorGUI.EndChangeCheck())
      {
        for (int i = 0; i < api.PackageCount; i++)
        {
          if (!api.GetPackage(i).hidden)
            api.GetPackage(i).selected = selectAll;
        }
      }
      EditorGUILayout.EndHorizontal();
      if (api.HasPackages)
      {
        for (int i = 0; i < api.PackageCount; i++)
        {
          if (!api.GetPackage(i).hidden)
            OnPackageGUI(api.GetPackage(i));
        }
        //publisher.selectedAsset = EditorGUILayout.Popup(publisher.selectedAsset, packageNameList, EditorStyles.toolbarPopup);
      }
      //if (cachedPackages != null && publisher.selectedAsset < cachedPackages.packages.Length)
      //OnPackageGUI(cachedPackages.packages[publisher.selectedAsset]);
      GUI.enabled = true;
      OnHelpGUI();
    }

    private void OnHelpGUI()
    {
      if (api.CallInProgress)
      {
        EditorGUILayout.HelpBox("An API call is in progress, please wait...", MessageType.Info);
      }
    }

    private void OnPackageGUI(Package package)
    {
      bool wasEnabled = GUI.enabled;
      EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
      package.foldout = EditorGUILayout.Foldout(package.foldout, package.name + " v" + package.version_name + " (" + package.status + ")", true);
      // 
      if (GUILayout.Button(hideButton, EditorStyles.toolbarButton, GUILayout.Width(24)))
      {
        package.hidden = true;
      }
      // Status : uploadable(draft) or not
      GUIContent packageStatus;
      if (package.IsDraft)
      {
        package.selected = EditorGUILayout.Toggle(package.selected, GUILayout.Width(24));
      }
      else
      {
        packageStatus = new GUIContent(errorStatus);
        packageStatus.tooltip += package.status;
        EditorGUILayout.Toggle(packageStatus, false, EditorStyles.toolbarButton, GUILayout.Width(24));
        if (package.selected)
          package.selected = false;
      }
      EditorGUILayout.EndHorizontal();
      if (package.foldout)
      {
        GUI.enabled = package.IsDraft;
        package.is_complete_project = EditorGUILayout.Toggle("Is complete project", package.is_complete_project);
        package.includeDependencies = EditorGUILayout.Toggle("Include dependencies", package.includeDependencies);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Root path");
        if (GUILayout.Button("Select...", GUILayout.Width(70)))
        {
          string newPath = EditorUtility.OpenFolderPanel("Select root path", Application.dataPath + package.root_path, "");
          if (!string.IsNullOrEmpty(newPath) && newPath.StartsWith(Application.dataPath))
            package.root_path = newPath.Substring(Application.dataPath.Length);
          
        }
        GUILayout.Label(package.root_path);
        EditorGUILayout.EndHorizontal();
      }
      GUI.enabled = wasEnabled;
    }

    private void UploadSelectedPackages()
    {
      for (int i = 0; i < api.PackageCount; i++)
      {
        Package package = api.GetPackage(i);
        // Upload package if draft, selected by user and not hidden (prevent uploading by mistake)
        if (api.ReadyToUpload(package))
          UploadPackage(package);
      }
    }

    public void Login(bool autoLogin = false)
    {
      // TODO : editor window with
      // for each package: known name/icon/path/version
      // ^ and + button to create new version (minor by default) or new package
      // status (red/orange/green) to indicate the status (red: too many errors to update, orange: one error to fix, green: all is ok, update is enabled)
      // check box for packages to update
      api.SessionConnected = SaveLoginData;
      if (autoLogin)
        api.AutoLogin();
      else
        api.Login(publisher.login, publisher.password);
    }

    void SaveLoginData()
    {
      // record login on success
      EditorPrefs.SetString(PUBLISHER_DATA_KEY, JsonUtility.ToJson(publisher));
    }

    void ListPackages()
    {
      api.FetchPackages();
    }

    void UploadPackage(Package package)
    {
      if (api.ExportPackage(package))
        api.Upload(package.id,
                               package.PackageExportPath,
                               package.root_path,
                               package.root_guid,
                               Application.dataPath,
                               Application.unityVersion,
                               (r, s) =>
                               {
                                 if (r.StatusCode != HttpStatusCode.OK)
                                 {
                                   Debug.Log("Upload failed: " + s);
                                 }
                                 else
                                   Debug.Log("Uploaded " + package.name);
                               });
    }

    private static IEnumerable<string> GetNamespacesInAssembly(Assembly asm)
    {
      Type[] types = asm.GetTypes();

      return types.Select(t => t.Namespace)
                  .Distinct()
                  .Where(n => n != null);
    }

  }
}