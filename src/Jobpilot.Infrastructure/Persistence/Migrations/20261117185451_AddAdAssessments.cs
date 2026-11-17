using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobpilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ad_assessments",
                columns: table => new
                {
                    ad_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    chance = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_assessments", x => x.ad_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ad_assessments");
        }
    }
}
