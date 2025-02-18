﻿using System.Text.RegularExpressions;

namespace FluentCMS.Services;

public interface IFolderService : IAutoRegisterService
{
    Task<Folder> Create(Folder folder, CancellationToken cancellationToken = default);
    Task<Folder> CreateRoot(Guid siteId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Folder>> GetAll(Guid siteId, CancellationToken cancellationToken = default);
    Task<Folder> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Folder> GetByPath(Guid siteId, string folderPath, CancellationToken cancellationToken = default);
    Task<List<Folder>> GetParentFolders(Guid folderId, CancellationToken cancellationToken = default);
    Task<Folder> Rename(Guid folderId, string name, CancellationToken cancellationToken = default);
    Task<Folder> Move(Guid folderId, Guid parentFolderId, CancellationToken cancellationToken = default);
    Task<Folder> Delete(Guid id, CancellationToken cancellationToken = default);
}

public partial class FolderService(IFolderRepository folderRepository, IFileRepository fileRepository) : IFolderService
{
    public async Task<Folder> CreateRoot(Guid siteId, CancellationToken cancellationToken = default)
    {
        var normalizedName = GetNormalizedFolderName(ServiceConstants.ROOT_FOLDER_PATH);

        if (!IsValidFolderName(normalizedName))
            throw new AppException(ExceptionCodes.FolderInvalidName);

        if (await folderRepository.GetByName(siteId, null, normalizedName, cancellationToken) != null)
            throw new AppException(ExceptionCodes.FolderAlreadyExists);

        var rootFolder = new Folder
        {
            Name = ServiceConstants.ROOT_FOLDER_PATH,
            NormalizedName = normalizedName,
            SiteId = siteId
        };

        return await folderRepository.Create(rootFolder, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderUnableToCreate);
    }

    public async Task<Folder> Create(Folder folder, CancellationToken cancellationToken = default)
    {
        folder.NormalizedName = GetNormalizedFolderName(folder.Name);

        if (!IsValidFolderName(folder.NormalizedName))
            throw new AppException(ExceptionCodes.FolderInvalidName);

        // check if parent folder exists
        if (folder.ParentId == null)
            throw new AppException(ExceptionCodes.FolderParentNotFound);

        _ = await folderRepository.GetById(folder.ParentId.Value, cancellationToken) ??
                throw new AppException(ExceptionCodes.FolderParentNotFound);

        // check if folder with the same name already exists
        var existingFolder = await folderRepository.GetByName(folder.SiteId, folder.ParentId, folder.NormalizedName, cancellationToken);
        if (existingFolder != null)
            throw new AppException(ExceptionCodes.FolderAlreadyExists);

        return await folderRepository.Create(folder, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderUnableToCreate);
    }

    public async Task<IEnumerable<Folder>> GetAll(Guid siteId, CancellationToken cancellationToken = default)
    {
        return await folderRepository.GetAllForSite(siteId, cancellationToken);
    }

    public async Task<Folder> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var folder = await folderRepository.GetById(id, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderNotFound);

        return folder;
    }

    public async Task<List<Folder>> GetParentFolders(Guid folderId, CancellationToken cancellationToken = default)
    {
        var folders = new List<Folder>();
        var currentFolder = await folderRepository.GetById(folderId, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderNotFound);

        folders.Add(currentFolder);

        while (currentFolder != null && currentFolder.ParentId != null)
        {
            currentFolder = await folderRepository.GetById(currentFolder.ParentId.Value, cancellationToken) ??
                throw new AppException(ExceptionCodes.FolderNotFound);
            if (currentFolder != null)
                folders.Add(currentFolder);
        }

        folders.Reverse();

        return folders;
    }

    public async Task<Folder> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var existingFolder = await folderRepository.GetById(id, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderNotFound);

        var allFolders = await GetAll(existingFolder.SiteId, cancellationToken);
        var allFiles = await fileRepository.GetAllForSite(existingFolder.SiteId, cancellationToken);

