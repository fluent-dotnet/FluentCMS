﻿namespace FluentCMS.Repositories.Caching;

public class FolderRepository(IFolderRepository repository, ICacheProvider cacheProvider) : SiteAssociatedRepository<Folder>(repository, cacheProvider), IFolderRepository
{
    public async Task<Folder?> GetByName(Guid siteId, Guid? parentId, string normalizedName, CancellationToken cancellationToken = default)
    {
        var allFolders = await GetAllForSite(siteId, cancellationToken);
        return allFolders.FirstOrDefault(x => x.ParentId == parentId && x.NormalizedName == normalizedName);
    }
}
