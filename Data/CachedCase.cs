using System.ComponentModel.DataAnnotations.Schema;

namespace LabSyncBackbone.Data
{
    public class CachedCase
    {
        public int Id { get; set; }

        public string CaseId { get; set; } = null!;

        public string AppName { get; set; } = null!;

        [Column(TypeName = "jsonb")]
        public string Data { get; set; } = null!;       // full SyncResponse stored as jsonb

        public DateTime CachedAt { get; set; }          // when it was first stored

        public DateTime ExpiresAt { get; set; }         // when it should be treated as expired
    }
}