        return await DeleteSubFolders(id, allFolders.ToList(), allFiles.ToList(), cancellationToken);
    }

    public Task<Folder> GetByPath(Guid siteId, string folderPath, CancellationToken cancellationToken = default)
    {
        folderPath = GetNormalizedFolderName(folderPath);

        // split folder path
        string[] folderNames = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return FindFolder(siteId, null, folderNames, 0, cancellationToken);
    }

    // find folder recursively by parent folder
    private async Task<Folder> FindFolder(Guid siteId, Guid? parentId, string[] folderNames, int index, CancellationToken cancellationToken)
    {
        if (index == folderNames.Length - 1)
        {
            return await folderRepository.GetByName(siteId, parentId, folderNames[index], cancellationToken) ??
                throw new AppException(ExceptionCodes.FolderNotFound);
        }
        var folder = await folderRepository.GetByName(siteId, parentId, folderNames[index], cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderNotFound);

        return await FindFolder(siteId, folder.Id, folderNames, index + 1, cancellationToken);
    }

    public async Task<Folder> Rename(Guid folderId, string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = GetNormalizedFolderName(name);

        if (!IsValidFolderName(normalizedName))
            throw new AppException(ExceptionCodes.FolderInvalidName);

        var existingFolder = await folderRepository.GetById(folderId, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderNotFound);

        // check if folder with the same name already exists
        var existingFolderSameName = await folderRepository.GetByName(existingFolder.SiteId, existingFolder.ParentId, normalizedName, cancellationToken);

        // check if the folder is not root folder
        if (existingFolder.ParentId == null)
            throw new AppException(ExceptionCodes.FolderCannotRenameRootFolder);

        if (existingFolderSameName != null && existingFolderSameName.Id != folderId)
            throw new AppException(ExceptionCodes.FolderAlreadyExists);

        existingFolder.Name = name;
        existingFolder.NormalizedName = normalizedName;

        return await folderRepository.Update(existingFolder, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderUnableToUpdate);
    }

    public async Task<Folder> Move(Guid folderId, Guid parentFolderId, CancellationToken cancellationToken = default)
    {
        // check if folder is not moving to itself
        if (folderId == parentFolderId)
            throw new AppException(ExceptionCodes.FolderCannotMoveToItself);

        // check if folder exists
        var existingFolder = await folderRepository.GetById(folderId, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderNotFound);

        // check if the folder is not root folder
        if (existingFolder.ParentId == null)
            throw new AppException(ExceptionCodes.FolderCannotMoveRootFolder);

        // check if parent folder exists
        var parentFolder = await folderRepository.GetById(parentFolderId, cancellationToken) ??
                throw new AppException(ExceptionCodes.FolderParentNotFound);

        // check if folder is not moving to its child
        var parentFolders = await GetParentFolders(parentFolderId, cancellationToken);
        if (parentFolders.Any(x => x.Id == folderId))
            throw new AppException(ExceptionCodes.FolderCannotMoveToChild);

        // check parent folder and folder is in the same site
        if (parentFolder.SiteId != existingFolder.SiteId)
            throw new AppException(ExceptionCodes.FolderNotFound);

        existingFolder.ParentId = parentFolderId;

        return await folderRepository.Update(existingFolder, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderUnableToUpdate);
    }

    private static string GetNormalizedFolderName(string folderName)
    {
        return folderName.Trim().ToLower();
    }

    // Regular expression to allow only alphanumeric, spaces, underscores, and certain special characters
    private static readonly Regex _regex = new Regex(@"^[\w\-. ]+$");

    private static bool IsValidFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return false; // Folder name should not be empty or whitespace

        return _regex.IsMatch(folderName);
    }

    private async Task<Folder> DeleteSubFolders(Guid parentFolderId, List<Folder> allFolders, List<File> allFiles, CancellationToken cancellationToken = default)
    {
        var fileIds = allFiles.Where(x => x.FolderId == parentFolderId).Select(x => x.Id);
        await fileRepository.DeleteMany(fileIds, cancellationToken);

        foreach (var childFolder in allFolders.Where(x => x.ParentId == parentFolderId))
            await DeleteSubFolders(childFolder.Id, allFolders, allFiles, cancellationToken);

        return await folderRepository.Delete(parentFolderId, cancellationToken) ??
            throw new AppException(ExceptionCodes.FolderUnableToDelete);
    }

}
