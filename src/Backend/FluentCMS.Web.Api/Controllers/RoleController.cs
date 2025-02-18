﻿namespace FluentCMS.Web.Api.Controllers;

public class RoleController(IMapper mapper, IRoleService roleService, IUserRoleService userRoleService) : BaseGlobalController
{
    public const string AREA = "Role Management";
    public const string READ = "Read";
    public const string UPDATE = $"Update/{READ}";
    public const string CREATE = "Create";
    public const string DELETE = $"Delete/{READ}";

    [HttpGet]
    [Policy(AREA, READ)]
    public async Task<IApiPagingResult<RoleDetailResponse>> GetAll([FromQuery] Guid siteId, CancellationToken cancellationToken = default)
    {
        var roles = await roleService.GetAllForSite(siteId, cancellationToken);
        var roleResponses = mapper.Map<IEnumerable<RoleDetailResponse>>(roles);

        // set users count for each role
        var userRoles = await userRoleService.GetAllForSite(siteId, cancellationToken);
        foreach (var roleResponse in roleResponses)
            roleResponse.UsersCount = userRoles.Count(ur => ur.RoleId == roleResponse.Id);

        return OkPaged(roleResponses);
    }

    [HttpGet("{id}")]
    [Policy(AREA, READ)]
    public async Task<IApiResult<RoleDetailResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var role = await roleService.GetById(id, cancellationToken);
        var roleResponse = mapper.Map<RoleDetailResponse>(role);
        return Ok(roleResponse);
    }

    [HttpPost]
    [Policy(AREA, CREATE)]
    public async Task<IApiResult<RoleDetailResponse>> Create([FromBody] RoleCreateRequest request, CancellationToken cancellationToken = default)
    {
        var role = mapper.Map<Role>(request);

        // all roles that user defines them, must be UserDefined.
        // this is added here and not in service, because service is used by system in Setup process.
        // Also it is not taken from user (front), because we have to encapsulate this logic in server side. 
        role.Type = RoleTypes.UserDefined;
        role.Name = role.Name.Trim();
        var newRole = await roleService.Create(role, cancellationToken);
        var roleResponse = mapper.Map<RoleDetailResponse>(newRole);
        return Ok(roleResponse);
    }

    [HttpPut]
    [Policy(AREA, UPDATE)]
    public async Task<IApiResult<RoleDetailResponse>> Update([FromBody] RoleUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var role = mapper.Map<Role>(request);
        role.Name = role.Name.Trim();
        var updated = await roleService.Update(role, cancellationToken);
        var roleResponse = mapper.Map<RoleDetailResponse>(updated);
        return Ok(roleResponse);
    }

    [HttpDelete("{id}")]
    [Policy(AREA, DELETE)]
    public async Task<IApiResult<bool>> Delete([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        await roleService.Delete(id, cancellationToken);
        return Ok(true);
    }
}
