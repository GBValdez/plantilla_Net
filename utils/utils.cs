using System.Text.Json;
using System.Text.Json.Serialization;
using project.utils.mapping;
using Microsoft.EntityFrameworkCore;
using project.utils.dto;
namespace project.utils;

public static class Utils
{
    public static bool indexValid<T>(List<T> list, int index) => index >= 0 && index < list.Count;
    public static async Task<errClass<resPag<TDto>>> paginate<T, TDto>(IQueryable<T> query, pagQueryDto paging, IObjectMapper mapper)
    {
        var result = new errClass<resPag<TDto>>();
        if (paging.pageSize is < 1 or > 100 || paging.pageNumber < 1)
        {
            result.error = new errorMessageDto("Paginación inválida."); return result;
        }
        var total = await query.CountAsync();
        var pages = (int)Math.Ceiling((double)total / paging.pageSize);
        if (total > 0 && paging.pageNumber > pages && !paging.all)
        {
            result.error = new errorMessageDto("La página solicitada no existe."); return result;
        }
        if (!paging.all) query = query.Skip((int)Math.Min(((long)paging.pageNumber - 1) * paging.pageSize, int.MaxValue)).Take(paging.pageSize);
        result.data = new resPag<TDto> { items = (await query.ToListAsync()).Select(e => mapper.Map<TDto>(e!)).ToList(), total = total, index = paging.pageNumber, totalPages = pages };
        return result;
    }
    public static T? CreateDeepCopy<T>(T? obj) where T : class
    {
        if (obj is null) return null;
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj, options), options);
    }
}
