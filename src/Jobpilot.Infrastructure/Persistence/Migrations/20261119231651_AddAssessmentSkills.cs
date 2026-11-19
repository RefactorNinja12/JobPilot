using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobpilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "matched_skills",
                table: "ad_assessments",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "missing_skills",
                table: "ad_assessments",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "matched_skills",
                table: "ad_assessments");

            migrationBuilder.DropColumn(
                name: "missing_skills",
                table: "ad_assessments");
        }
    }
}
