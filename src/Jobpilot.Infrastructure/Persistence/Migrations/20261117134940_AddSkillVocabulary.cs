using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobpilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "skill_vocabularies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    built_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concepts = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skill_vocabularies", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skill_vocabularies");
        }
    }
}
