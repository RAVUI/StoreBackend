using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Store.Models;


[Table("user_roles")]
    public class UserRole : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("email")]
        public string Email { get; set; } = string.Empty;
        [Column("role")]
        public string Role { get; set; } = "user";
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

