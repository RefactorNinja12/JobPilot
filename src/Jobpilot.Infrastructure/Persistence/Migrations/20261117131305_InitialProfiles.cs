using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jobpilot.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialProfiles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "profiles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_profiles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "profile_skills",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                skill_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                years_of_experience = table.Column<int>(type: "integer", nullable: false),
                profile_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_profile_skills", x => x.id);
                table.ForeignKey(
                    name: "fk_profile_skills_profiles_profile_id",
                    column: x => x.profile_id,
                    principalTable: "profiles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_profile_skills_profile_id",
            table: "profile_skills",
            column: "profile_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "profile_skills");

        migrationBuilder.DropTable(
            name: "profiles");
    }
}
