using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTaskCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastTaskNumber",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "Projects" AS p
                SET "LastTaskNumber" = t."MaxNumber"
                FROM (
                    SELECT "CompanyId", "ProjectId", MAX("Number") AS "MaxNumber"
                    FROM "TaskItems"
                    GROUP BY "CompanyId", "ProjectId"
                ) AS t
                WHERE p."CompanyId" = t."CompanyId" AND p."Id" = t."ProjectId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTaskNumber",
                table: "Projects");
        }
    }
}
