using project.roles;
using project.roles.dto;
using project.users;
using project.users.dto;
using project.users.Models;
using project.utils.catalogue;
using project.utils.catalogues.dto;
using project.utils.Catalogues.dto;
namespace project.utils.mapping;

public interface IObjectMapper
{
    T Map<T>(object source);
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
}
// Explicit mappings keep this reusable template dependency-free and prevent accidental field updates.
public sealed class ObjectMapper : IObjectMapper
{
    public T Map<T>(object source)
    {
        object mapped = source switch
        {
            userEntity u when typeof(T) == typeof(userDto) => new userDto { userName = u.UserName ?? "", email = u.Email ?? "", isActive = u.deleteAt == null, roles = [] },
            rolEntity r when typeof(T) == typeof(rolDto) => new rolDto { Id = r.Id, Name = r.Name ?? "" },
            rolCreationDto r when typeof(T) == typeof(rolEntity) => new rolEntity { Name = r.name },
            clientCreationDto c when typeof(T) == typeof(userCreationDto) => new userCreationDto { userName = c.userName, email = c.email, password = c.password },
            clientCreationDto c when typeof(T) == typeof(Client) => new Client { name = c.name, birthdate = c.birthdate, email = c.email, tel = c.tel, address = c.address },
            Client c when typeof(T) == typeof(clientDto) => new clientDto { name = c.name, birthdate = c.birthdate, email = c.email, tel = c.tel, address = c.address },
            Catalogue c when typeof(T) == typeof(catalogueDto) => ToCatalogueDto(c),
            catalogueCreationDto c when typeof(T) == typeof(Catalogue) => new Catalogue { name = c.name, description = c.description, catalogueParentId = c.catalogueParentId },
            catalogueTypeDtoCreation c when typeof(T) == typeof(catalogueType) => new catalogueType { code = c.code, name = c.name, description = c.description },
            catalogueType c when typeof(T) == typeof(catalogueTypeDtoCreation) => new catalogueTypeDtoCreation { code = c.code, name = c.name, description = c.description },
            _ => throw new InvalidOperationException($"Agrega un mapeo explícito de {source.GetType().Name} a {typeof(T).Name}.")
        };
        return (T)mapped;
    }
    private static catalogueDto ToCatalogueDto(Catalogue c) => new()
    {
        Id = c.Id,
        name = c.name,
        description = c.description ?? "",
        catalogueParent = c.catalogueParent is null ? null : new catalogueDto { Id = c.catalogueParent.Id, name = c.catalogueParent.name, description = c.catalogueParent.description ?? "" }
    };
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        switch (source, destination)
        {
            case (catalogueCreationDto dto, Catalogue entity):
                entity.name = dto.name; entity.description = dto.description; entity.catalogueParentId = dto.catalogueParentId; break;
            case (catalogueTypeDtoCreation dto, catalogueType entity):
                entity.code = dto.code; entity.name = dto.name; entity.description = dto.description; break;
            default: throw new InvalidOperationException("Agrega un mapeo explícito para esta actualización.");
        }
        return destination;
    }
}
