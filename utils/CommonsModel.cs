using System.ComponentModel.DataAnnotations.Schema;
using project.users;
using project.utils.interfaces;

namespace project.utils
{
    public class CommonsModel<idClass> : ICommonModel<idClass>
    {
        public idClass Id { get; set; } = default!;

        public string? userUpdateId { get; set; }

        [ForeignKey("userUpdateId")]
        public userEntity? userUpdate { get; set; }
        public DateTime? deleteAt { get; set; }
        public DateTime? createAt { get; set; }
        public DateTime? updateAt { get; set; }

    }
}
