using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobpilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "preferences",
                table: "profiles",
                type: "jsonb",
                nullable: false,
                // Existing profiles get JobPreferences.None (no requirements), the same as new profiles.
                defaultValue: """{"accepts_full_time":true,"accepts_part_time":true,"exclude_staffing_agencies":false,"commute_limit":null}""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preferences",
                table: "profiles");
        }
    }
}
