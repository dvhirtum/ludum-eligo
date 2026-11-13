using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LudumEligo.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "collection_items",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bgg_id = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_items", x => new { x.user_id, x.bgg_id });
                    table.ForeignKey(
                        name: "fk_collection_items_games_bgg_id",
                        column: x => x.bgg_id,
                        principalTable: "games",
                        principalColumn: "bgg_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_collection_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_collection_items_bgg_id",
                table: "collection_items",
                column: "bgg_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "collection_items");
        }
    }
}
