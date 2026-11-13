using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LudumEligo.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBggUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bgg_username",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bgg_username",
                table: "users");
        }
    }
}
