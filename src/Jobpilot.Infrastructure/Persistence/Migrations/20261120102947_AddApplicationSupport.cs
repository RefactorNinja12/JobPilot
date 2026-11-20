using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobpilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "application_support",
                columns: table => new
                {
                    ad_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    keywords = table.Column<string[]>(type: "text[]", nullable: false),
                    highlights = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    cv_suggestions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    letter_draft = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    company_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_support", x => x.ad_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_support");
        }
    }
}
