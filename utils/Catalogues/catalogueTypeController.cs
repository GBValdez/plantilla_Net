using Microsoft.AspNetCore.Authorization;
using project.utils.mapping;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using project.utils.catalogue;
using project.utils.Catalogues.dto;

namespace project.utils.Catalogues
{
    [Authorize(Roles = "ADMINISTRATOR")]
    [ApiController]
    [Route("[controller]")]
    public class catalogueTypeController : controllerCommons<catalogueType, catalogueTypeDtoCreation, catalogueTypeDtoCreation, object, object, long>
    {
        public catalogueTypeController(DBProyContext context, IObjectMapper mapper) : base(context, mapper)
        {
        }
    }
}
