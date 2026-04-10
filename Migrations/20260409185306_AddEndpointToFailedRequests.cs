using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSyncBackbone.Migrations
{
    /// <inheritdoc />
    public partial class AddEndpointToFailedRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Endpoint",
                table: "FailedRequests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Endpoint",
                table: "FailedRequests");
        }
    }
}
