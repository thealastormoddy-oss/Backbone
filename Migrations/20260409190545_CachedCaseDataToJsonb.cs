using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSyncBackbone.Migrations
{
    /// <inheritdoc />
    public partial class CachedCaseDataToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF's AlterColumn can't handle text→jsonb automatically.
            // The USING clause tells Postgres how to cast each value during the conversion.
            migrationBuilder.Sql(@"ALTER TABLE ""CachedCases"" ALTER COLUMN ""Data"" TYPE jsonb USING ""Data""::jsonb;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""CachedCases"" ALTER COLUMN ""Data"" TYPE text USING ""Data""::text;");
        }
    }
}
