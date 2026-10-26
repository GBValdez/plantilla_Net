using project.utils.mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.utils.dto;
using project.utils.interfaces;
namespace project.utils;

public abstract class controllerCommons<TEntity, TDtoCreation, TDto, TQuery, TQueryCreation, idClass>(DBProyContext db, IObjectMapper objectMapper) : ControllerBase
    where TEntity : class, ICommonModel<idClass>
    where TDto : class where TDtoCreation : class where TQuery : class
{
    protected readonly DBProyContext context = db;
    protected readonly IObjectMapper mapper = objectMapper;
    protected virtual bool showDeleted { get; set; } = false;
    [HttpGet]
    public virtual async Task<ActionResult<resPag<TDto>>> get([FromQuery] pagQueryDto paging, [FromQuery] TQuery queryParams)
    {
        if (paging.pageSize is < 1 or > 100 || paging.pageNumber < 1)
            return BadRequest(new errorMessageDto("Paginación inválida: página desde 1 y tamaño entre 1 y 100."));
        IQueryable<TEntity> query = context.Set<TEntity>().AsNoTracking();
        if (!showDeleted) query = query.Where(entity => entity.deleteAt == null);
        query = await modifyGet(query, queryParams);
        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / paging.pageSize);
        if (!paging.all && total > 0 && paging.pageNumber > totalPages)
            return BadRequest(new errorMessageDto("La página solicitada no existe."));
        query = query.OrderBy(entity => EF.Property<idClass>(entity, "Id"));
        if (!paging.all) query = query.Skip((int)Math.Min(((long)paging.pageNumber - 1) * paging.pageSize, int.MaxValue)).Take(paging.pageSize);
        var entities = await query.ToListAsync();
        modifyGetResult(entities);
        return new resPag<TDto> { items = entities.Select(e => mapper.Map<TDto>(e)).ToList(), total = total, index = paging.pageNumber, totalPages = totalPages };
    }
    protected virtual Task<IQueryable<TEntity>> modifyGet(IQueryable<TEntity> query, TQuery queryParams) => Task.FromResult(query);
    protected virtual void modifyGetResult(List<TEntity> list) { }
    [HttpPost]
    public virtual async Task<ActionResult<TDto>> post(TDtoCreation data, [FromQuery] TQueryCreation queryParams)
    {
        var entity = mapper.Map<TEntity>(data);
        var error = await validPost(entity, data, queryParams);
        if (error is not null) return BadRequest(error);
        context.Add(entity); await context.SaveChangesAsync(); await finallyPost(entity, data, queryParams);
        return mapper.Map<TDto>(entity);
    }
    protected virtual Task<errorMessageDto?> validPost(TEntity entity, TDtoCreation data, TQueryCreation query) => Task.FromResult<errorMessageDto?>(null);
    protected virtual Task finallyPost(TEntity entity, TDtoCreation data, TQueryCreation query) => Task.CompletedTask;
    [HttpDelete("{id}")]
    public virtual async Task<ActionResult> delete(idClass id)
    {
        var entity = await context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id!.Equals(id) && e.deleteAt == null);
        if (entity is null) return NotFound();
        var error = await validDelete(entity); if (error is not null) return BadRequest(error);
        entity.deleteAt = DateTime.UtcNow; await context.SaveChangesAsync(); return NoContent();
    }
    protected virtual Task<errorMessageDto?> validDelete(TEntity entity) => Task.FromResult<errorMessageDto?>(null);
    [HttpPut("{id}")]
    public virtual async Task<ActionResult> put(TDtoCreation data, [FromRoute] idClass id, [FromQuery] TQueryCreation queryCreation)
    {
        var entity = await context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id!.Equals(id) && e.deleteAt == null);
        if (entity is null) return NotFound();
        var error = await validPut(data, entity, queryCreation); if (error is not null) return BadRequest(error);
        await modifyPut(entity, data, queryCreation); mapper.Map(data, entity); await context.SaveChangesAsync();
        await finallyPut(entity, data, queryCreation); return NoContent();
    }
    protected virtual Task<errorMessageDto?> validPut(TDtoCreation data, TEntity entity, TQueryCreation query) => Task.FromResult<errorMessageDto?>(null);
    protected virtual Task modifyPut(TEntity entity, TDtoCreation data, TQueryCreation query) => Task.CompletedTask;
    protected virtual Task finallyPut(TEntity entity, TDtoCreation data, TQueryCreation query) => Task.CompletedTask;
}
