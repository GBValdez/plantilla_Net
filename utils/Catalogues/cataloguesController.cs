using project.utils.mapping;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.utils.catalogue;
using project.utils.catalogues.dto;
using project.utils.Catalogues;
using project.utils.dto;

namespace project.utils.catalogues
{
    public abstract class cataloguesController : controllerCommons<Catalogue, catalogueCreationDto, catalogueDto, catalogueQueryDto, object, long>
    {
        protected abstract string codCatalogue { get; }
        public cataloguesController(DBProyContext context, IObjectMapper mapper) : base(context, mapper)
        {
        }



        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
        public override Task<ActionResult<catalogueDto>> post(catalogueCreationDto newRegister, [FromQuery] object queryParams)
        {
            return base.post(newRegister, queryParams);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
        public override Task<ActionResult> put(catalogueCreationDto entityCurrent, [FromRoute] long id, [FromQuery] object queryParams)
        {
            return base.put(entityCurrent, id, queryParams);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
        public override Task<ActionResult> delete(long id)
        {
            return base.delete(id);
        }


        protected override async Task<errorMessageDto?> validPost(Catalogue entity, catalogueCreationDto dtoNew, object queryParams)
        {
            errorMessageDto? error = await validCataloguePost(entity, dtoNew, queryParams);
            if (error != null)
                return error;

            entity.catalogueTypeId = (await getCatalogueType()).Id;
            return null;
        }

        protected override async Task<errorMessageDto?> validPut(catalogueCreationDto dtoNew, Catalogue entity, object queryParams)
        {
            errorMessageDto? error = await validCataloguePut(entity, dtoNew, queryParams);
            if (error != null)
                return error;

            return null;
        }


        protected Task<catalogueType> getCatalogueType()
        {
            return GetRequiredCatalogueType();
        }

        private async Task<catalogueType> GetRequiredCatalogueType() =>
            await context.catalogueTypes.FirstOrDefaultAsync(db => db.code == codCatalogue && db.deleteAt == null)
            ?? throw new InvalidOperationException("Configura el tipo de catálogo antes de usarlo.");

        protected override async Task<IQueryable<Catalogue>> modifyGet(IQueryable<Catalogue> query, catalogueQueryDto queryParams)
        {
            catalogueType catalogueType = await getCatalogueType();
            if (queryParams.name != null)
                query = query.Where(db => db.name.Contains(queryParams.name));
            if (queryParams.catalogueParentId != null)
                query = query.Where(db => db.catalogueParentId == queryParams.catalogueParentId);
            if (queryParams.id != null)
                query = query.Where(db => db.Id == queryParams.id);

            return query.Where(db => db.catalogueTypeId == catalogueType.Id).Include(db => db.catalogueParent);
        }

        // Funciones modificables
        protected virtual Task<errorMessageDto?> validCataloguePost(Catalogue entity, catalogueCreationDto dtoNew, object queryParams) => Task.FromResult<errorMessageDto?>(null);

        protected virtual Task<errorMessageDto?> validCataloguePut(Catalogue entity, catalogueCreationDto dtoNew, object queryParams) => Task.FromResult<errorMessageDto?>(null);

    }
}
