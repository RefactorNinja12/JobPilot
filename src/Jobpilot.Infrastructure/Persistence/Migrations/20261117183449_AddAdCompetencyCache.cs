using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobpilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdCompetencyCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ad_competencies",
                columns: table => new
                {
                    ad_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    competencies = table.Column<string>(type: "jsonb", nullable: false),
                    enriched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_competencies", x => x.ad_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ad_competencies_enriched_at",
                table: "ad_competencies",
                column: "enriched_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ad_competencies");
        }
    }
}
