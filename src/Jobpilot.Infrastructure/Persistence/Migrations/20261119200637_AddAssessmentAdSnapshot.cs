using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobpilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentAdSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "application_deadline",
                table: "ad_assessments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employer",
                table: "ad_assessments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "headline",
                table: "ad_assessments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "municipality",
                table: "ad_assessments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at",
                table: "ad_assessments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "url",
                table: "ad_assessments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "application_deadline",
                table: "ad_assessments");

            migrationBuilder.DropColumn(
                name: "employer",
                table: "ad_assessments");

            migrationBuilder.DropColumn(
                name: "headline",
                table: "ad_assessments");

            migrationBuilder.DropColumn(
                name: "municipality",
                table: "ad_assessments");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "ad_assessments");

            migrationBuilder.DropColumn(
                name: "url",
                table: "ad_assessments");
        }
    }
}
