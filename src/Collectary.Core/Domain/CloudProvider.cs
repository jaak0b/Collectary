namespace Collectary.Core.Domain;

/// <summary>
/// Identifies how sync documents are stored. <see cref="Folder"/> is the local/mounted-folder
/// backend; the others sync directly to a cloud provider via its API.
/// </summary>
public enum CloudProvider
{
    Folder,
    OneDrive,
    GoogleDrive,
}
