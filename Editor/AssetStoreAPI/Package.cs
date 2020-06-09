using System;
using System.Collections.Generic;

[Serializable]
public class Package
{
  public string PackageExportPath
  {
    get
    {
      if (this.packageExportPath == null)
        this.packageExportPath = "Temp/uploadtool_" + this.name.Replace(' ', '_') + ".unitypackage";
      return this.packageExportPath;
    }
  }

  public string packageId; // Manually converted key to member.
  public string project_path;
  public string icon_url;
  public string root_guid;
  public string status;
  public string name;
  public bool is_complete_project;
  public string preview_url;
  public string version_name;
  public string root_path;
  public string id;

  /// <summary>
  /// Serialize user choice to upload this asset
  /// </summary>
  public bool selected;
  /// <summary>
  /// Serialize user choice to hide this asset
  /// </summary>
  public bool hidden;
  /// <summary>
  /// Should we include dependencies when uploading this package ?
  /// </summary>
  public bool includeDependencies;
  /// <summary>
  /// Used in the editor window only, set to false by default
  /// </summary>
  [NonSerialized]
  public bool foldout;

  public bool IsDraft {  get { return status == "draft"; } }

  private string packageExportPath;

  /// <inheritdoc />
  public override string ToString()
  {
    return string.Format("[{0}] [{1}] {2} | {3}", packageId, status, name, project_path);
  }
}