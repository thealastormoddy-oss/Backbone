using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSyncBackbone.Migrations
{
    /// <inheritdoc />
    public partial class RenameCacheKeyToCaseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CachedCases_CacheKey",
                table: "CachedCases");

            migrationBuilder.DropColumn(
                name: "CacheKey",
                table: "CachedCases");

            migrationBuilder.RenameColumn(
                name: "RecordId",
                table: "CachedCases",
                newName: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedCases_CaseId",
                table: "CachedCases",
                column: "CaseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CachedCases_CaseId",
                table: "CachedCases");

            migrationBuilder.RenameColumn(
                name: "CaseId",
                table: "CachedCases",
                newName: "RecordId");

            migrationBuilder.AddColumn<string>(
                name: "CacheKey",
                table: "CachedCases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CachedCases_CacheKey",
                table: "CachedCases",
                column: "CacheKey",
                unique: true);
        }
    }
}
