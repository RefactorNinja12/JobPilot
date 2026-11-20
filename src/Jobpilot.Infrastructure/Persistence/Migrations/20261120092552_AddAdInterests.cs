using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobpilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdInterests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ad_interests",
                columns: table => new
                {
                    ad_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    marked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_interests", x => x.ad_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ad_interests");
        }
    }
}
